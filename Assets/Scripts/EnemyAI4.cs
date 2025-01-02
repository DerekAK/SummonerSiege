using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using System.Linq;
using Unity.Mathematics;

public class EnemyAI4 : MonoBehaviour{
    private BaseAttackScript attackChosen;
    private bool hasMelee, hasMedium, hasLong;
    private int countMeleeInRow, countMediumInRow, countLongInRow = 0;
    private List<BaseAttackScript> meleeAttacks, mediumAttacks, longAttacks;
    private NavMeshAgent _agent;
    private Animator _anim;
    private Rigidbody _rb;
    private AnimatorOverrideController _templateOverrider;
    private AnimatorOverrideController _copyOverrider;
    private SphereCollider _aggroCollider;
    private List<Transform> playersInGame;
    private List<Transform> targetsInRangeOfEnemy = new List<Transform>();
    private List<BaseAttackScript> attackPool;
    private Transform currentTarget;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine alertCoroutine;
    private Coroutine speedCoroutine;
    private float attackCenterBoxRadius;
    private float enemyHeight;
    [SerializeField] private LayerMask obstacleL;
    [SerializeField] private LayerMask playerL;
    [SerializeField] private LayerMask enemyL;
    private LayerMask targetL;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string weaponTag = "Weapon";
    [SerializeField] private string enemyTag = "Enemy";
    private string targetTag;
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
    public class AttackEvent : EventArgs{
        public BaseAttackScript AttackChosen{get;set;}
        public Transform TargetTransform{ get;set;}
        public Transform AttackCenterForward{ get;set;}
        public LayerMask TargetL{get;set;}
    }
    //events to subscribe to
    public event EventHandler<AttackEvent> AnimationAttackEvent;
    private EnemyState nextAttackState;

