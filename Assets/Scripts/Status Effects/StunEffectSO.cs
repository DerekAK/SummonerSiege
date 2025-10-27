using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewStunEffect", menuName = "Scriptable Objects/Status Effects/Stun")]
public class StunEffectSO : BaseStatusEffectSO
{
    public override BaseStatusEffect CreateEffect(GameObject applier, GameObject target)
    {
        return new StunEffect(this, applier, target);
    }
}

public class StunEffect : BaseStatusEffect
{
    private NavMeshAgent agent; // Example component to disable

    public StunEffect(StunEffectSO effectSO, GameObject applier, GameObject target) : base(effectSO, applier, target)
    {
        agent = target.GetComponent<NavMeshAgent>();
    }

    public override void Apply()
    {
        base.Apply();
        if (agent != null)
        {
            agent.enabled = false;
        }
        // You could also disable player input scripts, AI behavior managers, etc.
    }

    public override void End()
    {
        base.End();
        if (agent != null)
        {
            agent.enabled = true;
        }
    }
}