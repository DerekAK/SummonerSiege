using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class EnemyAI3 : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _anim;
    private AnimatorOverrideController _animOverrider;
    private SphereCollider _aggroCollider;
    private List<Transform> playersInGame;
    private List<Transform> playersInRangeOfEnemy = new List<Transform>();
    private Transform currentPlayerTarget;
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
    [SerializeField] private float attackForceMultiplier = 1000f;
    private enum EnemyState{Idle = 0, Roaming = 1, Alert = 2, Chasing = 3, Attack1 = 4, Attack2 = 5, Attack3 = 6, StareDown = 7} //need an animation for each state, since animation is determined by this
    private enum AttackType{Attack1=1, Attack2=2, Attack3=3} //Generally, 1 is for melee, 2 is for medium, 3 is for far

    [SerializeField] private BaseAttackScript attack1;
    [SerializeField] private BaseAttackScript attack2;
    [SerializeField] private BaseAttackScript attack3;

    private void Awake(){
        _anim = GetComponent<Animator>();
        _animOverrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = (aggroDistance-1)/transform.localScale.y;
        _rightHand = transform.Find("RiggedEarthGuardian/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/mixamorig:RightHandIndex1/mixamorig:RightHandIndex2");
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        Debug.Log(_rightHand);
        Debug.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y+enemyHeight, transform.position.z), Color.red, 10f);
        attackCenterBoxRadius = enemyHeight/2; 
        _agent.stoppingDistance = 20f; //THIS IS REALLY IMPORTANT TO GET RIGHT
    }
    private void Start(){
        attack1.SetAnimationClip(_animOverrider);
        attack2.SetAnimationClip(_animOverrider);
        attack3.SetAnimationClip(_animOverrider);

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

        //check if the currentplayertarget still exists, otherwise need to go back to being alert
        if(currentPlayerTarget){
            float rotationDuration = 2f; 
            
            Vector3 directionToPlayer = (currentPlayerTarget.position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            float timeElapsed = 0f;
            while (timeElapsed < rotationDuration)
            {
                directionToPlayer = (currentPlayerTarget.position - transform.position).normalized;
                targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, timeElapsed / rotationDuration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRotation;
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

        if(playerProximityRatio < 0.33f){
            Chase();
        }
        else if(playerProximityRatio < 0.66){
            currentEnemyState = EnemyState.Attack2;
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            attack2.ExecuteAttack(currentPlayerTarget, _rightHand);
        }
        else{
            currentEnemyState = EnemyState.Attack3;
            _anim.SetInteger("EnemyState", (int)currentEnemyState);
            attack3.ExecuteAttack(currentPlayerTarget, _rightHand);
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
        attack1.ExecuteAttack(currentPlayerTarget, _rightHand);; //will call BecomeAlert()
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
    private void TrackHits(){ //triggered by animation event
        Collider[] hitColliders = Physics.OverlapBox(attackCenter.position, Vector3.one * attackCenterBoxRadius, attackCenter.rotation, playerL);
        
        foreach (Collider hitCollider in hitColliders)
        {
            //Debug.Log("Hit: " + hitCollider.name);
            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            Vector3 direction = (attackCenter.position-transform.position).normalized;
            rb.AddForce(new Vector3(direction.x*attackForceMultiplier, attackForceMultiplier/3, direction.z*attackForceMultiplier), ForceMode.Impulse);

            //rb.AddForce(direction*30f, ForceMode.VelocityChange);
            Debug.DrawRay(transform.position, direction, Color.red, 3f);
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
    void OnDrawGizmos()
    {
        if (attackCenter != null)
        {
            Gizmos.color = Color.red;
            // Draw a wireframe box to represent the OverlapBox area
            //this sets the origin of the next gizmos command to attackcenter.position, with the rotation of the attack center, and a scale of one (no scaling)
            Gizmos.matrix = Matrix4x4.TRS(attackCenter.position, attackCenter.rotation, Vector3.one); 
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one*attackCenterBoxRadius);  // Use the box size and center position
        }
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

    public Animator GetAnimator(){
        return _anim;
    }
}
