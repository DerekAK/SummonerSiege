using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "MeleeWeaponAttack", menuName = "Scriptable Objects/Attacks/MeleeWeapon")]
public class MeleeWeaponAttackSO : PlayerAttackSO
{
    public override void ExecuteAttack(CombatManager combatManager)
    {
        return;
    }

    public override void OnAnimationEvent(int numEvent, CombatManager combatManager)
    {
        return;
    }
}
