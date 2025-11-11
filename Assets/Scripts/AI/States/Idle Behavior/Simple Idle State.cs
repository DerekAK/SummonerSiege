using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleIdle", menuName = "Scriptable Objects/AI Behavior/States/Idle/SimpleIdle")]
public class SimpleIdleState : BaseIdleState
{
    public override void EnterState(BehaviorManager behaviorManager)
    {
        // Set animation speed to zero
        behaviorManager.HandleSpeedChangeWithValue(0);

        if (behaviorManager.IdleCoroutine != null)
        {
            behaviorManager.StopCoroutine(behaviorManager.IdleCoroutine);
            behaviorManager.IdleCoroutine = null;
        }
        behaviorManager.IdleCoroutine = behaviorManager.StartCoroutine(IdleCoroutine(behaviorManager));
    }

    private IEnumerator IdleCoroutine(BehaviorManager behaviorManager)
    {
        float elapsedTime = 0;
        float idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        while (elapsedTime < idleTime)
        {
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        behaviorManager.SwitchState(behaviorManager.PatrolState);
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
        // Ensure the agent can move again when leaving the idle state
        if (behaviorManager.IdleCoroutine != null) behaviorManager.StopCoroutine(behaviorManager.IdleCoroutine);
        behaviorManager.IdleCoroutine = null;
    }

    public override void UpdateState(BehaviorManager behaviorManager)
    {
        return;
    }

}
