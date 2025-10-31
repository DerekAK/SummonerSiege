using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The base class for all Patrol states. Patrol states involve moving between
/// a series of predefined points until interrupted by another state (e.g., spotting a target).
/// </summary>
public abstract class BasePatrolState : BaseBehaviorState
{

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        if (!behaviorManager.GetComponent<NavMeshAgent>().isActiveAndEnabled) return;
    }

    public override void DeInitializeState(BehaviorManager behaviorManager)
    {
        
    }


}
