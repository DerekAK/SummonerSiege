using UnityEngine;

public abstract class BaseBehaviorState : ScriptableObject
{
    public abstract void InitializeState(BehaviorManager behaviorManager);

    public abstract void DeInitializeState();

    public abstract void EnterState();

    public abstract void ExitState();

    public abstract void UpdateState();
}