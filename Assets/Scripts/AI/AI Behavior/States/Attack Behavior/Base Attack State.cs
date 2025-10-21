public abstract class BaseAttackState : BaseBehaviorState
{
    protected EnemyCombat _combatManager;

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        _combatManager = behaviorManager.GetComponent<EnemyCombat>();
    }

    public override void EnterState()
    {
        _combatManager.StartChosenAttack();
    }

    public override void DeInitializeState()
    {
        return;
    }

    // States that interrupt an attack (like getting stunned) would call this
    public override void ExitState()
    {
        return;
    }

    public override void UpdateState()
    {
        return;
    }
}