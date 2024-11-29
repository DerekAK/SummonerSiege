using UnityEngine;
using System.Collections.Generic;
public class EnemyAttackManager : MonoBehaviour
{
    [SerializeField] private Transform pfSpawnWeapon;
    private Transform currentWeaponEquipped;    
    public Transform GetWeaponEquipped(){return currentWeaponEquipped;}
    
    [SerializeField] private AnimationClip unsheathClip1Hand;
    [SerializeField] private AnimationClip sheathClip1Hand;
    [SerializeField] private AnimationClip unsheathClip2Hand;
    [SerializeField] private AnimationClip sheathClip2Hand;
    [SerializeField] private AnimationClip unsheathClipDoubleWield;
    [SerializeField] private AnimationClip sheathClipDoubleWield;
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private List<BaseAttackScript> defaultAttacks = new List<BaseAttackScript>();
    private EnemySpecificInfo _enemyInfo;
    private Transform _swordAttachPointTf;

    private void Awake(){
        _enemyInfo = GetComponent<EnemySpecificInfo>();
    }
    private void Start(){ //just in case the copy overrider hasn't been assigned yet in EnemyAI3.Awake()
        _swordAttachPointTf = _enemyInfo.GetSwordAttachPointTransform();
        if(pfSpawnWeapon){
            Debug.Log("Equip Weapon!");
            currentWeaponEquipped = Instantiate(pfSpawnWeapon, _swordAttachPointTf.position, Quaternion.identity);
            currentWeaponEquipped.SetParent(_swordAttachPointTf, true);
            currentWeaponEquipped.localPosition = new Vector3(0, 0, 0);
            currentWeaponEquipped.localEulerAngles = new Vector3(0, 0, 0);
            float enemyLocalScale = transform.root.localScale.x;
            Debug.Log("HASDUGAS" + enemyLocalScale);
            currentWeaponEquipped.localScale *= enemyLocalScale;
        }
        _anim = GetComponent<Animator>();
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        defaultAttacks.AddRange(GetComponents<BaseAttackScript>()); //initialize default attacks with all the attacks in prefab
    }

    public float HandleSheathAnimation(int weaponType){
        switch(weaponType){
            case 0:
                return 0;
            case 1:
                _animOverrider["Sheath Placeholder"] = sheathClip1Hand;
                return sheathClip1Hand.length;
            case 2:
                _animOverrider["Sheath Placeholder"] = sheathClip2Hand;
                return sheathClip2Hand.length;
            case 3:
                _animOverrider["Sheath Placeholder"] = sheathClipDoubleWield;
                return sheathClipDoubleWield.length;
            default:
                return 0;
        }
    }
    public float HandleUnsheathAnimation(int weaponType){
        switch(weaponType){
            case 0:
                return 0;
            case 1:
                _animOverrider["Unsheath Placeholder"] = unsheathClip1Hand;
                return unsheathClip1Hand.length;
            case 2:
                _animOverrider["Unsheath Placeholder"] = unsheathClip2Hand;
                return unsheathClip2Hand.length;
            case 3:
                _animOverrider["Unsheath Placeholder"] = unsheathClipDoubleWield;
                return unsheathClipDoubleWield.length;
            default:
                return 0;
        }
    }
    
}
//     [SerializeField] private AnimationClip unsheathAnimation

//     private List<BaseAttackScript> activeAttacks = new List<BaseAttackScript>();
//     private Dictionary<string, List<BaseAttackScript>> weaponDefaultAttacksDict = new Dictionary<string, List<BaseAttackScript>>();
//     private List<BaseAttackScript> learnedAttacks = new List<BaseAttackScript>();

//     public void AddAttack(Attack newAttack)
//     {
//         activeAttacks.Add(newAttack);
//         RecalculateWeights();
//     }

//     public void RemoveAttack(Attack attackToRemove)
//     {
//         activeAttacks.Remove(attackToRemove);
//         RecalculateWeights();
//     }

//     public void EquipWeapon(string weaponType)
//     {
//         // Remove existing weapon-based attacks
//         RemoveWeaponBasedAttacks();

//         // Add default attacks for the new weapon
//         if (weaponDefaultAttacks.TryGetValue(weaponType, out var defaultAttacks))
//         {
//             activeAttacks.AddRange(defaultAttacks);
//         }

//         // Retain learned attacks
//         activeAttacks.AddRange(learnedAttacks);
//         RecalculateWeights();
//     }

//     public void LearnAttack(Attack newLearnedAttack)
//     {
//         learnedAttacks.Add(newLearnedAttack);
//         activeAttacks.Add(newLearnedAttack);
//         RecalculateWeights();
//     }

//     private void RemoveWeaponBasedAttacks()
//     {
//         activeAttacks.RemoveAll(attack => !learnedAttacks.Contains(attack));
//     }

//     private void RecalculateWeights()
//     {
//         // Reassign weights to all attacks proportionally
//         float totalWeight = activeAttacks.Sum(a => a.weight);
//         foreach (var attack in activeAttacks)
//         {
//             attack.adjustedWeight = attack.weight / totalWeight;
//         }
//     }
// }

