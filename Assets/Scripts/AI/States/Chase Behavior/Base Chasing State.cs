

using UnityEngine;
using UnityEngine.AI;

public abstract class BaseChasingState : BaseBehaviorState
{
    protected NavMeshAgent _agent;
    protected Animator _anim;
    protected EntityStats _entityStats;
    protected ColliderManager _colliderManager;
    protected BehaviorManager _behaviorManager;

    public override void InitializeState(BehaviorManager behaviorManager)
    {
        _behaviorManager = behaviorManager;
        _agent = behaviorManager.GetComponent<NavMeshAgent>();
        _anim = behaviorManager.GetComponent<Animator>();
        _entityStats = behaviorManager.GetComponent<EntityStats>();
        _colliderManager = behaviorManager.GetComponent<ColliderManager>();
        if (!_agent.isActiveAndEnabled) return;
    }

    public override void DeInitializeState()
    {
        return;
    }
    
    public abstract void Chase();
}
