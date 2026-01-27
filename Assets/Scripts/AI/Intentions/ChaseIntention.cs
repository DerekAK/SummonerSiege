using UnityEngine;

[CreateAssetMenu(fileName = "ChaseIntention", menuName = "Scriptable Objects/AI Behavior/Intentions/Chase")]
public class ChaseIntention : Intention
{
    public override bool CanExecute(BehaviorManager ai)
    {
        if (ai.CurrentState == ai.ChasingState) return false;

        if (ai.CurrentTarget == null) return false;
        
        return true;
    }

    public override void Execute(BehaviorManager ai)
    {   
        ai.SwitchState(ai.ChasingState);
    }
}