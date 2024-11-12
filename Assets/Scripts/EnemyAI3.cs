using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
public class EnemyAI3 : MonoBehaviour
{
    //current problems so far:
    //enemy will get stuck if enemy is raised on box because of nav mesh navigation
    //if player is in air and in the 2nd tier, it will jump after them and then be out of nav mesh
    //it will do its attacks even if there isn't a clear line of sight, which it shouldn't. it should first walk until there is a clear line of sight.
    //boudler throw is at the player's transform, not the player's eyes.
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private SphereCollider _aggroCollider;
    private List<Transform> playersInGame;
    private List<Transform> playersInRangeOfEnemy = new List<Transform>();
    private Transform currentPlayerTarget;
    private Vector3 lastSeenTargetPosition;
    private EnemyState currentEnemyState;
    private Vector3 startPosition;
    private Vector3 roamPosition;
    private Coroutine idleCoroutine;
    private Coroutine alertCoroutine;
    private Coroutine chaseCoroutine;
    private float attackCenterBoxRadius;
    private float enemyHeight;
    private Transform _rightHand;
    [SerializeField] private LayerMask obstacleL;
    [SerializeField] private LayerMask playerL;
    [SerializeField] private Transform attackCenter;
    [SerializeField] private Transform eyes;
    [SerializeField] private float aggroDistance = 70f;
    [SerializeField] private float minRoamingRange = 50f;
    [SerializeField] private float maxRoamingRange = 70f;
    [SerializeField] private int minIdleTime = 5;
    [SerializeField] private int maxIdleTime = 20;
    [SerializeField] private int roamingSpeed = 10;
    [SerializeField] private int chasingSpeed = 15;
    private enum EnemyState{Idle = 0, Roaming = 1, Alert = 2, Chasing = 3, Attack1 = 4, Attack2 = 5, Attack3 = 6, StareDown = 7} //need an animation for each state, since animation is determined by this
    [SerializeField] private BaseAttackScript attack1;
    [SerializeField] private BaseAttackScript attack2;
    [SerializeField] private BaseAttackScript attack3;

    //idea for this is that the animation triggers a function which invokes one of these events. 
    public class AttackEvent : EventArgs{
        public Transform PlayerTransform{ get;set;}
        public Transform InstantiateTransform{ get;set;}
        public Vector3 LastSeenTargetPos{ get;set;}
        public Transform AttackCenterForward{ get;set;}
        public LayerMask PlayerL{get;set;}
    }
    public event EventHandler<AttackEvent> Attack1Event;
    public event EventHandler<AttackEvent> Attack2Event;
    public event EventHandler<AttackEvent> Attack3Event;

