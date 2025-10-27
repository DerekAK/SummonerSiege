using UnityEngine;

[CreateAssetMenu(fileName = "NewStatModEffect", menuName = "Scriptable Objects/Status Effects/Stat Temporary Modification")]
public class StatTemporaryModificationEffectSO : BaseStatusEffectSO
{
    public StatType StatToModify;
    public float ModifierValue; // Can be negative (slow/weaken) or positive (haste/strengthen)
    public bool IsPercentage; // Is the modifier a percentage or a flat value?

    public override BaseStatusEffect CreateEffect(GameObject applier, GameObject target)
    {
        return new StatModificationEffect(this, applier, target);
    }
}

public class StatModificationEffect : BaseStatusEffect
{
    private StatTemporaryModificationEffectSO statModEffectSO;
    private float originalValue;

    public StatModificationEffect(StatTemporaryModificationEffectSO effectSO, GameObject applier, GameObject target) : base(effectSO, applier, target)
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
                ? originalValue * ((100 + statModEffectSO.ModifierValue) / 100)
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