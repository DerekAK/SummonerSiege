using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{      
    [SerializeField] private string billBoardTag;
    public NetworkVariable<Vector3> PlayerPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    private int moveXParam = Animator.StringToHash("InputX");
    private int moveYParam = Animator.StringToHash("InputY");
    private int rollXParam = Animator.StringToHash("RollX");
    private int rollYParam = Animator.StringToHash("RollY");
    private int animMovementStateParam = Animator.StringToHash("Movement State");
    private int crouchLayerIndex = 1;
    private int strafeLayerIndex = 2;
    private float currentMoveSpeed = 0f;
    [SerializeField] private float rollTime = 1f;
    private Coroutine rollCoroutine;
    [SerializeField] private LayerMask groundLayers;
    
    [Header("Grounded Settings")]
    [SerializeField] private bool isGrounded = true;
    [Tooltip("Useful for rough ground")]
    [SerializeField] private float groundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    [SerializeField] private float groundedRadius = 0.5f;
    private float _verticalVelocity;
    private float gravity = Physics.gravity.y;
    [SerializeField] private float fastFallFactor = 100f;
    [SerializeField] private float jumpHeight = 100f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float animationSmoothSpeed = 10f;
    [SerializeField] private float lockOnFactor = 0.5f;
    [SerializeField] private float sprintFactor = 3f;
    [SerializeField] private float crouchFactor = 0.5f;
    [SerializeField] private Transform eyesTransform;    
    public enum MovementState{Locomotion=0, Jumping=1, Falling=2, Rolling=3}
    public MovementState currentMovementState{get; private set;}
    private CharacterController _characterController;
    private Animator _anim;
    private PlayerStats _playerStats;
    private PlayerState _playerState;
    private int playerTargetIndex = 0;
    private bool statsConfigured = false;
    
    //camera shit
    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    private const float _threshold = 0.01f;
    [SerializeField] private GameObject cinemachineCameraTarget;
    [SerializeField] private float topClamp = 70.0f;
    [SerializeField] private float bottomClamp = -30.0f;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    public GameObject _mainCamera;
    [SerializeField] private GameObject _playerFollowCamera;

    private void Awake(){
        Debug.Log("Awake!");
        _characterController = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _playerStats = GetComponent<PlayerStats>();
        _playerState = GetComponent<PlayerState>();
    }
    public override void OnNetworkSpawn(){
        Debug.Log("OnNetworkSpawn!");
        // Only activate the camera for the local player (the owner)
        if (!IsLocalPlayer){
            Destroy(_mainCamera);
            Destroy(_playerFollowCamera);
            return;
        }
        GetComponent<PlayerNetworkSyncHandler>().NetworkSyncEvent += SyncNetworkVariables;
        StartCoroutine(WaitForStatsLoaded());
        StartCoroutine(BillboardShit());
    }
    
    private IEnumerator BillboardShit(){
        while(isActiveAndEnabled){
            GameObject[] healthBars = GameObject.FindGameObjectsWithTag(billBoardTag);
            Debug.Log($"Length of healthbars is {healthBars.Length}");
            foreach(GameObject healthBar in healthBars){
                healthBar.transform.LookAt(healthBar.transform.position + _mainCamera.transform.forward, _mainCamera.transform.up);
            }
            yield return null;
        }
    }

    // idea is that this runs every [] seconds or so, that way it doesn't ove
    private void SyncNetworkVariables(object sender, EventArgs e){
        if(!IsLocalPlayer){
            return;
        }
        PlayerPosition.Value = transform.position;
    }

    private IEnumerator WaitForStatsLoaded(){
        yield return new WaitForSeconds(0.5f); // wait enough time for stats to be loaded for joining clients
        transform.position = PlayerPosition.Value;
        statsConfigured = true;
    }

    private void Start(){
        if(!IsLocalPlayer){return;}
        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
        // Apply initial cursor state
        SetCursorState(cursorLocked);
        GameInput.Instance.OnAttackButtonStarted += OnRightClickPerform;
    }

    public override void OnNetworkDespawn(){
        GameInput.Instance.OnAttackButtonStarted -= OnRightClickPerform;
        GetComponent<PlayerNetworkSyncHandler>().NetworkSyncEvent += SyncNetworkVariables;
    }

    private void Update(){
        if(!IsLocalPlayer | !statsConfigured){
            return;
        }
        GroundedCheck();
        UpdateVerticalVelocity(); //accounts for jumping
        HandleMovement();
        CursorStuffIDontUnderstand();
    }
    private void UpdateVerticalVelocity(){
        if(isGrounded){
            if (_verticalVelocity < 0.0f){
                _verticalVelocity = 0f;
            }
            if(GameInput.Instance.JumpPressed()){
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * fastFallFactor * gravity);
            }
        }
        else{_verticalVelocity += fastFallFactor * gravity * Time.deltaTime;}
    }
    private void HandleMovement(){
        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized();

        bool isMoving = moveDir != Vector2.zero;
        bool isSprinting = GameInput.Instance.SprintingPressed();
        bool isLockedOn = GameInput.Instance.IsAttackButtonPressed(GameInput.AttackInput.RightMouse);
        bool rollTriggered = GameInput.Instance.MouseMiddleTriggered();
        bool crouchPressed = GameInput.Instance.CrouchPressed();

        float targetMoveSpeed, targetX, targetY, targetCrouchWeight, targetStrafeWeight;

        if(isMoving){
            if (isSprinting && !isLockedOn){
                targetMoveSpeed = _playerStats.SpeedStat.Stat.Value * sprintFactor;
                targetX = 0;
                targetY = 2;
            }
            else{ // walking
                targetMoveSpeed = _playerStats.SpeedStat.Stat.Value;
                targetX = 0;
                targetY = 1;
            }
        }
        else{ //not moving
            targetMoveSpeed = 0;
            targetX = 0;
            targetY = 0;       
        }

        if(isLockedOn){
            targetMoveSpeed = _playerStats.SpeedStat.Stat.Value * lockOnFactor;
            targetX = moveDir.x;
            targetY = moveDir.y;
            targetStrafeWeight = 1;
        } 
        else{targetStrafeWeight = 0;}

        if(crouchPressed){
            if(!isLockedOn)
                targetMoveSpeed *= crouchFactor;
            targetCrouchWeight = 1;
        }
        else{targetCrouchWeight = 0;}

        float currentX = _anim.GetFloat(moveXParam);
        float currentY = _anim.GetFloat(moveYParam);
        float currCrouchWeight = _anim.GetLayerWeight(crouchLayerIndex);
        float currStrafeWeight = _anim.GetLayerWeight(strafeLayerIndex);
        float newX, newY, moveSpeed, newCrouchWeight, newStrafeWeight;

        // become lerped value when its absolute value is very small and its approaching zero (idle). 

        newX = (Math.Abs(currentX) > 1e-2 || Math.Abs(targetX) > Math.Abs(currentX))? Mathf.Lerp(currentX, targetX, Time.deltaTime * animationSmoothSpeed) : 0f;
        newY = (Math.Abs(currentY) > 1e-2 || Math.Abs(targetY) > Math.Abs(currentY))? Mathf.Lerp(currentY, targetY, Time.deltaTime * animationSmoothSpeed) : 0f;
        moveSpeed = (Math.Abs(currentMoveSpeed) > 1e-2 || Math.Abs(targetMoveSpeed) > Math.Abs(currentMoveSpeed))? Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, Time.deltaTime * animationSmoothSpeed) : 0f;
        newCrouchWeight = (currCrouchWeight > 1e-2 || targetCrouchWeight > currCrouchWeight)? Mathf.Lerp(currCrouchWeight, targetCrouchWeight, Time.deltaTime * animationSmoothSpeed) : 0f;
        newStrafeWeight = (currStrafeWeight > 1e-2 || targetStrafeWeight > currStrafeWeight)? Mathf.Lerp(currStrafeWeight, targetStrafeWeight, Time.deltaTime * animationSmoothSpeed) : 0f;
        currentMoveSpeed = moveSpeed;

        if (!isGrounded){
            if (_verticalVelocity < 0) {currentMovementState = MovementState.Falling;}
            else {currentMovementState = MovementState.Jumping;}
            _playerState.InAir = true;
        }
        else{
            _playerState.InAir = false;
            if(rollTriggered){
                rollCoroutine = StartCoroutine(WaitEndRoll());
                currentMovementState = MovementState.Rolling;
                if(isLockedOn){
                    _anim.SetFloat(rollXParam, moveDir.x);
                    _anim.SetFloat(rollYParam, moveDir.y);
                }
                else{
                    _anim.SetFloat(rollXParam, 0);
                    _anim.SetFloat(rollYParam, 1);
                }
                
            }
            else{currentMovementState = MovementState.Locomotion;}
        }

        // Update animator parameters
        _anim.SetFloat(moveXParam, newX);
        _anim.SetFloat(moveYParam, newY);
        _anim.SetInteger(animMovementStateParam, (int)currentMovementState);
        _anim.SetLayerWeight(crouchLayerIndex, newCrouchWeight);
        _anim.SetLayerWeight(strafeLayerIndex, newStrafeWeight);

        Vector3 targetDirection = HandlePlayerAndCameraRotation(isLockedOn, moveDir, isMoving);
        
        if(_playerState.Attacking){
            moveSpeed *= _playerState.currentAttack.movementSpeedFactor;
        }
        _characterController.Move(targetDirection.normalized * (moveSpeed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private IEnumerator WaitEndRoll(){
        _playerState.Rolling = true;
        if(rollCoroutine != null){StopCoroutine(rollCoroutine);}
        _anim.applyRootMotion = true;
        yield return new WaitForSeconds(rollTime);
        _anim.applyRootMotion = false;
        _playerState.Rolling = false;
    }
    private Vector3 HandlePlayerAndCameraRotation(bool isLockedOn, Vector3 moveDir, bool isMoving){
        Vector3 playerTargetDirection = Vector3.zero;
        bool scrolledUp = GameInput.Instance.ScrolledUp();
        bool scrolledDown = GameInput.Instance.ScrolledDown();
        float targetRotation;
        Quaternion targetRot;

        float rotationSpeedFactor = 1f;
        if(_playerState.Attacking){rotationSpeedFactor *= _playerState.currentAttack.rotationSpeedFactor;}

        if (isLockedOn){
            bool hasLockOnTarget = _playerState.lockOnTargets.Count > 0;
            if (hasLockOnTarget){
                if (scrolledUp) { playerTargetIndex = (playerTargetIndex + 1) % _playerState.lockOnTargets.Count; }
                if (scrolledDown) { playerTargetIndex = (playerTargetIndex - 1 + _playerState.lockOnTargets.Count) % _playerState.lockOnTargets.Count; }

                Transform lockOnTarget = _playerState.lockOnTargets[playerTargetIndex];
                Vector3 directionToTarget = (lockOnTarget.position - transform.position).normalized;
                targetRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            }
            else{
                targetRotation = _mainCamera.transform.eulerAngles.y;
            }

            Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
            playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * inputDirection;

            targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
        }

        else{
            Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
            targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            if(isMoving){
                targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
            }
        }

        return playerTargetDirection;
    }

    private void OnRightClickPerform(GameInput.AttackInput input){
        if(input == GameInput.AttackInput.RightMouse){
            _playerState.lockOnTargets.Sort((a, b) => Vector3.Distance(a.position, transform.position).CompareTo(Vector3.Distance(b.position, transform.position)));
            playerTargetIndex = 0;
        }
    }
        
    private void LateUpdate(){
        if (cursorInputForLook && cursorLocked){CameraRotation();}
    }
    private void GroundedCheck(){
        // Set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation(){
        // If there is input and camera position is not fixed
        if (GameInput.Instance.GetPlayerLookVectorNormalized().sqrMagnitude >= _threshold){
            _cinemachineTargetYaw += GameInput.Instance.GetPlayerLookVectorNormalized().x * 1.2f;
            _cinemachineTargetPitch += GameInput.Instance.GetPlayerLookVectorNormalized().y * 1.2f;
        }

        // Clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

        // Cinemachine will follow this target
        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax){
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnApplicationFocus(bool hasFocus){if (hasFocus){SetCursorState(cursorLocked);}}

    private void SetCursorState(bool newState){
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !newState;
    }
    private void CursorStuffIDontUnderstand(){
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape)){
            cursorLocked = false;
            SetCursorState(cursorLocked);
        }

        // Relock cursor when clicking after unlocking
        if (!cursorLocked && Input.GetMouseButtonDown(0)){
            cursorLocked = true;
            SetCursorState(cursorLocked);
        }
    }
    public Transform GetEyesTransform(){return eyesTransform;}
}