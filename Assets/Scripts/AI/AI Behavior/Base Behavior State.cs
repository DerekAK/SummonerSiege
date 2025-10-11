using UnityEngine;

public abstract class BaseBehaviorState: ScriptableObject
{
    public abstract void EnterState(BehaviorManager behaviorManager);

    public abstract void ExitState(BehaviorManager behaviorManager);

    public abstract void UpdateState(BehaviorManager behaviorManager);

    public abstract void OnCollision(BehaviorManager behaviorManager, Collision collision);
}