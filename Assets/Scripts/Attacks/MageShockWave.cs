using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MageShockWave : BaseAttackScript
{
    private bool hasStartCrashedDown;
    private Transform _rightHand;
    private NavMeshAgent _agent;

    private void Awake(){
        _enemyScript = GetComponent<EnemyAI3>();
        _agent = GetComponent<NavMeshAgent>();
        OverrideClip();
    }
    public override void ExecuteAttack(object sender, EnemyAI3.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += EndRotation;
        Debug.Log("Do Something!");

        EnemySpecificInfo enemyInfo = GetComponent<EnemySpecificInfo>();
        hasStartCrashedDown = false;
        StartCoroutine(RotateTowardsPlayerUntilCrash(e.TargetTransform));
    }

    private IEnumerator RotateTowardsPlayerUntilCrash(Transform targetTransform)
    {
        while (!hasStartCrashedDown)
        {
            _agent.SetDestination(targetTransform.position);
            transform.LookAt(new Vector3(targetTransform.position.x, transform.position.y, targetTransform.position.z));
            yield return null;
        }
    }

    private void EndRotation(object sender, EnemyAI3.AttackEvent e){ 
        _agent.ResetPath();
        _enemyScript.AnimationAttackEvent -= EndRotation;
        hasStartCrashedDown = true;
    }
}
