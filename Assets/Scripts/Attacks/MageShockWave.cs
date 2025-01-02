using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MageShockWave : BaseAttackScript
{
    [SerializeField] private float hoverSpeed = 10000f;
    private bool hasStartCrashedDown;
    private Transform _rightHand;
    private NavMeshAgent _agent;
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += EndRotation;

        _agent = _enemyGameObject.GetComponent<NavMeshAgent>();
        hasStartCrashedDown = false;
        StartCoroutine(RotateTowardsPlayerUntilCrash(_enemyScript.GetCurrentTarget()));
    }

    private IEnumerator RotateTowardsPlayerUntilCrash(Transform targetTransform){
        while (!hasStartCrashedDown) {
            Vector3 targetPosition = new Vector3(targetTransform.position.x, _enemyGameObject.transform.position.y, targetTransform.position.z);
            _enemyGameObject.transform.LookAt(targetPosition);
            yield return null;
        }
    }
    private void EndRotation(object sender, EnemyAI4.AttackEvent e){ 
        _agent.ResetPath();
        _enemyScript.AnimationAttackEvent -= EndRotation;
        hasStartCrashedDown = true;
    }
}
