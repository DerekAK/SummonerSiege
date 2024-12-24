using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using Unity.VisualScripting;
using JetBrains.Annotations;
using UnityEngine.InputSystem;

public class EnemyAI4 : MonoBehaviour{
    private float chaseGiveUpTime;
    private BaseAttackScript attackChosen;
    private bool hasMelee, hasMedium, hasLong;
    private int countMeleeInRow, countMediumInRow, countLongInRow = 0;
    private List<BaseAttackScript> meleeAttacks, mediumAttacks, longAttacks;
    private NavMeshAgent _agent;
    private Animator _anim;
    private AnimatorOverrideController _templateOverrider;
    private AnimatorOverrideController _copyOverrider;
    private SphereCollider _aggroCollider;
    private List<Transform> playersInGame;
    private List<Transform> targetsInRangeOfEnemy = new List<Transform>();
    private List<BaseAttackScript> attackPool;
    private Transform currentTarget;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine idleCoroutine;
    private Coroutine chaseCoroutine;
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
    [SerializeField] private float aggroDistance = 70f;
    [SerializeField] private float minRoamingRange = 50f;
    [SerializeField] private float maxRoamingRange = 70f;
    [SerializeField] private int minIdleTime = 5;
    [SerializeField] private int maxIdleTime = 20;
    [SerializeField] private int roamingSpeed = 10;
    [SerializeField] private int chasingSpeed = 15;
    private EnemySpecificInfo _enemyInfo;
    private EnemyAttackManager _enemyAttackManager;
    private bool isWeaponOut;
    public enum EnemyState{Idle=0, Roaming=1, Alert=2, StareDown=3, Chasing=4, AttackA=5, AttackB=6, 
    Equip=7, Unequip=8, Dodge=9, Block=10, Retreat=11, Reposition=12, ReceiveBuff=13, TakeHit=14, Die=15, Turn=16, Approach=17}
    private EnemyState currentEnemyState;
    public class AttackEvent : EventArgs{
        public BaseAttackScript AttackChosen{get;set;}
        public float ChaseGiveUpTime{get;set;} 
        public Transform TargetTransform{ get;set;}
        public Transform AttackCenterForward{ get;set;}
        public LayerMask TargetL{get;set;}
    }
    //events to subscribe to
    public event EventHandler<AttackEvent> AnimationAttackEvent;

