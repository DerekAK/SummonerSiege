using UnityEngine;
using System.Collections.Generic;
public class EnemyAttackManager : MonoBehaviour
{
    [Tooltip("Not every enemy needs a default weapon")]
    [SerializeField] private Transform defaultSpawnWeapon; //Not every enemy needs this
    private List<Transform> currentWeaponsEquipped = new List<Transform>(); //this is only in the case of holding two weapons at once, the enemy will have two currentWeapons
    public List<Transform> GetWeaponsEquipped(){return currentWeaponsEquipped;}
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private List<BaseAttackScript> defaultAttacks = new List<BaseAttackScript>();
    private EnemySpecificInfo _enemyInfo;

    [SerializeField] private AnimationClip unarmedIdleClip;
    [SerializeField] private AnimationClip unarmedRoamingClip;
    [SerializeField] private AnimationClip unarmedChasingClip;
    [SerializeField] private AnimationClip unarmedAlertClip;


    private void Awake(){
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        defaultAttacks.AddRange(GetComponents<BaseAttackScript>());
    }
    private void Start(){ //just in case the copy overrider hasn't been assigned yet in EnemyAI3.Awake()
        _anim = GetComponent<Animator>();
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        if(defaultSpawnWeapon && Random.value < _enemyInfo.GetWeaponSpawnProbability()){
            //enemy is spawned with weapon
            InstantiateWeapon(defaultSpawnWeapon);
            HandleSheathUnsheatheAnimation();
        } 
    }
    private void InstantiateWeapon(Transform weaponPf){//will always be instantiated in attachpoint, and when adding a new weapon, align it to fit in the attach point
        if(currentWeaponsEquipped.Count > 0){currentWeaponsEquipped.Clear();} //if instantiating a new weapon(s), remove old one(s)
        int weaponType = weaponPf.GetComponent<BaseWeaponScript>().GetWeaponType();
        if(weaponType == 1){ //single weapon, spawn one of them at attachpoints
            Transform attachPoint = _enemyInfo.GetSingleWeaponAttachPointTransform();
            currentWeaponsEquipped.Add(Instantiate(weaponPf, attachPoint.position, Quaternion.identity));
            Transform currWeapon = currentWeaponsEquipped[0];
            SetParentOfTransform(currWeapon, attachPoint, Vector3.zero, Vector3.zero);
            currWeapon.localScale *= transform.root.localScale.x;
        }
        else{ //double-wielding weapon, spawn two of them at attachpoints
            Transform attachPoint1 = _enemyInfo.GetDoubleWeaponAttachPointTransform1();
            Transform attachPoint2 = _enemyInfo.GetDoubleWeaponAttachPointTransform2();
            currentWeaponsEquipped.Add(Instantiate(weaponPf, attachPoint1.position, Quaternion.identity));
            currentWeaponsEquipped.Add(Instantiate(weaponPf, attachPoint2.position, Quaternion.identity));
            Transform currWeapon1 = currentWeaponsEquipped[0];
            Transform currWeapon2 = currentWeaponsEquipped[1];
            SetParentOfTransform(currWeapon1, attachPoint1, Vector3.zero, Vector3.zero);
            SetParentOfTransform(currWeapon2, attachPoint2, Vector3.zero, Vector3.zero);
            currWeapon1.localScale *= transform.root.localScale.x;
            currWeapon2.localScale *= transform.root.localScale.x;
        }
    }
    public void SetParentOfTransform(Transform childTransform, Transform parentTransform, Vector3 positionOffset, Vector3 rotationOffset){
        childTransform.SetParent(parentTransform, true);
        childTransform.localPosition = positionOffset;
        childTransform.localEulerAngles = rotationOffset;
    }

    //next several functions are all called by the Sheathe/Unsheathe AnimationClips
    private void MoveSingleWeaponToRightHandForUnsheath(){ //function called by unsheathe animations 
        Vector3 positionOffset = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetPositionOffsetFirstWeaponUnsheath();
        Vector3 rotationOffset = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetRotationOffsetFirstWeaponUnsheath();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset, rotationOffset); 
    }
    private void MoveSingleWeaponToRightHandForSheath(){ //function called by sheathe animations (this is required to change position of sword in hand when putting it back)
        Vector3 positionOffset = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetPositionOffsetFirstWeaponSheath();
        Vector3 rotationOffset = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetRotationOffsetFirstWeaponSheath();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset, rotationOffset); 
    }
    private void MoveSingleWeaponToAttachPoint(){ //function called by sheathe animations
        //can set to zero zero here because attach point is always correct orientation
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetSingleWeaponAttachPointTransform(), Vector3.zero, Vector3.zero); 
    }
    private void MoveDoubleWeaponsToHandsForUnsheath(){ //function called by unsheathe animations
        Vector3 positionOffset1 = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetPositionOffsetFirstWeaponUnsheath();
        Vector3 rotationOffset1 = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetRotationOffsetFirstWeaponUnsheath();
        Vector3 positionOffset2 = currentWeaponsEquipped[1].GetComponent<BaseWeaponScript>().GetPositionOffsetSecondWeaponUnsheath();
        Vector3 rotationOffset2 = currentWeaponsEquipped[1].GetComponent<BaseWeaponScript>().GetRotationOffsetSecondWeaponUnsheath();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveDoubleWeaponsToHandsForSheath(){ //function called by sheathe animations
        Vector3 positionOffset1 = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetPositionOffsetFirstWeaponSheath();
        Vector3 rotationOffset1 = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetRotationOffsetFirstWeaponSheath();
        Vector3 positionOffset2 = currentWeaponsEquipped[1].GetComponent<BaseWeaponScript>().GetPositionOffsetSecondWeaponSheath();
        Vector3 rotationOffset2 = currentWeaponsEquipped[1].GetComponent<BaseWeaponScript>().GetRotationOffsetSecondWeaponSheath();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveDoubleWeaponsToAttachPoints(){
        //can set to zero zero here because attach point is always correct orientation
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetDoubleWeaponAttachPointTransform1(), Vector3.zero, Vector3.zero);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetDoubleWeaponAttachPointTransform2(), Vector3.zero, Vector3.zero);
    }
    //called right before a weapon animation is played

    private void HandleSheathUnsheatheAnimation(){
        //need to do this here because runtimeAnimatorController not instantiated immediately.
        //Debug.Log("Handle SheatheUnsheateAnimation!");
        BaseWeaponScript currWeaponScript = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>();
        _animOverrider["Sheath Placeholder"] = currWeaponScript.GetSheatheClip();
        _animOverrider["Unsheath Placeholder"] = currWeaponScript.GetUnsheatheClip();
    }

    public void HandleAnimations(bool isWeaponOut){
        if(isWeaponOut){
            _animOverrider["Idle Placeholder"] = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetIdleWeaponClip();
            _animOverrider["Roaming Placeholder"] = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetRoamingWeaponClip();
            _animOverrider["Chasing Placeholder"] = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetChasingWeaponClip();
            _animOverrider["Alert Placeholder"] = currentWeaponsEquipped[0].GetComponent<BaseWeaponScript>().GetAlertWeaponClip();
            
        }
        else{
            _animOverrider["Idle Placeholder"] = unarmedIdleClip;
            _animOverrider["Roaming Placeholder"] = unarmedRoamingClip;
            _animOverrider["Chasing Placeholder"] = unarmedChasingClip;
            _animOverrider["Alert Placeholder"] = unarmedAlertClip;
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

