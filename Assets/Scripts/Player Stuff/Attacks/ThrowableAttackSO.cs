using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThrowableAttack", menuName = "Scriptable Objects/Attacks/Throwable")]
public class ThrowableAttackSO : PlayerAttackSO
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