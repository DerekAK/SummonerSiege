using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackAction", menuName = "Scriptable Objects/AI Behavior/Attacks/Test Attack")]
public class TestAttack : EnemyAttackSO
{
    public override void ExecuteAttack(CombatManager _combatManager)
    {
        return;
    }

    public override void OnAnimationEvent(int numEvent, CombatManager combatManager)
    {
        return;
    }
}
