using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "MeleeWeaponAttack", menuName = "Scriptable Objects/Attacks/MeleeWeapon")]
public class MeleeWeaponAttackSO : BaseAttackSO{

    [Header("2d list of bones, each row is bone associated with an attachpoint")]
    public HumanBodyBones[][] bones;

    public override void Enable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (HumanBodyBones bone in bones[combat.CurrHitboxIndex]){
            Transform boneWithWeapon = anim.GetBoneTransform(bone);
            if (boneWithWeapon == null){
                Debug.LogWarning($"Bone {bone} not found for {combat.name}");
                continue;
            }
            Transform weaponToEnable = null;
            foreach (Transform child in boneWithWeapon){
                if (child.CompareTag(PlayerCombat.WeaponTag)){
                    weaponToEnable = child;
                    break;
                }
            }
            if (!weaponToEnable){
                Debug.LogWarning($"No weapon found for {combat.name} on bone {bone}");
                continue;
            }
            Collider collider = weaponToEnable.GetComponent<Collider>();
            if (collider){collider.enabled = true;}
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (HumanBodyBones bone in bones[combat.CurrHitboxIndex]){
            Transform boneWithWeapon = anim.GetBoneTransform(bone);
            Transform weaponToDisable = null;
            foreach (Transform child in boneWithWeapon){
                if (child.CompareTag(PlayerCombat.WeaponTag)){
                    weaponToDisable = child;
                    break;
                }
            }
            Collider collider = weaponToDisable.GetComponent<Collider>();
            if (collider){collider.enabled = false;}
        }
        if(MatrixHitboxes.Count == 1){return;}
        combat.CurrHitboxIndex = (combat.CurrHitboxIndex + 1) % (MatrixHitboxes.Count-1);
    }
}
