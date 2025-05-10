using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "MeleeWeaponAttack", menuName = "Scriptable Objects/Attacks/MeleeWeapon")]
public class MeleeWeaponAttackSO : BaseAttackSO{
    public override void Enable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){

            Transform boneWithWeapon = anim.GetBoneTransform(hitbox.AttachBone);
            if (boneWithWeapon == null){
                Debug.LogWarning($"Bone {boneWithWeapon} not found for {combat.name}");
                continue;
            }
            foreach (Transform child in boneWithWeapon){
                if (child.CompareTag(PlayerCombat.AttachPointTag)){
                    foreach(Transform grandchild in child){
                        if(grandchild.TryGetComponent(out BaseWeapon weapon)){
                            grandchild.gameObject.GetComponent<Collider>().enabled = true;
                            grandchild.GetComponent<DamageCollider>().SetInfo(hitbox);
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){
            Transform boneWithWeapon = anim.GetBoneTransform(hitbox.AttachBone);
            foreach (Transform child in boneWithWeapon){
                if (child.CompareTag(PlayerCombat.AttachPointTag)){
                    foreach(Transform grandchild in child){
                        if(grandchild.TryGetComponent(out BaseWeapon weapon)){
                            grandchild.gameObject.GetComponent<Collider>().enabled = false;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        UpdateCurrHitboxIndex();
    }
}
