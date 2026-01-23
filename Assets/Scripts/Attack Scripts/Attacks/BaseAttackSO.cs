using System.Collections.Generic;
using Unity.Netcode;
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

    public enum eDamageType
    {
        Single,
        Continuous
    }


    [Header("General Settings")]
    public int UniqueID = 0; // set this to default because editor script will set this based on GUID
    //public AssetReference AnimationClipRef;
    
    [Tooltip("Base attack damage for all hitboxes of this attack")]
    public float AttackDamage;
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
        [Tooltip("radius factor of 1 should be set to around the size of the object, but this is set manually")] 
        public float sizeFactor;
        public eElement element;
        public BaseStatusEffectSO[] statusEffectSOs;
        public eDamageType damageType;
        [Tooltip("only relevant for continous damage type")] 
        public float damageTickRate;
        public float damageMultiplier;
    }

    [System.Serializable]
    public struct HitboxGroup
    {
        public List<Hitbox> hitboxes;
    }

    // Now accepts an index from the CombatManager
    public void EnableHitBoxes(Dictionary<eBodyPart, DamageCollider> damageColliderDict, int groupIndex)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (groupIndex < 0 || groupIndex >= HitboxGroups.Count) return;
        foreach (Hitbox hitbox in HitboxGroups[groupIndex].hitboxes)
        {
            Debug.Log("HERE!!");
            if (damageColliderDict.ContainsKey(hitbox.bodyPart))
            {
                Debug.Log("ENABLING COLLIDER!");
                DamageCollider damageCollider = damageColliderDict[hitbox.bodyPart];
                Collider collider = damageCollider.GetComponent<Collider>();

                collider.enabled = true;

                HandleColliderSize(collider, hitbox.sizeFactor);

                damageCollider.SetInfoForDamageCollider(hitbox, AttackDamage);
            }
        }
    }
    

    private void HandleColliderSize(Collider collider, float factor)
    {
        if (collider is SphereCollider)
        {
            SphereCollider sphereCollider = collider as SphereCollider;
            sphereCollider.radius *= factor;
        }

        else if (collider is BoxCollider)
        {
            BoxCollider boxCollider = collider as BoxCollider;
            boxCollider.size *= factor;
        }
    }

    public void DisableHitBoxes(Dictionary<eBodyPart, DamageCollider> damageColliderDict, int groupIndex)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (groupIndex < 0 || groupIndex >= HitboxGroups.Count) return;

        foreach (Hitbox hitbox in HitboxGroups[groupIndex].hitboxes)
        {
            if (damageColliderDict.ContainsKey(hitbox.bodyPart))
            {
                DamageCollider damageCollider = damageColliderDict[hitbox.bodyPart];
                Collider collider = damageColliderDict[hitbox.bodyPart].GetComponent<Collider>();

                damageCollider.ManualDisable();
                collider.enabled = false;
                HandleColliderSize(collider, 1/hitbox.sizeFactor);
            }
        }
    }

    public virtual void ExecuteAttack(CombatManager combatManager)
    {
        
    }
    public virtual void OnAnimationEvent(int numEvent, CombatManager combatManager)
    {
        
    }
}
