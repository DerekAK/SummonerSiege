using UnityEngine;

[CreateAssetMenu(fileName = "SimpleChase", menuName = "Scriptable Objects/AI Behavior/States/Chase/SimpleChase")]
public class SimpleChase : BaseChasingState
{
    [Tooltip("The speed factor relative to the character's max speed.")]
    [Range(0f, 2f)]
    [SerializeField] private float chaseSpeedFactor = 1f;

    public override void EnterState()
    {
        // Set the speed for chasing
        _behaviorManager.HandleSpeedChangeWithFactor(chaseSpeedFactor);          
    }

    public override void ExitState()
    {
        return;
    }

    public override void UpdateState()
    {
        if (_behaviorManager.CurrentTarget == null)
        {
            return;
        }
        
        // Standard chase logic
        if (!_agent.isOnOffMeshLink)
        {
            Chase();
        }
    }

    public override void Chase()
    {
        _agent.SetDestination(_behaviorManager.CurrentTarget.transform.position);
    }

}