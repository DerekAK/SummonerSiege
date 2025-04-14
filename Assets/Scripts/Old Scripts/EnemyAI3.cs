using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using TMPro;

public class EnemyAI3 : MonoBehaviour{
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
    private List<Transform> targetsInRangeOfEnemy = new List<Transform>();
    private List<BaseAttackScript> attackScripts;
    private Transform currentTarget;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine idleCoroutine;
    private Coroutine alertCoroutine;
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
    private enum EnemyState{Idle = 0, Roaming = 1, Alert = 2, Chasing = 3, DecideAttack = 4, MeleeAttack = 5, MediumAttack = 6, 
    LongRangeAttack = 7, UnsheathWeapon = 8, SheathWeapon = 9, ReceiveWeapon = 10}
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
        _enemyAttackManager = GetComponent<EnemyAttackManager>();
        isWeaponOut = false;

        SetUpAnimator(); //sets up a unique copy of the animatorOverrider for each enemy instance
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = (aggroDistance-1)/transform.localScale.y;
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        attackCenterBoxRadius = enemyHeight/2; 
        _agent.stoppingDistance = 10f; //THIS IS REALLY IMPORTANT TO GET RIGHT
        _agent.angularSpeed = 1000f;
        _agent.acceleration = 200f;
        _agent.radius = 0.1f;
        targetTag = playerTag;
        targetL = playerL;
        _enemyInfo = GetComponent<EnemySpecificInfo>();
    }
    //every enemy instance will get it's own overrider copy
    private void SetUpAnimator(){
        _anim = GetComponent<Animator>();
        _templateOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _copyOverrider = new AnimatorOverrideController(_templateOverrider);
        _anim.runtimeAnimatorController = _copyOverrider;
    }
    private void Start(){
        //attackScripts = _enemyAttackManager.GetCurrentAvailableAttacks();
        startPosition = feet.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }
    private void Update(){
        //Debug.Log("Count of inRange Targets: " + targetsInRangeOfEnemy.Count);
        Debug.Log(currentTarget);
        if(currentTarget != null){transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));}
    }
    private void Idle(){
        //if(isWeaponOut){_enemyAttackManager.HandleWeaponPosition(0);} //0 for idle
        _agent.ResetPath();
        currentEnemyState = EnemyState.Idle;
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
        //if(isWeaponOut){_enemyAttackManager.HandleWeaponPosition(1);} //1 for roaming
        currentEnemyState = EnemyState.Roaming;
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
            if(currentEnemyState == EnemyState.Idle || currentEnemyState == EnemyState.Roaming || currentEnemyState == EnemyState.ReceiveWeapon){ //only want to become alert if previously idle or roaming, because could be chasing, attacking etc.
                BecomeAlert();
            }
        }
    }
    private void OnTriggerExit(Collider other){ //can either be in alert, chasing, decideattack or attacking state
        if(other.CompareTag(targetTag)){
            targetsInRangeOfEnemy.Remove(other.transform); //will remove from list regardless, that way can check later on in waitstopattacking
            if(other.transform == currentTarget){
                Debug.Log("Registered currTarget leaving aggro sphere");
                if(currentEnemyState == EnemyState.DecideAttack || currentEnemyState == EnemyState.Chasing){ //safe to remove current target
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
        while(currentEnemyState != EnemyState.DecideAttack){
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
        //if(isWeaponOut){_enemyAttackManager.HandleWeaponPosition(2);} //2 for alert
        _agent.ResetPath(); 
        currentEnemyState = EnemyState.Alert;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        StopAllCoroutines();
        alertCoroutine = StartCoroutine(AlertCoroutine());
    }

    //dont want to start a new while loop if there already is a current player target. so if only players in range, don't want to necessarily start detecting bc might have currtarget. if only there's no currtarget, could be that theres no people in range
    private IEnumerator AlertCoroutine(){
        yield return new WaitForSeconds(3f);
        if(targetsInRangeOfEnemy.Count > 0){ //need to check if currentPlayerTarget is null because don't want to target a different player after each attack, since each attack will potentially go back to alert state
            while(targetsInRangeOfEnemy.Count > 0 && currentTarget == null){
                TryDetectPlayer(); //this will cancel alertocourotine if finds a player, so will not go back to being idle
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            Idle();
        }
        else{ //no one in range
            Idle();
        }
    }
    private void TryDetectPlayer(){
        foreach(Transform target in targetsInRangeOfEnemy){
            Vector3 directionToTarget;
            if(target.CompareTag(playerTag)){directionToTarget = target.GetComponent<PlayerMovement>().GetEyesTransform().position - eyes.position;}
            else{directionToTarget = target.GetComponent<EnemyAI3>().GetEyesTransform().position - eyes.position;}
            if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL))
            {
                // No obstacle, clear line of sight
                Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
                //Debug.Log("DETECTED PLAYER: " + target.name);
                currentTarget = target; 

                CancelAlert(); //this is to fully transition from alert to attacking
                StartCoroutine(StareDownRival());
                break;
            }
        }
    }
    IEnumerator StareDownRival(){ //this is to look at the player before you attack them, and pause for a bit
        // job of this function is to turn towards the player, decide if doing a chained attack, 
        // decide the attack, set animation of the attack, perform the attack and the attack animation will then call this function again
        //_enemyAttackManager.HandleAnimations(isWeaponOut);
        
        AnimationAttackEvent = null; //unsubscribe all subscribers from animationattackevent at this step too just in case
        currentEnemyState = EnemyState.DecideAttack;
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
        //attackChosen.HandleAnimation();
        
        // if(attackChosen.DoesRequireWeapon()){
        //     List<Transform> currWeapons = _enemyAttackManager.GetWeaponsEquipped();
        //     if(currWeapons.Count == 1){ //single handed weapon
        //         _enemyAttackManager.SetParentOfTransform(currWeapons[0], _enemyInfo.GetRightHandTransform(), 
        //         attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset());
        //     }
        //     else{ //double handed weapon
        //         _enemyAttackManager.SetParentOfTransform(currWeapons[0], _enemyInfo.GetRightHandTransform(), 
        //         attackChosen.GetFirstWeaponPositionOffset(), attackChosen.GetFirstWeaponRotationOffset());
        //         _enemyAttackManager.SetParentOfTransform(currWeapons[1], _enemyInfo.GetLeftHandTransform(), 
        //         attackChosen.GetSecondWeaponPositionOffset(), attackChosen.GetSecondWeaponRotationOffset());
        //     }
        // }
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
    private BaseAttackScript DecideAttack(){
        float distanceToPlayer = Vector3.Distance(feet.position, currentTarget.position);
        float playerProximityRatio = distanceToPlayer/aggroDistance; //always <= 1

        meleeAttacks = attackScripts.FindAll(attack => attack.GetAttackType() == 1);
        mediumAttacks = attackScripts.FindAll(attack => attack.GetAttackType() == 2);
        longAttacks = attackScripts.FindAll(attack => attack.GetAttackType() == 3);
        hasMelee = meleeAttacks.Count > 0;
        hasMedium = mediumAttacks.Count > 0;
        hasLong = longAttacks.Count > 0;

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
    private void Chase(float chaseTime){
        //if(isWeaponOut){_enemyAttackManager.HandleWeaponPosition(3);} //3 for chasing
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
    private Vector3 GetNewRoamingPosition(){
        Vector3 randDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        float roamingRange = UnityEngine.Random.Range(minRoamingRange, maxRoamingRange);
        Vector3 newPos = startPosition + (randDir * roamingRange);
        
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(newPos.x, 100f, newPos.z), Vector3.down, out hit, Mathf.Infinity)){   
            Debug.DrawRay(new Vector3(newPos.x, 100f, newPos.z), Vector3.down * 200f, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 100f, NavMesh.AllAreas)){return navHit.position;} // Return the valid NavMesh position
        }
        return startPosition;
    }
    private void CancelAlert(){
        if (alertCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }
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
        if(currentTarget.CompareTag(playerTag)){directionToTarget = currentTarget.GetComponent<PlayerMovement>().GetEyesTransform().position - eyes.position;}
        else{directionToTarget = currentTarget.GetComponent<EnemyAI3>().GetEyesTransform().position - eyes.position;}

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
    private Transform GetEyesTransform(){
        return eyes;
    }
    public Transform GetFeetTransform(){
        return feet;
    }
}