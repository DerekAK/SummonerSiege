using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ThrowableAttack", menuName = "Scriptable Objects/Attacks/Throwable")]
public class ThrowableAttackSO : BaseAttackSO
{
    [System.Serializable]
    public struct Projectile
    {
        public HumanBodyBones AttachBone;
        public GameObject ProjectilePrefab;
        public float Speed;
        public float Lifetime;
    }

    [Header("Projectiles")]
    public List<Projectile> Projectiles = new List<Projectile>();

    public override void Enable(PlayerCombat combat, Animator anim)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Projectile proj in Projectiles)
        {
            Transform bone = anim.GetBoneTransform(proj.AttachBone);
            if (bone == null)
            {
                Debug.LogWarning($"Bone {proj.AttachBone} not found for {combat.name}");
                continue;
            }
            GameObject projectile = Object.Instantiate(proj.ProjectilePrefab, bone.position, bone.rotation);
            NetworkObject netObj = projectile.GetComponent<NetworkObject>();
            if (netObj)
                netObj.Spawn();
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb)
                rb.linearVelocity = bone.forward * proj.Speed;
            Object.Destroy(projectile, proj.Lifetime);
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim)
    {
        // No cleanup needed (projectiles self-destruct)
    }
}