using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackAction", menuName = "Scriptable Objects/Attacks/Enemy/Basic Attack")]
public class BasicEnemyAttackSO: EnemyAttackSO
{
    public WeaponCategorySO WeaponCategorySO;


    public override void ExecuteAttack(CombatManager combatManager)
    {
        base.ExecuteAttack(combatManager);
        
        if (combatManager is EnemyCombat enemyCombat)
        {
            // Pick random arc direction
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 arc = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        
            enemyCombat.SetBasicAttackArc(arc);
        }
    }
}