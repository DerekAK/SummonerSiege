using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
public class EnemyAI4 : MonoBehaviour
{
    //current problems so far:
    //enemy will get stuck if enemy is raised on box because of nav mesh navigation
    //if player is in air and in the 2nd tier, it will jump after them and then be out of nav mesh
    //it will do its attacks even if there isn't a clear line of sight, which it shouldn't. it should first walk until there is a clear line of sight.
    //boudler throw is at the player's transform, not the player's eyes.

    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Animator _anim;
    private AnimatorOverrideController _templateOverrider;
    private AnimatorOverrideController _copyOverrider;
    private SphereCollider _aggroCollider;
    private List<Transform> playersInGame;
    private List<Transform> targetsInRangeOfEnemy = new List<Transform>();
    private Transform currentTarget;
    private Vector3 lastSeenTargetPosition;
    private EnemyState currentEnemyState;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine idleCoroutine;
    private Coroutine alertCoroutine;
    private Coroutine chaseCoroutine;
    private float attackCenterBoxRadius;
    private float enemyHeight;
    [SerializeField] private LayerMask obstacleL;
    [SerializeField] private LayerMask playerL;
    [SerializeField] private LayerMask enemyL;
    private LayerMask targetL;
    [SerializeField] private string playerTag = "Player";
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
    private enum EnemyState{Idle = 0, Roaming = 1, Alert = 2, Chasing = 3, Attack1 = 4, Attack2 = 5, Attack3 = 6, StareDown = 7} //need an animation for each state, since animation is determined by this

    //idea for this is that the animation triggers a function which invokes one of these events. 
    public class AttackEvent : EventArgs{
        public Transform TargetTransform{ get;set;}
        public Transform AttackCenterForward{ get;set;}
        public LayerMask TargetL{get;set;}
    }
    public event EventHandler<AttackEvent> Attack1Event;
    public event EventHandler<AttackEvent> Attack2Event;
    public event EventHandler<AttackEvent> Attack3Event;

    private void Awake(){
        HandleAnimation(); //sets up a unique copy of the animatorOverrider for each enemy instance
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = (aggroDistance-1)/transform.localScale.y;
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        attackCenterBoxRadius = enemyHeight/2; 
        _agent.stoppingDistance = 5f; //THIS IS REALLY IMPORTANT TO GET RIGHT
        _agent.angularSpeed = 1000f;
        _agent.acceleration = 200f;
        _agent.radius = 0.1f;
        targetTag = playerTag;
        targetL = playerL;
    }
    private void HandleAnimation(){
        _anim = GetComponent<Animator>();
        _templateOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _copyOverrider = new AnimatorOverrideController(_templateOverrider);
        _anim.runtimeAnimatorController = _copyOverrider;
    }
    private void Start(){
        playersInGame = GameManager.Instance.getPlayerTransforms();
        startPosition = feet.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }
    private void Update(){
        Debug.Log("Count of inRange Targets: " + targetsInRangeOfEnemy.Count);
    }
    private void Idle(){
        _agent.ResetPath();
        currentEnemyState = EnemyState.Idle;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        CancelIdle();
        CancelAlert();
        idleCoroutine = StartCoroutine(IdleCoroutine());
    }
    IEnumerator IdleCoroutine(){
        float idleTime = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
        yield return new WaitForSeconds(idleTime);
        idleCoroutine = null;
        Roam();
    }

    private void Roam(){
        //Debug.Log("Roam!");
        currentEnemyState = EnemyState.Roaming;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        _agent.speed = roamingSpeed;
        roamPosition = GetNewRoamingPosition();
        _agent.SetDestination(roamPosition);
        InvokeRepeating(nameof(checkArrivalRoamingPosition), 0.5f, 0.5f); //this is to prevent doing it in update
    }

