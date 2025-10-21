using UnityEngine;

[CreateAssetMenu(fileName = "DistanceConsideration", menuName = "Scriptable Objects/AI Behavior/Considerations/Distance")]
public class DistanceConsideration : Consideration
{
    // We can use an AnimationCurve to visually define the ideal range.
    // X-axis = Distance, Y-axis = Score (0 to 1)
    public AnimationCurve distanceScoreCurve;

    public override float Evaluate(BehaviorManager ai)
    {
        // you would want this to normalize from 0 to the agent's max range, its max range being the range of its furthest range attack, 
        // which is not implemented at this point in time. 
        if (ai.CurrentTarget == null) return 0;

        SphereCollider maxDetectionCollider = ai.GetComponentInChildren<ColliderManager>().TargetExitColliderGO.GetComponent<SphereCollider>();

        Vector3 worldScaleDistance = maxDetectionCollider.transform.lossyScale * maxDetectionCollider.radius;
        
        float maxPossibleDistance = Mathf.Max(worldScaleDistance.x, worldScaleDistance.y, worldScaleDistance.z);

        float distance = Vector3.Distance(ai.transform.position, ai.CurrentTarget.transform.position);

        float normalizedDistance = distance / maxPossibleDistance;
        
        return Mathf.Clamp(distanceScoreCurve.Evaluate(normalizedDistance), 0, 1);
    }
}