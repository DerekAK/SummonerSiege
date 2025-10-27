using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SimpleIdle", menuName = "Scriptable Objects/AI Behavior/States/Idle/SimpleIdle")]
public class SimpleIdleState : BaseIdleState
{
    public override void EnterState()
    {
        // Set animation speed to zero
        _behaviorManager.HandleSpeedChangeWithValue(0);

        if (idleCoroutine != null)
        {
            _behaviorManager.StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
        idleCoroutine = _behaviorManager.StartCoroutine(IdleCoroutine());
    }

    private IEnumerator IdleCoroutine()
    {
        float elapsedTime = 0;
        float idleTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        while (elapsedTime < idleTime)
        {
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        _behaviorManager.SwitchState(_behaviorManager.PatrolState);
    }

    public override void ExitState()
    {
        // Ensure the agent can move again when leaving the idle state
        _behaviorManager.StopCoroutine(idleCoroutine);
        idleCoroutine = null;
        
    }

    public override void UpdateState()
    {
        return;
    }

}
