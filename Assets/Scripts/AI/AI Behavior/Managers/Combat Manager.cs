using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class CombatManager: NetworkBehaviour
{   
    [HideInInspector] public BaseAttackSO ChosenAttack;
    protected Dictionary<BaseAttackSO.eBodyPart, DamageCollider> damageColliderDict = new();
    
    // This state is now managed per-player on the CombatManager, 
    // not on the shared ScriptableObject.
    private int currentHitboxGroupIndex = 0;

    public void RegisterDamageCollider(DamageCollider damageCollider)
    {
        if (!damageColliderDict.ContainsKey(damageCollider.BodyPart))
        {
            damageColliderDict[damageCollider.BodyPart] = damageCollider;
        }
        else
        {
            Debug.LogError($"{damageCollider.transform.root.name} contains multiple damage colliders with {damageCollider.BodyPart} bodypart!");
        }
    }

    public void ResetHitboxIndex()
    {
        currentHitboxGroupIndex = 0;
    }

    public void AnimationEvent_EnableHitBoxes()
    {
        if (ChosenAttack == null) return;
        ChosenAttack.EnableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
    }

    public void AnimationEvent_DisableHitBoxes()
    {
        if (ChosenAttack == null) return;
        ChosenAttack.DisableHitBoxes(damageColliderDict, currentHitboxGroupIndex);
        
        if (ChosenAttack.HitboxGroups.Count > 0)
        {
            currentHitboxGroupIndex = (currentHitboxGroupIndex + 1) % ChosenAttack.HitboxGroups.Count;
        }
    }

    public virtual void AnimationEvent_Trigger(int numEvent)
    {
        ChosenAttack?.OnAnimationEvent(numEvent, this);
    }

    public abstract void AnimationEvent_AttackFinished();

    // These events are for your specific combo logic.
    // They are called from the animator.
}
