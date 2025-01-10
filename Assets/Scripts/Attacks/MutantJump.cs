using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class MutantJump : BaseAttackScript{

    private float jumpUpDuration = 0.4f;
    private float jumpDownDuration = 0.3f;
    private bool hasReachedPeakOfJump;
    private NavMeshAgent _agent;
    private Rigidbody _rb;
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ //in this case, its the start of the jump
        base.ExecuteAttack(sender, e);
        Debug.Log(_enemyScript.name);
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += JumpUp;
        _agent = _enemyGameObject.GetComponent<NavMeshAgent>();
        _rb = _enemyGameObject.GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        hasReachedPeakOfJump = false;
        StartCoroutine(UtilityFunctions.LookAtCoroutine(_enemyGameObject.transform, e.TargetTransform, ()=>hasReachedPeakOfJump));
    }
    private void JumpUp(object sender, EnemyAI4.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= JumpUp;
        _enemyScript.AnimationAttackEvent += CrashDown;
        Vector3 endDestination = e.TargetTransform.position + Vector3.up * 30f;
        Vector3 origin = _enemyGameObject.transform.position;
        Vector3 destination = origin + (endDestination - origin) * 0.7f;
        _agent.enabled = false;
        StartCoroutine(UtilityFunctions.MoveWithGravityRigidbody(_enemyGameObject.GetComponent<Rigidbody>(), destination, jumpUpDuration));
    }

    private void CrashDown(object sender, EnemyAI4.AttackEvent e){ //in this case, its the end of the jump
        _enemyScript.AnimationAttackEvent -= CrashDown;
        hasReachedPeakOfJump = true;
        StartCoroutine(JumpDown(_enemyGameObject.transform, e.TargetTransform));
    }

    private IEnumerator JumpDown(Transform enemyTransform, Transform playerTransform){        
        Vector3 landPosition = UtilityFunctions.FindNavMeshPosition(playerTransform.position, enemyTransform.position);
        yield return StartCoroutine(UtilityFunctions.MoveWithGravityRigidbody(_enemyGameObject.GetComponent<Rigidbody>(), landPosition, jumpDownDuration));
        _agent.enabled = true;
        _rb.isKinematic = false;
    }

    
}
