using System;
using UnityEngine;

public class ColliderManager : MonoBehaviour
{
    public TriggerZone HitDetectionColliderGO;
    public float HitDetectionColliderGORadius;
    public TriggerZone TargetEnterColliderGO;
    public float TargetEnterColliderGORadius;
    public TriggerZone TargetExitColliderGO;
    public float TargetExitColliderGORadius;
    public event Action<Collider> OnHitDetection;
    public event Action<Collider> OnTargetEntrance;
    public event Action<Collider> OnTargetExit;

    private void Awake()
    {
        HitDetectionColliderGO.PassColliderManagerReference(this);
        TargetEnterColliderGO.PassColliderManagerReference(this);
        TargetExitColliderGO.PassColliderManagerReference(this);
    }

    public void OnZoneEntered(TriggerType triggerType, Collider collider)
    {
        if (triggerType == TriggerType.HitDetection)
        {
            OnHitDetection?.Invoke(collider);
        }
        else if (triggerType == TriggerType.TargetEnter)
        {
            OnTargetEntrance?.Invoke(collider);
        }
    }
    
    public void OnZoneExited(TriggerType triggerType, Collider collider)
    {
        if (triggerType == TriggerType.TargetExit)
        {
            OnTargetExit?.Invoke(collider);
        }
    }
}

public enum TriggerType
{
    HitDetection,
    TargetEnter,
    TargetExit
}