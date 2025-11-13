using UnityEngine;

public class SynchronizedJoint : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    public Transform TargetTransform => targetTransform;
    private ConfigurableJoint _configJoint;
    private Quaternion startLocalRotation;

    private void Awake()
    {
        _configJoint = GetComponent<ConfigurableJoint>();
    }

    private void Start()
    {
        startLocalRotation = transform.localRotation;
    }

    public void SyncJoint()
    {
        ConfigurableJointExtensions.SetTargetRotationLocal(_configJoint, targetTransform.localRotation, startLocalRotation);
    }
}
