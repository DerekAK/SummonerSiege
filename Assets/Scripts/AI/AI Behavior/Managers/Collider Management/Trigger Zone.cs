using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TriggerZone: MonoBehaviour
{
    private ColliderManager _colliderManager;
    [SerializeField] private TriggerType triggerType;
    private SphereCollider _sphereCollider;

    private void Awake()
    {
        _sphereCollider = GetComponent<SphereCollider>();
    }
    public void PassColliderManagerReference(ColliderManager colliderManager)
    {
        _colliderManager = colliderManager;

        float colliderGORadius;
        switch (triggerType)
        {
            case TriggerType.HitDetection:
                colliderGORadius = _colliderManager.HitDetectionColliderGORadius;
                break;
            case TriggerType.TargetEnter:
                colliderGORadius = _colliderManager.TargetEnterColliderGORadius;
                break;
            case TriggerType.TargetExit:
                colliderGORadius = _colliderManager.TargetExitColliderGORadius;
                break;
            default:
                colliderGORadius = default;
                break;
        }

        _sphereCollider.radius = colliderGORadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        _colliderManager.OnZoneEntered(triggerType, other);
    }

    private void OnTriggerExit(Collider other)
    {
        _colliderManager.OnZoneExited(triggerType, other);
    }
}
