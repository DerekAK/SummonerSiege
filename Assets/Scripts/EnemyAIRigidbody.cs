using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Diagnostics.CodeAnalysis;


// idea
// what do we have to call in the update() function? -> just 


public class EnemyAI : MonoBehaviour
{
    //[SerializeField] private Animator _anim;
    private Animator _anim;
    private Coroutine idleCoroutine;
    private Coroutine attackCoroutine;
    private Transform playerTransform;
    [SerializeField] private LayerMask playerL;
    private EnemyState currentEnemyState;
    //[SerializeField] private NavMeshAgent agent;
    private NavMeshAgent _agent;
    private SphereCollider _aggroCollider;
    [SerializeField] private Transform attackCenter;
    [SerializeField] private int attackCenterBoxRadius;
    private float kickDelay = 0.5f; //this is how long the kicking animation has to play before it switches. if too low, will just do a bitch kick
    //keep in mind that this might become a problem if during the coroutine, the player is able to escape the enemyspherecollider, because it goes to 
    //aggro state but the player is already out of the sphere, so will be forever in an aggro state.

    //[SerializeField] private string rightToePath;
    //private SphereCollider _legCollider;

    [SerializeField] private float attackRange;
    [SerializeField] private float aggroDistance;

    //for character roaming
    private Vector3 startPosition;
    private Vector4 roamPosition;
    [SerializeField] private float minRoamingRange = 15f;
    [SerializeField] private float maxRoamingRange = 70f;
    [SerializeField] private int minIdleTime = 5;
    [SerializeField] private int maxIdleTime = 20;
    [SerializeField] private int roamingSpeed = 10;
    [SerializeField] private int chasingSpeed = 15;

    private enum EnemyState{Idle=0, Roaming=1, Aggro=2, Attacking=3}
    //private enum MeleeAttack{Punch=0, Kick=1}

    private void Start(){
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _aggroCollider = GetComponent<SphereCollider>();
        //_legCollider = transform.Find(rightToePath).GetComponent<SphereCollider>(); //This will come up with an error if rightToePath is wrong
        _aggroCollider.radius = aggroDistance;

        playerTransform = GameManager.Instance.getPlayerTransform();
        currentEnemyState = EnemyState.Idle; //for debugging purposes
        startPosition = transform.position;

        //start idling
        if (idleCoroutine != null){
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        idleCoroutine = StartCoroutine(IdleCoroutine());
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

    private void Update(){
        if(currentEnemyState == EnemyState.Aggro){
            Chase();
        }
        Debug.Log("Current Enemy State: " +  currentEnemyState);
        _anim.SetInteger("EnemyState", (int)currentEnemyState);
        //Debug.Log("Current distance from enemy: " + Vector3.Distance(transform.position, playerTransform.position));
        //Debug.DrawLine(transform.position, playerTransform.position, Color.red);
    }

    private void OnTriggerEnter(Collider other){ //using triggers for player detection for computation optimization, need to set sphere collider to a trigger
        if(other.CompareTag("Player")){
            if (idleCoroutine != null){ //enemy was idling when it senses the player
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
            _agent.speed = chasingSpeed;
            currentEnemyState = EnemyState.Aggro;
        }
    }
    private void OnTriggerExit(Collider other){
        if(other.CompareTag("Player")){
            if (idleCoroutine != null){ //should never happen, because shouldn't be idle and chasing at same time, and this is when its chasing
                Debug.Log("THIS SHOULD NEVER HAPPEN 1!");
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
            Roam();
        }
    }
    IEnumerator IdleCoroutine()
    {
        float idleTime = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
        //Debug.Log("Enemy is idling for " + idleTime + " seconds.");
        yield return new WaitForSeconds(idleTime);
        idleCoroutine = null;
        Roam();
    }
    private void Roam(){
        _agent.speed = roamingSpeed;
        currentEnemyState = EnemyState.Roaming;
        roamPosition = GetNewRoamingPosition();
        Debug.Log("Enemy is Roaming.");
        _agent.SetDestination(roamPosition);
        InvokeRepeating(nameof(checkArrivalRoamingPosition), 0f, 0.5f); //this is to prevent doing it in update
    }

    private void checkArrivalRoamingPosition(){
        if (Vector3.Distance(transform.position, roamPosition) < 3f){
            Debug.Log("Arrived at roaming position");
            CancelInvoke(nameof(checkArrivalRoamingPosition));
            
            //transition from roaming back to idle
            currentEnemyState = EnemyState.Idle; //for debugging purposes
            if (idleCoroutine != null){ //just to make sure no other coroutine is occurring, should never happen
                Debug.Log("THIS SHOULD NEVER HAPPEN!2");
                StopCoroutine(idleCoroutine);
                idleCoroutine = null; 
            }
            idleCoroutine = StartCoroutine(IdleCoroutine());
            currentEnemyState = EnemyState.Idle;
        }
    }
    private void Chase(){
        if(Vector3.Distance(transform.position, playerTransform.position) > attackRange){
            _agent.SetDestination(playerTransform.position);
        }
        else{ //inside attack range
            _agent.ResetPath(); //this is to get it to stop moving
            Attack();
        }
    }
    private void Attack(){
        currentEnemyState = EnemyState.Attacking; //sets the attacking animation, which will trigger TrackHits() at frame 10 I believe
        //this is interesting, im learning how to make combat harder and more interesting. In other words, there needs to be enough time in between the 
        //the animation starting and it triggering the event for the player to dodge it or block it or something, and the player needs to be able to roll 
        //out of the way of the overlapbox, so make sure that the overlapbox isn't too large, but then if its too small, the enemy needs to only trigger Attack()
        //when its super close to the character.
        Debug.Log("Enemy is Attacking.");
        if (attackCoroutine != null){
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        attackCoroutine = StartCoroutine(AttackCoroutine(kickDelay));
    }
    IEnumerator AttackCoroutine(float delay){
        yield return new WaitForSeconds(delay);
        attackCoroutine = null;
        currentEnemyState = EnemyState.Aggro; //don't just want to call Chase() here, because Chase() needs to be called in Update() every frame
    }
    private void TrackHits(){ //triggered by animation event
        Debug.Log("HERHERHEHREHRE");
        Collider[] hitColliders = Physics.OverlapBox(attackCenter.position, Vector3.one * attackCenterBoxRadius, attackCenter.rotation, playerL);
        
        foreach (Collider hitCollider in hitColliders)
        {
            Debug.Log("Hit: " + hitCollider.name);
            Rigidbody rb = hitCollider.gameObject.GetComponent<Rigidbody>();
            Vector3 direction = (attackCenter.position-transform.position).normalized;
            rb.velocity = Vector3.zero;
            rb.AddForce(direction*30f, ForceMode.Impulse);
            Debug.Log(direction*30f);
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
            //Debug.DrawRay(new Vector3(newPos.x, 100f, newPos.z), Vector3.down * 200f, Color.red, 3f);
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
}