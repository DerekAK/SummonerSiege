using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleChase", menuName = "Scriptable Objects/AI Behavior/Chase/SimpleChase")]
public class SimpleChase : BaseChasingState
{
    private Coroutine chaseCoroutine;
    private float timeToMaxSpeed = 10;
    private EntityStats _entityStats;

    public override void EnterState(BehaviorManager behaviorManager)
    {
        // Give the command to use grounded physics.
        behaviorManager.CurrentLocomotionState = LocomotionState.Grounded;

        EnterChaseState(behaviorManager);
        _entityStats = behaviorManager.GetComponent<EntityStats>();
        _entityStats.TryGetStat(StatType.Speed, out NetStat speed);
        
        _agent.speed = speed.MaxValue;

        if (chaseCoroutine == null)
        {
            chaseCoroutine = behaviorManager.StartCoroutine(UpdateSpeed(timeToMaxSpeed, speed.CurrentValue, speed.MaxValue));
        }
        else
        {
            Debug.Log("Chase coroutine IS NOT NULL AT ENTER STATE!");
        }
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
        if (chaseCoroutine != null)
        {
            behaviorManager.StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
    }

    public override void UpdateState(BehaviorManager behaviorManager)
    {
        if (_agent.isOnOffMeshLink)
        {
            // Here you could switch to a new "JumpAcrossGap" state that
            // sets locomotion to TraversingLink and plays a jump animation.
        }
        else
        {
            Chase(behaviorManager, behaviorManager.CurrentTarget);
        }
    }

    public override void OnCollision(BehaviorManager behaviorManager, Collision collision)
    {
        // Example: When we hit the target, we might switch to an Attack state.
        // behaviorManager.SwitchState(attackState);
    }

    public override void Chase(BehaviorManager behaviorManager, Transform target)
    {
        if (target != null && _agent.isActiveAndEnabled)
        {
            Debug.Log("Setting Agent Destination!");
            _agent.SetDestination(target.position);
        }
    }

    private IEnumerator UpdateSpeed(float timeToMaxSpeed, float startSpeed, float maxSpeed)
    {
        if (!_entityStats) yield break;

        float updateTime = 0.2f;
        WaitForSeconds wait = new WaitForSeconds(updateTime);
        float speedIncrease = (maxSpeed - startSpeed) * updateTime / timeToMaxSpeed;

        while (_entityStats.TryGetStat(StatType.Speed, out NetStat currentSpeed) && currentSpeed.CurrentValue < maxSpeed)
        {
            Debug.Log(currentSpeed.CurrentValue);
            if (_agent.isActiveAndEnabled)
            {
                _entityStats.ModifyStatServerRpc(StatType.Speed, speedIncrease);
                if (_anim) _anim.SetFloat(BehaviorManager.AnimSpeedY, currentSpeed.CurrentValue / maxSpeed);
            }
            yield return wait;
        }

        _entityStats.SetStatServerRpc(StatType.Speed, maxSpeed);
        if (_anim) _anim.SetFloat(BehaviorManager.AnimSpeedY, 1);
    }
}