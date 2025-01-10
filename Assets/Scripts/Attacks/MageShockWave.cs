using UnityEngine;
using UnityEngine.AI;

public class MageShockWave : BaseAttackScript
{
    private float hoverSpeed = 100f;
    private bool hasStartCrashedDown;
    private NavMeshAgent _agent;
    private float jumpDownDuration = 0.2f;
    private float baseSpeed, baseAcceleration, baseAngularSpeed;
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){
        base.ExecuteAttack(sender, e);
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += StartMoving;
        _agent = _enemyGameObject.GetComponent<NavMeshAgent>();
        baseSpeed = _agent.speed;
        baseAcceleration = _agent.acceleration;
        baseAngularSpeed = _agent.angularSpeed;
        hasStartCrashedDown = false;
        //StartCoroutine(UtilityFunctions.MoveTowardsPositionInvolvingAir(_enemyGameObject.transform, _enemyGameObject.transform.position + Vector3.up * 20f, moveUpSpeed, ()=>hasReachedPeak, false));
        StartCoroutine(UtilityFunctions.LookAtCoroutine(_enemyGameObject.transform, e.TargetTransform, ()=>hasStartCrashedDown));
        StartCoroutine(UtilityFunctions.MoveTowardsPositionNavMesh(_enemyGameObject.transform, e.TargetTransform, ()=>hasStartCrashedDown, hoverSpeed));
    }
    private void StartMoving(object sender, EnemyAI4.AttackEvent e){
        _enemyScript.AnimationAttackEvent -= StartMoving;
        _enemyScript.AnimationAttackEvent += EndRotation;
        //StartCoroutine(UtilityFunctions.MoveTowardsMovingTransform(_enemyGameObject.transform, e.TargetTransform, hoverSpeed, ()=>hasStartCrashedDown, true, false));
    }
    private void EndRotation(object sender, EnemyAI4.AttackEvent e){ 
        _enemyScript.AnimationAttackEvent -= EndRotation;
        hasStartCrashedDown = true;
        Vector3 landPosition = UtilityFunctions.FindNavMeshPosition(_enemyGameObject.transform.position, _enemyGameObject.transform.position);
        StartCoroutine(UtilityFunctions.MoveWithGravityRigidbody(_enemyGameObject.GetComponent<Rigidbody>(), landPosition, jumpDownDuration));

        _agent.speed = baseSpeed;
        _agent.acceleration = baseAcceleration;
        _agent.angularSpeed = baseAngularSpeed;
    }
}
