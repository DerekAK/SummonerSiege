using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.Netcode;

public class EnemyAI4 : NetworkBehaviour{

    public class AttackEvent : EventArgs{
        public GameObject EnemyInstance { get; }
        public BaseAttackScript AttackChosen { get; }
        public Transform TargetTransform { get; }
        public Transform AttackCenterForward { get; }
        public LayerMask TargetL { get; }
        public AttackEvent(GameObject enemyInstance, BaseAttackScript attackChosen, Transform targetTransform, Transform attackCenterForward, LayerMask targetL){
            EnemyInstance = enemyInstance;
            AttackChosen = attackChosen;
            TargetTransform = targetTransform;
            AttackCenterForward = attackCenterForward;
            TargetL = targetL;
        }
    }
    public event EventHandler<AttackEvent> AnimationAttackEvent;

    // for enemies, they will each have their own movement 
    // [SerializeField] private attackAnimationClipListSO;
    private BaseAttackScript attackChosen;
    private bool hasMelee, hasMedium, hasLong;
    private int countMeleeInRow, countMediumInRow, countLongInRow = 0;
    private List<EnemyAttackManager.AttackData> meleeAttacks, mediumAttacks, longAttacks;
    private NavMeshAgent _agent;
    private Animator _anim;
    private Rigidbody _rb;
    private AnimatorOverrideController _templateOverrider;
    private AnimatorOverrideController _copyOverrider;
    private SphereCollider _aggroCollider;
    private CapsuleCollider _capsuleCollider;
    private List<Transform> targetsInRangeOfEnemy = new List<Transform>();
    private List<EnemyAttackManager.AttackData> attackPool;
    private Transform currentTarget;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine alertCoroutine;
    private Coroutine speedCoroutine;
    private float enemyHeight;
    [SerializeField] private LayerMask obstacleL;
    [SerializeField] private LayerMask targetL;
    private string playerTag = "Player";
    [SerializeField] private string targetTag;
    [SerializeField] private Transform attackCenter;
    [SerializeField] private Transform eyes;
    [SerializeField] private Transform feet;
    private EnemySpecificInfo _enemyInfo;
    private EnemyAttackManager _enemyAttackManager;
    private Vector3 currentJumpPosition;
    private BaseAttackScript attackChosenInstance;
    private bool isWeaponOut;
    public enum EnemyState{Idle=0, Roaming=1, Alert=2, StareDown=3, Approach=4, AttackA=5, AttackB=6, 
    Equip=7, Unequip=8, Dodge=9, Block=10, Jump=11, Reposition=12, ReceiveBuff=13, TakeHit=14, Die=15, RightTurn=16, Retreat=17, LeftTurn=18}
    private EnemyState currentEnemyState;
    private EnemyState nextAttackState;
    private bool hasFinishedJump = false;
    public void SetHasFinishedJump(bool finished){hasFinishedJump = finished;}

    public NetworkVariable<EnemyState> enemyAnimationState = new NetworkVariable<EnemyState>();

    private void Awake(){
        SetUpAnimator(); //sets up a unique copy of the animatorOverrider for each enemy instance
        _enemyAttackManager = GetComponent<EnemyAttackManager>();
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false;
        _rb = GetComponent<Rigidbody>();
        _aggroCollider = GetComponent<SphereCollider>();
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
    }

    //every enemy instance will get it's own overrider copy
    private void SetUpAnimator(){
        _anim = GetComponent<Animator>();
        _templateOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _copyOverrider = new AnimatorOverrideController(_templateOverrider);
        _anim.runtimeAnimatorController = _copyOverrider;
    }

    public override void OnNetworkSpawn(){
        enemyAnimationState.OnValueChanged += ChangeAnimationState;
        if(!IsServer){return;}
        _agent.enabled = true;
        nextAttackState = EnemyState.AttackA;
        _aggroCollider.radius = (_enemyInfo.GetAggroDistance()-1)/transform.localScale.y;
        _agent.stoppingDistance = 1.5f * transform.root.localScale.x; //THIS IS REALLY IMPORTANT TO GET RIGHT
        _agent.angularSpeed = 1000f;
        _agent.acceleration = 200f;
        _agent.radius = 0.1f;
        isWeaponOut = false;
        attackPool = _enemyAttackManager.GetCurrentAvailableAttacks();
        startPosition = feet.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }

