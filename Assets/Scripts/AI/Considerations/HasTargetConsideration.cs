using UnityEngine;

[CreateAssetMenu(fileName = "HasTargetConsideration", menuName = "Scriptable Objects/AI Behavior/Considerations/HasTarget")]
public class HasTargetConsidationConsideration : Consideration
{
    
    public bool NeedsTarget;

    public override float Evaluate(BehaviorManager ai)
    {

        bool hasTarget = ai.CurrentTarget != null;

        if (hasTarget && NeedsTarget || !hasTarget && !NeedsTarget) return 1;

        return 0;

    }
}