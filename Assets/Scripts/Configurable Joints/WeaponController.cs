using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform wristTransform; // The target sphere/wrist
    
    private Rigidbody rb;
    private ConfigurableJoint joint;
    private Quaternion startLocalRotation;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    
    private void Start()
    {
        startLocalRotation = transform.localRotation;
    }
    
    private void FixedUpdate()
    {
        UpdateWeaponOrientation();
    }
    
    private void UpdateWeaponOrientation()
{
    if (wristTransform == null) return;
    
    Rigidbody wristRb = wristTransform.GetComponent<Rigidbody>();
    if (wristRb == null) return;
    
    Vector3 wristVelocity = wristRb.linearVelocity;
    
    Quaternion targetLocalRotation;

    // Only adjust orientation if moving above threshold
    if (wristVelocity.magnitude < 0.5f)
    {
        // If not moving much, just align with wrist rotation
        targetLocalRotation = wristTransform.localRotation;
        ConfigurableJointExtensions.SetTargetRotationLocal(joint, targetLocalRotation, startLocalRotation);
        return;
    }
    
    // Calculate desired rotation: green axis (Y/up) points along velocity
    Vector3 swingDirection = wristVelocity.normalized;
    
    // The weapon's local UP (green, Y-axis) should point along swing direction
    Vector3 desiredUp = swingDirection;
    
    // Forward can be derived perpendicular to swing and world right
    Vector3 desiredForward = Vector3.Cross(Vector3.right, desiredUp);
    if (desiredForward.magnitude < 0.1f)
    {
        // Fallback if swing is perfectly left/right
        desiredForward = Vector3.Cross(Vector3.forward, desiredUp);
    }
    desiredForward.Normalize();
    
    // Create world rotation: Y-axis along swing, Z-axis perpendicular
    Quaternion targetWorldRotation = Quaternion.LookRotation(desiredForward, desiredUp);
    
    // Convert to local rotation relative to wrist
    targetLocalRotation = Quaternion.Inverse(wristTransform.rotation) * targetWorldRotation;
    
    // Draw debug rays
    Debug.DrawRay(transform.position, swingDirection * 0.5f, Color.yellow, 0.02f); // Swing direction
    Debug.DrawRay(transform.position, transform.up * 0.3f, Color.green, 0.02f); // Hilt to tip (should align with yellow)
    Debug.DrawRay(transform.position, transform.right * 0.3f, Color.red, 0.02f); // Blade edge
    
    // Set joint target rotation in local space
    ConfigurableJointExtensions.SetTargetRotationLocal(joint, targetLocalRotation, startLocalRotation);
}
    
}