using UnityEngine;

[CreateAssetMenu(fileName = "WeaponlessAttack", menuName = "Scriptable Objects/Attacks/Weaponless")]
public class WeaponlessAttackSO : PlayerAttackSO{

    public override void ExecuteAttack(CombatManager combatManager)
    {
        return;
    }

    public override void OnAnimationEvent(int numEvent, CombatManager combatManager)
    {
        return;
    }
}