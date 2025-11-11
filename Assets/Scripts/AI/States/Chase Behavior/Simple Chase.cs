using UnityEngine;

[CreateAssetMenu(fileName = "SimpleChase", menuName = "Scriptable Objects/AI Behavior/States/Chase/SimpleChase")]
public class SimpleChase : BaseChasingState
{
    [Tooltip("The speed factor relative to the character's max speed.")]
    [Range(0f, 2f)]
    [SerializeField] private float chaseSpeedFactor = 0.2f;

    public override void EnterState(BehaviorManager behaviorManager)
    {
        // Set the speed for chasing
        behaviorManager.HandleSpeedChangeWithFactor(chaseSpeedFactor);          
    }

    public override void ExitState(BehaviorManager behaviorManager)
    {
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