    private void checkArrivalRoamingPosition(){
        if (_agent.remainingDistance <= _agent.stoppingDistance){
            CancelInvoke(nameof(checkArrivalRoamingPosition));
            Idle();
        }
    }
    private void OnTriggerEnter(Collider other){ //using triggers for player detection for computation optimization, need to set sphere collider to a trigger
        if(other.CompareTag(targetTag)){
            targetsInRangeOfEnemy.Add(other.transform);
            Debug.Log("Got IN range!");
            if(currentEnemyState == EnemyState.Idle || currentEnemyState == EnemyState.Roaming){ //only want to become alert if previously idle or roaming, because could be chasing, attacking etc.
                BecomeAlert();
            }
        }
    }
    private void OnTriggerExit(Collider other){ //can either be in alert, chasing, or attacking state
        if(other.CompareTag(targetTag)){
            if(currentEnemyState == EnemyState.Alert || currentEnemyState == EnemyState.Chasing || currentEnemyState == EnemyState.StareDown){
                Debug.Log("HERE1!");
                targetsInRangeOfEnemy.Remove(other.transform);
                Debug.Log("Got OUT range!");
                if (other.transform == currentTarget){
                    currentTarget = null;
                }
            }
            else{ //is in attacking mode
                Debug.Log("HERE2!");
                StartCoroutine(WaitStopAttacking(other.transform));
            }
        } 
    }
    private IEnumerator WaitStopAttacking(Transform targetTransform){
        while(currentEnemyState != EnemyState.Alert && currentEnemyState != EnemyState.Chasing && currentEnemyState != EnemyState.StareDown){
            yield return null;
        }
        Debug.Log("HERE3!");
        targetsInRangeOfEnemy.Remove(targetTransform);
        if(targetTransform == currentTarget){
            currentTarget = null;
        }
        yield break;
    }

    private void BecomeAlert(){
        _agent.ResetPath(); 
        currentEnemyState = EnemyState.Alert;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
    
        CancelIdle(); //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
        CancelInvoke(nameof(checkArrivalRoamingPosition)); //fully transition from roaming to alert
        CancelAlert();

        alertCoroutine = StartCoroutine(AlertCoroutine());
    }

