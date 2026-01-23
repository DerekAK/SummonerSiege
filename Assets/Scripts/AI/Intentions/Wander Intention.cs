using UnityEngine;

[CreateAssetMenu(fileName = "WanderIntention", menuName = "Scriptable Objects/AI Behavior/Intentions/Wander")]
public class WanderIntention : Intention
{
    public override bool CanExecute(BehaviorManager ai)
    {
        if (ai.CurrentState == ai.PatrolState || ai.CurrentState == ai.IdleState)
        {
            return false;
        }
        return true;
    }

    public override void Execute(BehaviorManager ai)
    {   
        ai.SwitchState(ai.IdleState);
    }
}