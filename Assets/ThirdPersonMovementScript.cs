using UnityEngine;

public class ThirdPersonMovementScript : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;

    [Header("Grounded Settings")]
    [SerializeField] private bool isGrounded = true;
    [Tooltip("Useful for rough ground")][SerializeField] private float groundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    [SerializeField] private float groundedRadius = 0.5f;
    private float _verticalVelocity;
    [SerializeField] private float gravity = -10f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float walkingSpeed = 5f;
    [SerializeField] private float runningSpeed = 10f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    private float _targetRotation = 0.0f;
    private enum PlayerState{Idle = 0, Walking = 1, Running = 2, Jumping=3, Falling=4}
    private PlayerState currentPlayerState;

    private CharacterController _characterController;
    private Animator _anim;
    
    
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
        if (_mainCamera == null){
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
        _characterController = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
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
        if(isGrounded){
            if (_verticalVelocity < 0.0f){
                _verticalVelocity = -2f;
            }
            if(GameInput.Instance.JumpTriggered()){
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else{_verticalVelocity += gravity * Time.deltaTime;}
    }
    private void Move(){
        float moveSpeed;
        if(GameInput.Instance.SprintingPressed()){
            moveSpeed = runningSpeed;
        }
        else{
            moveSpeed = walkingSpeed;
        }
        Vector2 moveDir = GameInput.Instance.GetPlayerMovementVectorNormalized() * moveSpeed;
        
        bool isMoving = moveDir != Vector2.zero;
        if(isMoving){
            if(GameInput.Instance.SprintingPressed()){ //running
                if(isGrounded){currentPlayerState = PlayerState.Running;}
                moveSpeed = runningSpeed;
            }
            else{ //walking
                if(isGrounded){currentPlayerState = PlayerState.Walking;}
                moveSpeed = walkingSpeed;
            }
        }
        else{ //player is not moving
            moveSpeed = 0.0f;
            if(isGrounded){currentPlayerState = PlayerState.Idle;}
        }

        if(_verticalVelocity < 0 && !isGrounded){currentPlayerState = PlayerState.Falling;}
        
        if(_verticalVelocity > 0){currentPlayerState = PlayerState.Jumping;}//is jumping

        _anim.SetInteger("PlayerState", (int)currentPlayerState);

        Vector3 inputDirection = new Vector3(GameInput.Instance.GetPlayerMovementVectorNormalized().x, 0.0f, GameInput.Instance.GetPlayerMovementVectorNormalized().y).normalized;
        if (moveDir != Vector2.zero){
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref rotationSpeed,
                rotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

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
}