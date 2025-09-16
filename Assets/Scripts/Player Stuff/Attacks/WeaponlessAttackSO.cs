using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "WeaponlessAttack", menuName = "Scriptable Objects/Attacks/Weaponless")]
public class WeaponlessAttackSO : BaseAttackSO{

    public override void Enable(PlayerCombat combat, Animator anim){
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;
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
            hitboxTransform.GetComponent<SphereCollider>().radius = hitbox.Size;
            hitboxTransform.GetComponent<SphereCollider>().enabled = true;
            hitboxTransform.GetComponent<DamageCollider>().SetInfo(hitbox);
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){
            Transform bone = anim.GetBoneTransform(hitbox.AttachBone);
            foreach (Transform child in bone){
                if (child.CompareTag(PlayerCombat.HitboxTag)){
                    child.gameObject.GetComponent<SphereCollider>().enabled = false;
                    child.gameObject.GetComponent<DamageCollider>().DisableManually();
                }
            }
        }
        UpdateCurrHitboxIndex();
    }
}