using UnityEngine;

public class ArmPhysicsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform targetSphere; // Your physics hand sphere
    [SerializeField] private Rigidbody wristRigidbody;
    
    [Header("Force Settings")]
    [SerializeField] private float springStrength = 5000f;
    [SerializeField] private float damping = 500f;
    [SerializeField] private float maxForce = 10000f;
    
    [Header("Optional - For Tuning")]
    [SerializeField] private bool applyTorque = false; // Set to true if wrist doesn't rotate properly
    [SerializeField] private float torqueStrength = 100f;
    
    void FixedUpdate()
    {
        if (targetSphere == null || wristRigidbody == null) return;
        
        // Calculate force to pull wrist toward sphere
        Vector3 toTarget = targetSphere.position - wristRigidbody.position;
        Vector3 desiredForce = toTarget * springStrength;
        
        // Add damping to prevent oscillation
        desiredForce -= wristRigidbody.linearVelocity * damping;
        
        // Clamp force to prevent instability
        if (desiredForce.magnitude > maxForce)
        {
            desiredForce = desiredForce.normalized * maxForce;
        }
        
        // Apply force to wrist
        wristRigidbody.AddForce(desiredForce, ForceMode.Force);
        
        // Optional: Apply torque to match sphere rotation (if needed)
        if (applyTorque)
        {
            Quaternion targetRotation = targetSphere.rotation;
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(wristRigidbody.rotation);
            
            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);
            
            if (angle > 180f) angle -= 360f;
            
            Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad * torqueStrength);
            wristRigidbody.AddTorque(torque, ForceMode.Force);
        }
    }
    
    // For debugging
    void OnDrawGizmos()
    {
        if (wristRigidbody != null && targetSphere != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(wristRigidbody.position, targetSphere.position);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wristRigidbody.position, 0.05f);
        }
    }
}