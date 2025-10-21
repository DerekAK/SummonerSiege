using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The base class for all Idle states. Idle states are for when the AI is not actively
/// pursuing any objective and is waiting for a trigger, like seeing a target.
/// </summary>
public abstract class BaseIdleState : BaseBehaviorState
{
    // You could add shared properties or methods for all idle states here in the future.
    // For example, a reference to a specific idle animation.

    [Tooltip("Min and Max idling time between patrol points")]
    [SerializeField] protected Vector2 idleTimeRange;
    protected BehaviorManager _behaviorManager;
    protected NavMeshAgent _agent;
    protected Coroutine idleCoroutine;

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        _behaviorManager = behaviorManager;
        _agent = _behaviorManager.GetComponent<NavMeshAgent>();
        
    }

    public override void DeInitializeState()
    {
        return;
    }

}
