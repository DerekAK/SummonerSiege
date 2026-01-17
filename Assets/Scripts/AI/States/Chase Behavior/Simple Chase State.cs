using System;
using UnityEngine;

/*
    Description: Changes speed over a certain {lerpTime} until reaches a constant speed. Is not responsible for handling speed upon exit
*/

[CreateAssetMenu(fileName = "SimpleChaseState", menuName = "Scriptable Objects/AI Behavior/States/Chase/SimpleChaseState")]
public class SimpleChaseState : BaseChasingState
{
    [Tooltip("The speed factor relative to the character's max speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float chaseSpeedFactor = 1;
    [SerializeField] private float lerpTime = 2f;

    private Coroutine lerpSpeedCoroutine;

    public override void EnterState(BehaviorManager behaviorManager)
    {

        if (!behaviorManager.GetComponent<EntityStats>().TryGetStat(StatType.Speed, out NetStat speedStat)) return;
        float startSpeed = speedStat.CurrentValue;
        float endSpeed = speedStat.MaxValue * chaseSpeedFactor;

        lerpSpeedCoroutine = behaviorManager.StartCoroutine(behaviorManager.LerpSpeed(startSpeed, endSpeed, lerpTime));
        // Set the speed for chasing
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
        if(lerpSpeedCoroutine != null)
        {
            behaviorManager.StopCoroutine(lerpSpeedCoroutine);
            lerpSpeedCoroutine = null;
        }
        return;
    }

    public override void UpdateState(BehaviorManager behaviorManager)
    {
        if (behaviorManager.CurrentTarget == null)
        {
            return;
        }

        // Standard chase logic
        Chase(behaviorManager);
    }

    public override void Chase(BehaviorManager behaviorManager)
    {
        behaviorManager.MoveTowardsTarget(behaviorManager.CurrentTarget.transform.position);
    }

}