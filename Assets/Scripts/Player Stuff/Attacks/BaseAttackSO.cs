using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseAttackSO : ScriptableObject
{
    public enum Element{
        None,
        Fire,
        Earth,
        Ice,
        Lightning
    }

    public enum eDamageType{
        Single,
        Continuous
    }

    [Header("General Settings")]
    public AnimationClip AttackClip;
    public bool AirAttack;
    public bool Holdable;
    public float MovementSpeedFactor = 1f;
    public float RotationSpeedFactor = 1f;
    public float Cooldown;
    
    protected int currHitboxIndex;

    [Header("2d list of hitboxes, each row is hitboxes activated at one animation event")]
    public List<HitboxGroup> MatrixHitboxes = new List<HitboxGroup>();

    [System.Serializable]
    public struct Hitbox{
        [Tooltip("Bone that has child gameobject with damage collider on it")]
        public HumanBodyBones AttachBone;
        public float Size;
        public Element ElementType;
        public List<BaseStatusEffect> StatusEffects;

        [Header("Damage Settings")]
        public eDamageType DamageType;
        [Tooltip("For continuous damage (e.g., fire tick)")]
        public float TickRate;
        public float DamageMultiplier;
        public float KnockbackMultiplier;
    }
    
    [System.Serializable]
    public struct HitboxGroup{
        public List<Hitbox> Hitboxes;
    }
    
    protected void UpdateCurrHitboxIndex(){currHitboxIndex = (currHitboxIndex + 1) % MatrixHitboxes.Count;}

    // this is used to keep track of current attack index in cases where enable hitbox is called more than once on the attack animatio
    public abstract void Enable(PlayerCombat combat, Animator anim);
    public abstract void Disable(PlayerCombat combat, Animator anim);
}