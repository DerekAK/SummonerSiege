public abstract class BaseAttackState : BaseBehaviorState
{

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        
    }

    public override void EnterState(BehaviorManager behaviorManager)
    {
        behaviorManager.GetComponent<EnemyCombat>().StartChosenAttack();
    }

    public override void DeInitializeState(BehaviorManager behaviorManager)
    {
        
    }

    // States that interrupt an attack (like getting stunned) would call this
    public override void ExitState(BehaviorManager behaviorManager)
    {
        
    }

    public override void UpdateState(BehaviorManager behaviorManager)
    {
       
    }
}