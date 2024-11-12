using System.Collections;
using UnityEngine;

public class StoneBoulder : BaseAttackScript
{
    [SerializeField] private Transform pfStoneBoulder;
    [SerializeField] private AnimationClip clip;
    private Rigidbody _rbStone;
    private float _rbMass;
    private float _rbForceMultiplier;
    private EnemyAI3 parentScript;
    private Transform currBoulder;
    private bool hasThrownBoulder;
    //first attacks are only called with current player target, but for one attack that triggers multiple animation events that depend on current player target like this one, need this 
    private Vector3 lastSeenTargetPosition; 
    private Coroutine rotateCoroutine;
    
    public void Start(){
        parentScript.Attack3Event += ExecuteAttack;
    }
    public override void SetAnimationClip(AnimatorOverrideController overrideController){overrideController[ph3] = clip;}
    public override void ProvideInstance(EnemyAI3 script){parentScript = script;}
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){ //can be null here 
        parentScript.Attack3Event -= ExecuteAttack;
        parentScript.Attack3Event += ReleaseBoulder;
        lastSeenTargetPosition = e.LastSeenTargetPos;
        hasThrownBoulder = false;
        if(currBoulder){Destroy(currBoulder.gameObject);}
        Transform projectileInstantiation = e.InstantiateTransform;
        currBoulder = Instantiate(pfStoneBoulder, projectileInstantiation.position, Quaternion.identity);
        currBoulder.SetParent(projectileInstantiation);
        _rbStone = currBoulder.GetComponent<Rigidbody>();
        _rbMass = _rbStone.mass;
        _rbForceMultiplier = _rbMass * 120;
        _rbStone.isKinematic = true;
        rotateCoroutine = StartCoroutine(RotateTowardsPlayerUntilThrown(e.PlayerTransform));
    }
    private IEnumerator RotateTowardsPlayerUntilThrown(Transform playerTransform)
    {
        // Keep rotating towards the player until hasThrownBoulder is true
        while (!hasThrownBoulder)
        {
            if(playerTransform == null){
                parentScript.transform.LookAt(new Vector3(lastSeenTargetPosition.x, parentScript.transform.position.y, lastSeenTargetPosition.z));
                yield return null;
            }
            else{
                parentScript.transform.LookAt(new Vector3(playerTransform.position.x, parentScript.transform.position.y, playerTransform.position.z));
                lastSeenTargetPosition = playerTransform.position;
                yield return null;
            }
        }
    }
    private void ReleaseBoulder(object sender, EnemyAI3.AttackEvent e){ //subscriber to the Attack3 event in enemyai3script
        parentScript.Attack3Event -= ReleaseBoulder;
        parentScript.Attack3Event += ExecuteAttack;
        Debug.Log("RELEASE BOULDER!");
        hasThrownBoulder = true;
        StopCoroutine(rotateCoroutine);
        _rbStone.isKinematic = false;
        currBoulder.SetParent(null);
        Vector3 playerPosition;
        if (e.PlayerTransform == null) {//can be null because might have moved out of range after ExecuteAttack();
            playerPosition = lastSeenTargetPosition;
        }
        else{playerPosition = e.PlayerTransform.position;}
        Vector3 directionToPlayer = (playerPosition+(Vector3.up*10) - e.InstantiateTransform.position).normalized;
        _rbStone.AddForce(directionToPlayer * _rbForceMultiplier, ForceMode.Impulse);
    }
}
