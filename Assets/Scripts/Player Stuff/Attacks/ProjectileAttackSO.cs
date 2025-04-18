using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "ProjectileAttack", menuName = "Scriptable Objects/Attacks/Projectile")]
public class ProjectileAttackSO : BaseAttackSO
{
    [Header("Projectile")]
    public HumanBodyBones AttachBone;
    public GameObject ProjectilePrefab;
    public float Speed;
    public float Lifetime;

    public override void Enable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        Transform bone = anim.GetBoneTransform(AttachBone);
        if (bone == null)
        {
            Debug.LogWarning($"Bone {AttachBone} not found for {combat.name}");
            return;
        }
        GameObject projectile = Object.Instantiate(ProjectilePrefab, bone.position, bone.rotation);
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        if (netObj)
            netObj.Spawn();
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb)
            rb.linearVelocity = bone.forward * Speed;
        Destroy(projectile, Lifetime);
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        // No cleanup needed (projectile self-destructs)
    }
}