using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.AI;
using Unity.VisualScripting.FullSerializer;
public class EnemyAttackManager : MonoBehaviour
{

    //IMPORTANT TO UNDERSTAND: the offsets for sheathe and unsheathe are called by the animation clips calling animation events, 
    //while the offsets for alert, roaming, chasing, idle, etc. are called by logic in enemyai3 script, since those will always be set 
    //at beginning of animation, so it's better to not have to deal with that for every single non-attack armed animation
    [SerializeField] AnimationCurve jumpCurve;
    [Tooltip("Not every enemy needs a default weapon or default shield, but will never spawn a shield without a weapon")]
    [SerializeField] private Transform defaultSpawnWeapon; //Not every enemy needs this
    [SerializeField] private Transform defaultShield; //Not every enemy needs this
    private Transform currentShieldEquipped;
    private List<Transform> currentWeaponsEquipped = new List<Transform>(); //this is only in the case of holding two weapons at once, the enemy will have two currentWeapons
    public List<Transform> GetWeaponsEquipped(){return currentWeaponsEquipped;}
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private EnemySpecificInfo _enemyInfo;

    [System.Serializable]
    public class AttackData{
        public BaseAttackScript attack;
        public float weight;            
    }
    [SerializeField] private List<AttackData> defaultAttacks = new List<AttackData>();
    [SerializeField] private List<AttackData> singleWeaponAttacks = new List<AttackData>();
    [SerializeField] private List<AttackData> swordShieldAttacks = new List<AttackData>();
    [SerializeField] private List<AttackData> doubleWieldingAttacks = new List<AttackData>();

    private Coroutine smoothMoveCoroutine1 = null;
    private Coroutine smoothMoveCoroutine2 = null;

    private EnemyAI4 _enemyScript;
    public List<AttackData> GetCurrentAvailableAttacks(){
        if(currentWeaponsEquipped.Count > 0){
            if(currentWeaponsEquipped.Count == 2){return defaultAttacks.Concat(doubleWieldingAttacks).ToList();}
            else if(currentShieldEquipped){return defaultAttacks.Concat(swordShieldAttacks).ToList();}
            else{return defaultAttacks.Concat(singleWeaponAttacks).ToList();}
        }
        else{return defaultAttacks;}
    }
    public List<AttackData> GetAllAttacks(){
        return defaultAttacks.Concat(singleWeaponAttacks).Concat(doubleWieldingAttacks).Concat(swordShieldAttacks).ToList();
    }

    //unarmed clips
    [SerializeField] private AnimationClip unarmedIdleClip;
    [SerializeField] private AnimationClip unarmedRoamingClip;
    [SerializeField] private AnimationClip unarmedAlertClip;
    [SerializeField] private AnimationClip unarmedStareDownClip;
    [SerializeField] private AnimationClip unarmedDodgeClip;
    [SerializeField] private AnimationClip unarmedJumpClip;
    [SerializeField] private AnimationClip unarmedRepositionClip;
    [SerializeField] private AnimationClip unarmedRetreatClip;
    [SerializeField] private AnimationClip unarmedReceiveBuffClip;
    [SerializeField] private AnimationClip unarmedTakeHitClip;
    [SerializeField] private AnimationClip unarmedDieClip;
    [SerializeField] private AnimationClip unarmedRightTurnClip;
    [SerializeField] private AnimationClip unarmedLeftTurnClip;
    [SerializeField] private AnimationClip unarmedApproachClip;

