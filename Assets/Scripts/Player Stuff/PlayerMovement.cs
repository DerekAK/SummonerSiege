using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    private int moveXParam = Animator.StringToHash("InputX");
    private int moveYParam = Animator.StringToHash("InputY");
    private int rollXParam = Animator.StringToHash("RollX");
    private int rollYParam = Animator.StringToHash("RollY");
    private int animMovementStateParam = Animator.StringToHash("Movement State");
    private int crouchLayerIndex = 1;
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
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float lockOnFactor = 0.5f;
    [SerializeField] private Transform eyesTransform;    
    public enum MovementState{Locomotion=0, Jumping=1, Falling=2, Rolling=3}
    public MovementState currentMovementState{get; private set;}

    private CharacterController _characterController;
    private Animator _anim;
    private PlayerStats _playerStats;
    private PlayerState _playerState;
    private int playerTargetIndex = 0;
    
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
    private GameObject _mainCamera;

    private void Awake(){
        if (_mainCamera == null){_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");}
        _characterController = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _playerStats = GetComponent<PlayerStats>();
        _playerState = GetComponent<PlayerState>();
    }
    private void Start(){
        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
        // Apply initial cursor state
        SetCursorState(cursorLocked);
        GameInput.Instance.OnAttackButtonStarted += OnRightClickPerform;
    }
    private void OnDisable(){
        GameInput.Instance.OnAttackButtonStarted -= OnRightClickPerform;
    }

    private void Update(){
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

        float targetMoveSpeed, targetX, targetY, targetCrouchWeight;

        if (isMoving && !isLockedOn){
            if (isSprinting){
                targetMoveSpeed = _playerStats.speed * _playerStats.sprintFactor;
                targetX = 0;
                targetY = 2;
            }
            else{ // walking
                targetMoveSpeed = _playerStats.speed;
                targetX = 0;
                targetY = 1;
            }
            if(crouchPressed){
                targetMoveSpeed = _playerStats.speed * _playerStats.crouchFactor;
                targetX = 0;
                targetY = _playerStats.crouchFactor;
                targetCrouchWeight = 1;
            }
            else{targetCrouchWeight = 0;}
        }
        else if(isLockedOn){
            targetMoveSpeed = _playerStats.speed * lockOnFactor;
            targetX = moveDir.x;
            targetY = moveDir.y;
            if(crouchPressed){
                targetMoveSpeed *= _playerStats.crouchFactor;
                targetX *= _playerStats.crouchFactor;
                targetY *= _playerStats.crouchFactor;
                targetCrouchWeight = 1;
            }
            else{targetCrouchWeight = 0;}
        }
        else{ //not moving
            targetMoveSpeed = 0.0f;
            targetX = 0f;
            targetY = 0f;       
            if(crouchPressed){targetCrouchWeight = 1;} 
            else{targetCrouchWeight = 0;}
        }

        float currentX = _anim.GetFloat(moveXParam);
        float currentY = _anim.GetFloat(moveYParam);
        float currCrouchWeight = _anim.GetLayerWeight(crouchLayerIndex);
        float newX, newY, moveSpeed, newCrouchWeight;

        // become lerped value when its absolute value is very small and its approaching zero (idle). 

        newX = (Math.Abs(currentX) > 1e-2 || Math.Abs(targetX) > Math.Abs(currentX))? Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothSpeed) : 0f;
        newY = (Math.Abs(currentY) > 1e-2 || Math.Abs(targetY) > Math.Abs(currentY))? Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothSpeed) : 0f;
        moveSpeed = (Math.Abs(currentMoveSpeed) > 1e-2 || Math.Abs(targetMoveSpeed) > Math.Abs(currentMoveSpeed))? Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, Time.deltaTime * smoothSpeed) : 0f;
        newCrouchWeight = (currCrouchWeight > 1e-2 || targetCrouchWeight > currCrouchWeight)? Mathf.Lerp(currCrouchWeight, targetCrouchWeight, Time.deltaTime * smoothSpeed) : 0f;
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

        Vector3 targetDirection = HandlePlayerAndCameraRotation(isLockedOn, moveDir, isMoving);
        
        if(_playerState.Attacking){moveSpeed *= _playerState.currentAttack.movementSpeedFactor;}
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
        

        if(isLockedOn){
            bool hasLockOnTarget = _playerState.lockOnTargets.Count > 0;            
            float playerRotation;
            if(hasLockOnTarget){ //has a lock on target
                if(scrolledUp){playerTargetIndex = (playerTargetIndex + 1) % _playerState.lockOnTargets.Count;} // count is guaranteed to be at least 1 here
                if(scrolledDown){playerTargetIndex = (playerTargetIndex - 1 + _playerState.lockOnTargets.Count) % _playerState.lockOnTargets.Count;}

                Transform lockOnTarget = _playerState.lockOnTargets[playerTargetIndex];
                
                Vector3 directionToTarget = (lockOnTarget.position - transform.position).normalized;
                targetRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg; 
                
                playerRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationSpeed, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, playerRotation, 0.0f);
            }

            else{
                targetRotation = _mainCamera.transform.eulerAngles.y;
                playerRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationSpeed, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, playerRotation, 0.0f);
            }
            
            Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
            playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * inputDirection;
        }
        else{
            if(isMoving){
                Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;

                //determines the desired rotation of the player based on the input direction and the camera's orientation
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                
                //rotate player towards target rotation
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationSpeed, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                
                //target direction is just the forward vector in front of your target rotation
                playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
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