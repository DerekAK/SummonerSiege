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
    [SerializeField, Tooltip("Useful for rough ground")]
    private float groundedOffset = -0.14f;
    [SerializeField, Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    private float groundedRadius;
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
    public enum MovementState { Locomotion = 0, Jumping = 1, Falling = 2, Rolling = 3 }
    public MovementState currentMovementState { get; private set; }
    private CharacterController _characterController;
    private Animator _anim;
    private EntityStats _playerStats;
    private PlayerState _playerState;
    private int playerTargetIndex = 0;
    
    [Header("Physics Settings")]
    [SerializeField, Tooltip("Rate to dampen external forces (1/s).")]
    private float forceDecay = 5f;
    private NetworkVariable<Vector3> nvPhysicsVelocity = new NetworkVariable<Vector3>(Vector3.zero);
    private Vector3 physicsVelocity;

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

    private bool statsConfigured = false;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
        _playerStats = GetComponent<EntityStats>();
        _playerState = GetComponent<PlayerState>();
    }

    private void StatsConfigured()
    {
        Debug.Log("StatsConfigured!!");
        statsConfigured = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            _playerStats.OnStatsConfigured += StatsConfigured;
            Initialize();
        }
        else
        {
            if (_mainCamera != null) _mainCamera.SetActive(false);
            if (_playerFollowCamera != null) _playerFollowCamera.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (GameInput.Instance != null) GameInput.Instance.OnAttackButtonStarted -= OnRightClickPerform;

        if (NetworkManager.Singleton != null) nvPhysicsVelocity.OnValueChanged -= PhysicsVelocityChanged;
        
    }

    private void Initialize()
    {
        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
        SetCursorState(cursorLocked);
        GameInput.Instance.OnAttackButtonStarted += OnRightClickPerform;
        StartCoroutine(BillboardShit());
        nvPhysicsVelocity.OnValueChanged += PhysicsVelocityChanged;
    }

    private void Update()
    {

        if (!IsOwner) return;
        if (!statsConfigured) return;

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            _playerStats.ModifyStatServerRpc(StatType.Speed, 10);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            _playerStats.ModifyStatServerRpc(StatType.Health, 10);
        }

        GroundedCheck();
        UpdateVerticalVelocity();
        HandleMovement();
        CursorStuffIDontUnderstand();
    }
    
    private IEnumerator BillboardShit(){
        while (isActiveAndEnabled){
            if (_mainCamera == null) yield break;
            GameObject[] healthBars = GameObject.FindGameObjectsWithTag(billBoardTag);
            foreach (GameObject healthBar in healthBars){
                healthBar.transform.LookAt(healthBar.transform.position + _mainCamera.transform.forward, _mainCamera.transform.up);
            }
            yield return null;
        }
    }

    private void UpdateVerticalVelocity(){
        if (isGrounded){
            if (_verticalVelocity < 0.0f)
                _verticalVelocity = 0f;
            if (GameInput.Instance.JumpPressed())
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * fastFallFactor * gravity);
        }
        else{
            _verticalVelocity += fastFallFactor * gravity * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized();
        bool isMoving = moveDir.sqrMagnitude > _threshold;
        bool isSprinting = GameInput.Instance.SprintingPressed();
        bool isLockedOn = GameInput.Instance.IsAttackButtonPressed(GameInput.AttackInput.RightMouse);
        bool rollTriggered = GameInput.Instance.MouseMiddleTriggered();
        bool crouchPressed = GameInput.Instance.CrouchPressed();

        float targetMoveSpeed, targetX, targetY, targetCrouchWeight, targetStrafeWeight;
        float speed;

        if (_playerStats.TryGetStat(StatType.Speed, out NetStat speedStat))
        {
            speed = speedStat.CurrentValue;
        }
        else
        {
            return;
        }

        if (isMoving)
        {
            if (isSprinting && !isLockedOn)
            {
                targetMoveSpeed = speed * sprintFactor;
                targetX = 0;
                targetY = 2;
            }
            else
            {
                targetMoveSpeed = speed;
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
            targetMoveSpeed = speed * lockOnFactor;
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
            if (!isLockedOn)
                targetMoveSpeed *= crouchFactor;
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
        float moveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, Time.deltaTime * animationSmoothSpeed);
        float newCrouchWeight = Mathf.Lerp(currCrouchWeight, targetCrouchWeight, Time.deltaTime * animationSmoothSpeed);
        float newStrafeWeight = Mathf.Lerp(currStrafeWeight, targetStrafeWeight, Time.deltaTime * animationSmoothSpeed);
        currentMoveSpeed = moveSpeed;

        if (!isGrounded)
        {
            if (_verticalVelocity < 0) currentMovementState = MovementState.Falling;
            else currentMovementState = MovementState.Jumping;
            _playerState.InAir = true;
        }
        else
        {
            _playerState.InAir = false;
            if (rollTriggered)
            {
                rollCoroutine = StartCoroutine(WaitEndRoll());
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

        if (_playerState.Attacking)
        {
            moveSpeed *= _playerState.currentAttack.MovementSpeedFactor;
        }

        Vector3 moveVector = targetDirection.normalized * moveSpeed + new Vector3(0.0f, _verticalVelocity, 0.0f);
        moveVector += physicsVelocity;
        if (moveVector.sqrMagnitude > _threshold)
        {
            _characterController.Move(moveVector * Time.deltaTime);
        }
    }

    public void ApplyForce(Vector3 force)
    {
        ApplyForceServerRpc(force);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ApplyForceServerRpc(Vector3 force)
    {
        nvPhysicsVelocity.Value = force;
    }

    private void PhysicsVelocityChanged(Vector3 oldValue, Vector3 newValue) {
        if (!IsOwner) { return; }
        StartCoroutine(UpdateLocalPhysicsVelocity(newValue));
    }

    private IEnumerator UpdateLocalPhysicsVelocity(Vector3 newValue) {
        physicsVelocity = newValue;
        while (physicsVelocity != Vector3.zero) {
            physicsVelocity.x = Mathf.Lerp(physicsVelocity.x, 0, forceDecay * Time.deltaTime);
            physicsVelocity.z = Mathf.Lerp(physicsVelocity.z, 0, forceDecay * Time.deltaTime);
            physicsVelocity.y = Math.Abs(physicsVelocity.y) < 3f ? 0 :
                Mathf.Lerp(physicsVelocity.y, 0, forceDecay * Time.deltaTime);
            yield return null;
        }
    }

    private Vector3 HandlePlayerAndCameraRotation(bool isLockedOn, Vector2 moveDir, bool isMoving) {
        Vector3 playerTargetDirection = Vector3.zero;
        bool scrolledUp = GameInput.Instance.ScrolledUp();
        bool scrolledDown = GameInput.Instance.ScrolledDown();
        float targetRotation;
        float rotationSpeedFactor = _playerState.Attacking ? _playerState.currentAttack.RotationSpeedFactor : 1f;

        if (isLockedOn) {
            bool hasLockOnTarget = _playerState.lockOnTargets.Count > 0;
            if (hasLockOnTarget) {
                if (scrolledUp) playerTargetIndex = (playerTargetIndex + 1) % _playerState.lockOnTargets.Count;
                if (scrolledDown) playerTargetIndex = (playerTargetIndex - 1 + _playerState.lockOnTargets.Count) % _playerState.lockOnTargets.Count;

                Transform lockOnTarget = _playerState.lockOnTargets[playerTargetIndex];
                Vector3 directionToTarget = (lockOnTarget.position - transform.position).normalized;
                targetRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            }
            else {
                targetRotation = _mainCamera.transform.eulerAngles.y;
            }

            Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y);
            if (inputDirection.sqrMagnitude > _threshold) {
                playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * inputDirection.normalized;
            }

            Quaternion targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
        }
        else {
            if (isMoving) {
                Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                playerTargetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

                Quaternion targetRot = Quaternion.Euler(0.0f, targetRotation, 0.0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * rotationSpeedFactor * Time.deltaTime);
            }
        }
        return playerTargetDirection;
    }

    private IEnumerator WaitEndRoll() {
        _playerState.Rolling = true;
        if (rollCoroutine != null) { StopCoroutine(rollCoroutine); }
        _anim.applyRootMotion = true;
        yield return new WaitForSeconds(rollTime);
        _anim.applyRootMotion = false;
        _playerState.Rolling = false;
    }

    private void OnRightClickPerform(GameInput.AttackInput input) {
        if (input == GameInput.AttackInput.RightMouse) {
            _playerState.lockOnTargets.Sort((a, b) => Vector3.Distance(a.position, transform.position).CompareTo(Vector3.Distance(b.position, transform.position)));
            playerTargetIndex = 0;
        }
    }

    private void LateUpdate() {
        if (cursorInputForLook && cursorLocked) { CameraRotation(); }
    }

    private void GroundedCheck() {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation() {
        if (GameInput.Instance.GetPlayerLookVectorNormalized().sqrMagnitude >= _threshold)
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

    public Transform GetEyesTransform() {
        return eyesTransform;
    }
}