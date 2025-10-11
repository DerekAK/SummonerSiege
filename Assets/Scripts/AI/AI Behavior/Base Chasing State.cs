

using System;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public abstract class BaseChasingState : BaseBehaviorState
{
    public NavMeshAgent _agent;
    public Animator _anim;

    public void EnterChaseState(BehaviorManager behaviorManager)
    {
        _agent = behaviorManager.GetComponent<NavMeshAgent>();
        _anim = behaviorManager.GetComponent<Animator>();
        if (!_agent.isActiveAndEnabled) return;
    }
    public abstract void Chase(BehaviorManager behaviorManager, Transform target);
}
