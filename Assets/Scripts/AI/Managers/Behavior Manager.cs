using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

// This enum defines the physical states the character can be in.

[RequireComponent(typeof(EntityStats), typeof(JumpManager), typeof(EnemyCombat))]
public class BehaviorManager : NetworkBehaviour
{
    // Components
    private Animator _anim;
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private EntityStats _entityStats;
    private JumpManager _jumpManager;
    private ColliderManager _colliderManager;
    private EnemyCombat _combatManager;

    // Animator fields
    private static int animSpeedX = Animator.StringToHash("SpeedX");
    private static int animSpeedY = Animator.StringToHash("SpeedY");


    [Header("States")]
    private BaseBehaviorState currentState;
    public BaseBehaviorState CurrentState => currentState;
    [SerializeField] private BaseIdleState idleState;
    [SerializeField] private BasePatrolState patrolState;
    [SerializeField] private BaseChasingState chasingState;
    [SerializeField] private BaseAttackState attackState;

    // Public properties to allow states to request a switch
    public BaseIdleState IdleState => idleState;
    public BasePatrolState PatrolState => patrolState;
    public BaseChasingState ChasingState => chasingState;
    public BaseAttackState AttackState => attackState;

    [Header("Intentions")]
    [SerializeField] private List<Intention> availableIntentions;
    [SerializeField] private float intentionDecisionFrequency = 1.0f; // How often to re-evaluate our goal (in seconds)
    private Intention currentIntention;
    private float lastIntentionDecisionTime;

