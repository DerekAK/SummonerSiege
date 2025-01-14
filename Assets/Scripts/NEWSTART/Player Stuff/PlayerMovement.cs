using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;

    [Header("Grounded Settings")]
    [SerializeField] private bool isGrounded = true;
    [Tooltip("Useful for rough ground")][SerializeField] private float groundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    [SerializeField] private float groundedRadius = 0.5f;
    private PlayerStats _playerStats;
    private float _verticalVelocity;
    private float gravity = Physics.gravity.y;
    [SerializeField] private float fastFallFactor = 100f;
    [SerializeField] private float jumpHeight = 100f;
    [SerializeField] private float walkingSpeed = 5f;
    [SerializeField] private float runningSpeed = 10f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    private float _targetRotation = 0.0f;
    public enum PlayerState{Locomotion=0, Jumping=1, Falling=2}
    public PlayerState currentPlayerState{get; private set;}

    private CharacterController _characterController;
    private Animator _anim;
    private float currentAnimThreshold;
    [SerializeField] private Transform eyesTransform;
    
    
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
    }
    private void Start(){
        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        // Apply initial cursor state
        SetCursorState(cursorLocked);
    }

    private void Update(){
        GroundedCheck();
        UpdateVerticalVelocity(); //accounts for jumping
        Move();

        CursorStuffIDontUnderstand();
    }

    private void UpdateVerticalVelocity(){
        Debug.Log(gravity);
        if(isGrounded){
            if (_verticalVelocity < 0.0f){
                _verticalVelocity = -2f;
            }
            if(GameInput.Instance.JumpPressed()){
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * fastFallFactor * gravity);
            }
        }
        else{_verticalVelocity += fastFallFactor * gravity * Time.deltaTime;}
    }
    private void Move()
    {
        float targetAnimThreshold; // The desired animation threshold value
        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized();

        bool isMoving = moveDir != Vector2.zero;
        bool isSprinting = GameInput.Instance.SprintingPressed();
        float moveSpeed;

        // Determine the target values based on movement and sprinting
        if (isMoving) // Moving
        {
            if (isSprinting) // Running
            {
                moveSpeed = runningSpeed;
                targetAnimThreshold = 1f;
            }
            else // Walking
            {
                moveSpeed = walkingSpeed;
                targetAnimThreshold = 0.5f;
            }
        }
        else // Idle
        {
            moveSpeed = 0.0f;
            targetAnimThreshold = 0f;
        }

        // Smoothly interpolate the animation threshold
        currentAnimThreshold = _anim.GetFloat("Speed");
        float smoothSpeed = 5f; // Adjust this value to control smoothing speed
        float animThreshold;
        if(currentAnimThreshold > 1e-4f){ //small threshold so Math.Lerp doesn't continue forever.
            animThreshold = Mathf.Lerp(currentAnimThreshold, targetAnimThreshold, Time.deltaTime * smoothSpeed);
        }
        else{
            animThreshold = targetAnimThreshold;
        }
        currentAnimThreshold = _anim.GetFloat("Speed");
        // Handle jumping and grounded state
        if (!isGrounded)
        {
            // Doesn't matter what animThreshold is because not in blend tree state, instead in jumping
            if (_verticalVelocity < 0) { currentPlayerState = PlayerState.Falling; } // Falling
            else { currentPlayerState = PlayerState.Jumping; }
        }
        else
        {
            currentPlayerState = PlayerState.Locomotion;
        }

        // Update animator parameters
        _anim.SetFloat("Speed", animThreshold);
        _anim.SetInteger("PlayerState", (int)currentPlayerState);

        // Calculate movement direction and rotate character
        Vector3 inputDirection = new Vector3(moveDir.x, 0.0f, moveDir.y).normalized;
        if (isMoving)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref rotationSpeed, rotationSmoothTime);
            // Rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        // Move the character
        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        _characterController.Move(targetDirection.normalized * (moveSpeed * Time.deltaTime) +
                                new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (cursorInputForLook && cursorLocked){
            CameraRotation();
        }
    }
    private void GroundedCheck()
    {
        // Set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
        // If there is input and camera position is not fixed
        if (GameInput.Instance.GetPlayerLookVectorNormalized().sqrMagnitude >= _threshold)
        {
            _cinemachineTargetYaw += GameInput.Instance.GetPlayerLookVectorNormalized().x * 1.2f;
            _cinemachineTargetPitch += GameInput.Instance.GetPlayerLookVectorNormalized().y * 1.2f;
        }

        // Clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

        // Cinemachine will follow this target
        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch,
            _cinemachineTargetYaw, 0.0f);
    }
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            SetCursorState(cursorLocked);
        }
    }

    private void SetCursorState(bool newState)
    {
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
    public Transform GetEyesTransform(){
        return eyesTransform;
    }
}