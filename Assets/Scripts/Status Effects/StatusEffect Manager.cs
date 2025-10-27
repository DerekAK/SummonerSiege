using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffectManager : MonoBehaviour
{
    private readonly List<BaseStatusEffect> activeEffects = new List<BaseStatusEffect>();

    private void Update()
    {
        // Iterate backwards because we might remove items from the list
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            BaseStatusEffect statusEffect = activeEffects[i];
            statusEffect.Tick();

            if (statusEffect.IsFinished)
            {
                statusEffect.End();
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void ApplyEffect(GameObject applier, BaseStatusEffectSO effectSO)
    {
        // Handle stacking logic
        if (effectSO.Stacking != StackingBehavior.AddInstance)
        {
            // Try to find an existing effect of the same type
            BaseStatusEffect existingEffect = activeEffects.FirstOrDefault(effect => effect.EffectSO.GetType() == effectSO.GetType());
            
            if (existingEffect != null)
            {
                if (effectSO.Stacking == StackingBehavior.RefreshDuration)
                {
                    existingEffect.RefreshDuration();
                    Debug.Log($"Refreshed duration of {effectSO.name} on {gameObject.name}.");
                }
                // If stacking is Prevent, we do nothing and return.
                return;
            }
        }

        // If we're here, we need to add a new instance.
        BaseStatusEffect newEffect = effectSO.CreateEffect(applier, gameObject);
        activeEffects.Add(newEffect);
        newEffect.Apply();
        // Debug.Log($"Applied {effectSO.name} to {gameObject.name}.");
    }
}

