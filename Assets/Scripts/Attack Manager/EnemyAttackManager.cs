using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
public class EnemyAttackManager : MonoBehaviour
{

    //IMPORTANT TO UNDERSTAND: the offsets for sheathe and unsheathe are called by the animation clips calling animation events, 
    //while the offsets for alert, roaming, chasing, idle, etc. are called by logic in enemyai3 script, since those will always be set 
    //at beginning of animation, so it's better to not have to deal with that for every single non-attack armed animation


    [Tooltip("Not every enemy needs a default weapon or default shield, but will never spawn a shield without a weapon")]
    [SerializeField] private Transform defaultSpawnWeapon; //Not every enemy needs this
    [SerializeField] private Transform defaultShield; //Not every enemy needs this
    private Transform currentShieldEquipped;
    private List<Transform> currentWeaponsEquipped = new List<Transform>(); //this is only in the case of holding two weapons at once, the enemy will have two currentWeapons
    public List<Transform> GetWeaponsEquipped(){return currentWeaponsEquipped;}
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private EnemySpecificInfo _enemyInfo;
    [SerializeField] private List<BaseAttackScript> defaultAttacks = new List<BaseAttackScript>();
    [SerializeField] private List<BaseAttackScript> singleWeaponAttacks = new List<BaseAttackScript>();
    [SerializeField] private List<BaseAttackScript> swordShieldAttacks = new List<BaseAttackScript>();
    [SerializeField] private List<BaseAttackScript> doubleWieldingAttacks = new List<BaseAttackScript>();

    public List<BaseAttackScript> GetCurrentAvailableAttacks(){
        if(currentWeaponsEquipped.Count > 0){
            if(currentWeaponsEquipped.Count == 2){return defaultAttacks.Concat(doubleWieldingAttacks).ToList();}
            else if(currentShieldEquipped){return defaultAttacks.Concat(swordShieldAttacks).ToList();}
            else{return defaultAttacks.Concat(singleWeaponAttacks).ToList();}
        }
        else{return defaultAttacks;}
    }
    //unarmed clips
    [SerializeField] private AnimationClip unarmedIdleClip;
    [SerializeField] private AnimationClip unarmedRoamingClip;
    [SerializeField] private AnimationClip unarmedChasingClip;
    [SerializeField] private AnimationClip unarmedAlertClip;
    [SerializeField] private AnimationClip unarmedStareDownClip;
    [SerializeField] private AnimationClip unarmedDodgeClip;
    [SerializeField] private AnimationClip unarmedRetreatClip;
    [SerializeField] private AnimationClip unarmedRepositionClip;
    [SerializeField] private AnimationClip unarmedReceiveBuffClip;
    [SerializeField] private AnimationClip unarmedTakeHitClip;
    [SerializeField] private AnimationClip unarmedDieClip;
    [SerializeField] private AnimationClip unarmedTurnClip;
    [SerializeField] private AnimationClip unarmedApproachClip;

    //SINGLE WEAPON CLIPS (This assumes same non-attack animations for single and double handed weapons, which makes it simpler)
    [SerializeField] private ArmedAnimationScript singleWeaponIdleClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRoamingClip;
    [SerializeField] private ArmedAnimationScript singleWeaponChasingClip;
    [SerializeField] private ArmedAnimationScript singleWeaponAlertClip;
    [SerializeField] private ArmedAnimationScript singleWeaponEquipClip;
    [SerializeField] private ArmedAnimationScript singleWeaponUnequipClip;
    [SerializeField] private ArmedAnimationScript singleWeaponStareDownClip;
    [SerializeField] private ArmedAnimationScript singleWeaponDodgeClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRetreatClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRepositionClip;
    [SerializeField] private ArmedAnimationScript singleWeaponReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript singleWeaponTakeHitClip;
    [SerializeField] private ArmedAnimationScript singleWeaponDieClip;
    [SerializeField] private ArmedAnimationScript singleWeaponTurnClip;
    [SerializeField] private ArmedAnimationScript singleWeaponApproachClip;
    
    //DOUBLE WIELDING WEAPON CLIPS
    [SerializeField] private ArmedAnimationScript doubleWieldingIdleClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRoamingClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingChasingClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingAlertClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingEquipClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingUnequipClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingStareDownClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingDodgeClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRetreatClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRepositionClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingTakeHitClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingDieClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingTurnClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingApproachClip;

