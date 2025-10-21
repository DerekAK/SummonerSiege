using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "ProjectileAttack", menuName = "Scriptable Objects/Attacks/Projectile")]
public class ProjectileAttackSO : PlayerAttackSO
{
    public override void ExecuteAttack(CombatManager combatManager)
    {
        throw new System.NotImplementedException();
    }

    public override void OnAnimationEvent(int numEvent, CombatManager combatManager)
    {
        throw new System.NotImplementedException();
    }
}