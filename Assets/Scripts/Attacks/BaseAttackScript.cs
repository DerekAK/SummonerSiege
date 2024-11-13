using UnityEngine;

public abstract class BaseAttackScript : MonoBehaviour
{
    public string ph1 = "Attack1 Placeholder";
    public string ph2 = "Attack2 Placeholder";
    public string ph3 = "Attack3 Placeholder";
    public abstract void ExecuteAttack(object sender, EnemyAI3.AttackEvent e);
}
