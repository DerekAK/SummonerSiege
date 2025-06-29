using UnityEngine;
using Unity.Netcode;

[CreateAssetMenu(fileName = "MeleeWeaponAttack", menuName = "Scriptable Objects/Attacks/MeleeWeapon")]
public class MeleeWeaponAttackSO : BaseAttackSO{
    public override void Enable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){

            foreach(BaseWeapon weapon in combat.EquippedWeapons){
                if(weapon.AttachedBone == hitbox.AttachBone){
                    weapon.GetComponent<Collider>().enabled = true;
                    weapon.GetComponent<DamageCollider>().SetInfo(hitbox);
                    break;
                }
            }
        }
    }

    public override void Disable(PlayerCombat combat, Animator anim){
        if (!NetworkManager.Singleton.IsServer) return;
        foreach (Hitbox hitbox in MatrixHitboxes[currHitboxIndex].Hitboxes){

            foreach(BaseWeapon weapon in combat.EquippedWeapons){
                if(weapon.AttachedBone == hitbox.AttachBone){
                    weapon.GetComponent<Collider>().enabled = false;
                    break;
                }
            }
        }
        UpdateCurrHitboxIndex();
    }
}