    private void Awake(){
    
        SetUpAnimator(); //sets up a unique copy of the animatorOverrider for each enemy instance
        _enemyAttackManager = GetComponent<EnemyAttackManager>();
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _aggroCollider = GetComponent<SphereCollider>();
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        attackCenterBoxRadius = enemyHeight/2; 
        targetTag = playerTag;
        targetL = playerL;
    }
    //every enemy instance will get it's own overrider copy
    private void SetUpAnimator(){
        _anim = GetComponent<Animator>();
        _templateOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _copyOverrider = new AnimatorOverrideController(_templateOverrider);
        _anim.runtimeAnimatorController = _copyOverrider;
    }
    private void Start(){
        nextAttackState = EnemyState.AttackA;
        _aggroCollider.radius = (_enemyInfo.GetAggroDistance()-1)/transform.localScale.y;
        _agent.stoppingDistance = 10f; //THIS IS REALLY IMPORTANT TO GET RIGHT
        _agent.angularSpeed = 1000f;
        _agent.acceleration = 200f;
        _agent.radius = 0.1f;
        isWeaponOut = false;
        attackPool = _enemyAttackManager.GetCurrentAvailableAttacks();
        playersInGame = GameManager.Instance.getPlayerTransforms();
        startPosition = feet.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }
    private void Idle(){
        _agent.ResetPath();
        currentEnemyState = EnemyState.Idle;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        //CancelIdle();
        //CancelAlert();
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
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
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
        _agent.ResetPath(); 
        currentEnemyState = EnemyState.Alert;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
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
            Debug.Log("anspogiuanspudgiasjdngiusajngiulasjng");
            Idle();
        }
        else{Idle();} // no one in range
    }
    private void TryDetectPlayer(){
        foreach(Transform target in targetsInRangeOfEnemy){
            Vector3 directionToTarget;
            if(target.CompareTag(playerTag)){directionToTarget = target.GetComponent<ThirdPersonMovementScript>().GetEyesTransform().position - eyes.position;}
            else{directionToTarget = target.GetComponent<EnemyAI4>().GetEyesTransform().position - eyes.position;}
            if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL)){
                Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
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
        _anim.speed = 3f;
        while (true){
            targetPosition = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (math.abs(angle) < 3f){ //because angle changes by a bit each frame, could potentially be up to 3, maybe even more so this might not always work
                _anim.applyRootMotion = false;
                _anim.speed = 1f;
                yield break;
            }
            yield return null;
        }
    }
    private IEnumerator StareDownCoroutine(){
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
        float randomAngle = UnityEngine.Random.Range(-90f, 90f); // Adjust within a 45-degree cone
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 adjustedDirection = rotation * directionAwayFromTarget;
        float retreatDistance = _enemyInfo.GetRetreatDistance();
        Vector3 newPosition = transform.position + adjustedDirection * retreatDistance;
        newPosition = FindNavMeshPosition(newPosition);
        _agent.speed = _enemyInfo.GetRetreatSpeed();
        _agent.SetDestination(newPosition);
        yield return new WaitForSeconds(0.1f);
        float elapsedTime = 0f;
        float giveUpTime = 10f;
        while(_agent.remainingDistance > _agent.stoppingDistance && elapsedTime <= giveUpTime){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DecideNextCombatStep();
    }
    private IEnumerator ApproachCoroutine(){
        _agent.SetDestination(currentTarget.position);
        float baseSpeed = _enemyInfo.GetApproachSpeed();
        _agent.speed = baseSpeed;
        yield return new WaitForSeconds(0.1f);
        if(currentTarget){
            float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
            float maxDistance = currentDistance;
            while (_agent.remainingDistance > _agent.stoppingDistance){
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
            HandleAttackTransition();
            PerformAttack();
        }
        else{
            BecomeAlert();
        }
    }
    private IEnumerator JumpCoroutine(Vector3? newPos){ 
        currentJumpPosition = newPos.Value;
        yield return new WaitForSeconds(_copyOverrider["Jump Placeholder"].length); //animation itself will take care of the jump logic

        if(attackChosen.GetAttackType() == 1){
            currentEnemyState = EnemyState.Approach;
            if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            StartCoroutine(ApproachCoroutine());
        }
        else{
            HandleAttackTransition();
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
        _agent.SetDestination(FindNavMeshPosition(newPos.Value));
        _agent.speed = _enemyInfo.GetRepositionSpeed();
        yield return new WaitForSeconds(0.1f);
        float elapsedTime = 0f;
        while(_agent.remainingDistance > _agent.stoppingDistance && elapsedTime < 10f){
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if(attackChosen.GetAttackType() == 1){
            currentEnemyState = EnemyState.Approach;
            if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            StartCoroutine(ApproachCoroutine());
        }
        else{
            HandleAttackTransition();
            PerformAttack();
        }
    }
    private void HandleAttackTransition(){
        currentEnemyState = nextAttackState;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPositionForAttack(attackChosen);}
    }
    private void PerformAttack(){
        AnimationAttackEvent = null;
        if(IsVisible()){
            if(attackChosenInstance){Destroy(attackChosenInstance.gameObject);}
            attackChosenInstance = Instantiate(attackChosen);
            attackChosenInstance.SetGameObjectReference(this.gameObject);
            AnimationAttackEvent += attackChosenInstance.ExecuteAttack;
        }
        else{
            StartCoroutine(ApproachUntilVisible());
        }
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
        currentEnemyState = decision;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        switch(decision){
            case EnemyState.RightTurn:
                float angle = DetermineTurnDirection();
                currentEnemyState = angle > 0 ? EnemyState.RightTurn : EnemyState.LeftTurn;
                _anim.SetInteger("EnemyState", (int)currentEnemyState);
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

                    //from here, might not attack directly, but can still set the attack to this because know that this is necessarily next attack
                    if(nextAttackState == EnemyState.AttackA){_copyOverrider["AttackA Placeholder"] = attackChosen.getAnimationClip();}
                    else{_copyOverrider["AttackB Placeholder"] = attackChosen.getAnimationClip();}

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
            newPos = FindNavMeshPosition(GetRandomPositionInRange(mode));
            return _enemyAttackManager.HasJump() ? (EnemyState.Jump, newPos) : (EnemyState.Reposition, newPos);
        }
        if(attackType == 1){return (EnemyState.Approach, null);}
        return (nextAttackState, null);
    }
    private BaseAttackScript DecideAttack(){
        
        meleeAttacks = attackPool.FindAll(attack => attack.GetAttackType() == 1);
        mediumAttacks = attackPool.FindAll(attack => attack.GetAttackType() == 2);
        longAttacks = attackPool.FindAll(attack => attack.GetAttackType() == 3);
        hasMelee = meleeAttacks.Count > 0;
        hasMedium = mediumAttacks.Count > 0;
        hasLong = longAttacks.Count > 0;
        Debug.Log($"hasMelee: {hasMelee}, hasMedium: {hasMedium}, hasLong: {hasLong}");
        
        float distanceToPlayer = Vector3.Distance(feet.position, currentTarget.position);
        float playerProximityRatio = Mathf.Clamp(distanceToPlayer/_enemyInfo.GetAggroDistance(), 0f, 1f); //always <= 1
        Dictionary<BaseAttackScript, float> originalWeights = new Dictionary<BaseAttackScript, float>();

        /*
        accounting for if weapon is out, distance to player, attack range preference of enemy, 
        */
        float totalWeight = 0f;
        foreach (var attack in attackPool){
            float originalWeight = attack.GetAttackWeight();
            originalWeights[attack] = originalWeight;

            float adjustedWeight = originalWeight;
            if (isWeaponOut && attack.DoesRequireWeapon()) { adjustedWeight *= 2f; }

            if (attack.GetAttackType() == 1){
                adjustedWeight *= _enemyInfo.GetMeleeAffinity();
                if (playerProximityRatio < 0.33f) { adjustedWeight *= 3f; }
                else if (playerProximityRatio < 0.67f) { adjustedWeight *= 1.5f; }
            }
            else if (attack.GetAttackType() == 2){
                adjustedWeight *= _enemyInfo.GetMediumAffinity();
                if (playerProximityRatio > 0.33f && playerProximityRatio < 0.67f) { adjustedWeight *= 3.5f; }
            }
            else if (attack.GetAttackType() == 3){
                adjustedWeight *= _enemyInfo.GetRangedAffinity();
                if (playerProximityRatio > 0.33f && playerProximityRatio < 0.67f) { adjustedWeight *= 1.5f; }
                else if (playerProximityRatio > 0.67f) { adjustedWeight *= 3f; }
            }
            totalWeight += adjustedWeight;
            attack.SetAttackWeight(adjustedWeight);
        }

        foreach (var attack in attackPool){attack.SetAttackWeight(attack.GetAttackWeight()/totalWeight);}
        float randomValue = UnityEngine.Random.Range(0f, 1f);
        float cumulativeProbability = 0f;
        BaseAttackScript retAttack = null;
        foreach (var attack in attackPool){
            cumulativeProbability += attack.GetAttackWeight();
            if (randomValue <= cumulativeProbability){
                //this is attack to pick
                retAttack = attack;
                break;
            }
        }
        if(!retAttack){
            retAttack = attackPool[0];
            Debug.Log("Assigned a default decided attack, THIS SHOULD NEVER HAPPEN!");
        }
        // Reset to original weights
        foreach (var kvp in originalWeights){kvp.Key.SetAttackWeight(kvp.Value);}
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
    private Vector3 FindNavMeshPosition(Vector3 position){
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(position.x, 500f, position.z), Vector3.down, out hit, Mathf.Infinity)){   
            Debug.DrawRay(new Vector3(position.x, 500f, position.z), Vector3.down * 500f, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 1000f, NavMesh.AllAreas)){return navHit.position;} // Return the valid NavMesh position
        }
        return transform.position;
    }
    private Vector3 GetNewRoamingPosition(){
        Vector3 randDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        float roamingRange = UnityEngine.Random.Range(_enemyInfo.GetMinRoamingRange(), _enemyInfo.GetMaxRoamingRange());
        Vector3 newPos = startPosition + (randDir * roamingRange);
        return FindNavMeshPosition(newPos);
    }
    private IEnumerator ApproachUntilVisible(){
        currentEnemyState = EnemyState.Approach;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
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
        if(currentTarget.CompareTag(playerTag)){directionToTarget = currentTarget.GetComponent<ThirdPersonMovementScript>().GetEyesTransform().position - eyes.position;}
        else{directionToTarget = currentTarget.GetComponent<EnemyAI4>().GetEyesTransform().position - eyes.position;}

        if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL)){
            Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
            return true;
        }
        return false;
    }
    private void AnimationEventNextStep(){
        AnimationAttackEvent?.Invoke(this, new AttackEvent{AttackChosen = attackChosen, TargetTransform=currentTarget, AttackCenterForward=attackCenter, TargetL=targetL});
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
    public Transform GetCurrentTarget(){return currentTarget;}
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