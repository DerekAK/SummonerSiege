using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SimplePatrol", menuName = "Scriptable Objects/AI Behavior/States/Patrol/SimplePatrolState")]
public class SimplePatrolState : BasePatrolState
{
    [Tooltip("The speed factor relative to the character's max speed while patrolling.")]
    [Range(0f, 1f)]
    [SerializeField] private float patrolSpeedFactor = 0.3f;
    [SerializeField] private float patrolRange = 30;
    [SerializeField] private float lerpTime = 1;

    private Coroutine lerpSpeedCoroutine;


    public override void InitializeState(BehaviorManager behaviorManager)
    {
        base.InitializeState(behaviorManager);
    }

    public override void EnterState(BehaviorManager behaviorManager)
    {
        // Set the speed for patrolling

        if (!behaviorManager.GetComponent<EntityStats>().TryGetStat(StatType.Speed, out NetStat speedStat)) return;
        float startSpeed = speedStat.CurrentValue;
        float endSpeed = speedStat.MaxValue * patrolSpeedFactor;
        lerpSpeedCoroutine = behaviorManager.StartCoroutine(behaviorManager.LerpSpeed(startSpeed, endSpeed, lerpTime));

        behaviorManager.MoveTowardsTarget(GetPatrolPosition(behaviorManager));
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
        if(lerpSpeedCoroutine != null)
        {
            behaviorManager.StopCoroutine(lerpSpeedCoroutine);
            lerpSpeedCoroutine = null;
        }
        behaviorManager.GetComponent<NavMeshAgent>().ResetPath();
    }

    public override void UpdateState(BehaviorManager behaviorManager)
    {
        NavMeshAgent agent = behaviorManager.GetComponent<NavMeshAgent>();
        // Check if we've reached the destination

        if (!agent.isActiveAndEnabled) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            behaviorManager.SwitchState(behaviorManager.IdleState);
        }
    }

    private Vector3 GetPatrolPosition(BehaviorManager behaviorManager){
    
        Vector3 randDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        float roamingRange = Random.Range(10, patrolRange);
        Vector3 newPos = behaviorManager.StartPosition + (randDir * roamingRange);

        return UtilityFunctions.FindNavMeshPosition(newPos, behaviorManager.GetComponent<NavMeshAgent>());
    }

}
