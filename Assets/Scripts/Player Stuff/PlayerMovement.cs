using Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement: NetworkBehaviour
{

    // Core Settings
    protected Animator _anim;
    protected EntityStats _playerStats;
    protected PlayerCombat _playerCombat;


    [Header("Movement Settings")]
    protected bool inAir;
    public bool InAir => inAir;
    protected bool isRolling;
    public bool IsRolling => isRolling;
    [Range(0, 1)] [SerializeField] protected float walkSpeedFactor = 0.5f;
    [Range(0, 1)] [SerializeField] protected float crouch_lockOn_Factor = 0.5f;
    [SerializeField] protected float fastFallFactor = 1.5f;
    [SerializeField] protected float jumpHeight = 10;
    [SerializeField] protected float rollForce = 10;
    [SerializeField] protected float rotationSpeed = 3f;


    [Header("Camera Settings")]
    [SerializeField] protected Camera mainCamera;
    [SerializeField] protected GameObject cinemachineCameraTarget;
    [SerializeField] protected GameObject playerFollowCamera;
    [SerializeField] protected float topClamp = 70.0f;
    [SerializeField] protected float bottomClamp = -30.0f;
    [SerializeField] protected float minCameraDistance = -1f;  // First person
    [SerializeField] protected float maxCameraDistance = 8.0f;  // Third person far
    [Tooltip("How far camera is before head renderer is enabled/disabled")]
    [SerializeField] protected float firstPersonCameraDistanceThreshold = 0.0f; 
    [SerializeField] protected float zoomSpeed = 1.0f;
    [SerializeField] protected float currentCameraDistance = 5.0f;
    protected Cinemachine3rdPersonFollow thirdPersonFollow;
    protected float cinemachineTargetYaw;
    protected float cinemachineTargetPitch;


    [Header("Grounded Settings")]
    [SerializeField] protected float groundedOffset;
    [SerializeField, Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")] 
    protected float groundedRadius;
    [SerializeField] protected LayerMask groundLayers;
    protected bool isGrounded = true;


    [Header("Animator settings")]
    [SerializeField] protected float animationSmoothSpeed = 10f;
    protected int moveXParam = Animator.StringToHash("InputX");
    protected int moveYParam = Animator.StringToHash("InputY");
    protected int rollXParam = Animator.StringToHash("RollX");
    protected int rollYParam = Animator.StringToHash("RollY");
    protected int animMovementStateParam = Animator.StringToHash("Movement State");
    protected int crouchLayerIndex = 1;
    protected int strafeLayerIndex = 2;


    [Header("Miscellaneous")]
    [SerializeField] protected string billBoardTag = "BillBoard";
    protected enum MovementState { Locomotion = 0, Jumping = 1, Falling = 2, Rolling = 3 }
    protected MovementState currentMovementState;
    protected bool statsConfigured = false;
    protected int playerTargetIndex = 0;


    public virtual void AnimationEvent_EndRoll()
    {
        
    }
}
