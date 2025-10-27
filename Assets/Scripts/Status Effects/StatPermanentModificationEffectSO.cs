using UnityEngine;

[CreateAssetMenu(fileName = "NewPermanentDamageEffect", menuName = "Scriptable Objects/Status Effects/Stat Permanent Modification")]
public class StatPermanentModificationEffectSO : BaseStatusEffectSO
{
    public float ModificationValue;
    public StatType StatToModify = StatType.Health; 

    public override BaseStatusEffect CreateEffect(GameObject applier, GameObject target)
    {
        return new StatPermanentModificationEffect(this, applier, target);
    }
}

public class StatPermanentModificationEffect : BaseStatusEffect
{
    private StatPermanentModificationEffectSO _damageEffectSO;

    public StatPermanentModificationEffect(StatPermanentModificationEffectSO effectSO, GameObject applier, GameObject target) : base(effectSO, applier, target)
    {
        _damageEffectSO = effectSO;
    }

    public override void Apply()
    {
        base.Apply();

        if (targetStats != null)
        {
            targetStats.ModifyStatServerRpc(_damageEffectSO.StatToModify, -_damageEffectSO.ModificationValue);
        }

        // By setting this to true immediately, we tell the status effect manager to clean this effect up on its next Update.
        // Duration is irrelevant for this class
        IsFinished = true; 
    }
}