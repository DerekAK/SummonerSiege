using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAttackSO : ScriptableObject
{
    public enum eBodyPart
    {
        RightHand,
        LeftHand,
        RightFoot,
        LeftFoot,
        MeleeWeapon,

    }

    public enum eElement
    {
        None,
        Fire,
        Earth,
        Ice,
        Lightning
    }


    [Header("General Settings")]
    public AnimationClip AttackClip;
    public bool AirAttack;
    public float MovementSpeedFactor = 1f;
    public float RotationSpeedFactor = 1f;
    public float Cooldown;


    [Header("2d list of hitboxes, each row is hitboxes activated at one animation event")]
    public List<HitboxGroup> HitboxGroups = new List<HitboxGroup>();

    [System.Serializable]
    public struct Hitbox
    {
        public eBodyPart bodyPart;
        public float radiusFactor;
        public eElement element;
        public BaseStatusEffectSO[] statusEffectSOs;
        public float knockbackMultiplier;
    }

    [System.Serializable]
    public struct HitboxGroup
    {
        public List<Hitbox> hitboxes;
    }

    // Now accepts an index from the CombatManager
    public void EnableHitBoxes(Dictionary<eBodyPart, DamageCollider> damageColliderDict, int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= HitboxGroups.Count) return;

        foreach (Hitbox hitbox in HitboxGroups[groupIndex].hitboxes)
        {
            if (damageColliderDict.ContainsKey(hitbox.bodyPart))
            {
                DamageCollider damageCollider = damageColliderDict[hitbox.bodyPart];
                SphereCollider sphereCollider = damageCollider.GetComponent<SphereCollider>();

                sphereCollider.enabled = true;
                sphereCollider.radius *= hitbox.radiusFactor;
                damageCollider.SetInfoForDamageCollider(hitbox);
            }
        }
    }

    public void DisableHitBoxes(Dictionary<eBodyPart, DamageCollider> damageColliderDict, int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= HitboxGroups.Count) return;

        foreach (Hitbox hitbox in HitboxGroups[groupIndex].hitboxes)
        {
            if (damageColliderDict.ContainsKey(hitbox.bodyPart))
            {
                SphereCollider sphereCollider = damageColliderDict[hitbox.bodyPart].GetComponent<SphereCollider>();

                sphereCollider.enabled = false;
                sphereCollider.radius /= hitbox.radiusFactor;
            }
        }
    }

    public abstract void ExecuteAttack(CombatManager combatManager);
    public abstract void OnAnimationEvent(int numEvent, CombatManager combatManager);
}
