using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SwordComboA : BaseAttackScript
{
    private List<float> moveTimes = new List<float> {0.33f, 0.33f, 0.33f, 0.33f};
    private int moveIndex = 0;
    private int maxMoveIndex = 3; //the attack has 4 moves
    private NavMeshAgent _agent;
    float baseSpeed, baseAcceleration, baseAngularSpeed;

    private bool finishedRotate = false;
    public override void ExecuteAttack(object sender, EnemyAI4.AttackEvent e){ 
        base.ExecuteAttack(sender, e);
        _enemyScript.AnimationAttackEvent -= ExecuteAttack;
        _enemyScript.AnimationAttackEvent += StopRotatePlusMove;
        _agent = _enemyGameObject.GetComponent<NavMeshAgent>();
        StartCoroutine(UtilityFunctions.LookAtCoroutine(_enemyGameObject.transform, e.TargetTransform, ()=>finishedRotate));
    } 
    private void StopRotatePlusMove(object sender, EnemyAI4.AttackEvent e){ 
        Debug.Log("StopRotatePlusMove");
        _enemyScript.AnimationAttackEvent -= StopRotatePlusMove;

        finishedRotate = true;
        StartCoroutine(UtilityFunctions.MoveTowardsPositionSimulated(_enemyGameObject.transform, e.TargetTransform.position, moveTimes[moveIndex], 200, _agent.stoppingDistance));
        moveIndex ++;

        if(moveIndex <= maxMoveIndex){_enemyScript.AnimationAttackEvent += StartRotate;}
    } 
    private void StartRotate(object sender, EnemyAI4.AttackEvent e){ 
        Debug.Log("StartRotate");
        _enemyScript.AnimationAttackEvent -= StartRotate;
        _enemyScript.AnimationAttackEvent += StopRotatePlusMove;

        finishedRotate = false;
        StartCoroutine(UtilityFunctions.LookAtCoroutine(_enemyGameObject.transform, e.TargetTransform, ()=>finishedRotate));
    } 

}