    //SHIELD SWORD CLIPS
    [SerializeField] private ArmedAnimationScript shieldSwordIdleClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRoamingClip;
    [SerializeField] private ArmedAnimationScript shieldSwordChasingClip;
    [SerializeField] private ArmedAnimationScript shieldSwordAlertClip;
    [SerializeField] private ArmedAnimationScript shieldSwordEquipClip;
    [SerializeField] private ArmedAnimationScript shieldSwordUnequipClip;
    [SerializeField] private ArmedAnimationScript shieldSwordStareDownClip;
    [SerializeField] private ArmedAnimationScript shieldSwordDodgeClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRetreatClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRepositionClip;
    [SerializeField] private ArmedAnimationScript shieldSwordReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript shieldSwordTakeHitClip;
    [SerializeField] private ArmedAnimationScript shieldSwordDieClip;
    [SerializeField] private ArmedAnimationScript shieldSwordTurnClip;
    [SerializeField] private ArmedAnimationScript shieldSwordApproachClip;
    [SerializeField] private ArmedAnimationScript shieldSwordBlockClip;

    private void Awake(){
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        _anim = GetComponent<Animator>();
    }
    private void Start(){ //just in case the copy overrider hasn't been assigned yet in EnemyAI3.Awake()
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        if(defaultSpawnWeapon && Random.value < _enemyInfo.GetWeaponSpawnProbability()){
            //enemy is spawned with weapon
            InstantiateWeapon(defaultSpawnWeapon);
            if(defaultShield && defaultSpawnWeapon.GetComponent<BaseWeaponScript>().GetWeaponType() == 1){InstantiateShield(defaultShield);}
        } 
        HandleAnimations(false);
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
    private void InstantiateShield(Transform shieldPf){
        Transform attachPoint = _enemyInfo.GetSingleWeaponAttachPointTransform();
        currentShieldEquipped = Instantiate(shieldPf, attachPoint.position, Quaternion.identity);
        Vector3 positionOffset = new Vector3(0f, 0.3f, -0.1f);
        Vector3 rotationOffset = new Vector3(0f, 0f, 180f);
        SetParentOfTransform(currentShieldEquipped, attachPoint, positionOffset, rotationOffset);
        currentShieldEquipped.localScale *= transform.root.localScale.x;
    }
    public void SetParentOfTransform(Transform childTransform, Transform parentTransform, Vector3 positionOffset, Vector3 rotationOffset){
        childTransform.SetParent(parentTransform, true);
        childTransform.localPosition = positionOffset;
        childTransform.localEulerAngles = rotationOffset;
    }
    
    public void HandleWeaponShieldPosition(EnemyAI4.EnemyState animationType){
        if(currentWeaponsEquipped.Count == 1 && !currentShieldEquipped){    
            Vector3 positionOffset, rotationOffset;
            Transform weapon = currentWeaponsEquipped[0];
            switch(animationType){
                case EnemyAI4.EnemyState.Idle:
                    positionOffset = singleWeaponIdleClip.GetPositionOffset1();
                    rotationOffset = singleWeaponIdleClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Roaming: 
                    positionOffset = singleWeaponRoamingClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRoamingClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Alert: 
                    positionOffset = singleWeaponAlertClip.GetPositionOffset1();
                    rotationOffset = singleWeaponAlertClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Chasing: 
                    positionOffset = singleWeaponChasingClip.GetPositionOffset1();
                    rotationOffset = singleWeaponChasingClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.StareDown: 
                    positionOffset = singleWeaponStareDownClip.GetPositionOffset1();
                    rotationOffset = singleWeaponStareDownClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Dodge: 
                    positionOffset = singleWeaponDodgeClip.GetPositionOffset1();
                    rotationOffset = singleWeaponDodgeClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset = singleWeaponRetreatClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRetreatClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset = singleWeaponRepositionClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRepositionClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.ReceiveBuff: 
                    positionOffset = singleWeaponReceiveBuffClip.GetPositionOffset1();
                    rotationOffset = singleWeaponReceiveBuffClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.TakeHit: 
                    positionOffset = singleWeaponTakeHitClip.GetPositionOffset1();
                    rotationOffset = singleWeaponTakeHitClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Die: 
                    positionOffset = singleWeaponDieClip.GetPositionOffset1();
                    rotationOffset = singleWeaponDieClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Turn: 
                    positionOffset = singleWeaponTurnClip.GetPositionOffset1();
                    rotationOffset = singleWeaponTurnClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset = singleWeaponApproachClip.GetPositionOffset1();
                    rotationOffset = singleWeaponApproachClip.GetRotationOffset1();
                    break;
                default:
                    positionOffset = Vector3.zero;
                    rotationOffset = Vector3.zero;
                    break;
            }
            weapon.localPosition = positionOffset;
            weapon.localEulerAngles = rotationOffset;
        }
        else if (currentShieldEquipped){
            Vector3 positionOffset1, positionOffset2, rotationOffset1, rotationOffset2;
            Transform weapon = currentWeaponsEquipped[0];
            Transform shield = currentShieldEquipped;
            switch(animationType){
                case (int)EnemyAI4.EnemyState.Idle:
                    positionOffset1 = shieldSwordIdleClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordIdleClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordIdleClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordIdleClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Roaming: 
                    positionOffset1 = shieldSwordRoamingClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRoamingClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRoamingClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRoamingClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Alert: 
                    positionOffset1 = shieldSwordAlertClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordAlertClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordAlertClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordAlertClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Chasing: 
                    positionOffset1 = shieldSwordChasingClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordChasingClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordChasingClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordChasingClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.StareDown: 
                    positionOffset1 = shieldSwordStareDownClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordStareDownClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordStareDownClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordStareDownClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Dodge: 
                    positionOffset1 = shieldSwordDodgeClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordDodgeClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordDodgeClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordDodgeClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset1 = shieldSwordRetreatClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRetreatClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRetreatClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRetreatClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset1 = shieldSwordRepositionClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRepositionClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRepositionClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRepositionClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.ReceiveBuff: 
                    positionOffset1 = shieldSwordReceiveBuffClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordReceiveBuffClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordReceiveBuffClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordReceiveBuffClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.TakeHit: 
                    positionOffset1 = shieldSwordTakeHitClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordTakeHitClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordTakeHitClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordTakeHitClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Die: 
                    positionOffset1 = shieldSwordDieClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordDieClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordDieClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordDieClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Turn: 
                    positionOffset1 = shieldSwordTurnClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordTurnClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordTurnClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordTurnClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset1 = shieldSwordApproachClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordApproachClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordApproachClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordApproachClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Block: 
                    positionOffset1 = shieldSwordBlockClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordBlockClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordBlockClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordBlockClip.GetRotationOffset2();
                    break;
                default:
                    positionOffset1 = Vector3.zero;
                    rotationOffset1 = Vector3.zero;
                    positionOffset2 = Vector3.zero;
                    rotationOffset2 = Vector3.zero;
                    break;
            }
            weapon.localPosition = positionOffset1;
            weapon.localEulerAngles = rotationOffset1;
            shield.localPosition = positionOffset2;
            shield.localEulerAngles = rotationOffset2;
        }
        else if(currentWeaponsEquipped.Count == 2){
            Vector3 positionOffset1, positionOffset2, rotationOffset1, rotationOffset2;
            Transform weapon1 = currentWeaponsEquipped[0];
            Transform weapon2 = currentWeaponsEquipped[1];
            switch(animationType){
                case EnemyAI4.EnemyState.Idle:
                    positionOffset1 = doubleWieldingIdleClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingIdleClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingIdleClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingIdleClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Roaming: 
                    positionOffset1 = doubleWieldingRoamingClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRoamingClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRoamingClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRoamingClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Alert: 
                    positionOffset1 = doubleWieldingAlertClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingAlertClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingAlertClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingAlertClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Chasing: 
                    positionOffset1 = doubleWieldingChasingClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingChasingClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingChasingClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingChasingClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.StareDown: 
                    positionOffset1 = doubleWieldingStareDownClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingStareDownClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingStareDownClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingStareDownClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Dodge: 
                    positionOffset1 = doubleWieldingDodgeClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingDodgeClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingDodgeClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingDodgeClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset1 = doubleWieldingRetreatClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRetreatClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRetreatClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRetreatClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset1 = doubleWieldingRepositionClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRepositionClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRepositionClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRepositionClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.ReceiveBuff: 
                    positionOffset1 = doubleWieldingReceiveBuffClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingReceiveBuffClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingReceiveBuffClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingReceiveBuffClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.TakeHit: 
                    positionOffset1 = doubleWieldingTakeHitClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingTakeHitClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingTakeHitClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingTakeHitClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Die: 
                    positionOffset1 = doubleWieldingDieClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingDieClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingDieClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingDieClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Turn: 
                    positionOffset1 = doubleWieldingTurnClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingTurnClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingTurnClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingTurnClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset1 = doubleWieldingApproachClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingApproachClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingApproachClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingApproachClip.GetRotationOffset2();
                    break;
                default:
                    positionOffset1 = Vector3.zero;
                    rotationOffset1 = Vector3.zero;
                    positionOffset2 = Vector3.zero;
                    rotationOffset2 = Vector3.zero;
                    break;
            }
            weapon1.localPosition = positionOffset1;
            weapon1.localEulerAngles = rotationOffset1;
            weapon2.localPosition = positionOffset2;
            weapon2.localEulerAngles = rotationOffset2;
        }
    }

    public void HandleAnimations(bool isWeaponOut){ //if weapon is out, that means that shield will also be out if the enemy has a shield
        if(currentWeaponsEquipped.Count == 1 && !currentShieldEquipped){
            _animOverrider["Unequip Placeholder"] = singleWeaponUnequipClip.GetAnimationClip();
            _animOverrider["Equip Placeholder"] = singleWeaponEquipClip.GetAnimationClip();
        }
        else if(currentShieldEquipped){
            _animOverrider["Unequip Placeholder"] = shieldSwordUnequipClip.GetAnimationClip();
            _animOverrider["Equip Placeholder"] = shieldSwordEquipClip.GetAnimationClip();
        }
        else if(currentWeaponsEquipped.Count == 2){
            _animOverrider["Unequip Placeholder"] = doubleWieldingUnequipClip.GetAnimationClip();
            _animOverrider["Equip Placeholder"] = doubleWieldingEquipClip.GetAnimationClip();
        }

        if(isWeaponOut){
            if(currentWeaponsEquipped.Count == 1 && !currentShieldEquipped){
                _animOverrider["Idle Placeholder"] = singleWeaponIdleClip.GetAnimationClip();
                _animOverrider["Roaming Placeholder"] = singleWeaponRoamingClip.GetAnimationClip();
                _animOverrider["Chasing Placeholder"] = singleWeaponChasingClip.GetAnimationClip();
                _animOverrider["Alert Placeholder"] = singleWeaponAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = singleWeaponStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = singleWeaponDodgeClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = singleWeaponRetreatClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = singleWeaponRepositionClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = singleWeaponReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = singleWeaponTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = singleWeaponDieClip.GetAnimationClip();
                _animOverrider["Turn Placeholder"] = singleWeaponTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = singleWeaponApproachClip.GetAnimationClip();
            }
            else if(currentShieldEquipped){
                _animOverrider["Idle Placeholder"] = shieldSwordIdleClip.GetAnimationClip();
                _animOverrider["Roaming Placeholder"] = shieldSwordRoamingClip.GetAnimationClip();
                _animOverrider["Chasing Placeholder"] = shieldSwordChasingClip.GetAnimationClip();
                _animOverrider["Alert Placeholder"] = shieldSwordAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = shieldSwordStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = shieldSwordDodgeClip.GetAnimationClip();
                _animOverrider["Block Placeholder"] = shieldSwordBlockClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = shieldSwordRetreatClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = shieldSwordRepositionClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = shieldSwordReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = shieldSwordTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = shieldSwordDieClip.GetAnimationClip();
                _animOverrider["Turn Placeholder"] = shieldSwordTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = shieldSwordApproachClip.GetAnimationClip();
            }
            else{ //2 weapons
                _animOverrider["Idle Placeholder"] = doubleWieldingIdleClip.GetAnimationClip();
                _animOverrider["Roaming Placeholder"] = doubleWieldingRoamingClip.GetAnimationClip();
                _animOverrider["Chasing Placeholder"] = doubleWieldingChasingClip.GetAnimationClip();
                _animOverrider["Alert Placeholder"] = doubleWieldingAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = doubleWieldingStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = doubleWieldingDodgeClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = doubleWieldingRetreatClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = doubleWieldingRepositionClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = doubleWieldingReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = doubleWieldingTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = doubleWieldingDieClip.GetAnimationClip();
                _animOverrider["Turn Placeholder"] = doubleWieldingTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = doubleWieldingApproachClip.GetAnimationClip();
            }
        }
        else{
            _animOverrider["Idle Placeholder"] = unarmedIdleClip;
            _animOverrider["Roaming Placeholder"] = unarmedRoamingClip;
            _animOverrider["Chasing Placeholder"] = unarmedChasingClip;
            _animOverrider["Alert Placeholder"] = unarmedAlertClip;
            _animOverrider["StareDown Placeholder"] = unarmedStareDownClip;
            _animOverrider["Dodge Placeholder"] = unarmedDodgeClip;
            _animOverrider["Retreat Placeholder"] = unarmedRetreatClip;
            _animOverrider["Reposition Placeholder"] = unarmedRepositionClip;
            _animOverrider["ReceiveBuff Placeholder"] = unarmedReceiveBuffClip;
            _animOverrider["TakeHit Placeholder"] = unarmedTakeHitClip;
            _animOverrider["Die Placeholder"] = unarmedDieClip;
            _animOverrider["Turn Placeholder"] = unarmedTurnClip;
            _animOverrider["Approach Placeholder"] = unarmedApproachClip;
        }
    }

    //next several functions are all called by the equip/unequip AnimationClips
    private void MoveSingleWeaponToRightHandForEquip(){ //function called by unsheathe animations 
        Vector3 positionOffset = singleWeaponEquipClip.GetPositionOffset1();
        Vector3 rotationOffset = singleWeaponEquipClip.GetRotationOffset1();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset, rotationOffset); 
    }
    private void MoveSingleWeaponToRightHandForUnequip(){ //function called by sheathe animations (this is required to change position of sword in hand when putting it back)
        Vector3 positionOffset = singleWeaponUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset = singleWeaponUnequipClip.GetRotationOffset1();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset, rotationOffset); 
    }
    private void MoveSingleWeaponToAttachPoint(){ //function called by sheathe animations
        //can set to zero zero here because attach point is always correct orientation
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetSingleWeaponAttachPointTransform(), Vector3.zero, Vector3.zero); 
    }
    private void MoveShieldSwordToHandsForEquip(){ //function called by unsheathe animations
        Vector3 positionOffset1 = shieldSwordEquipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = shieldSwordEquipClip.GetRotationOffset1();
        Vector3 positionOffset2 = shieldSwordEquipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = shieldSwordEquipClip.GetRotationOffset2();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentShieldEquipped, _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveShieldSwordToHandsForUnequip(){ //function called by sheathe animations
        Vector3 positionOffset1 = shieldSwordUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = shieldSwordUnequipClip.GetRotationOffset1();
        Vector3 positionOffset2 = shieldSwordUnequipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = shieldSwordUnequipClip.GetRotationOffset2();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentShieldEquipped, _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveShieldSwordToAttachPoint(){
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetSingleWeaponAttachPointTransform(), Vector3.zero, Vector3.zero);
        Vector3 positionOffset = new Vector3(0f, 0.3f, -0.1f);
        Vector3 rotationOffset = new Vector3(0f, 0f, 180f);
        SetParentOfTransform(currentShieldEquipped, _enemyInfo.GetSingleWeaponAttachPointTransform(), positionOffset, rotationOffset);
    }
    private void MoveDoubleWeaponsToHandsForEquip(){ //function called by unsheathe animations
        Vector3 positionOffset1 = doubleWieldingEquipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = doubleWieldingEquipClip.GetRotationOffset1();
        Vector3 positionOffset2 = doubleWieldingEquipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = doubleWieldingEquipClip.GetRotationOffset2();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveDoubleWeaponsToHandsForUnequip(){ //function called by sheathe animations
        Vector3 positionOffset1 = doubleWieldingUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = doubleWieldingUnequipClip.GetRotationOffset1();
        Vector3 positionOffset2 = doubleWieldingUnequipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = doubleWieldingUnequipClip.GetRotationOffset2();
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(), positionOffset1, rotationOffset1);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetLeftHandTransform(), positionOffset2, rotationOffset2);
    }
    private void MoveDoubleWeaponsToAttachPoints(){
        //can set to zero zero here because attach points are always correct orientation
        SetParentOfTransform(currentWeaponsEquipped[0], _enemyInfo.GetDoubleWeaponAttachPointTransform1(), Vector3.zero, Vector3.zero);
        SetParentOfTransform(currentWeaponsEquipped[1], _enemyInfo.GetDoubleWeaponAttachPointTransform2(), Vector3.zero, Vector3.zero);
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

