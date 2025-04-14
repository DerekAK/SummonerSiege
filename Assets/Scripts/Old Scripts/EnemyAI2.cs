using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class EnemyAI2 : MonoBehaviour
{    
    private NavMeshAgent _agent;
    private Animator _anim;
    private SphereCollider _aggroCollider;
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
    [SerializeField] private int aggroDistance;
    [SerializeField] private float minRoamingRange = 50f;
    [SerializeField] private float maxRoamingRange = 70f;
    [SerializeField] private int minIdleTime = 5;
    [SerializeField] private int maxIdleTime = 20;
    [SerializeField] private int roamingSpeed = 10;
    [SerializeField] private int chasingSpeed = 15;
    [SerializeField] private int attackForceMultiplier;
    private enum EnemyState{Idle=0, Roaming=1, Alert=2, Chasing=3, Attacking=4} //need an animation for each state, since animation is determined by this
    private enum AttackType{Melee=0, Medium=1, Long=2}

    public event EventHandler<AttackEventArgs> AttackEvent;
    public class AttackEventArgs : EventArgs{
        public Transform playerTransform;
        public Transform rightHandTransform;
    }
    
    private void Awake(){
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        _aggroCollider.radius = aggroDistance;
        _rightHand = transform.Find("RiggedEarthGuardian/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/mixamorig:RightHandIndex1/mixamorig:RightHandIndex2");
        enemyHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        Debug.Log(_rightHand);
        Debug.DrawLine(transform.position, new Vector3(transform.position.x, transform.position.y+enemyHeight, transform.position.z), Color.red, 10f);
        attackCenterBoxRadius = enemyHeight/2; 
    }
    private void Start(){
        startPosition = transform.position;
        //can set to idle immediately because spawnscript will ensure no enemy is spawned in with a player in its aggrosphere
        Idle();
    }
    private void Update(){
        Debug.Log("Count of inRange Players: " + playersInRangeOfEnemy.Count);
    }
    private void OnTriggerEnter(Collider other){ //using triggers for player detection for computation optimization, need to set sphere collider to a trigger
        if(other.CompareTag("Player")){
            playersInRangeOfEnemy.Add(other.transform);
            Debug.Log("Got IN range!");
            if(currentEnemyState == EnemyState.Idle || currentEnemyState == EnemyState.Roaming){
                BecomeAlert();
            }
        }
    }
    private void BecomeAlert(){
        //Debug.Log("Become Alert");
        /*
        * THIS IS IMPORTANT TO UNDERSTAND:
        * a player entering the sphere collider is what determines whether or not the enemy will start continously checking its list of players
        */
        currentEnemyState = EnemyState.Alert;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        
        //fully transition from idle to alert
        if (idleCoroutine != null){ //this is if the enemy went from idle to alert, need to do this so doesn't go back to roaming
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        if (alertCoroutine != null){ //this actually does happen, in the case of multiple enemies in sphere and targeted one exits the sphere
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }

        //fully transition from roaming to alert, and will make sure that enemy won't go back to idle 
        CancelInvoke(nameof(checkArrivalRoamingPosition));

        alertCoroutine = StartCoroutine(AlertCoroutine());
    }
    private IEnumerator AlertCoroutine(){
        Debug.Log("STARTING ALERT COUROTINE!");
        _agent.ResetPath();
        yield return new WaitForSeconds(3);
        if (playersInRangeOfEnemy.Count > 0) { 
            Debug.Log("This should be running for as long as there are players in range");
            // if(currentEnemyState == EnemyState.Alert){ //this is to make sure that this will only try to detect players if the enemy is not chasing or attacking
            //     TryDetectPlayer();
            // }
            while(currentPlayerTarget == null){
                Debug.Log("Current player Target is null");
                TryDetectPlayer();
            }
        }
        //Debug.Log("This should run when there are no enemies in range");
        //if come from chasing the player to no more players in range
        else{    
            if(chaseCoroutine != null){
                StopCoroutine(chaseCoroutine);
                chaseCoroutine = null;
            }
            alertCoroutine = null;
            //transitions directly from chasing to idle in this case if no enemies are present
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
                // chase player
                AttackType attackType = AttackType.Melee; //do this for now, but later randomize type of attack or have some way of determining which attack to perform
                Chase(player, attackType);
                break;
            }
        }
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
    private void Chase(Transform player, AttackType attack){
        //Debug.Log("Chasing");
        currentEnemyState = EnemyState.Chasing;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        if(chaseCoroutine != null){
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
        Vector3 randomDestination = GetNewRoamingPosition();
        //_agent.SetDestination(player.position); //initial set navmesh agent to player position
        _agent.SetDestination(player.position);
        _agent.speed = chasingSpeed;
        chaseCoroutine = StartCoroutine(ChaseCoroutine(player));
    }
    private IEnumerator ChaseCoroutine(Transform player){
        //need to give a few frames for thenavmesh to calculate the path to the player, toher wise remaining distance will be zero
        yield return new WaitForSeconds(0.5f);
        while(_agent.remainingDistance > _agent.stoppingDistance){
            _agent.SetDestination(player.position);
            yield return null;
        }
        //in attack range
        //this is with new attacksystem
        AttackEvent(this, new AttackEventArgs{playerTransform = currentPlayerTarget, rightHandTransform = _rightHand});
        Attack();
        chaseCoroutine = null;
    }

    private void Attack(){
        currentEnemyState = EnemyState.Attacking;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        //going to have the end of the attack be determined by the end of the animation. That way can't stop an animation mid animation
        StartCoroutine(AttackCoroutine());
    }
    private IEnumerator AttackCoroutine(){ //this is just temporary. We do not want a coroutine for attacking but instead an animator event that has an event
    // for trigger trackhits() and also for triggering becomealert() at the end of the animation
        yield return new WaitForSeconds(1);
        if(currentPlayerTarget){ //this means that player current target is still in range, so want to continue to pursue him
            Chase(currentPlayerTarget, AttackType.Melee);
        }
        else{
            Debug.Log("BECOME ALERT");
            BecomeAlert();
        }
    }

    private void Idle(){
        _agent.ResetPath();
        //Debug.Log("Idle!");
        currentEnemyState = EnemyState.Idle;
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        if (idleCoroutine != null){
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
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
}