using UnityEngine;
using UnityEngine.AI; // Example for stun

// --- 1. Damage Over Time (Poison, Burn) ---

[CreateAssetMenu(fileName = "NewStatTickEffect", menuName = "Status Effects/Stat Over Time Modification")]
public class StatOverTimeEffectSO : BaseStatusEffectSO
{
    [Tooltip("1 would be every second")]
    public float TickFrequency;
    public float ValuePerTick;
    public StatType StatToModify;

    public override BaseStatusEffect CreateEffect(GameObject target)
    {
        return new StatOverTimeEffect(this, target);
    }
}

public class StatOverTimeEffect : BaseStatusEffect
{
    private float lastTickTime;
    private StatOverTimeEffectSO effectSO;

    public StatOverTimeEffect(StatOverTimeEffectSO effectSO, GameObject target) : base(effectSO, target)
    {
        this.effectSO = effectSO;
        lastTickTime = Time.time;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime); // Handle duration countdown

        if (Time.time >= lastTickTime + effectSO.TickFrequency)
        {
            lastTickTime = Time.time;
            if (targetStats != null && targetStats.TryGetStat(effectSO.StatToModify, out _))
            {
                targetStats.ModifyStatServerRpc(effectSO.StatToModify, effectSO.ValuePerTick);
            }
        }
    }
}

[CreateAssetMenu(fileName = "NewStatModEffect", menuName = "Status Effects/Stat Modification")]
public class StatModificationEffectSO : BaseStatusEffectSO
{
    public StatType StatToModify;
    public float ModifierValue; // Can be negative (slow/weaken) or positive (haste/strengthen)
    public bool IsPercentage; // Is the modifier a percentage or a flat value?

    public override BaseStatusEffect CreateEffect(GameObject target)
    {
        return new StatModificationEffect(this, target);
    }
}

public class StatModificationEffect : BaseStatusEffect
{
    private StatModificationEffectSO statModEffectSO;
    private float originalValue;

    public StatModificationEffect(StatModificationEffectSO effectSO, GameObject target) : base(effectSO, target)
    {
        statModEffectSO = effectSO;
    }

    public override void Apply()
    {
        base.Apply();
        if (targetStats != null && targetStats.TryGetStat(statModEffectSO.StatToModify, out var stat))
        {
            originalValue = stat.CurrentValue;
            float newValue = statModEffectSO.IsPercentage
                ? originalValue * (1 + statModEffectSO.ModifierValue)
                : originalValue + statModEffectSO.ModifierValue;

            targetStats.SetStatServerRpc(statModEffectSO.StatToModify, newValue);
        }
    }

    public override void End()
    {
        base.End();
        if (targetStats != null)
        {
            targetStats.SetStatServerRpc(statModEffectSO.StatToModify, originalValue);
        }
    }
}


[CreateAssetMenu(fileName = "NewStunEffect", menuName = "Status Effects/Stun")]
public class StunEffectSO : BaseStatusEffectSO
{
    public override BaseStatusEffect CreateEffect(GameObject target)
    {
        return new StunEffect(this, target);
    }
}

public class StunEffect : BaseStatusEffect
{
    private NavMeshAgent agent; // Example component to disable

    public StunEffect(StunEffectSO effectSO, GameObject target) : base(effectSO, target)
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
        Debug.Log($"{target.name} is stunned!");
    }

    public override void End()
    {
        base.End();
        if (agent != null)
        {
            agent.enabled = true;
        }
        Debug.Log($"{target.name} is no longer stunned.");
    }
}

