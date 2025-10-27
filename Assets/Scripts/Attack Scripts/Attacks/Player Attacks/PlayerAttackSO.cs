using UnityEngine;

public abstract class PlayerAttackSO : BaseAttackSO
{

    [Header("General Settings")]
    public bool Holdable;
    
    public virtual void OnHoldCanceled(CombatManager combatManager)
    {
    }
}