    private void Awake(){
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = (aggroDistance-1)/transform.localScale.y;
        _rightHand = transform.Find("RiggedEarthGuardian/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/mixamorig:RightHandIndex1/mixamorig:RightHandIndex2");
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        attackCenterBoxRadius = enemyHeight/2; 
        _agent.stoppingDistance = 5f; //THIS IS REALLY IMPORTANT TO GET RIGHT

        attack1.SetAnimationClip(_animOverrider);
        attack1.ProvideInstance(this);
        attack2.SetAnimationClip(_animOverrider);
        attack2.ProvideInstance(this);
        attack3.SetAnimationClip(_animOverrider);
        attack3.ProvideInstance(this);
    }
    private void Start(){
        playersInGame = GameManager.Instance.getPlayerTransforms();
        startPosition = transform.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }
    private void Update(){
        Debug.Log("Count of inRange Players: " + playersInRangeOfEnemy.Count);
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
        if(other.CompareTag("Player")){
            playersInRangeOfEnemy.Add(other.transform);
            Debug.Log("Got IN range!");
            if(currentEnemyState == EnemyState.Idle || currentEnemyState == EnemyState.Roaming){ //only want to become alert if previously idle or roaming, because could be chasing, attacking etc.
                BecomeAlert();
            }
        }
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
        if(playersInRangeOfEnemy.Count > 0 && currentPlayerTarget == null){ //need to check if currentPlayerTarget is null because don't want to target a different player after each attack, since each attack will potentially go back to alert state
            while(playersInRangeOfEnemy.Count > 0 && currentPlayerTarget == null){
                TryDetectPlayer(); //this will cancel alertocourotine if finds a player, so will not go back to being idle
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            Idle();
        }
        else if(playersInRangeOfEnemy.Count > 0 && currentPlayerTarget){ //people in range but already have a target
            StartCoroutine(StareDownRival());
        }
        else{ //no one in range
            Idle();
        }
    }
    private void TryDetectPlayer(){
        foreach(Transform player in playersInRangeOfEnemy){
            Vector3 directionToPlayer = player.position - eyes.position;
            if (!Physics.Raycast(eyes.position, directionToPlayer.normalized, out RaycastHit hit, directionToPlayer.magnitude, obstacleL))
            {
                // No obstacle, clear line of sight
                Debug.DrawRay(eyes.position, directionToPlayer, Color.red, 10f);
                Debug.Log("DETECTED PLAYER: " + player.name);
                currentPlayerTarget = player; 

                CancelAlert(); //this is to fully transition from alert to attacking
                StartCoroutine(StareDownRival());
                break;
            }
        }
    }
    IEnumerator StareDownRival(){ //this is to look at the player before you attack them, and pause for a bit
        CancelAlert();
        currentEnemyState = EnemyState.StareDown;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);

        //check if the currentplayertarget still exists, otherwise need to go back to being alert. This is important because this is what each attack goes back to, 
        if(currentPlayerTarget){
            float rotationDuration = 2f; 
            float timeElapsed = 0f;
            while (timeElapsed < rotationDuration)
            {
                if(currentPlayerTarget){
                    transform.LookAt(new Vector3(currentPlayerTarget.position.x, transform.position.y, currentPlayerTarget.position.z));
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
                else{
                    BecomeAlert();
                    yield break;
                }
            }
            if(currentPlayerTarget){
                DecideAttack();
            }
            else{
                BecomeAlert();
            }
        }
        else{
            yield return new WaitForSeconds(0.5f);
            BecomeAlert();  //goes from staredown to alert if there is no current player targeted. This is because there still might be people in range of him
        }
    }
    private void DecideAttack(){

        Debug.Log("DECIDE ATTACK!");
        // turn towards the player
        // what information do we need to decide the attack?
        // player speed, player distance, what else?

        //tmp strat is just distance
        float distanceToPlayer = Vector3.Distance(transform.position, currentPlayerTarget.position);
        //3 cases, based on ratio compared to aggrodistance
        Debug.Log("Distanceplayer: " + distanceToPlayer);
        Debug.Log("AggroDistance: " + aggroDistance);

        float playerProximityRatio = distanceToPlayer/aggroDistance; //always <= 1
        Debug.Log(playerProximityRatio);
        lastSeenTargetPosition = currentPlayerTarget.position; //possible that when call executeattack(), especially for attack3's, the animation will start playing only if there is a currentplayertarget, but the executeattack might have a bit of delay in which time the currentplayertarget could exit the aggrosphere. 

        if(playerProximityRatio < 0.33f){
            Chase();
        }
        else if(playerProximityRatio < 0.66){
            currentEnemyState = EnemyState.Attack2;
            _anim.SetInteger("EnemyState", (int)currentEnemyState); //animation will carry out the attack
            // attack2.ExecuteAttack(currentPlayerTarget,transform, _rightHand);
        }
        else{
            currentEnemyState = EnemyState.Attack3; //animation will carry out the attack
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            // attack3.ExecuteAttack(currentPlayerTarget, transform, _rightHand);
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
        _agent.SetDestination(currentPlayerTarget.position);
        yield return new WaitForSeconds(0.5f);
        while(_agent.remainingDistance > _agent.stoppingDistance){
            if(currentPlayerTarget){
                _agent.SetDestination(currentPlayerTarget.position);
                yield return null;
            }
            else{
                BecomeAlert();
                CancelChase();
                yield break;
            }
        }
        currentEnemyState = EnemyState.Attack1;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        //attack1.ExecuteAttack(currentPlayerTarget, transform, _rightHand);; //will call BecomeAlert()
        chaseCoroutine = null;
    }
    private void OnTriggerExit(Collider other){
        if(other.CompareTag("Player")){
            playersInRangeOfEnemy.Remove(other.transform);
            Debug.Log("Got OUT range!");
            if (other.transform == currentPlayerTarget){
                currentPlayerTarget = null;
            }
        } 
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
        Attack1Event?.Invoke(this, new AttackEvent{PlayerTransform=currentPlayerTarget, InstantiateTransform=_rightHand, LastSeenTargetPos=lastSeenTargetPosition, AttackCenterForward=attackCenter, PlayerL=playerL});
    }
    private void AnimationEvent2(){
        Attack2Event?.Invoke(this, new AttackEvent{PlayerTransform=currentPlayerTarget, InstantiateTransform=_rightHand, LastSeenTargetPos=lastSeenTargetPosition, AttackCenterForward=attackCenter, PlayerL=playerL});
    }
    private void AnimationEvent3(){
        Attack3Event?.Invoke(this, new AttackEvent{PlayerTransform=currentPlayerTarget, InstantiateTransform=_rightHand, LastSeenTargetPos=lastSeenTargetPosition, AttackCenterForward=attackCenter, PlayerL=playerL});
    }
}
