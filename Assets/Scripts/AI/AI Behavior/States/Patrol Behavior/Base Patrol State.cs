using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The base class for all Patrol states. Patrol states involve moving between
/// a series of predefined points until interrupted by another state (e.g., spotting a target).
/// </summary>
public abstract class BasePatrolState : BaseBehaviorState
{
    protected NavMeshAgent _agent;
    protected ColliderManager _colliderManager;
    protected BehaviorManager _behaviorManager;

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        _behaviorManager = behaviorManager;
        _agent = behaviorManager.GetComponent<NavMeshAgent>();
        if (!_agent.isActiveAndEnabled) return;

        _colliderManager = behaviorManager.GetComponent<ColliderManager>();
    }

    public override void DeInitializeState()
    {
        return;
    }


}
