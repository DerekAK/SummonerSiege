using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SimpleChase", menuName = "Scriptable Objects/AI Behavior/States/Chase/SimpleChase")]
public class SimpleChase : BaseChasingState
{
    [Tooltip("The speed factor relative to the character's max speed.")]
    [Range(0f, 2f)]
    [SerializeField] private float chaseSpeedFactor = 1f;

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
        if (!behaviorManager.GetComponent<NavMeshAgent>().isOnOffMeshLink)
        {
            Chase(behaviorManager);
        }
    }

    public override void Chase(BehaviorManager behaviorManager)
    {
        behaviorManager.GetComponent<NavMeshAgent>().SetDestination(behaviorManager.CurrentTarget.transform.position);
    }

}