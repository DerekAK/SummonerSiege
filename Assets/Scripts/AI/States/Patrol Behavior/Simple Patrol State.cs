using UnityEditor.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SimplePatrol", menuName = "Scriptable Objects/AI Behavior/States/Patrol/SimplePatrol")]
public class SimplePatrolState : BasePatrolState
{
    [Tooltip("The speed factor relative to the character's max speed while patrolling.")]
    [Range(0f, 1f)]
    [SerializeField] private float patrolSpeedFactor = 0.3f;
    [SerializeField] private float patrolRange = 30;


    public override void InitializeState(BehaviorManager behaviorManager)
    {
        base.InitializeState(behaviorManager);
    }

    public override void EnterState(BehaviorManager behaviorManager)
    {
        // Set the speed for patrolling
        behaviorManager.HandleSpeedChangeWithFactor(patrolSpeedFactor);

        behaviorManager.GetComponent<NavMeshAgent>().SetDestination(GetPatrolPosition(behaviorManager));
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
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
