public abstract class BaseChasingState : BaseBehaviorState
{

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        
    }

    public override void DeInitializeState(BehaviorManager behaviorManager)
    {
        
    }
    
    public abstract void Chase(BehaviorManager behaviorManager);
}
