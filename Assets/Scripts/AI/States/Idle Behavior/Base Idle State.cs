using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The base class for all Idle states. Idle states are for when the AI is not actively
/// pursuing any objective and is waiting for a trigger, like seeing a target.
/// </summary>
public abstract class BaseIdleState : BaseBehaviorState
{

    [Tooltip("Min and Max idling time between patrol points")]
    [SerializeField] protected Vector2 idleTimeRange;

    public override void InitializeState(BehaviorManager behaviorManager)
    {
    }

    public override void DeInitializeState(BehaviorManager behaviorManager)
    {
    }

}