    //SINGLE WEAPON CLIPS (This assumes same non-attack animations for single and double handed weapons, which makes it simpler)
    [SerializeField] private ArmedAnimationScript singleWeaponIdleClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRoamingClip;
    [SerializeField] private ArmedAnimationScript singleWeaponAlertClip;
    [SerializeField] private ArmedAnimationScript singleWeaponEquipClip;
    [SerializeField] private ArmedAnimationScript singleWeaponUnequipClip;
    [SerializeField] private ArmedAnimationScript singleWeaponStareDownClip;
    [SerializeField] private ArmedAnimationScript singleWeaponDodgeClip;
    [SerializeField] private ArmedAnimationScript singleWeaponJumpClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRepositionClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRetreatClip;
    [SerializeField] private ArmedAnimationScript singleWeaponReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript singleWeaponTakeHitClip;
    [SerializeField] private ArmedAnimationScript singleWeaponDieClip;
    [SerializeField] private ArmedAnimationScript singleWeaponRightTurnClip;
    [SerializeField] private ArmedAnimationScript singleWeaponLeftTurnClip;
    [SerializeField] private ArmedAnimationScript singleWeaponApproachClip;
    
    //DOUBLE WIELDING WEAPON CLIPS
    [SerializeField] private ArmedAnimationScript doubleWieldingIdleClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRoamingClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingAlertClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingEquipClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingUnequipClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingStareDownClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingDodgeClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingJumpClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRepositionClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRetreatClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingTakeHitClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingDieClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingRightTurnClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingLeftTurnClip;
    [SerializeField] private ArmedAnimationScript doubleWieldingApproachClip;

    //SHIELD SWORD CLIPS
    [SerializeField] private ArmedAnimationScript shieldSwordIdleClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRoamingClip;
    [SerializeField] private ArmedAnimationScript shieldSwordAlertClip;
    [SerializeField] private ArmedAnimationScript shieldSwordStareDownClip;
    [SerializeField] private ArmedAnimationScript shieldSwordApproachClip;
    [SerializeField] private ArmedAnimationScript shieldSwordEquipClip;
    [SerializeField] private ArmedAnimationScript shieldSwordUnequipClip;
    [SerializeField] private ArmedAnimationScript shieldSwordDodgeClip;
    [SerializeField] private ArmedAnimationScript shieldSwordBlockClip;
    [SerializeField] private ArmedAnimationScript shieldSwordJumpClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRepositionClip;
    [SerializeField] private ArmedAnimationScript shieldSwordReceiveBuffClip;
    [SerializeField] private ArmedAnimationScript shieldSwordTakeHitClip;
    [SerializeField] private ArmedAnimationScript shieldSwordDieClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRightTurnClip;
    [SerializeField] private ArmedAnimationScript shieldSwordLeftTurnClip;
    [SerializeField] private ArmedAnimationScript shieldSwordRetreatClip;

    public bool HasJump(bool isWeaponOut){
        if(isWeaponOut){
            if(currentWeaponsEquipped.Count == 1 && !currentShieldEquipped){return singleWeaponJumpClip != null;}
            else if(currentWeaponsEquipped.Count == 1){return shieldSwordJumpClip != null;}
            else{return doubleWieldingJumpClip;}
        }
        else{return unarmedJumpClip != null;}
    }
    private void Awake(){
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        _anim = GetComponent<Animator>();
        _enemyScript = GetComponent<EnemyAI4>();
    }
    private void Start(){ //just in case the copy overrider hasn't been assigned yet in EnemyAI3.Awake()
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        if(defaultSpawnWeapon && Random.value < _enemyInfo.GetWeaponSpawnProbability()){
            InstantiateWeapon(defaultSpawnWeapon);
        }
        if(defaultShield && Random.value < _enemyInfo.GetShieldSpawnProbability() && defaultSpawnWeapon.GetComponent<BaseWeaponScript>().GetWeaponType() == 1){
            InstantiateShield(defaultShield);
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
            UtilityFunctions.SetParentOfTransform(currWeapon, attachPoint, Vector3.zero, Vector3.zero);
        }
        else{ //double-wielding weapon, spawn two of them at attachpoints
            Transform attachPoint1 = _enemyInfo.GetDoubleWeaponAttachPointTransform1();
            Transform attachPoint2 = _enemyInfo.GetDoubleWeaponAttachPointTransform2();
            currentWeaponsEquipped.Add(Instantiate(weaponPf, attachPoint1.position, Quaternion.identity));
            currentWeaponsEquipped.Add(Instantiate(weaponPf, attachPoint2.position, Quaternion.identity));
            Transform currWeapon1 = currentWeaponsEquipped[0];
            Transform currWeapon2 = currentWeaponsEquipped[1];
            UtilityFunctions.SetParentOfTransform(currWeapon1, attachPoint1, Vector3.zero, Vector3.zero);
            UtilityFunctions.SetParentOfTransform(currWeapon2, attachPoint2, Vector3.zero, Vector3.zero);
        }
    }
    private void InstantiateShield(Transform shieldPf){
        Transform attachPoint = _enemyInfo.GetSingleWeaponAttachPointTransform();
        currentShieldEquipped = Instantiate(shieldPf, attachPoint.position, Quaternion.identity);
        UtilityFunctions.SetParentOfTransform(currentShieldEquipped, attachPoint, new Vector3(0f, 0.3f, -0.1f), new Vector3(0f, 0f, 180f));
    }
    
