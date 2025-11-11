using System;
using System.Collections;
using Cinemachine;
using Unity.Entities.UniversalDelegates;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // Core Settings
    private Animator _anim;
    private Rigidbody _rb;
    private ConfigurableJoint _configJoint;
    private EntityStats _playerStats;
    private PlayerCombat _playerCombat;

    [Header("Grounded Settings")]
    [SerializeField] private float groundedOffset;
    [SerializeField] private float groundedRadius;
    [SerializeField] private LayerMask groundLayers;
    private bool isGrounded = true;

    [Header("Movement Settings")]
    [Range(0, 1)] [SerializeField] private float walkSpeedFactor = 0.3f;
    [SerializeField] private float fastFallFactor = 1.5f;
    [SerializeField] private float jumpHeight = 10;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float crouch_lockOn_Factor;
    [SerializeField] private float movementSmoothSpeed = 10f;
    private float walkTargetSpeed;
    private float sprintTargetSpeed;
    private float currentSpeed;
    private const float moveThreshold = 0.01f;
    
    // settings to decouple input processing in update() and actual movement in fixedupdate()
    private bool moveRequested;
    private Vector3 moveForce;
    private bool jumpRequested;


    // Rolling / Air variables
    private bool inAir = false;
    public bool InAir => inAir;
    private bool isRolling = false;
    public bool IsRolling => isRolling;


    [Header("Animator settings")]
    [SerializeField] private float animationSmoothSpeed = 10f;
    private int moveXParam = Animator.StringToHash("InputX");
    private int moveYParam = Animator.StringToHash("InputY");
    private int rollXParam = Animator.StringToHash("RollX");
    private int rollYParam = Animator.StringToHash("RollY");
    private int animMovementStateParam = Animator.StringToHash("Movement State");
    private int crouchLayerIndex = 1;
    private int strafeLayerIndex = 2;

    [Header("Miscellaneous")]
    [SerializeField] private string billBoardTag = "BillBoard";
    private enum MovementState { Locomotion = 0, Jumping = 1, Falling = 2, Rolling = 3 }
    private MovementState currentMovementState;
    private bool statsConfigured = false;
    private int playerTargetIndex = 0;


    // mouse settings
    private bool cursorLocked = true;
    private bool cursorInputForLook = true;


    [Header("Camera Settings")]
    [SerializeField] private GameObject cinemachineCameraTarget;
    [SerializeField] private GameObject playerFollowCamera;
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private float topClamp = 70.0f;
    [SerializeField] private float bottomClamp = -30.0f;
    [SerializeField] private float minCameraDistance = 0.2f;  // First person
    [SerializeField] private float maxCameraDistance = 8.0f;  // Third person far
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private float currentCameraDistance = 5.0f;
    [SerializeField] private float firstPersonThreshold = 0.5f; // Hide body below this distance
    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;


    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        _playerStats = GetComponent<EntityStats>();
        _playerCombat = GetComponent<PlayerCombat>();
        _configJoint = GetComponent<ConfigurableJoint>();
    }

    private void Start()
    {
        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
        SetCursorState(cursorLocked);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _playerStats.OnStatsConfigured += OnStatsConfigured;
            GameInput.Instance.OnAttackButtonStarted += OnRightClickPerform;
            StartCoroutine(BillboardShit());
            thirdPersonFollow = playerFollowCamera.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        }
        else
        {
            if (mainCamera != null) mainCamera.SetActive(false);
            if (playerFollowCamera != null) playerFollowCamera.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnAttackButtonStarted -= OnRightClickPerform;
        _playerStats.OnStatsConfigured -= OnStatsConfigured;
    }

    private void OnStatsConfigured()
    {
        statsConfigured = true;
        HandleTargetSpeeds();
    }
    
    private void HandleTargetSpeeds()
    {
        if (!_playerStats.TryGetStat(StatType.Speed, out NetStat speedStat)) return;
        walkTargetSpeed = speedStat.MaxValue * walkSpeedFactor;
        sprintTargetSpeed = speedStat.MaxValue;
    }

    private void Update()
    {
        if (!IsOwner || !statsConfigured) return;

        GroundedCheck();
        HandleCameraZoom();
        CursorStuffIDontUnderstand();
        HandleMovement();
        HandleJump();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !statsConfigured) return;

        if (moveRequested)
        {
            _rb.AddForce(moveForce, ForceMode.VelocityChange);
            moveRequested = false;
        }

        if (jumpRequested)
        {
            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            
            _rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            jumpRequested = false;
        }

        if (!isGrounded)
        {
            //_rb.AddForce(Vector3.down * fastFallFactor, ForceMode.Force);
        }
        
    }

    private void GroundedCheck() {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void HandleJump(){

        if (isGrounded && GameInput.Instance.JumpPressed())
        {
            jumpRequested = true;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized();
        bool isMoving = moveDir.sqrMagnitude > moveThreshold;
        bool isSprinting = GameInput.Instance.SprintingPressed();
        bool isLockedOn = GameInput.Instance.IsAttackButtonPressed(GameInput.AttackInput.RightMouse);
        bool rollTriggered = GameInput.Instance.MouseMiddleTriggered();
        bool crouchPressed = GameInput.Instance.CrouchPressed();

        float targetMoveSpeed, targetX, targetY, targetCrouchWeight, targetStrafeWeight;

        if (isMoving)
        {
            if (isSprinting && !isLockedOn)
            {
                targetMoveSpeed = sprintTargetSpeed;
                targetX = 0;
                targetY = 2;
            }
            else
            {
                targetMoveSpeed = walkTargetSpeed;
                targetX = 0;
                targetY = 1;
            }
        }
        else
        {
            targetMoveSpeed = 0;
            targetX = 0;
            targetY = 0;
        }

        if (isLockedOn)
        {
            targetMoveSpeed *= crouch_lockOn_Factor;
            targetX = moveDir.x;
            targetY = moveDir.y;
            targetStrafeWeight = 1;
        }
        else
        {
            targetStrafeWeight = 0;
        }

        if (crouchPressed)
        {
            if (!isLockedOn) targetMoveSpeed *= crouch_lockOn_Factor;
            targetCrouchWeight = 1;
        }
        else
        {
            targetCrouchWeight = 0;
        }

        float currentX = _anim.GetFloat(moveXParam);
        float currentY = _anim.GetFloat(moveYParam);
        float currCrouchWeight = _anim.GetLayerWeight(crouchLayerIndex);
        float currStrafeWeight = _anim.GetLayerWeight(strafeLayerIndex);

        float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * animationSmoothSpeed);
        float newY = Mathf.Lerp(currentY, targetY, Time.deltaTime * animationSmoothSpeed);
        float moveSpeed = Mathf.Lerp(currentSpeed, targetMoveSpeed, Time.deltaTime * animationSmoothSpeed);
        float newCrouchWeight = Mathf.Lerp(currCrouchWeight, targetCrouchWeight, Time.deltaTime * animationSmoothSpeed);
        float newStrafeWeight = Mathf.Lerp(currStrafeWeight, targetStrafeWeight, Time.deltaTime * animationSmoothSpeed);
        currentSpeed = moveSpeed;

        if (!isGrounded)
        {
            if (_rb.linearVelocity.y < 0) currentMovementState = MovementState.Falling;
            else currentMovementState = MovementState.Jumping;
            inAir = true;
        }
        else
        {
            inAir = false;
            if (rollTriggered)
            {
                Roll();
                currentMovementState = MovementState.Rolling;
                if (isLockedOn)
                {
                    _anim.SetFloat(rollXParam, moveDir.x);
                    _anim.SetFloat(rollYParam, moveDir.y);
                }
                else
                {
                    _anim.SetFloat(rollXParam, 0);
                    _anim.SetFloat(rollYParam, 1);
                }
            }
            else
            {
                currentMovementState = MovementState.Locomotion;
            }
        }

        _anim.SetFloat(moveXParam, newX);
        _anim.SetFloat(moveYParam, newY);
        _anim.SetInteger(animMovementStateParam, (int)currentMovementState);
        _anim.SetLayerWeight(crouchLayerIndex, newCrouchWeight);
        _anim.SetLayerWeight(strafeLayerIndex, newStrafeWeight);

        Vector3 targetDirection = HandlePlayerAndCameraRotation(isLockedOn, moveDir, isMoving);
       

        if (_playerCombat.InAttack)
        {
            moveSpeed *= _playerCombat.ChosenAttack.MovementSpeedFactor;
        }

        Vector3 moveVector = targetDirection.normalized * moveSpeed * 0.05f;
        if (moveVector.sqrMagnitude > moveThreshold)
        {
            moveForce = moveVector;
            moveRequested = true;
        }
    }

    private IEnumerator BillboardShit(){
        while (isActiveAndEnabled){
            GameObject[] healthBars = GameObject.FindGameObjectsWithTag(billBoardTag);
            foreach (GameObject healthBar in healthBars){
                healthBar.transform.LookAt(healthBar.transform.position + mainCamera.transform.forward, mainCamera.transform.up);
            }
            yield return null;
        }
    }

    private Vector3 HandlePlayerAndCameraRotation(bool isLockedOn, Vector2 moveDir, bool isMoving)
    {
        Vector3 playerTargetDirection = Vector3.zero;
        bool scrolledUp = GameInput.Instance.ScrolledUp();
        bool scrolledDown = GameInput.Instance.ScrolledDown();
        float targetRotation;
        float rotationSpeedFactor = _playerCombat.InAttack ? _playerCombat.ChosenAttack.RotationSpeedFactor : 1f;

        if (isLockedOn)
        {
            bool hasLockOnTarget = _playerCombat.LockOnTargets.Count > 0;
            if (hasLockOnTarget)
            {
                if (scrolledUp) playerTargetIndex = (playerTargetIndex + 1) % _playerCombat.LockOnTargets.Count;
                if (scrolledDown) playerTargetIndex = (playerTargetIndex - 1 + _playerCombat.LockOnTargets.Count) % _playerCombat.LockOnTargets.Count;

                Transform lockOnTarget = _playerCombat.LockOnTargets[playerTargetIndex];
                Vector3 directionToTarget = (lockOnTarget.position - transform.position).normalized;
                targetRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            }
            else
            {
                targetRotation = mainCamera.transform.eulerAngles.y;
            }

            Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y);
            if (inputDirection.sqrMagnitude > moveThreshold)
            {
                playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * inputDirection.normalized;
            }

            // Quaternion targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
            // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
            SetJointTargetRotation(targetRotation, rotationSpeedFactor);
        }
        else
        {
            if (isMoving)
            {
                Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

                // Quaternion targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
                // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
                SetJointTargetRotation(targetRotation, rotationSpeedFactor);
            }
        }
        return playerTargetDirection;
    }

    private void SetJointTargetRotation(float targetWorldYAngle, float rotationSpeedFactor)
    {   
        Quaternion desiredWorldRotation = Quaternion.Euler(0f, targetWorldYAngle, 0f);
    
        Quaternion localTargetRotation = Quaternion.Inverse(transform.rotation) * desiredWorldRotation;

        // Set as target
        _configJoint.targetRotation = Quaternion.RotateTowards(_configJoint.targetRotation, localTargetRotation, rotationSpeedFactor);
    }
    
    private void HandleCameraZoom()
    {
        if (thirdPersonFollow == null) return;

        // Get scroll input
        float scrollInput = GameInput.Instance.GetMouseScrollValue();
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Adjust camera distance
            currentCameraDistance -= scrollInput * zoomSpeed;
            currentCameraDistance = Mathf.Clamp(currentCameraDistance, minCameraDistance, maxCameraDistance);
            
            // Apply to camera
            thirdPersonFollow.CameraDistance = currentCameraDistance;
            
            // Hide body in first person
        }
    }

    private void Roll()
    {
        isRolling = true;
        _anim.applyRootMotion = true;
    }
    
    private void AnimationEvent_EndRoll()
    {
        _anim.applyRootMotion = false;
        isRolling = false;
    }

    private void OnRightClickPerform(GameInput.AttackInput input) {
        if (input == GameInput.AttackInput.RightMouse) {
            _playerCombat.LockOnTargets.Sort((a, b) => Vector3.Distance(a.position, transform.position).CompareTo(Vector3.Distance(b.position, transform.position)));
            playerTargetIndex = 0;
        }
    }

    private void LateUpdate() {
        if (cursorInputForLook && cursorLocked) { CameraRotation(); }
    }


    private void CameraRotation() {
        if (GameInput.Instance.GetPlayerLookVectorNormalized().sqrMagnitude >= moveThreshold)
        {
            _cinemachineTargetYaw += GameInput.Instance.GetPlayerLookVectorNormalized().x * 1.2f;
            _cinemachineTargetPitch += GameInput.Instance.GetPlayerLookVectorNormalized().y * 1.2f;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnApplicationFocus(bool hasFocus) {
        if (hasFocus) { SetCursorState(cursorLocked); }
    }

    private void SetCursorState(bool newState) {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !newState;
    }

    private void CursorStuffIDontUnderstand() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            cursorLocked = false;
            SetCursorState(cursorLocked);
        }

        if (!cursorLocked && Input.GetMouseButtonDown(0)) {
            cursorLocked = true;
            SetCursorState(cursorLocked);
        }
    }

}