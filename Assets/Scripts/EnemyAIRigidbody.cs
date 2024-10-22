using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;
using Unity.VisualScripting;


// idea
// what do we have to call in the update() function? -> just 


public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Animator _anim;
    private Coroutine idleCoroutine;
    private Transform playerTransform;
    private EnemyState currentEnemyState;
    [SerializeField] private NavMeshAgent agent;

    [SerializeField] private float attackRange;
    //[SerializeField] private float aggroDistance;

    //for character roaming
    private Vector3 startPosition;
    private Vector4 roamPosition;
    [SerializeField] private float minRoamingRange = 10f;
    [SerializeField] private float maxRoamingRange = 70f;
    [SerializeField] private int minIdleTime = 5;
    [SerializeField] private int maxIdleTime = 20;

    private enum EnemyState{Idle, Roaming, Aggro, Attacking}
    private void Start(){
        playerTransform = GameManager.Instance.getPlayerTransform();
        currentEnemyState = EnemyState.Idle;
        startPosition = transform.position;
        roamPosition = GetNewRoamingPosition();
    }

    private void OnTriggerEnter(Collider other){ //using triggers for player detection for computation optimization
        if(other.CompareTag("Player")){
            if (idleCoroutine != null){
                Debug.Log("THIS IS GOOD");
                StopCoroutine(idleCoroutine);
            }
            currentEnemyState = EnemyState.Aggro;
        }
    }
    private void OnTriggerExit(Collider other){
        if(other.CompareTag("Player")){
            idleCoroutine = null;
            currentEnemyState = EnemyState.Idle;
        }
    }
    private void Update(){
        DecideEnemyActionandAnimation();
        //Debug.Log(Vector3.Distance(transform.position, playerTransform.position));
    }
    private void DecideEnemyActionandAnimation(){
        switch(currentEnemyState){
            case EnemyState.Idle:
                // can't call this every frame because will start multiple coroutines
                _anim.SetBool("IsIdle", true);
                _anim.SetBool("IsChasing", false);
                _anim.SetBool("IsRoaming", false);
                if(idleCoroutine == null){
                    Debug.Log("Idle Coroutine is null, so start a new one. This should run everytime roaming stops");
                    idleCoroutine = StartCoroutine(IdleCoroutine());
                }
                break;
            case EnemyState.Roaming:
                //call this every state 
                Roam();
                _anim.SetBool("IsRoaming", true);
                _anim.SetBool("IsChasing", false);
                _anim.SetBool("IsIdle", false);
                break;
            case EnemyState.Aggro:
                //call this every frame because its changing its destination every frame
                Chase();
                _anim.SetBool("IsChasing", true);
                _anim.SetBool("IsRoaming", false);
                _anim.SetBool("IsIdle", false);
                break;
            case EnemyState.Attacking:
                Attack();
                break;
        }
    }
    IEnumerator IdleCoroutine()
    {
        // start idling time
        float idleTime = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
        Debug.Log("Enemy is idling for " + idleTime + " seconds.");
        yield return new WaitForSeconds(idleTime);
        //finished idling for idleTime
        //transition from roaming to idling
        currentEnemyState = EnemyState.Roaming; //for debugging purposes
        idleCoroutine = null;
        roamPosition = GetNewRoamingPosition();
        Roam();
    }
    private void Roam(){
        Debug.Log("Enemy is Roaming.");
        agent.SetDestination(roamPosition);
        if (Vector3.Distance(transform.position, roamPosition) < 1f){
            Debug.Log("Arrived at roaming position");
            
            //transition from roaming back to idle
            currentEnemyState = EnemyState.Idle; //for debugging purposes
            
        }
    }
    private void Chase(){
        Debug.Log("Enemy is Chasing.");
        if(Vector3.Distance(transform.position, playerTransform.position) > attackRange){
            agent.SetDestination(playerTransform.position);
        }
        else{ //inside attack range
            currentEnemyState = EnemyState.Attacking;
        }
    }
    private void Attack(){
        Debug.Log("Enemy is Attacking.");
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