    private bool isStatsConfigured = false;
    private Vector3 startPosition;
    public Vector3 StartPosition => startPosition;
    private List<GameObject> targetsInRange = new List<GameObject>();
    public List<GameObject> TargetsInRange => targetsInRange;
    private GameObject currentTarget = null;
    public GameObject CurrentTarget => currentTarget;
    public string TargetTag;
    public Coroutine IdleCoroutine;


    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _entityStats = GetComponent<EntityStats>();
        _jumpManager = GetComponent<JumpManager>();
        _colliderManager = GetComponentInChildren<ColliderManager>();
        _combatManager = GetComponent<EnemyCombat>();
    }

    private void OnEnable()
    {
        _agent.enabled = false;
        _entityStats.OnStatsConfigured += StatsConfigured;

        // call the initialize function for each state
        IdleState.InitializeState(this);
        PatrolState.InitializeState(this);
        ChasingState.InitializeState(this);
        AttackState.InitializeState(this);

        _colliderManager.OnHitDetection += HitDetected;
        _colliderManager.OnTargetEntrance += TargetEntered;
        _colliderManager.OnTargetExit += TargetExited;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"BehaviorManager.OnNetworkSpawn() called. IsSpawned={IsSpawned}");
        base.OnNetworkSpawn();

        _agent.enabled = true;

        if (IsServer && isStatsConfigured)
        {
            DecideNextIntention();
        }
    }

    private void StatsConfigured()
    {
        isStatsConfigured = true;
        if (IsSpawned && IsServer)
        {
            DecideNextIntention();
        }
    }

    private void OnDisable()
    {
        _entityStats.OnStatsConfigured -= StatsConfigured;

        // call the deinitialize function for each state
        IdleState.DeInitializeState(this);
        PatrolState.DeInitializeState(this);
        ChasingState.DeInitializeState(this);
        AttackState.DeInitializeState(this);

        _colliderManager.OnHitDetection -= HitDetected;
        _colliderManager.OnTargetEntrance -= TargetEntered;
        _colliderManager.OnTargetExit -= TargetExited;
    }

    private void Start()
    {
        _agent.updateRotation = false;
        _agent.autoTraverseOffMeshLink = false;
        _rb.isKinematic = true;
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!isStatsConfigured || !IsSpawned || currentState == null) return;

        if (!IsServer) return;
        
        HandleRotation();
        HandleJumping();
        currentState.UpdateState(this);

        if (_combatManager.InAttack) return; // needs to come before deciding an intention and setting a target

        if (Time.time > lastIntentionDecisionTime + intentionDecisionFrequency)
        {
            DecideNextIntention();
        }

        SetCurrentTarget();

        if (Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[{gameObject.name}] IsServer={IsServer}, IsSpawned={IsSpawned}, currentState={currentState?.name}, currentTarget={CurrentTarget?.name}");
        }
    }

    public void DecideNextIntention()
    {
        lastIntentionDecisionTime = Time.time;

        float bestScore = -1f;
        Intention bestIntention = null;

        foreach (var intention in availableIntentions)
        {
            float score = intention.ScoreIntention(this);
            if (score > bestScore)
            {
                bestScore = score;
                bestIntention = intention;
            }
        }

        if (bestIntention)
        {
            currentIntention = bestIntention;
            currentIntention.Execute(this);
        }
    }

    private void HandleRotation()
    {
        Vector3 lookDirection = Vector3.zero;
        if ((_agent.isOnOffMeshLink || _combatManager.InAttack) && currentTarget)
        {
            if (_combatManager.StopRotate) return;

            lookDirection = currentTarget.transform.position - transform.position;
            lookDirection.y = transform.position.y;
        }
        else if (_agent.isOnNavMesh)
        {
            lookDirection = _agent.desiredVelocity;
        }

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            // Create a rotation that looks in that direction
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // Smoothly rotate the agent's transform towards the target rotation
            transform.rotation = Quaternion.Slerp(_agent.transform.rotation, targetRotation, _agent.angularSpeed * Time.deltaTime);
        }
    }
    
    private void HandleJumping()
    {
        if (_agent.isOnOffMeshLink)
        {
            OffMeshLinkData data = _agent.currentOffMeshLinkData;
            Vector3 startPos = _agent.transform.position;
            Vector3 endPos = data.endPos + Vector3.up * _agent.baseOffset;

            _jumpManager.InitiateJump(startPos, endPos); // will only initiate if not in current jump, this is handled by jumpmanager
        }
    }

    private void SetCurrentTarget()
    {
        if (currentTarget != null) {
            if (!targetsInRange.Contains(currentTarget) && targetsInRange.Count > 0)
            {
                currentTarget = TryGetNewTarget();
            }
            else if (targetsInRange.Count == 0)
            {
                currentTarget = null;
            }
        }
        else if (currentTarget == null)
        {
            if (targetsInRange.Count > 0)
            {
                currentTarget = TryGetNewTarget(); // if not null, SHOULD notify current state
            }
        }
    }

    private GameObject TryGetNewTarget()
    {
        foreach (GameObject target in targetsInRange)
        {
            if (CanDetect(target))
            {
                return target;
            }
        }
        return null;
    }
    private bool CanDetect(GameObject target)
    {
        return true;
    }
    
    public void HandleSpeedChangeWithValue(float speedValue)
    {
        _entityStats.TryGetStat(StatType.Speed, out NetStat speedNetStat);
        float animSpeedValue = speedValue / speedNetStat.MaxValue;

        // 1.
        _entityStats.SetStatServerRpc(StatType.Speed, speedValue);
        // 2.
        _agent.speed = speedValue;
        // 3.
        _anim.SetFloat(animSpeedY, animSpeedValue);
    }
    
    public void HandleSpeedChangeWithFactor(float speedFactor)
    {
        // Debug.Log($"BehaviorManager IsSpawned: {IsSpawned}");
        // Debug.Log($"EntityStats IsSpawned: {_entityStats.IsSpawned}");
        // Debug.Log($"EntityStats IsServer: {_entityStats.IsServer}");
        // Debug.Log($"EntityStats NetworkObject: {_entityStats.NetworkObject}");
        // Debug.Log($"EntityStats NetworkObjectId: {_entityStats.NetworkObjectId}");

        _entityStats.TryGetStat(StatType.Speed, out NetStat speedNetStat);
        float newSpeedValue = speedFactor * speedNetStat.MaxValue;

        // 1.
        _entityStats.SetStatServerRpc(StatType.Speed, newSpeedValue);
        // 2.
        _agent.speed = newSpeedValue;
        // 3.
        _anim.SetFloat(animSpeedY, speedFactor);
    }

    public void SwitchState(BaseBehaviorState newState)
    {
        // Don't switch to a null state
        if (newState == null) return;

        currentState?.ExitState(this);
        Debug.Log($"Switching state to new state: {newState.name}");
        currentState = newState;
        currentState.EnterState(this);
    }

    private void HitDetected(Collider other)
    {

    }
    private void TargetEntered(Collider other)
    {
        if (other.CompareTag(TargetTag) && !targetsInRange.Contains(other.gameObject))
        {
            targetsInRange.Add(other.gameObject);
        }
    }
    
    private void TargetExited(Collider other)
    {
        if (other.CompareTag(TargetTag) && targetsInRange.Contains(other.gameObject))
        {
            targetsInRange.Remove(other.gameObject);
        }
    }
}
