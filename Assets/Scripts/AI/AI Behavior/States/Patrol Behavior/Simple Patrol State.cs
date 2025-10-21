using UnityEngine;

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

    public override void EnterState()
    {
        // Set the speed for patrolling
        _behaviorManager.HandleSpeedChangeWithFactor(patrolSpeedFactor);

        MoveToPatrolPosition();

    }

    public override void ExitState()
    {
        _agent.ResetPath();
    }

    public override void UpdateState()
    {
        // Check if we've reached the destination
        if (!_agent.pathPending && _agent.remainingDistance < _agent.stoppingDistance)
        {
            // Move to the next patrol point
            _behaviorManager.SwitchState(_behaviorManager.IdleState);
        }
    }

    private void MoveToPatrolPosition(){
        Vector3 randDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        float roamingRange = Random.Range(10, patrolRange);
        Vector3 newPos = _behaviorManager.StartPosition + (randDir * roamingRange);
        _agent.SetDestination(UtilityFunctions.FindNavMeshPosition(newPos, _agent));
    }

}
