using UnityEngine;

public abstract class BaseStatusEffectSO : ScriptableObject
{
    public float Duration; // Duration in seconds. Use a value <= 0 for permanent effects.

    public StackingBehavior Stacking;

    public abstract BaseStatusEffect CreateEffect(GameObject applier, GameObject target);
}

public enum StackingBehavior
{
    // Refreshes the duration of the existing effect
    RefreshDuration,
    // Adds a new, separate instance of the effect
    AddInstance,
    // Prevents a new instance if one is already active
    Prevent
}
