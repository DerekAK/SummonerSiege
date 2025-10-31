using UnityEngine;

public abstract class BaseBehaviorState : ScriptableObject
{
    public abstract void InitializeState(BehaviorManager behaviorManager);

    public abstract void DeInitializeState(BehaviorManager behaviorManager);

    public abstract void EnterState(BehaviorManager behaviorManager);

    public abstract void ExitState(BehaviorManager behaviorManager);

    public abstract void UpdateState(BehaviorManager behaviorManager);
}