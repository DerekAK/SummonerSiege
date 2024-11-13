using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class StoneBoulder : BaseAttackScript
{
    [SerializeField] private Transform pfStoneBoulder;
    [SerializeField] private AnimationClip clip;
    private AnimatorOverrideController _overrider;
    private Animator _anim;
    private EnemyAI3 _enemyScript; //all should have, because these are events 
    private Rigidbody rbStone;
    private float rbStoneMass;
    private float rbThrowForceMultiplier;
    private Transform currBoulder;
    private bool hasThrownBoulder;
    //first attacks are only called with current player target, but for one attack that triggers multiple animation events that depend on current player target like this one, need this 
    private Vector3 lastSeenTargetPosition; 
    private Coroutine rotateCoroutine;
    private float forceMultiplier;

    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        _anim = GetComponent<Animator>();
        _overrider = (AnimatorOverrideController)_anim.runtimeAnimatorController;
    }
    private void Start(){
        _enemyScript.Attack3Event += ExecuteAttack;
        _overrider[ph3] = clip;
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ //can be null here 
        _enemyScript.Attack3Event -= ExecuteAttack;
        _enemyScript.Attack3Event += ReleaseBoulder;
        lastSeenTargetPosition = e.LastSeenTargetPos;
        hasThrownBoulder = false;
        if(currBoulder){Destroy(currBoulder.gameObject);}
        Transform projectileInstantiation = e.InstantiateTransform;
        currBoulder = Instantiate(pfStoneBoulder, projectileInstantiation.position, Quaternion.identity);
        currBoulder.SetParent(projectileInstantiation);
        rbStone = currBoulder.GetComponent<Rigidbody>();
        rbStoneMass = rbStone.mass;
        rbThrowForceMultiplier = rbStoneMass * 120;
        rbStone.isKinematic = true;
        currBoulder.GetComponent<SphereCollider>().enabled = false;
        rotateCoroutine = StartCoroutine(RotateTowardsPlayerUntilThrown(e.PlayerTransform));
    }
    private IEnumerator RotateTowardsPlayerUntilThrown(Transform playerTransform)
    {
        // Keep rotating towards the player until hasThrownBoulder is true
        while (!hasThrownBoulder)
        {
            if(playerTransform == null){
                transform.LookAt(new Vector3(lastSeenTargetPosition.x, transform.position.y, lastSeenTargetPosition.z));
                yield return null;
            }
            else{
                transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
                lastSeenTargetPosition = playerTransform.position;
                yield return null;
            }
        }
    }
    private void ReleaseBoulder(object sender, EnemyAI3.AttackEvent e){ //subscriber to the Attack3 event in enemyai3script
        _enemyScript.Attack3Event -= ReleaseBoulder;
        _enemyScript.Attack3Event += ExecuteAttack;
        Debug.Log("RELEASE BOULDER!");
        hasThrownBoulder = true;
        StopCoroutine(rotateCoroutine);
        currBoulder.GetComponent<SphereCollider>().enabled = true;
        rbStone.isKinematic = false;
        currBoulder.SetParent(null);
        Vector3 playerPosition;
        if (e.PlayerTransform == null) {//can be null because might have moved out of range after ExecuteAttack();
            playerPosition = lastSeenTargetPosition;
        }
        else{playerPosition = e.PlayerTransform.position;}
        float distanceToPlayer = Vector3.Distance(playerPosition, transform.position);
        Vector3 directionToPlayer = (playerPosition+(Vector3.up*(distanceToPlayer/6.6f)) - e.InstantiateTransform.position).normalized;
        rbStone.AddForce(directionToPlayer * rbThrowForceMultiplier, ForceMode.Impulse);
    }
}
