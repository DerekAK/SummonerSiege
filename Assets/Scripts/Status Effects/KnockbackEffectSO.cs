using UnityEngine;

[CreateAssetMenu(fileName = "NewKnockbackEffect", menuName = "Scriptable Objects/Status Effects/Knockback Effect")]
public class KnockbackEffectSO : BaseStatusEffectSO
{
    public float KnockbackForce;
    public override BaseStatusEffect CreateEffect(GameObject applier, GameObject target)
    {
        return new KnockbackEffect(this, applier, target);
    }
}


public class KnockbackEffect : BaseStatusEffect
{
    Rigidbody _targetRb;
    PlayerMovement _targetPlayerMovement;
    KnockbackEffectSO _effectSO;
    public KnockbackEffect(KnockbackEffectSO effectSO, GameObject applier, GameObject target) : base(effectSO, applier, target)
    {
        _targetRb = target.GetComponent<Rigidbody>();
        _targetPlayerMovement = target.GetComponent<PlayerMovement>();
        _effectSO = effectSO;
    }

    public override void Apply()
    {
        base.Apply();
        if (_targetPlayerMovement)
        {
            Vector3 forceDir = (target.transform.position - applier.transform.position).normalized;
            forceDir.y = Random.Range(0.2f, 0.5f);
            forceDir *= _effectSO.KnockbackForce;
            _targetPlayerMovement.ApplyForce(forceDir);
        }

        IsFinished = true;
    }
}