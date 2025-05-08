using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "WeaponlessAttack", menuName = "Scriptable Objects/Attacks/Weaponless")]
public class WeaponlessAttackSO : BaseAttackSO{

    public override void Enable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){
            // Debug.Log($"Index: {currHitboxIndex} and bone: {hitbox.AttachBone}");

            Transform bone = anim.GetBoneTransform(hitbox.AttachBone);
            if (bone == null){
                Debug.LogWarning($"Bone {hitbox.AttachBone} not found for {combat.name}");
                continue;
            }
            Transform hitboxTransform = null;
            foreach (Transform child in bone){
                if (child.CompareTag(PlayerCombat.HitboxTag)){
                    hitboxTransform = child;
                    break;
                }
            }
            if (!hitboxTransform){
                Debug.LogWarning($"No hitbox found for {combat.name}, bone: {hitbox.AttachBone}");
                continue;
            }
            SphereCollider collider = hitboxTransform.GetComponent<SphereCollider>();
            collider.enabled = true;
            collider.radius = hitbox.Size;
            DamageCollider damageColliderScript = hitboxTransform.GetComponent<DamageCollider>();
            damageColliderScript.SetInfo(hitbox);
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){
            Transform bone = anim.GetBoneTransform(hitbox.AttachBone);
            foreach (Transform child in bone){
                if (child.CompareTag(PlayerCombat.HitboxTag)){
                    child.GetComponent<SphereCollider>().enabled = false;
                }
            }
        }
        UpdateCurrHitboxIndex();
    }
}