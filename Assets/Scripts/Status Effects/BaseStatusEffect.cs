using UnityEngine;

public abstract class BaseStatusEffect
{
    // The ScriptableObject that defines this effect's properties
    public BaseStatusEffectSO EffectSO { get; }
    public float TimeLeft { get; protected set; }
    public bool IsFinished { get; protected set; }

    protected readonly GameObject target;
    protected readonly GameObject applier;
    protected readonly EntityStats targetStats;

    // Constructor
    public BaseStatusEffect(BaseStatusEffectSO effectSO, GameObject applier, GameObject target)
    {
        this.EffectSO = effectSO;
        this.TimeLeft = effectSO.Duration;
        this.target = target;
        this.targetStats = target.GetComponent<EntityStats>();
        this.applier = applier;
    }

    public virtual void Apply()
    {
        // Base implementation can be empty
    }

    public virtual void Tick()
    {
        // If duration is 0 or less, it's a permanent effect that must be removed manually.
        if (EffectSO.Duration > 0)
        {
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0)
            {
                IsFinished = true;
            }
        }
    }
    public virtual void End()
    {
        // Base implementation can be empty
    }

    public void RefreshDuration()
    {
        if (EffectSO.Duration > 0)
        {
            TimeLeft = EffectSO.Duration;
        }
    }
}
