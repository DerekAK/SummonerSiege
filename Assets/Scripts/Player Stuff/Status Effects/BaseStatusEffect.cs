using UnityEngine;

[System.Serializable]
public class BaseStatusEffect
{
    public enum eEffectType{
        Stun,
        Slow,
        Burn,
    }

    [SerializeField] private eEffectType effectType;
    [SerializeField] private float duration;
    [SerializeField] private float damage;
}
