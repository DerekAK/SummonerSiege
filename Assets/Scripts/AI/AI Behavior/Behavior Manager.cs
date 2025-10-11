using UnityEngine;
using UnityEngine.AI;

// This enum defines the physical states the character can be in.
public enum LocomotionState
{
    Grounded,      // Sticks to the ground and follows the NavMesh.
    Airborne,      // Standard physics, affected by gravity for jumps/knockbacks.
    TraversingLink // For scripted movement like jumping gaps via NavMesh links.
}

[RequireComponent(typeof(EntityStats))]
public class BehaviorManager : MonoBehaviour
{
    public static int AnimSpeedX = Animator.StringToHash("SpeedX");
    public static int AnimSpeedY = Animator.StringToHash("SpeedY");

    [Header("AI Configuration")]
    public Transform CurrentTarget;
    [SerializeReference] public BaseChasingState baseChasingState;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.3f;

    // Public property for states to control locomotion.
    public LocomotionState CurrentLocomotionState { get; set; }

    private BaseBehaviorState currentState;
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private EntityStats _entityStats;
    private float turnSpeed = 10f; // Adjusted for Slerp
    private bool isStatsConfigured = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _entityStats = GetComponent<EntityStats>();
    }

    private void OnEnable()
    {
        _entityStats.OnStatsConfigured += StatsConfigured;
    }

    private void OnDisable()
    {
        _entityStats.OnStatsConfigured -= StatsConfigured;
    }

    private void Start()
    {
        _agent.enabled = true;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
    }

    private void StatsConfigured()
    {
        _entityStats.SetStatServerRpc(StatType.Speed, 0);
        currentState = Instantiate(baseChasingState);
        currentState.EnterState(this);
        isStatsConfigured = true;
    }

    private void Update()
    {
        if (!isStatsConfigured || currentState == null) return;
        currentState.UpdateState(this);
    }

    private void FixedUpdate()
    {
        if (!isStatsConfigured) return;

        Vector3 moveDirection = _agent.desiredVelocity.normalized;
        _entityStats.TryGetStat(StatType.Speed, out NetStat speed);
        Vector3 desiredVelocity = moveDirection * speed.CurrentValue;

        // Switch physics logic based on the current locomotion state.
        switch (CurrentLocomotionState)
        {
            case LocomotionState.Grounded:
                HandleGroundedMovement(desiredVelocity);
                break;
            case LocomotionState.Airborne:
                HandleAirborneMovement(desiredVelocity);
                break;
            case LocomotionState.TraversingLink:
                // Logic for moving across NavMesh links would go here.
                break;
        }

        HandleRotation();
    }

    // Behavior Manager.cs

private void HandleGroundedMovement(Vector3 desiredVelocity)
{
    // --- High-Fidelity NavMesh Sticking Logic ---

    // 1. Calculate the potential next position based on the desired velocity.
    Vector3 nextPosition = _rb.position + desiredVelocity * Time.fixedDeltaTime;

    // 2. Sample the NavMesh to find the closest valid point to our potential next position.
    // We search in a small radius (e.g., 1.0f) to find a valid spot.
    if (NavMesh.SamplePosition(nextPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
    {
        // 3. We found a valid point on the NavMesh (hit.position).
        //    Now, calculate a new, corrected velocity to move us from our current
        //    position to that valid point over one physics step.
        Vector3 correctedVelocity = (hit.position - _rb.position) / Time.fixedDeltaTime;

        // 4. Apply the corrected velocity. This also handles ground projection implicitly.
        _rb.linearVelocity = correctedVelocity;
    }
    else
    {
        // If we can't find a valid point on the NavMesh (highly unlikely if grounded),
        // it might mean we've been pushed far away. Fall back to simple movement.
        HandleAirborneMovement(desiredVelocity);
    }
}

    private void HandleAirborneMovement(Vector3 desiredVelocity)
    {
        _rb.linearVelocity = new Vector3(desiredVelocity.x, _rb.linearVelocity.y, desiredVelocity.z);
    }

    private void HandleRotation()
    {
        if (_agent.hasPath)
        {
            Vector3 lookDirection = _agent.steeringTarget - _rb.position;
            lookDirection.y = 0;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * turnSpeed);
            }
        }
    }

    public void SwitchState(BaseBehaviorState newState)
    {
        currentState?.ExitState(this);
        currentState = Instantiate(newState);
        currentState.EnterState(this);
    }
}