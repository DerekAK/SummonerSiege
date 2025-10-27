using UnityEngine;


[CreateAssetMenu(fileName = "NewStatTickEffect", menuName = "Scriptable Objects/Status Effects/Stat Over Time Modification")]
public class StatOverTimeEffectSO : BaseStatusEffectSO
{
    [Tooltip("1 would be every second")]
    public float TickFrequency;
    public float ValuePerTick;
    public StatType StatToModify;

    public override BaseStatusEffect CreateEffect(GameObject applier, GameObject target)
    {
        return new StatOverTimeEffect(this, applier, target);
    }
}

public class StatOverTimeEffect : BaseStatusEffect
{
    private float lastTickTime;
    private StatOverTimeEffectSO effectSO;

    public StatOverTimeEffect(StatOverTimeEffectSO effectSO, GameObject applier, GameObject target) : base(effectSO, applier, target)
    {
        this.effectSO = effectSO;
        lastTickTime = Time.time;
    }

    public override void Tick()
    {
        base.Tick(); // Handle duration countdown

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