    private void Awake(){
        
        SetUpAnimator(); //sets up a unique copy of the animatorOverrider for each enemy instance
        _enemyAttackManager = GetComponent<EnemyAttackManager>();
        _enemyInfo = GetComponent<EnemySpecificInfo>();
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = (aggroDistance-1)/transform.localScale.y;
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
        idleCoroutine = StartCoroutine(IdleCoroutine());
    }
    IEnumerator IdleCoroutine(){
        float idleTime = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
        yield return new WaitForSeconds(idleTime);
        idleCoroutine = null;
        Roam();
    }
    private void Roam(){
        currentEnemyState = EnemyState.Roaming;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);} //1 for roaming
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        _agent.speed = roamingSpeed;
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
                if(currentEnemyState == EnemyState.StareDown || currentEnemyState == EnemyState.Chasing){ //safe to remove current target
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
        while(currentEnemyState != EnemyState.StareDown){
            yield return null;
        }
        //in decide attack at this point, so need to have a conditional in decide attack if currenttarget exists or not, but that should be the only place possible for it to become null
        if(!targetsInRangeOfEnemy.Contains(currentTarget)){
            currentTarget = null;
        }
        //else, keep current target, because this means that current target went out of aggrosphere when enemy was attacking and came back in during same time, so still in aggro sphere
        yield break;
    }
    private void BecomeAlert(){
        _agent.ResetPath(); 
        currentEnemyState = EnemyState.Alert;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);} //2 for alert
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        StopAllCoroutines();
        StartCoroutine(AlertCoroutine());
    }

    //dont want to start a new while loop if there already is a current player target. so if only players in range, don't want to necessarily start detecting bc might have currtarget. if only there's no currtarget, could be that theres no people in range
    private IEnumerator AlertCoroutine(){
        yield return new WaitForSeconds(_enemyInfo.GetDetectionTime());
        if(targetsInRangeOfEnemy.Count > 0){ //need to check if currentPlayerTarget is null because don't want to target a different player after each attack, since each attack will potentially go back to alert state
            while(targetsInRangeOfEnemy.Count > 0 && currentTarget == null){
                TryDetectPlayer(); //this will cancel alertocourotine if finds a player, so will not go back to being idle
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
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
                StopCoroutine(AlertCoroutine());
                DecideNextCombatStep();
                break;
            }
        }
    }
    private IEnumerator TurnCoroutine(){
        currentEnemyState = EnemyState.Turn;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        Vector3 targetPosition = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
        Vector3 directionToTarget = (targetPosition-transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        while(Quaternion.Angle(transform.rotation, targetRotation) < 0.1f){
            if(currentTarget){
                targetPosition = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
                directionToTarget = (targetPosition-transform.position).normalized;
                targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _enemyInfo.GetTurnSpeed() * Time.deltaTime);
            }
            else{
                BecomeAlert();
                yield break;
            }
        }
        DecideNextCombatStep();
    }
    private IEnumerator StareDownCoroutine(){
        float stareDownTime = UnityEngine.Random.Range(_enemyInfo.GetStareDownTime()-2f, _enemyInfo.GetStareDownTime()+2f);
        float elapsedTime = 0f;
        float circleRadius = Vector3.Distance(transform.position, currentTarget.position);
        float circleSpeed = _enemyInfo.GetCirclingSpeed(); // Speed of circling
        while (elapsedTime < stareDownTime){
            elapsedTime += Time.deltaTime;
            float angle = elapsedTime * circleSpeed; // Angle increases over time
            float xOffset = Mathf.Cos(angle) * circleRadius;
            float zOffset = Mathf.Sin(angle) * circleRadius;
            Vector3 newPosition = new Vector3(currentTarget.position.x + xOffset,transform.position.y, currentTarget.position.z + zOffset);
            transform.position = FindNavMeshPosition(newPosition);
            transform.LookAt(currentTarget);
            yield return null; // Wait for the next frame
        }
        DecideNextCombatStep();
    }

    private IEnumerator RepositionCoroutine(){
        Vector3 directionAwayFromTarget = (transform.position - currentTarget.position).normalized;
        float randomAngle = UnityEngine.Random.Range(-90f, 90f); // Adjust within a 45-degree cone
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 adjustedDirection = rotation * directionAwayFromTarget;
        float repositionDistance = _enemyInfo.GetRepositionDistance();
        Vector3 newPosition = transform.position + adjustedDirection * repositionDistance;
        newPosition = FindNavMeshPosition(newPosition);
        _agent.speed = _enemyInfo.GetRepositionSpeed();
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

    //if currently in a state where next decision can be an attack, first decide whether an attack makes sense before deciding that you definitely will attack
    //that way can decouple the deciding of the type of the attack. So say, does an attack make sense here based on my available attacks and 

    private EnemyState DecideNextCombatStep(){
        //chase, equip, unequip will be decided only if enemy decides to attack first
        Dictionary<EnemyState, float> possibleTransitions;
        switch (currentEnemyState){
            case EnemyState.Alert:
                possibleTransitions = new Dictionary<EnemyState, float>
                {{EnemyState.Turn, 1}};
                break;
            case EnemyState.Turn:
                possibleTransitions = new Dictionary<EnemyState, float>
                {{EnemyState.StareDown, 100}, {EnemyState.AttackA, 50}, {EnemyState.Reposition, 50}};
                break;
            case EnemyState.StareDown:
                possibleTransitions = new Dictionary<EnemyState, float>
                {{EnemyState.AttackA, 50}, {EnemyState.Reposition, 50}};
                break;
            case EnemyState.Chasing:
                break;
            case EnemyState.AttackA:
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
            case EnemyState.Approach: //wouldn't be a transition i come from because will always attack after an approach
                break;
            default:
                Debug.Log("Don't know what to decide in combat!");
                break;
        }
        EnemyState decision = DecidedWeightedCombatDecision(possibleTransitions);
        currentEnemyState = decision;
        if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        switch(decision){
            case EnemyState.Turn:
                StartCoroutine(TurnCoroutine());
                break;
            case EnemyState.StareDown:
                StartCoroutine(StareDownCoroutine());
                break;
            case EnemyState.Chasing:
                break;
            case EnemyState.AttackA:
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
            case EnemyState.Retreat:
                break;
            case EnemyState.Reposition:
                StartCoroutine(RepositionCoroutine());
                break;
            case EnemyState.ReceiveBuff:
                break;
            case EnemyState.TakeHit:
                break;
            case EnemyState.Die:
                break;
            case EnemyState.Approach: //wouldn't be a transition i come from because will always attack after an approach
                break;
            default:
                break;
        }

        //worry about block, dodge, take hit, die later
        //state: possible states coming from
        //alert: from every state. 
        //chase: attack, turn, staredown, approach
        //turn: alert
        //staredown: turn
        //equip: turn, staredown
        //unequip: turn, staredown
        //approach: turn, staredown
        //attack: turn, staredown, unequip, equip
        //reposition: attack
        //receive buff: attack
        //if coming from an alert, attack, dodge, block,
        //if coming from equip or unequip, can go to alert (if current enemy was gone), or attack

        //idea is to get all of the possible different attacks whatever state you are coming from

    }
    private EnemyState DecidedWeightedCombatDecision(Dictionary<EnemyState, float> possibleTransitions){
        // at very minimum, need long range vs melee range, aggressive vs patient, smart vs dumb, combo tendency vs not, 
        // e.g. 
        // MODIFIERS:
        // aggression (-1 to 1):
            // weight probability of doing an attack (intuition: vs. retreating or staring down)
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

        
        float totalWeight = 0f;
        foreach(var key in possibleTransitions.Keys){
            switch(key){
                case EnemyState.Turn:
                    break;
                case EnemyState.StareDown:
                    break;
                case EnemyState.Chasing:
                    //if target is running away 
                    break;
                case EnemyState.AttackA:
                    possibleTransitions[key] += possibleTransitions[key] * _enemyInfo.GetAggression();
                    break;
                case EnemyState.Equip:
                    break;
                case EnemyState.Unequip:
                    break;
                case EnemyState.Dodge:
                    break;
                case EnemyState.Block:
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
                case EnemyState.Approach:
                    break;
                default:
                    break;
            }
        }
        foreach (var item in possibleTransitions){totalWeight += item.Value;}
        //normalize weights to add up to 1
        foreach (var key in possibleTransitions.Keys){possibleTransitions[key] = possibleTransitions[key]/totalWeight;}

        float randomValue = UnityEngine.Random.Range(0f, 1f);
        float cumulativeProbability = 0f;
        foreach (var item in possibleTransitions){
            cumulativeProbability += item.Value;
            if (randomValue <= cumulativeProbability){
                if (item.Key != EnemyState.AttackA){return item.Key;}
                else{
                    //need to determine which attack to use, and then determine if going to attack immediately or do some other stuff before
                    BaseAttackScript attack = DecideAttack();
                    

                    //based on attack, needs to either equip, unequip, approach (if in melee range), retreat (get further away to do attack),
                    // or approach fast(seconds before a melee attack) 
                }
            }
        }
        Debug.Log("SHOULD NEVER GET HERE!");
        return EnemyState.Idle;
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
        float playerProximityRatio = Mathf.Clamp(distanceToPlayer/aggroDistance, 0f, 1f); //always <= 1
        Dictionary<BaseAttackScript, float> originalWeights = new Dictionary<BaseAttackScript, float>();

        /*
        accounting for if weapon is out, distance to player, attack range preference of enemy, 
        */
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
            attack.SetAttackWeight(adjustedWeight);
        }

        // Reset to original weights
        foreach (var kvp in originalWeights){kvp.Key.SetAttackWeight(kvp.Value);}


        if(playerProximityRatio < 0.33f){
            if(hasMelee){return GetAttack(meleeAttacks);}
            else if(hasMedium){return GetAttack(mediumAttacks);}
            else{return GetAttack(longAttacks);}
        }
        else if(playerProximityRatio < 0.66){
            if(hasMedium){return GetAttack(mediumAttacks);}   
            else if(hasLong){return GetAttack(longAttacks);}
            else{return GetAttack(meleeAttacks);}
        }
        else{
            if(hasLong){return GetAttack(longAttacks);}
            else if(hasMedium){return GetAttack(mediumAttacks);}
            else{return GetAttack(meleeAttacks);} 
        }
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


    IEnumerator StareDownRival(){ //this is to look at the player before you attack them, and pause for a bit
        // job of this function is to turn towards the player, decide if doing a chained attack, 
        // decide the attack, set animation of the attack, perform the attack and the attack animation will then call this function again
        _enemyAttackManager.HandleAnimations(isWeaponOut);


        
        // AnimationAttackEvent = null; //unsubscribe all subscribers from animationattackevent at this step too just in case
        currentEnemyState = EnemyState.StareDown;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        yield return new WaitForSeconds(0.1f);
        if(currentTarget){
            //Debug.Log("CountMeleeInRow: " + countMeleeInRow + "     CountMediumInRow: " + countMediumInRow + "       CountLongInRow: " + countLongInRow);
            _agent.ResetPath();
            bool isChainAttack = UnityEngine.Random.value < _enemyInfo.GetChainProbability();
            float waitTime = _enemyInfo.GetWaitTimeAfterAttack();
            chaseGiveUpTime = _enemyInfo.GetChaseGiveUpTime();
            if(isChainAttack){
                waitTime = 0f;
                chaseGiveUpTime = 2f;
            }
            float elapsedTime = 0f;
            while(elapsedTime < waitTime){
                if(currentTarget){
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                else{
                    BecomeAlert();
                    yield break;
                }
            }
            bool isCurrentPlayerVisible = IsVisible();
            if (isCurrentPlayerVisible){
                //we have rotated towards the current target after a time, and the current target exists, so we are safe to decide an attack
                
                attackChosen = DecideAttack();
                //Debug.Log("Chosen Attack: " + attackChosen);
                if(attackChosen.DoesRequireWeapon() && !isWeaponOut){
                    //Debug.Log("UNSHEATHE WEAPON!");
                    currentEnemyState = EnemyState.UnsheathWeapon;
                    _anim.SetInteger("EnemyState", (int)currentEnemyState);
                    isWeaponOut = true;
                    AnimationAttackEvent += EndOfSheatheUnsheatheAnimation;
                }
                else if(!attackChosen.DoesRequireWeapon() && isWeaponOut){
                    //Debug.Log("SHEATHE WEAPON!");
                    currentEnemyState = EnemyState.SheathWeapon;
                    _anim.SetInteger("EnemyState", (int)currentEnemyState);
                    isWeaponOut = false;
                    AnimationAttackEvent += EndOfSheatheUnsheatheAnimation;
                }
                else{
                    //Debug.Log("NEITHER SHEATHE NOR UNSHEATHE!");
                    PerformAttack(attackChosen, chaseGiveUpTime);
                }
            }
            else{
                StartCoroutine(ChaseUntilVisible());
                yield break;
            }
        }
        else{
            BecomeAlert();  //goes from staredown to alert if there is no current player targeted. This is because there still might be people in range of him
            yield break;
        }
    }
    private void EndOfSheatheUnsheatheAnimation(object sender, AttackEvent e){ //this will be invoked from animationattackevent in unsheathe and sheathe animations
        AnimationAttackEvent -= EndOfSheatheUnsheatheAnimation;
        PerformAttack(e.AttackChosen, e.ChaseGiveUpTime);
    }
    private void OnDisable(){AnimationAttackEvent = null;}
    private void PerformAttack(BaseAttackScript attackChosen, float chaseGiveUpTime){
        currentEnemyState = EnemyState.DecideAttack;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        AnimationAttackEvent = null; //unsubscribe all subscribers from animationattackevent before each new attack
        attackChosen.HandleAnimation();
        
        if(attackChosen.DoesRequireWeapon()){
            List<Transform> currWeapons = _enemyAttackManager.GetWeaponsEquipped();
            if(currWeapons.Count == 1){ //single handed weapon
                _enemyAttackManager.SetParentOfTransform(currWeapons[0], _enemyInfo.GetRightHandTransform(), 
                attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset());
            }
            else{ //double handed weapon
                _enemyAttackManager.SetParentOfTransform(currWeapons[0], _enemyInfo.GetRightHandTransform(), 
                attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset());
                _enemyAttackManager.SetParentOfTransform(currWeapons[1], _enemyInfo.GetLeftHandTransform(), 
                attackChosen.GetSecondWeaponPositionOffset(), attackChosen.GetSecondWeaponRotationOffset());
            }
        }
        switch(attackChosen.GetAttackType()){
            case 1: //for melee
                Chase(chaseGiveUpTime);
                break;
            case 2: //for medium
                StartCoroutine(TransitionToAttack(2));
                break;
            case 3: //for long
                StartCoroutine(TransitionToAttack(3));
                break;
        }
    }
    private IEnumerator TransitionToAttack(int mode){
        yield return new WaitForSeconds(0.1f);
        if(currentTarget){
            if(mode == 2){
                currentEnemyState = EnemyState.MediumAttack;
                _anim.SetInteger("EnemyState", (int)currentEnemyState);
                countMediumInRow ++;
                countLongInRow =0;
                countMeleeInRow =0;
            }
            else if(mode == 3){
                currentEnemyState = EnemyState.LongRangeAttack;
                _anim.SetInteger("EnemyState", (int)currentEnemyState);
                countLongInRow ++;
                countMediumInRow =0;
                countMeleeInRow =0;
            }
        }
        else{
            BecomeAlert();
        }
    }
    private void Chase(float chaseTime){
        _agent.speed = chasingSpeed;
        chaseCoroutine = StartCoroutine(ChaseCoroutine(chaseTime));
    }
    private IEnumerator ChaseCoroutine(float chaseTime){
        //need to give a few frames for thenavmesh to calculate the path to the player, otherwise remaining distance will be zero
        _agent.SetDestination(currentTarget.position);
        yield return new WaitForSeconds(0.1f);
        if(_agent.remainingDistance < _agent.stoppingDistance){ //can immediately attack, doesn't need to go to chase animation
            currentEnemyState = EnemyState.MeleeAttack;
            _anim.SetInteger("EnemyState", (int)currentEnemyState); 
        }
        else{
            currentEnemyState = EnemyState.Chasing;
            if(isWeaponOut){_enemyAttackManager.HandleWeaponShieldPosition(currentEnemyState);}
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            float elapsedTime = 0f;

            while(_agent.remainingDistance > _agent.stoppingDistance && elapsedTime < chaseTime){
                if(currentTarget){
                    _agent.SetDestination(currentTarget.position);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                else{
                    BecomeAlert();
                    CancelChase();
                    yield break;
                }
            }
            // either in range of target or have to give up chasing after specified time
            if(currentTarget){
                if(elapsedTime < chaseTime){
                    currentEnemyState = EnemyState.MeleeAttack;
                    _anim.SetInteger("EnemyState", (int)currentEnemyState);
                    countMeleeInRow ++;
                    countLongInRow =0;
                    countMediumInRow =0;        
                }
                else{
                    List<BaseAttackScript> filteredMediumAttacks;
                    List<BaseAttackScript> filteredLongAttacks;
                    if(isWeaponOut){
                        filteredMediumAttacks = mediumAttacks.FindAll(attack => attack.DoesRequireWeapon());
                        filteredLongAttacks = longAttacks.FindAll(attack => attack.DoesRequireWeapon());
                        if(filteredMediumAttacks.Count > 0){
                            PerformAttack(GetAttack(filteredMediumAttacks), 0); //number doesn't matter because only used for melee attacks
                        }
                        else if(filteredLongAttacks.Count > 0){PerformAttack(GetAttack(filteredLongAttacks), 0);}
                        else{StartCoroutine(StareDownRival());}
                    }
                    else{ //weapon is not out
                        filteredMediumAttacks = mediumAttacks.FindAll(attack => !attack.DoesRequireWeapon());
                        filteredLongAttacks = longAttacks.FindAll(attack => !attack.DoesRequireWeapon());
                        if(filteredMediumAttacks.Count > 0){
                            PerformAttack(GetAttack(filteredMediumAttacks), 0); //number doesn't matter because only used for melee attacks
                        }
                        else if(filteredLongAttacks.Count > 0){PerformAttack(GetAttack(filteredLongAttacks), 0);}
                        else{StartCoroutine(StareDownRival());}
                    }
                }
            }
            else{BecomeAlert();}
            CancelChase();
        }
    }
    private Vector3 FindNavMeshPosition(Vector3 position){
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(position.x, 500f, position.z), Vector3.down, out hit, Mathf.Infinity)){   
            Debug.DrawRay(new Vector3(position.x, 500f, position.z), Vector3.down * 500f, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 100f, NavMesh.AllAreas)){return navHit.position;} // Return the valid NavMesh position
        }
        return position;
    }
    private Vector3 GetNewRoamingPosition(){
        Vector3 randDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        float roamingRange = UnityEngine.Random.Range(minRoamingRange, maxRoamingRange);
        Vector3 newPos = startPosition + (randDir * roamingRange);
        return FindNavMeshPosition(newPos);
    }
    private void CancelChase(){
        if (chaseCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
    }
    private IEnumerator ChaseUntilVisible(){
        currentEnemyState = EnemyState.Chasing;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        _agent.speed = chasingSpeed;
        while(currentTarget && !IsVisible()){
            _agent.SetDestination(currentTarget.position);
            yield return null;
        }
        if(!currentTarget){
            BecomeAlert();
            yield break;
        }
        //reached here means currtarget still exists and we have made eye contact with him
        else{StartCoroutine(StareDownRival());}
    }
    private bool IsVisible(){
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
        AnimationAttackEvent?.Invoke(this, new AttackEvent{AttackChosen = attackChosen, ChaseGiveUpTime = chaseGiveUpTime, TargetTransform=currentTarget, AttackCenterForward=attackCenter, TargetL=targetL});
    }
    private void SlowAnim(){SetAnimationSpeed(0);} // Slow mode
    private void SpeedAnim(){SetAnimationSpeed(1);} // Fast mode
    private void VarySpeedAnim(){
        int mode = UnityEngine.Random.value < 0.5f ? 0 : 1; // Randomly pick slow (0) or fast (1)
        SetAnimationSpeed(mode);
    }
    private void SetAnimationSpeed(int mode){
        if (speedCoroutine != null){StopCoroutine(speedCoroutine);}
        float pauseTime = UnityEngine.Random.value;
        speedCoroutine = StartCoroutine(ChangeSpeedAnimator(pauseTime, mode));
    }
    private IEnumerator ChangeSpeedAnimator(float pauseTime, int mode){
        float newSpeed;
        float newPauseTime = pauseTime;
        if (mode == 0){ // slow mode
            newSpeed = UnityEngine.Random.Range(0.2f, 0.8f);
        }
        else if (mode == 1){ // fast mode
            newSpeed = UnityEngine.Random.Range(1.2f, 1.8f);
        }
        else{ // normal mode
            newSpeed = 1f; // Default speed
        }
        bool animChange = UnityEngine.Random.value < _enemyInfo.GetAnimChangeProbability();
        if(!animChange){
            newSpeed = 1f;
            newPauseTime = 0f;
        }
        //Debug.Log($"Setting animation speed to {newSpeed} for {newPauseTime} seconds.");
        _anim.speed = newSpeed;
        yield return new WaitForSeconds(newPauseTime);
        _anim.speed = 1f; 
        speedCoroutine = null;
    }
    public Transform GetEyesTransform(){return eyes;}
    public Transform GetFeetTransform(){return feet;}
}