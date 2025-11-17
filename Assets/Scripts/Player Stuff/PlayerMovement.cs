using System;
using System.Collections;
using Cinemachine;
using Unity.Netcode;
using UnityEditor.Callbacks;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    // Core Settings
    private Animator _anim;
    private Rigidbody _rb;
    private ConfigurableJoint _configJoint;
    private EntityStats _playerStats;
    private PlayerCombat _playerCombat;
    private PhysicsManager _physicsManager;
    private Rigidbody[] ragdollRigidbodies;
    private SynchronizedJoint[] ragdollJoints;
    

    [Header("Model References")]
    [SerializeField] private GameObject ragdollGO;
    [SerializeField] private GameObject animatedGO;


    [Header("Grounded Settings")]
    [SerializeField] private float groundedOffset;
    [SerializeField] private float groundedRadius;
    [SerializeField] private LayerMask groundLayers;
    private bool isGrounded = true;

    [Header("Movement Settings")]
    [Range(0, 1)] [SerializeField] private float walkSpeedFactor = 0.5f;
    [SerializeField] private float fastFallFactor = 1.5f;
    [SerializeField] private float jumpHeight = 10;
    [SerializeField] private float rollForce = 10;
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
        _anim = animatedGO.GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        _configJoint = GetComponent<ConfigurableJoint>();
        _playerStats = GetComponent<EntityStats>();
        _playerCombat = GetComponent<PlayerCombat>();
        _physicsManager = GetComponent<PhysicsManager>();
        ragdollRigidbodies = ragdollGO.GetComponentsInChildren<Rigidbody>();
        ragdollJoints = ragdollGO.GetComponentsInChildren<SynchronizedJoint>();
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
        HandleMovementInput();
        HandleJumpInput();
    }

    private void LateUpdate() {
        if (cursorInputForLook && cursorLocked) { CameraRotation(); }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !statsConfigured) return;

        HandleRotation();
        HandleMovementJumpExecution();
        SyncJointsWithAnimation();

    }

    private void SyncJointsWithAnimation()
    {
        foreach (SynchronizedJoint joint in ragdollJoints)
        {
            joint.SyncJoint();
        }
    }

    private void HandleMovementJumpExecution()
    {
        if (moveRequested)
        {
            _rb.linearVelocity = new Vector3(
                moveForce.x,
                _rb.linearVelocity.y,
                moveForce.z
            );
            moveRequested = false;
        }

        if (moveRequested)
        {
            _rb.AddForce(moveForce, ForceMode.VelocityChange);
            moveRequested = false;
        }

        if (jumpRequested)
        {
            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            foreach (var rb in ragdollRigidbodies)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
            }
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVelocity, _rb.linearVelocity.z);
            jumpRequested = false;
        }

        if (!isGrounded && _rb.linearVelocity.y < 1)
        {
            _rb.AddForce(Vector3.down * fastFallFactor, ForceMode.Acceleration);
        }
    }

    private void GroundedCheck() {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void HandleJumpInput()
    {
        if (isGrounded && GameInput.Instance.JumpPressed() && !isRolling) jumpRequested = true;  
    }

    private void HandleMovementInput()
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
                currentMovementState = MovementState.Rolling;
                if (isLockedOn && moveDir != Vector2.zero)
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

        if (newCrouchWeight > 0.1 && !_physicsManager.IsInAnimationMode)
        {
            _physicsManager.EnableAnimationMode();
        }
        else if (newCrouchWeight < 0.1 && _physicsManager.IsInAnimationMode)
        {
            _physicsManager.EnablePhysicsMode();
        }


        // Calculate movement direction
        Vector3 playerTargetDirectionNormalized = CalculateMovementDirection(isLockedOn, moveDir, isMoving).normalized;

        if (rollTriggered && isGrounded)
        {
            Roll(playerTargetDirectionNormalized);
        }

        if (_playerCombat.InAttack)
        {
            moveSpeed *= _playerCombat.ChosenAttack.MovementSpeedFactor;
        }

        if (isRolling)
        {
            moveSpeed = 0;
        }

        moveForce = playerTargetDirectionNormalized * moveSpeed;
        if (moveForce.sqrMagnitude > moveThreshold)
        {
            moveRequested = true;
        }
    }

    private Vector3 CalculateMovementDirection(bool isLockedOn, Vector2 moveDir, bool isMoving)
    {
        if (!isMoving) return Vector3.zero;

        Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y);

        if (isLockedOn)
        {
            // Movement is relative to current facing (strafe movement)
            if (inputDirection.sqrMagnitude > moveThreshold)
            {
                float targetRotation;
                bool hasLockOnTarget = _playerCombat.LockOnTargets.Count > 0;

                if (hasLockOnTarget)
                {
                    Transform lockOnTarget = _playerCombat.LockOnTargets[playerTargetIndex];
                    Vector3 directionToTarget = (lockOnTarget.position - transform.position).normalized;
                    targetRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
                }
                else
                {
                    targetRotation = mainCamera.transform.eulerAngles.y;
                }

                return Quaternion.Euler(0.0f, targetRotation, 0.0f) * inputDirection.normalized;
            }
        }
        else
        {
            // Movement is in camera-relative direction
            float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            return Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
        }

        return Vector3.zero;
    }

    private void HandleRotation()
    {
        if (isRolling) return;

        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized();
        bool isMoving = moveDir.sqrMagnitude > moveThreshold;
        bool isLockedOn = GameInput.Instance.IsAttackButtonPressed(GameInput.AttackInput.RightMouse);
        
        float rotationSpeedFactor = _playerCombat.InAttack ? _playerCombat.ChosenAttack.RotationSpeedFactor : 1f;

        Vector3 targetDirection;

        if (isLockedOn)
        {
            bool hasLockOnTarget = _playerCombat.LockOnTargets.Count > 0;
            if (hasLockOnTarget)
            {
                Transform lockOnTarget = _playerCombat.LockOnTargets[playerTargetIndex];
                targetDirection = (lockOnTarget.position - transform.position).normalized;
                targetDirection.y = 0;
            }
            else
            {
                targetDirection = mainCamera.transform.forward;
                targetDirection.y = 0;
                targetDirection.Normalize();
            }

            SetJointTargetRotation(targetDirection, rotationSpeedFactor);
        }
        else
        {
            if (isMoving)
            {
                Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
                // Calculate the world direction we want to face
                targetDirection = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0) * inputDirection;

                SetJointTargetRotation(targetDirection, rotationSpeedFactor);
            }
        }
    }

    private void SetJointTargetRotation(Vector3 worldDirection, float rotationSpeedFactor)
    {   
        // Create rotation from the direction vector
        Quaternion desiredWorldRotation = Quaternion.LookRotation(worldDirection);
        float targetAngle = desiredWorldRotation.eulerAngles.y;
        Vector3 currentEuler = _configJoint.targetRotation.eulerAngles;
        
        // use negated angle, because this for some reason is how configurable joints expect it
        float newY = Mathf.LerpAngle(currentEuler.y, -targetAngle, rotationSpeed * rotationSpeedFactor * Time.fixedDeltaTime * 0.01f);
        _configJoint.targetRotation = Quaternion.Euler(0, newY, 0);
        
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

    private void Roll(Vector3 moveDir)
    {
        _physicsManager.EnableAnimationMode();
        isRolling = true;

        Vector2 jumpForceXY = new Vector2(moveDir.x, moveDir.z) * rollForce;

        foreach (var rb in ragdollRigidbodies)
        {
            rb.linearVelocity = new Vector3(jumpForceXY.x, rb.linearVelocity.y, jumpForceXY.y);
        }
        _rb.linearVelocity = new Vector3(jumpForceXY.x, _rb.linearVelocity.y, jumpForceXY.y);
    }
    
    public void AnimationEvent_EndRoll()
    {
        _physicsManager.EnablePhysicsMode();
        isRolling = false;
    }

    private void OnRightClickPerform(GameInput.AttackInput input) {
        if (input == GameInput.AttackInput.RightMouse) {
            _playerCombat.LockOnTargets.Sort((a, b) => Vector3.Distance(a.position, transform.position).CompareTo(Vector3.Distance(b.position, transform.position)));
            playerTargetIndex = 0;
        }
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