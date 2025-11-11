using UnityEngine;

[CreateAssetMenu(fileName = "WanderIntention", menuName = "Scriptable Objects/AI Behavior/Intentions/Wander")]
public class WanderIntention : Intention
{
    public override void Execute(BehaviorManager ai)
    {
        // We only want to INITIATE the wander loop.
        // If the AI is already wandering (patrolling or idling), let it continue its loop.
        // This prevents interrupting an idle timer every time this intention is chosen.
        if (ai.CurrentState == ai.PatrolState || ai.CurrentState == ai.IdleState)
        {
            return; // Already wandering, do nothing.
        }
        // If not wandering, start the loop by patrolling.
        ai.SwitchState(ai.IdleState);
    }
}