    //dont want to start a new while loop if there already is a current player target. so if only players in range, don't want to necessarily start detecting bc might have currtarget. if only there's no currtarget, could be that theres no people in range
    private IEnumerator AlertCoroutine(){
        yield return new WaitForSeconds(3f);
        if(targetsInRangeOfEnemy.Count > 0 && currentTarget == null){ //need to check if currentPlayerTarget is null because don't want to target a different player after each attack, since each attack will potentially go back to alert state
            while(targetsInRangeOfEnemy.Count > 0 && currentTarget == null){
                TryDetectPlayer(); //this will cancel alertocourotine if finds a player, so will not go back to being idle
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            Idle();
        }
        else if(targetsInRangeOfEnemy.Count > 0 && currentTarget){ //people in range but already have a target
            StartCoroutine(StareDownRival());
        }
        else{ //no one in range
            Idle();
        }
    }
    private void TryDetectPlayer(){
        foreach(Transform target in targetsInRangeOfEnemy){
            Vector3 directionToTarget;
            if(target.CompareTag(playerTag)){directionToTarget = target.GetComponent<ThirdPersonMovementScript>().GetEyesTransform().position - eyes.position;}
            else{directionToTarget = target.GetComponent<EnemyAI3>().GetEyesTransform().position - eyes.position;}
            if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL))
            {
                // No obstacle, clear line of sight
                Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
                Debug.Log("DETECTED PLAYER: " + target.name);
                currentTarget = target; 

                CancelAlert(); //this is to fully transition from alert to attacking
                StartCoroutine(StareDownRival());
                break;
            }
        }
    }
    IEnumerator StareDownRival(){ //this is to look at the player before you attack them, and pause for a bit
        _agent.ResetPath();
        CancelAlert();
        currentEnemyState = EnemyState.StareDown;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);

        //time to transition between anmation states
        yield return new WaitForSeconds(0.1f);
        //check if the currentplayertarget still exists, otherwise need to go back to being alert. This is important because this is what each attack goes back to, 
        if(currentTarget){
            bool isCurrentPlayerVisible = IsVisible();
            if (isCurrentPlayerVisible){
                float rotationDuration = 1f; 
                float timeElapsed = 0f;
                while (timeElapsed < rotationDuration)
                {
                    if(currentTarget){
                        transform.LookAt(new Vector3(currentTarget.position.x, feet.position.y, currentTarget.position.z));
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    else{
                        BecomeAlert();
                        yield break;
                    }
                }
                if(currentTarget){ //could get out of range after rotate
                    DecideAttack();
                }
                else{
                    BecomeAlert();
                }
            }
            else{
                Debug.Log("CHASE UNTIL VISIBLE!");
                //chase currTarget until he's visible, and then decide attack
                StartCoroutine(ChaseUntilVisible());
            }
        }
        else{
            yield return new WaitForSeconds(0.5f);
            BecomeAlert();  //goes from staredown to alert if there is no current player targeted. This is because there still might be people in range of him
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
        else{directionToTarget = currentTarget.GetComponent<EnemyAI3>().GetEyesTransform().position - eyes.position;}

        if (!Physics.Raycast(eyes.position, directionToTarget.normalized, out RaycastHit hit, directionToTarget.magnitude, obstacleL))
        {
            Debug.DrawRay(eyes.position, directionToTarget, Color.red, 10f);
            return true;
        }
        return false;
    }
    private void DecideAttack(){
        //these steps are necessary if coming from chasing until visible
        Debug.Log("DECIDE ATTACK!");

        float distanceToPlayer = Vector3.Distance(feet.position, currentTarget.position);
        Debug.Log("Distanceplayer: " + distanceToPlayer);
        Debug.Log("AggroDistance: " + aggroDistance);

        float playerProximityRatio = distanceToPlayer/aggroDistance; //always <= 1
        Debug.Log(playerProximityRatio);
        lastSeenTargetPosition = currentTarget.position; //possible that when call executeattack(), especially for attack3's, the animation will start playing only if there is a currentplayertarget, but the executeattack might have a bit of delay in which time the currentplayertarget could exit the aggrosphere. 

        if(playerProximityRatio < 0.33f){
            Chase();
        }
        else if(playerProximityRatio < 0.66){
            currentEnemyState = EnemyState.Attack2;
            _anim.SetInteger("EnemyState", (int)currentEnemyState); //animation will carry out the attack
        }
        else{
            currentEnemyState = EnemyState.Attack3; //animation will carry out the attack
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
        }
    }
    private void Chase(){
        currentEnemyState = EnemyState.Chasing;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        CancelChase();
        _agent.speed = chasingSpeed;
        chaseCoroutine = StartCoroutine(ChaseCoroutine());
    }
    private IEnumerator ChaseCoroutine(){
        //need to give a few frames for thenavmesh to calculate the path to the player, otherwise remaining distance will be zero
        _agent.SetDestination(currentTarget.position);
        yield return new WaitForSeconds(0.5f);
        while(_agent.remainingDistance > _agent.stoppingDistance){
            if(currentTarget){
                _agent.SetDestination(currentTarget.position);
                yield return null;
            }
            else{
                BecomeAlert();
                CancelChase();
                yield break;
            }
        }
        if(currentTarget){
            currentEnemyState = EnemyState.Attack1;
            _anim.SetInteger("EnemyState", (int)currentEnemyState);            
        }
        else{BecomeAlert();}
        CancelChase();
    }
    private Vector3 GetNewRoamingPosition(){
        Vector3 randDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        float roamingRange = UnityEngine.Random.Range(minRoamingRange, maxRoamingRange);
        Vector3 newPos = startPosition + (randDir * roamingRange);
        
        RaycastHit hit; //this is to determine the exact y coordinate of the xz coordinate determined by newpos
        if (Physics.Raycast(new Vector3(newPos.x, 100f, newPos.z), Vector3.down, out hit, Mathf.Infinity))
        {   
            Debug.DrawRay(new Vector3(newPos.x, 100f, newPos.z), Vector3.down * 200f, Color.red, 3f);
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 100f, NavMesh.AllAreas))
            {
                // Successfully found a valid NavMesh position
                //Debug.Log("Sampled NavMesh position at: " + navHit.position);
                return navHit.position; // Return the valid NavMesh position
            }
        }
        return startPosition;
    }
    private void CancelIdle(){
        //fully transition from idle
        if (idleCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }
    private void CancelAlert(){
        //fully transition from alert
        if (alertCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }
    }
    private void CancelChase(){
        //fully transition from alert
        if (chaseCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
    }
    private void AnimationEvent1(){
        Attack1Event?.Invoke(this, new AttackEvent{TargetTransform=currentTarget, AttackCenterForward=attackCenter, TargetL=targetL});
    }
    private void AnimationEvent2(){
        Attack2Event?.Invoke(this, new AttackEvent{TargetTransform=currentTarget, AttackCenterForward=attackCenter, TargetL=targetL});
    }
    private void AnimationEvent3(){
        Attack3Event?.Invoke(this, new AttackEvent{TargetTransform=currentTarget, AttackCenterForward=attackCenter, TargetL=targetL});
    }

    public Transform GetEyesTransform(){
        return eyes;
    }
}