    public void HandleWeaponShieldPositionForAttack(BaseAttackScript attackChosen){
        if(currentWeaponsEquipped.Count == 1 && !currentShieldEquipped){
            Transform weapon = currentWeaponsEquipped[0];
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon, attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset(), 1));
        }
        else if(currentShieldEquipped){
            Transform weapon = currentWeaponsEquipped[0];
            Transform shield = currentShieldEquipped;
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine2);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon, attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset(), 1));
            smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(shield, attackChosen.GetSecondWeaponPositionOffset(), attackChosen.GetSecondWeaponRotationOffset(), 1));
        }
        else if(currentWeaponsEquipped.Count == 2){
            Transform weapon1 = currentWeaponsEquipped[0];
            Transform weapon2 = currentWeaponsEquipped[1];
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine2);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon1, attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset(), 1));
            smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon2, attackChosen.GetSecondWeaponPositionOffset(), attackChosen.GetSecondWeaponRotationOffset(), 1));
        }
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
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset = singleWeaponApproachClip.GetPositionOffset1();
                    rotationOffset = singleWeaponApproachClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.StareDown: 
                    positionOffset = singleWeaponStareDownClip.GetPositionOffset1();
                    rotationOffset = singleWeaponStareDownClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Dodge: 
                    positionOffset = singleWeaponDodgeClip.GetPositionOffset1();
                    rotationOffset = singleWeaponDodgeClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Jump: 
                    positionOffset = singleWeaponJumpClip.GetPositionOffset1();
                    rotationOffset = singleWeaponJumpClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset = singleWeaponRepositionClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRepositionClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset = singleWeaponRetreatClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRetreatClip.GetRotationOffset1();
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
                case EnemyAI4.EnemyState.RightTurn: 
                    positionOffset = singleWeaponRightTurnClip.GetPositionOffset1();
                    rotationOffset = singleWeaponRightTurnClip.GetRotationOffset1();
                    break;
                case EnemyAI4.EnemyState.LeftTurn: 
                    positionOffset = singleWeaponLeftTurnClip.GetPositionOffset1();
                    rotationOffset = singleWeaponLeftTurnClip.GetRotationOffset1();
                    break;
                default:
                    positionOffset = Vector3.zero;
                    rotationOffset = Vector3.zero;
                    break;
            }
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon, positionOffset, rotationOffset, 1));
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
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset1 = shieldSwordApproachClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordApproachClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordApproachClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordApproachClip.GetRotationOffset2();
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
                case EnemyAI4.EnemyState.Jump: 
                    positionOffset1 = shieldSwordJumpClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordJumpClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordJumpClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordJumpClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset1 = shieldSwordRepositionClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRepositionClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRepositionClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRepositionClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset1 = shieldSwordRetreatClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRetreatClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRetreatClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRetreatClip.GetRotationOffset2();
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
                case EnemyAI4.EnemyState.LeftTurn: 
                    positionOffset1 = shieldSwordLeftTurnClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordLeftTurnClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordLeftTurnClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordLeftTurnClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.RightTurn: 
                    positionOffset1 = shieldSwordRightTurnClip.GetPositionOffset1();
                    rotationOffset1 = shieldSwordRightTurnClip.GetRotationOffset1();
                    positionOffset2 = shieldSwordRightTurnClip.GetPositionOffset2();
                    rotationOffset2 = shieldSwordRightTurnClip.GetRotationOffset2();
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
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine2);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon, positionOffset1, rotationOffset1, 1));
            smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(shield, positionOffset2, rotationOffset2, 1));
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
                case EnemyAI4.EnemyState.Approach: 
                    positionOffset1 = doubleWieldingApproachClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingApproachClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingApproachClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingApproachClip.GetRotationOffset2();
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
                case EnemyAI4.EnemyState.Jump: 
                    positionOffset1 = doubleWieldingJumpClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingJumpClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingJumpClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingJumpClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Reposition: 
                    positionOffset1 = doubleWieldingRepositionClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRepositionClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRepositionClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRepositionClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.Retreat: 
                    positionOffset1 = doubleWieldingRetreatClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRetreatClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRetreatClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRetreatClip.GetRotationOffset2();
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
                case EnemyAI4.EnemyState.LeftTurn: 
                    positionOffset1 = doubleWieldingLeftTurnClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingLeftTurnClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingLeftTurnClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingLeftTurnClip.GetRotationOffset2();
                    break;
                case EnemyAI4.EnemyState.RightTurn: 
                    positionOffset1 = doubleWieldingRightTurnClip.GetPositionOffset1();
                    rotationOffset1 = doubleWieldingRightTurnClip.GetRotationOffset1();
                    positionOffset2 = doubleWieldingRightTurnClip.GetPositionOffset2();
                    rotationOffset2 = doubleWieldingRightTurnClip.GetRotationOffset2();
                    break;
                default:
                    positionOffset1 = Vector3.zero;
                    rotationOffset1 = Vector3.zero;
                    positionOffset2 = Vector3.zero;
                    rotationOffset2 = Vector3.zero;
                    break;
            }
            if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
            if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine2);} 
            smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon1, positionOffset1, rotationOffset1, 1));
            smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(weapon2, positionOffset2, rotationOffset2, 1));
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
                _animOverrider["Alert Placeholder"] = singleWeaponAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = singleWeaponStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = singleWeaponDodgeClip.GetAnimationClip();
                _animOverrider["Jump Placeholder"] = singleWeaponJumpClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = singleWeaponRepositionClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = singleWeaponRetreatClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = singleWeaponReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = singleWeaponTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = singleWeaponDieClip.GetAnimationClip();
                _animOverrider["RightTurn Placeholder"] = singleWeaponRightTurnClip.GetAnimationClip();
                _animOverrider["LeftTurn Placeholder"] = singleWeaponLeftTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = singleWeaponApproachClip.GetAnimationClip();
            }
            else if(currentShieldEquipped){
                _animOverrider["Idle Placeholder"] = shieldSwordIdleClip.GetAnimationClip();
                _animOverrider["Roaming Placeholder"] = shieldSwordRoamingClip.GetAnimationClip();
                _animOverrider["Alert Placeholder"] = shieldSwordAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = shieldSwordStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = shieldSwordDodgeClip.GetAnimationClip();
                _animOverrider["Block Placeholder"] = shieldSwordBlockClip.GetAnimationClip();
                _animOverrider["Jump Placeholder"] = shieldSwordJumpClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = shieldSwordRepositionClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = shieldSwordRetreatClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = shieldSwordReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = shieldSwordTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = shieldSwordDieClip.GetAnimationClip();
                _animOverrider["RightTurn Placeholder"] = shieldSwordRightTurnClip.GetAnimationClip();
                _animOverrider["LeftTurn Placeholder"] = shieldSwordLeftTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = shieldSwordApproachClip.GetAnimationClip();
            }
            else{ //2 weapons
                _animOverrider["Idle Placeholder"] = doubleWieldingIdleClip.GetAnimationClip();
                _animOverrider["Roaming Placeholder"] = doubleWieldingRoamingClip.GetAnimationClip();
                _animOverrider["Alert Placeholder"] = doubleWieldingAlertClip.GetAnimationClip();
                _animOverrider["StareDown Placeholder"] = doubleWieldingStareDownClip.GetAnimationClip();
                _animOverrider["Dodge Placeholder"] = doubleWieldingDodgeClip.GetAnimationClip();
                _animOverrider["Jump Placeholder"] = doubleWieldingJumpClip.GetAnimationClip();
                _animOverrider["Reposition Placeholder"] = doubleWieldingRepositionClip.GetAnimationClip();
                _animOverrider["Retreat Placeholder"] = doubleWieldingRetreatClip.GetAnimationClip();
                _animOverrider["ReceiveBuff Placeholder"] = doubleWieldingReceiveBuffClip.GetAnimationClip();
                _animOverrider["TakeHit Placeholder"] = doubleWieldingTakeHitClip.GetAnimationClip();
                _animOverrider["Die Placeholder"] = doubleWieldingDieClip.GetAnimationClip();
                _animOverrider["RightTurn Placeholder"] = doubleWieldingRightTurnClip.GetAnimationClip();
                _animOverrider["LeftTurn Placeholder"] = doubleWieldingLeftTurnClip.GetAnimationClip();
                _animOverrider["Approach Placeholder"] = doubleWieldingApproachClip.GetAnimationClip();
            }
        }
        else{
            _animOverrider["Idle Placeholder"] = unarmedIdleClip;
            _animOverrider["Roaming Placeholder"] = unarmedRoamingClip;
            _animOverrider["Alert Placeholder"] = unarmedAlertClip;
            _animOverrider["StareDown Placeholder"] = unarmedStareDownClip;
            _animOverrider["Dodge Placeholder"] = unarmedDodgeClip;
            _animOverrider["Jump Placeholder"] = unarmedJumpClip;
            _animOverrider["Reposition Placeholder"] = unarmedRepositionClip;
            _animOverrider["Retreat Placeholder"] = unarmedRetreatClip;
            _animOverrider["ReceiveBuff Placeholder"] = unarmedReceiveBuffClip;
            _animOverrider["TakeHit Placeholder"] = unarmedTakeHitClip;
            _animOverrider["Die Placeholder"] = unarmedDieClip;
            _animOverrider["RightTurn Placeholder"] = unarmedRightTurnClip;
            _animOverrider["LeftTurn Placeholder"] = unarmedLeftTurnClip;
            _animOverrider["Approach Placeholder"] = unarmedApproachClip;
        }
    }

    //next several functions are all called by the equip/unequip AnimationClips
    private void MoveSingleWeaponToRightHandForEquip(){ //function called by equip animations 
        Vector3 positionOffset = singleWeaponEquipClip.GetPositionOffset1();
        Vector3 rotationOffset = singleWeaponEquipClip.GetRotationOffset1();
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(),
            positionOffset, rotationOffset, 1));
    }
    private void MoveSingleWeaponToRightHandForUnequip(){ //function called by equip animations (this is required to change position of sword in hand when putting it back)
        Vector3 positionOffset = singleWeaponUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset = singleWeaponUnequipClip.GetRotationOffset1();
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(currentWeaponsEquipped[0],
            positionOffset, rotationOffset, 1));
    }
    private void MoveSingleWeaponToAttachPoint(){ //function called by unequip animations
        //can set to zero zero here because attach point is always correct orientation
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetSingleWeaponAttachPointTransform(),
            Vector3.zero, Vector3.zero, 1));
        }
    private void MoveShieldSwordToHandsForEquip(){ //function called by unsheathe animations
        Vector3 positionOffset1 = shieldSwordEquipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = shieldSwordEquipClip.GetRotationOffset1();
        Vector3 positionOffset2 = shieldSwordEquipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = shieldSwordEquipClip.GetRotationOffset2();
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(),
            positionOffset1, rotationOffset1, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentShieldEquipped, _enemyInfo.GetLeftHandTransform(),
            positionOffset2, rotationOffset2, 1));
    }
    private void MoveShieldSwordToHandsForUnequip(){ //function called by sheathe animations
        Vector3 positionOffset1 = shieldSwordUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = shieldSwordUnequipClip.GetRotationOffset1();
        Vector3 positionOffset2 = shieldSwordUnequipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = shieldSwordUnequipClip.GetRotationOffset2();
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(currentWeaponsEquipped[0],
            positionOffset1, rotationOffset1, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(currentShieldEquipped,
            positionOffset2, rotationOffset2, 1));
    }
    private void MoveShieldSwordToAttachPoint(){
        Vector3 positionOffset = new Vector3(0f, 0.3f, -0.1f);
        Vector3 rotationOffset = new Vector3(0f, 0f, 180f);
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetSingleWeaponAttachPointTransform(),
            Vector3.zero, Vector3.zero, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentShieldEquipped, _enemyInfo.GetSingleWeaponAttachPointTransform(),
            positionOffset, rotationOffset, 1));
    }
    private void MoveDoubleWeaponsToHandsForEquip(){ //function called by unsheathe animations
        Vector3 positionOffset1 = doubleWieldingEquipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = doubleWieldingEquipClip.GetRotationOffset1();
        Vector3 positionOffset2 = doubleWieldingEquipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = doubleWieldingEquipClip.GetRotationOffset2();

        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetRightHandTransform(),
            positionOffset1, rotationOffset1, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[1], _enemyInfo.GetLeftHandTransform(),
            positionOffset2, rotationOffset2, 1));
    }
    private void MoveDoubleWeaponsToHandsForUnequip(){ //function called by sheathe animations
        Vector3 positionOffset1 = doubleWieldingUnequipClip.GetPositionOffset1();
        Vector3 rotationOffset1 = doubleWieldingUnequipClip.GetRotationOffset1();
        Vector3 positionOffset2 = doubleWieldingUnequipClip.GetPositionOffset2();
        Vector3 rotationOffset2 = doubleWieldingUnequipClip.GetRotationOffset2();
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(currentWeaponsEquipped[0],
            positionOffset1, rotationOffset1, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransforms(currentWeaponsEquipped[1],
            positionOffset2, rotationOffset2, 1));
    }
    private void MoveDoubleWeaponsToAttachPoints(){
        //can set to zero zero here because attach points are always correct orientation
        if(smoothMoveCoroutine1 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine1 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[0], _enemyInfo.GetDoubleWeaponAttachPointTransform1(),
            Vector3.zero, Vector3.zero, 1));
        if(smoothMoveCoroutine2 != null){StopCoroutine(smoothMoveCoroutine1);} 
        smoothMoveCoroutine2 = StartCoroutine(UtilityFunctions.SmoothMoveBetweenTransformsChangeParent(currentWeaponsEquipped[1], _enemyInfo.GetDoubleWeaponAttachPointTransform2(),
            Vector3.zero, Vector3.zero, 1));
    }
    private void StartJump(float timeToJump){  
        Debug.Log("START JUMP!");
        Vector3 landDestination = _enemyScript.GetJumpPosition();
        Debug.Log("Jump Destination: " + landDestination);
        Vector3 origin = transform.position;
        float baseJumpDistance = 100f;
        float jumpDistance = Vector3.Distance(origin, landDestination);
        float speedChange = jumpDistance/baseJumpDistance;
        _anim.speed = speedChange;
        timeToJump *= speedChange;
        StartCoroutine(UtilityFunctions.MoveRigidBodyWithAnimationCurve(gameObject.GetComponent<Rigidbody>(), landDestination, jumpCurve, timeToJump));
    }
    private void EndJumpAnimation(){
        Debug.Log("END JUMP!");
        _enemyScript.SetHasFinishedJump(true);
        _anim.speed = 1;
    }
}