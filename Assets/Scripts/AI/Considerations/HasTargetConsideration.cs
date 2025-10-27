using UnityEngine;

[CreateAssetMenu(fileName = "HasTargetConsideration", menuName = "Scriptable Objects/AI Behavior/Considerations/HasTarget")]
public class HasTargetConsideration : Consideration
{
    [Tooltip("If true, scores 1 if a target EXISTS. If false, scores 1 if a target does NOT exist.")]
    [SerializeField] private bool desiredResult = true;

    public override float Evaluate(BehaviorManager ai)
    {
        bool hasTarget = ai.CurrentTarget != null;
        return (hasTarget == desiredResult) ? 1f : 0f;
    }
}