    // will be called on all connected clients to change the animation of the enemy 
    private void ChangeAnimationState(EnemyState prevState, EnemyState newState){
        _anim.SetInteger("EnemyState", (int)newState);
    }

    private void Idle(){
        Debug.Log("CLIENT: " + OwnerClientId + " IDLE FUNCTION");
        _agent.ResetPath();
        currentEnemyState = EnemyState.Idle;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        enemyAnimationState.Value = currentEnemyState;
        StartCoroutine(IdleCoroutine());
    }
    IEnumerator IdleCoroutine(){
        float idleTime = UnityEngine.Random.Range(_enemyInfo.GetMinIdleTime(), _enemyInfo.GetMaxIdleTime());
        yield return new WaitForSeconds(idleTime);
        Roam();
    }
    private void Roam(){
        currentEnemyState = EnemyState.Roaming;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);} //1 for roaming
        enemyAnimationState.Value = currentEnemyState;
        _agent.speed = _enemyInfo.GetRoamingSpeed();
        roamPosition = GetNewRoamingPosition();
        _agent.SetDestination(roamPosition);
        StartCoroutine(RoamingCoroutine());
    }
    private IEnumerator RoamingCoroutine(){
        yield return new WaitForSeconds(0.1f);
        while(_agent.remainingDistance > _agent.stoppingDistance){
            yield return null;
        }
        Idle();
    }
    private void OnTriggerEnter(Collider other){ //using triggers for player detection for computation optimization, need to set sphere collider to a trigger
        if(!IsServer){return;}
        if(other.CompareTag(targetTag)){
            targetsInRangeOfEnemy.Add(other.transform);
            if(currentEnemyState == EnemyState.Idle || currentEnemyState == EnemyState.Roaming){ //only want to become alert if previously idle or roaming, because could be chasing, attacking etc.
                BecomeAlert();
            }
        }
    }
    private void OnTriggerExit(Collider other){ //can either be in alert, chasing, decideattack or attacking state
        if(other.CompareTag(targetTag)){
            targetsInRangeOfEnemy.Remove(other.transform); //will remove from list regardless, that way can check later on in waitstopattacking
            if(other.transform == currentTarget){
                Debug.Log("Registered currTarget leaving aggro sphere");
                if(currentEnemyState == EnemyState.Approach){ //safe to remove current target
                    Debug.Log("Safe to remove currentTarget");
                    currentTarget = null;
                }
                else{ //not safe to remove current target since most attacks depend on currTarget transform, so need to wait until not attacking
                    StartCoroutine(WaitStopAttacking());
                }
            }
        }  
    }
    private IEnumerator WaitStopAttacking(){
        Debug.Log("Not safe to remove currentTarget");
        while(currentEnemyState != EnemyState.Approach){yield return null;}
        if(!targetsInRangeOfEnemy.Contains(currentTarget)){currentTarget = null;}
        yield break;
    }
    private void BecomeAlert(){
        Debug.Log("CLIENT: " + OwnerClientId + " BECOME ALERT FUNCTION");
        _agent.ResetPath(); 
        currentEnemyState = EnemyState.Alert;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        enemyAnimationState.Value = currentEnemyState;
        StopAllCoroutines();
        alertCoroutine = StartCoroutine(AlertCoroutine());
    }
    private IEnumerator AlertCoroutine(){
        yield return new WaitForSeconds(_enemyInfo.GetDetectionTime() + 0.1f);
        if(targetsInRangeOfEnemy.Count > 0){ //need to check if currentPlayerTarget is null because don't want to target a different player after each attack, since each attack will potentially go back to alert state
            while(targetsInRangeOfEnemy.Count > 0 && currentTarget == null){
                TryDetectPlayer(); //this will cancel alertocourotine if finds a player, so will not go back to being idle
                yield return null;
            }
            Idle();
        }
        else{Idle();} // no one in range
    }
    private void TryDetectPlayer(){
        foreach(Transform target in targetsInRangeOfEnemy){
            Vector3 directionToTarget;
            if(target.CompareTag(playerTag)){directionToTarget = target.transform.position - eyes.position;}
            else{directionToTarget = target.GetComponent<EnemyAI4>().GetEyesTransform().position - eyes.position;}
            if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL)){
                currentTarget = target; 
                StopCoroutine(alertCoroutine);
                alertCoroutine = null;
                DecideNextCombatStep();
                break;
            }
        }
    }
    private IEnumerator TurnCoroutine(){        
        Vector3 targetPosition = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(TurnTowardsTarget(targetPosition));
        //yield return new WaitForSeconds(5f);
        Debug.Log("GOT HERE!");
        DecideNextCombatStep();
    }
    private IEnumerator TurnTowardsTarget(Vector3 targetPosition){
        Debug.Log("TurningTowardsTarget Coroutine!");
        _anim.applyRootMotion = true;
        while (true){
            targetPosition = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (math.abs(angle) < 3f){ //because angle changes by a bit each frame, could potentially be up to 3, maybe even more so this might not always work
                _anim.applyRootMotion = false;
                yield break;
            }
            yield return null;
        }
    }
    private IEnumerator StareDownCoroutine(){
        _agent.speed = 10;
        _agent.SetDestination(currentTarget.position);
        yield return new WaitForSeconds(0.2f);
        _agent.ResetPath();
        float stareDownTime = UnityEngine.Random.Range(_enemyInfo.GetStareDownTime()-2f, _enemyInfo.GetStareDownTime()+2f);
        float elapsedTime = 0f;
        _agent.SetDestination(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));
        yield return new WaitForSeconds(0.2f);
        _agent.ResetPath();
        while (elapsedTime < stareDownTime){
            elapsedTime += Time.deltaTime;
            transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));
            yield return null; // Wait for the next frame
        }
        DecideNextCombatStep();
    }

    private IEnumerator RetreatCoroutine(){
        Vector3 directionAwayFromTarget = (transform.position - currentTarget.position).normalized;
        float retreatDistance = _enemyInfo.GetRetreatDistance() + _agent.stoppingDistance;
        Vector3 newPosition = UtilityFunctions.GetRandomPositionInDirection(transform, directionAwayFromTarget, 90, retreatDistance);
        newPosition = UtilityFunctions.FindNavMeshPosition(newPosition, transform.position);
        _agent.speed = _enemyInfo.GetRetreatSpeed();
        _agent.SetDestination(newPosition);
        yield return new WaitForNextFrameUnit();
        float elapsedTime = 0f;
        float giveUpTime = 10f;
        NavMeshPath path = new NavMeshPath();
        while(_agent.remainingDistance > _agent.stoppingDistance && elapsedTime <= giveUpTime){
            if (!(_agent.CalculatePath(newPosition, path) && path.status == NavMeshPathStatus.PathComplete)){
                newPosition = UtilityFunctions.GetRandomPositionInDirection(transform, directionAwayFromTarget, 90, retreatDistance);
                newPosition = UtilityFunctions.FindNavMeshPosition(newPosition, transform.position);
                _agent.SetDestination(newPosition);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DecideNextCombatStep();
    }
    private IEnumerator ApproachCoroutine(){
        _agent.SetDestination(currentTarget.position);
        float baseSpeed = _enemyInfo.GetApproachSpeed();
        _agent.speed = baseSpeed;
        yield return new WaitForNextFrameUnit();
        if(currentTarget){
            float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
            float maxDistance = currentDistance;
            while (Vector3.Distance(transform.position, currentTarget.position) > _agent.stoppingDistance){
                if(currentTarget){
                    _agent.SetDestination(currentTarget.position);
                    currentDistance = Vector3.Distance(transform.position, currentTarget.position);
                    float blendDistance = Mathf.Clamp01(1 - (currentDistance / maxDistance));
                    _anim.SetFloat("Distance", blendDistance);
                    _agent.speed = Mathf.Lerp(baseSpeed, baseSpeed * 3f, blendDistance);
                    yield return null;
                }
                else{
                    BecomeAlert();
                    yield break;
                }
            }
            Debug.Log("Reached agent stopping distance!");
            PerformAttack();
        }
        else{
            BecomeAlert();
        }
    }
    private IEnumerator JumpCoroutine(Vector3? newPos){ 
        currentJumpPosition = newPos.Value;
        _agent.enabled = false;
        _rb.useGravity = false;
        yield return new WaitUntil(() => hasFinishedJump == true);
        _agent.enabled = true;
        if(NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, _agent.areaMask)){
            _agent.Warp(hit.position);
        }
        _rb.useGravity = true;
        FinishedJumpReposition();
        yield return new WaitForEndOfFrame();
        hasFinishedJump = false;
    }

    private void FinishedJumpReposition(){
        
        if(attackChosen.GetAttackType() == 1 && Vector3.Distance(transform.position, currentTarget.position) > 1.5 * _agent.stoppingDistance){
            currentEnemyState = EnemyState.Approach;
            if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
            enemyAnimationState.Value = currentEnemyState;
            StartCoroutine(ApproachCoroutine());
        }
        else{
            PerformAttack();
        }
    }

    private float DetermineTurnDirection(){
        Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
        directionToTarget.y = 0;
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0;
        float angle = Vector3.SignedAngle(forwardDirection, directionToTarget, Vector3.up);
        return angle;
    }

    // Enemy will reposition itself before an attack
    private IEnumerator RepositionCoroutine(Vector3? newPos){
        _agent.SetDestination(UtilityFunctions.FindNavMeshPosition(newPos.Value, transform.position));
        _agent.speed = _enemyInfo.GetRepositionSpeed();
        yield return new WaitForSeconds(0.1f);
        float elapsedTime = 0f;
        while(_agent.remainingDistance > _agent.stoppingDistance && elapsedTime < 10f){
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        FinishedJumpReposition();

    }
    private void PerformAttack(){
        if(IsVisible()){StartCoroutine(EasyTurnIntoAttack());}
        else{StartCoroutine(ApproachUntilVisible());}
    }
    private IEnumerator EasyTurnIntoAttack(){
        if(currentEnemyState != EnemyState.Approach){
            _agent.speed = 10f;
            _agent.SetDestination(currentTarget.position);
            yield return new WaitForSeconds(0.2f);
            _agent.ResetPath();
        }
        _agent.ResetPath();
        currentEnemyState = nextAttackState;
        enemyAnimationState.Value = currentEnemyState;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPositionForAttack(attackChosen);}
        AnimationAttackEvent = null;
        if(attackChosenInstance){Destroy(attackChosenInstance.gameObject);}
        attackChosenInstance = Instantiate(attackChosen);
        AnimationAttackEvent += attackChosenInstance.ExecuteAttack;
        //AnimationAttackEvent += attackChosen.ExecuteAttack;
    }

    //if currently in a state where next decision can be an attack, first decide whether an attack makes sense before deciding that you definitely will attack
    //that way can decouple the deciding of the type of the attack. So say, does an attack make sense here based on my available attacks and 

    private void DecideNextCombatStep(){
        //chase, equip, unequip will be decided only if enemy decides to attack first
        Dictionary<EnemyState, float> possibleTransitions;
        Debug.Log("Coming from: " + currentEnemyState);
        
        // IMPORTANT! ALL TRANSITIONS HERE WIL BE TREATED AS RIGHT TURN, BUT HANDLED AS BEING RIGHT OR LEFT TURN
        // Having a possible attack a transition means possible equip, unequip, reposition, jump, approach
        switch (currentEnemyState){
            case EnemyState.Alert:
                possibleTransitions = new Dictionary<EnemyState, float>{{EnemyState.RightTurn, 2}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.RightTurn:
                possibleTransitions = new Dictionary<EnemyState, float>
                    {{EnemyState.StareDown, 2}, {EnemyState.AttackA, 1}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.LeftTurn:
                possibleTransitions = new Dictionary<EnemyState, float>
                    {{EnemyState.StareDown, 2}, {EnemyState.AttackA, 1}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.StareDown:
                possibleTransitions = new Dictionary<EnemyState, float>
                    {{EnemyState.AttackA, 1}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.Retreat:
                possibleTransitions = new Dictionary<EnemyState, float>
                    {{EnemyState.StareDown, 1}, {EnemyState.AttackA, 2}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.AttackA:
                nextAttackState = EnemyState.AttackB;
                possibleTransitions = new Dictionary<EnemyState, float>
                {{EnemyState.StareDown, 1}, {EnemyState.AttackA, 1}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.AttackB:
                nextAttackState = EnemyState.AttackA;
                //same next possible transitions as from attacka
                possibleTransitions = new Dictionary<EnemyState, float>
                {{EnemyState.StareDown, 1}, {EnemyState.AttackA, 1}, {EnemyState.Retreat, 1}};
                break;
            case EnemyState.Approach: //this will only be called from approachUntilVisible()
                //still need this because want to reconsider what attack to do after approaching from approachuntilvisible()
                possibleTransitions = new Dictionary<EnemyState, float>{{EnemyState.AttackA, 1}};
                break;

            // will never arrive from jump, reposition, equip, unequip because always attack afterwards
            
            // IMPLEMENT LATER
            // case EnemyState.Dodge:
            //     break;
            // case EnemyState.Block:
            //     break;
            
            // IMPLEMENT LATER
            // case EnemyState.ReceiveBuff:
            //     break;
            // case EnemyState.TakeHit:
            //     break;
            // case EnemyState.Die:
            //     break;
            default:
                Debug.Log("DecideNextCombatStep() ERROR! Arrived from: " + currentEnemyState);
                possibleTransitions = new Dictionary<EnemyState, float>{{EnemyState.Die, 1}}; 
                break;
        }
        (EnemyState decision, Vector3? repositionLocation) = DecidedWeightedCombatDecision(possibleTransitions);
        Debug.Log("From: " + currentEnemyState + ", Decided on: " + decision);
        HandleNextCombatDecision(decision, repositionLocation);
    }

    private void HandleNextCombatDecision(EnemyState decision, Vector3? repositionLocation){
        if(decision != EnemyState.AttackA && decision != EnemyState.AttackB){
            currentEnemyState = decision;
            if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
            enemyAnimationState.Value = currentEnemyState;
        } // this is because PerformAttack() will handle this, and that will first wait for a delay for a smooth turn towards its target
        switch(decision){
            case EnemyState.RightTurn:
                float angle = DetermineTurnDirection();
                currentEnemyState = angle > 0 ? EnemyState.RightTurn : EnemyState.LeftTurn;
                enemyAnimationState.Value = currentEnemyState;
                StartCoroutine(TurnCoroutine());
                break;
            case EnemyState.StareDown:
                StartCoroutine(StareDownCoroutine());
                break;
            case EnemyState.Approach:
                StartCoroutine(ApproachCoroutine());
                break;
            case EnemyState.AttackA:
                PerformAttack();
                break;
            case EnemyState.AttackB:
                PerformAttack();
                break;
            case EnemyState.Equip:
                StartCoroutine(WaitEndEquipAnimation());
                //animation will call the attackChosen, don't need to do anything
                break;
            case EnemyState.Unequip:
                //animation will call the attackChosen, don't need to do anything
                StartCoroutine(WaitEndUnequipAnimation());
                break;
            case EnemyState.Dodge:
                break;
            case EnemyState.Block:
                break;
            case EnemyState.Jump:
                //edit this later to not have to run and jump but instead just side jump or something based on direction
                StartCoroutine(JumpCoroutine(repositionLocation));
                break;
            case EnemyState.Retreat:
                StartCoroutine(RetreatCoroutine());
                break;
            case EnemyState.Reposition:
                StartCoroutine(RepositionCoroutine(repositionLocation));
                break;
            case EnemyState.ReceiveBuff:
                break;
            case EnemyState.TakeHit:
                break;
            case EnemyState.Die:
                break;
            default:
                break;
        }
    }
    private IEnumerator WaitEndEquipAnimation(){
        yield return new WaitForSeconds(_copyOverrider["Equip Placeholder"].length);
        isWeaponOut = true;
        (EnemyState transition, Vector3? newPos) = DecideNextTransitionForAttack();
        HandleNextCombatDecision(transition, newPos);
    }
    private IEnumerator WaitEndUnequipAnimation(){
        yield return new WaitForSeconds(_copyOverrider["Unequip Placeholder"].length);
        isWeaponOut = false;
        (EnemyState transition, Vector3? newPos) = DecideNextTransitionForAttack();
        HandleNextCombatDecision(transition, newPos);
    }
    private (EnemyState, Vector3?) DecidedWeightedCombatDecision(Dictionary<EnemyState, float> possibleTransitions){        
        //initial application of weights
        float totalWeight = 0f;
        foreach(var key in possibleTransitions.Keys.ToList()){
            switch(key){
                case EnemyState.RightTurn:
                    break;
                case EnemyState.StareDown:
                    break;
                case EnemyState.Approach:
                    break;
                case EnemyState.AttackA:
                    possibleTransitions[key] += possibleTransitions[key] * _enemyInfo.GetAggression();
                    break;
                case EnemyState.AttackB:
                    break;
                case EnemyState.Equip:
                    break;
                case EnemyState.Unequip:
                    break;
                case EnemyState.Dodge:
                    break;
                case EnemyState.Block:
                    break;
                case EnemyState.Jump:
                    break;
                case EnemyState.Retreat:
                    break;
                case EnemyState.Reposition:
                    break;
                case EnemyState.ReceiveBuff:
                    break;
                case EnemyState.TakeHit:
                    break;
                case EnemyState.Die:
                    break;
                default:
                    break;
            }
        }
        foreach (var item in possibleTransitions){totalWeight += item.Value;}
        //normalize weights to add up to 1
        foreach (var key in possibleTransitions.Keys.ToList()){possibleTransitions[key] = possibleTransitions[key] / totalWeight;}

        float randomValue = UnityEngine.Random.Range(0f, 1f);
        float cumulativeProbability = 0f;
        foreach (var item in possibleTransitions){
            cumulativeProbability += item.Value;
            if (randomValue <= cumulativeProbability){
                //picked our choice
                if (item.Key != EnemyState.AttackA){return (item.Key, null);}
                else{
                    //need to determine which attack to use, and then determine if going to attack immediately or do some other stuff before
                    attackChosen = DecideAttack();

                    //int attackChosenIndex = GetIndexFromAttack(attackChosen);

                    //SetAttackAnimationClientRpc(attackChosenIndex);

                    if(attackChosen.DoesRequireWeapon() && !isWeaponOut){return (EnemyState.Equip, null);}
                    if(!attackChosen.DoesRequireWeapon() && isWeaponOut){return (EnemyState.Unequip, null);}
                    
                    return DecideNextTransitionForAttack();                    

                    //based on attack, needs to either equip, unequip, approach (if in melee range), jump (get further away or closer to do attack),
                    // or approach fast(seconds before a melee attack) 
                }
            }
        }
        Debug.Log("SHOULD NEVER GET HERE! Could not decide a combat transition");
        return (EnemyState.Alert, null);
    }

    // private int GetIndexFromAttack(BaseAttackScript attackChosen){
    //     return animationsListSO.IndexOf(attackChosen);
    // }

    // private BaseAttackScript GetAttackFromIndex(int attackIndex){
    //     return animationsListSO[attackIndex];
    // }

    [ClientRpc]
    private void SetAttackAnimationClientRpc(int attackIndex){
        //from here, might not attack directly, but can still set the attack to this because know that this is necessarily next attack
        
        //BaseAttackScript attackChosen = GetAttackFromIndex(attackIndex);
        Debug.Log("Running this HERE!");
        if(nextAttackState == EnemyState.AttackA){_copyOverrider["AttackA Placeholder"] = attackChosen.getAnimationClip();}
        else{_copyOverrider["AttackB Placeholder"] = attackChosen.getAnimationClip();}
    }

    private (EnemyState, Vector3?) DecideNextTransitionForAttack(){
        float distanceToPlayer = Vector3.Distance(feet.position, currentTarget.position);
        float playerProximityRatio = Mathf.Clamp(distanceToPlayer/_enemyInfo.GetAggroDistance(), 0f, 1f);
        int attackType = attackChosen.GetAttackType();
        if(attackType == 1 && playerProximityRatio > 0.33f ||
            attackType == 2 && playerProximityRatio < 0.33f && playerProximityRatio > 0.66f ||
            attackType == 3 && playerProximityRatio < 0.33f){
            Vector3 newPos;
            int mode;
            if(attackType == 1){mode = 1;}
            else if(attackType == 2){mode = 2;}
            else{mode = 3;}
            newPos = UtilityFunctions.FindNavMeshPosition(GetRandomPositionInRange(mode), transform.position);
            return _enemyAttackManager.HasJump(isWeaponOut) ? (EnemyState.Jump, newPos) : (EnemyState.Reposition, newPos);
        }
        if(attackType == 1 && Vector3.Distance(transform.position, currentTarget.position) > 1.5 * _agent.stoppingDistance){return (EnemyState.Approach, null);}
        return (nextAttackState, null);
    }
    private BaseAttackScript DecideAttack(){
        
        Debug.Log(attackPool.Count);
        meleeAttacks = attackPool.FindAll(attackData => attackData.attack.GetAttackType() == 1);
        mediumAttacks = attackPool.FindAll(attackData => attackData.attack.GetAttackType() == 2);
        longAttacks = attackPool.FindAll(attackData => attackData.attack.GetAttackType() == 3);
        hasMelee = meleeAttacks.Count > 0;
        hasMedium = mediumAttacks.Count > 0;
        hasLong = longAttacks.Count > 0;
        Debug.Log($"hasMelee: {hasMelee}, hasMedium: {hasMedium}, hasLong: {hasLong}");
        
        float distanceToPlayer = Vector3.Distance(feet.position, currentTarget.position);
        float playerProximityRatio = Mathf.Clamp(distanceToPlayer/_enemyInfo.GetAggroDistance(), 0f, 1f); //always <= 1
        Dictionary<EnemyAttackManager.AttackData, float> originalWeights = new Dictionary<EnemyAttackManager.AttackData, float>();

        /*
        accounting for if weapon is out, distance to player, attack range preference of enemy, 
        */
        float totalWeight = 0f;
        foreach (var attackData in attackPool){
            float originalWeight = attackData.weight;
            originalWeights[attackData] = originalWeight;

            float adjustedWeight = originalWeight;
            if (isWeaponOut && attackData.attack.DoesRequireWeapon()) { adjustedWeight *= 2f; }

            if (attackData.attack.GetAttackType() == 1){
                adjustedWeight *= _enemyInfo.GetMeleeAffinity();
                if (playerProximityRatio < 0.33f) { adjustedWeight *= 3f; }
                else if (playerProximityRatio < 0.67f) { adjustedWeight *= 1.5f; }
            }
            else if (attackData.attack.GetAttackType() == 2){
                adjustedWeight *= _enemyInfo.GetMediumAffinity();
                if (playerProximityRatio > 0.33f && playerProximityRatio < 0.67f) { adjustedWeight *= 3.5f; }
            }
            else if (attackData.attack.GetAttackType() == 3){
                adjustedWeight *= _enemyInfo.GetRangedAffinity();
                if (playerProximityRatio > 0.33f && playerProximityRatio < 0.67f) { adjustedWeight *= 1.5f; }
                else if (playerProximityRatio > 0.67f) { adjustedWeight *= 3f; }
            }
            attackData.weight = adjustedWeight;
            totalWeight += adjustedWeight;
        }
        foreach (var attackData in attackPool){
            attackData.weight /= totalWeight;
            // Debug.Log("Adjusted weight of attack " + attackData.attack + ": " + attackData.weight);
        }
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        float cumulativeProbability = 0f;
        BaseAttackScript retAttack = null;
        foreach (var attackData in attackPool){
            cumulativeProbability += attackData.weight;
            if (randomValue <= cumulativeProbability){
                retAttack = attackData.attack;
                break;
            }
        }
        if(!retAttack){
            retAttack = attackPool[0].attack;
            Debug.Log("Assigned a default decided attack, THIS SHOULD NEVER HAPPEN!");
        }
        // Reset to original weights
        foreach (var kvp in originalWeights){kvp.Key.weight = kvp.Value;}
        return retAttack;
    }
    private BaseAttackScript GetAttack(List<BaseAttackScript> filteredAttacks){
        float randomValue = UnityEngine.Random.value;
        float cumulativeWeight = 0f;
        foreach (var attack in filteredAttacks){
            cumulativeWeight += attack.GetAttackWeight();
            if (randomValue <= cumulativeWeight){
                return attack; // Selected attack
            }
        }
        return null;
    }
          
    private Vector3 GetRandomPositionInRange(int mode){
        float minRatio = 0f;
        float maxRatio = 0f;
        switch (mode){
            case 1:
                minRatio = 0f;
                maxRatio = 0.33f;
                break;
            case 2:
                minRatio = 0.33f;
                maxRatio = 0.66f;
                break;
            case 3:
                minRatio = 0.66f;
                maxRatio = 1f;
                break;
        }
        Vector3 targetToEnemy = (transform.position - currentTarget.position).normalized;
        float minDistance = minRatio * _enemyInfo.GetAggroDistance();
        float maxDistance = maxRatio * _enemyInfo.GetAggroDistance();
        float randomDistance = UnityEngine.Random.Range(minDistance, maxDistance);
        float randomAngle = UnityEngine.Random.Range(-90f, 90f);
        Quaternion rotation = Quaternion.AngleAxis(randomAngle, Vector3.up);
        Vector3 randomDirection = rotation * targetToEnemy;
        Vector3 randomPosition = currentTarget.position + randomDirection * randomDistance;
        return randomPosition;
    }
    private Vector3 GetNewRoamingPosition(){
        Vector3 randDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        float roamingRange = UnityEngine.Random.Range(_enemyInfo.GetMinRoamingRange(), _enemyInfo.GetMaxRoamingRange());
        Vector3 newPos = startPosition + (randDir * roamingRange);
        return UtilityFunctions.FindNavMeshPosition(newPos, transform.position);
    }
    private IEnumerator ApproachUntilVisible(){
        currentEnemyState = EnemyState.Approach;
        enemyAnimationState.Value = currentEnemyState;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        _agent.speed = _enemyInfo.GetApproachSpeed();
        while(currentTarget && !IsVisible()){
            _agent.SetDestination(currentTarget.position);
            yield return null;
        }
        if(!currentTarget){
            BecomeAlert();
            yield break;
        }
        else{
            _agent.ResetPath();
            DecideNextCombatStep();
        }
    }
    private bool IsVisible(){
        Debug.Log("Checking if is Visible!");
        Vector3 directionToTarget;
        if(currentTarget.CompareTag(playerTag)){directionToTarget = currentTarget.transform.position - eyes.position;}
        else{directionToTarget = currentTarget.GetComponent<EnemyAI4>().GetEyesTransform().position - eyes.position;}

        if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL)){
            Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
            return true;
        }
        return false;
    }
    private void AnimationEventNextStep(){
        var attackEvent = new AttackEvent(this.gameObject, attackChosen, currentTarget, attackCenter, targetL);
        AnimationAttackEvent?.Invoke(this, attackEvent);
    }
    private void SlowAnim(float time){SetAnimationSpeed(0, time);} // Slow mode
    private void SpeedAnim(float time){SetAnimationSpeed(1, time);} // Fast mode
    private void VarySpeedAnim(float time){
        int mode = UnityEngine.Random.value < 0.5f ? 0 : 1; // Randomly pick slow (0) or fast (1)
        SetAnimationSpeed(mode, time);
    }
    private void SetAnimationSpeed(int mode, float time){
        Debug.Log("SettingAnimationSpeed!");
        if (speedCoroutine != null){StopCoroutine(speedCoroutine);}
        speedCoroutine = StartCoroutine(ChangeSpeedAnimator(time, mode));
    }
    private IEnumerator ChangeSpeedAnimator(float pauseTime, int mode){
        float newSpeed;
        float newPauseTime;
        if (mode == 0){newSpeed = UnityEngine.Random.Range(0.2f, 0.8f);}
        else if (mode == 1){newSpeed = UnityEngine.Random.Range(1.2f, 1.8f);}
        else{ newSpeed = 1f;}
        bool animChange = UnityEngine.Random.value < _enemyInfo.GetAnimChangeProbability();
        if(!animChange){
            newSpeed = 1f;
            newPauseTime = 0f;
        }
        //Debug.Log($"Setting animation speed to {newSpeed} for {newPauseTime} seconds.");
        _anim.speed = newSpeed;
        newPauseTime = pauseTime/newSpeed; //e.g. if pauseTime was 2 seconds, and newSpeed = 0.5, then newPauseTime needs to be 2/0.5 = 4 seconds
        yield return new WaitForSeconds(newPauseTime);
        _anim.speed = 1f; 
        speedCoroutine = null;
    }
    public Transform GetEyesTransform(){return eyes;}
    public Transform GetFeetTransform(){return feet;}
    public Vector3 GetJumpPosition(){return currentJumpPosition;}
    private void OnDisable(){AnimationAttackEvent = null;}

    // at very minimum, need long range vs melee range, aggressive vs patient, smart vs dumb, combo tendency vs not, 
        // e.g. 
        // MODIFIERS:
        // aggression (-1 to 1):
            // weight probability of doing an attack (intuition: vs. jumping back or staring down)
            // 
        
        //
        // chaining probability (0 to 1). probability of starting a chained attack
        // chaining length (0 to 1). need to yield 0 to 6 possible chains?
        // attack intelligence: factors that influence what attack it will pick
            // distance to target (factored in regardless of intelligence), will weight that type of attack (1, 2, 3 (melee, short, long))
            // if weapon is equipped by enemy, will weight armed attacks (attack requires weapon boolean)
            // DON'T want to take into account the type of weapon the enemy has equipped, because that could make them choose a melee attack for a
            // melee player if the melee player has a bow equipped temporarily
            // depending on player type (e.g. berserker, weight long range), (e.g. mage, weight melee)
            // depending on elemental weakness of target, if have an attack that is strong against that element
            // depending on status of player (e.g. stunned, low health), might choose to be more or less aggressive
        //(-1 to 1) attack picking smartness. When deciding an attack, will take into account the class of the player
        //player 
        //defensiveness (-1 to 1)
        //

        //approach is for melee fights
        //don't want to approach into every type of attack though. 

}