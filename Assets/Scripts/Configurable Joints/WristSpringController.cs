using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WristSpringController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform shoulderTransform;
    [SerializeField] private Transform forwardReference; // Player facing direction
    [SerializeField] private Camera playerCamera;
    
    
    [Header("Character Stats")]
    [SerializeField] private float characterStrength = 50f;
    [SerializeField] private float weaponWeight = 20f;
    
    [Header("Control Settings")]
    [SerializeField] private KeyCode combatActivationKey = KeyCode.Mouse1;
    [SerializeField] private bool compensateForAspectRatio = true;
    [SerializeField] private float verticalSensitivityMultiplier = 1.0f;
    
    [Header("Spring Settings")]
    [SerializeField] private float targetArmExtension = 1.7f; // Target distance along arm line
    [SerializeField] private float radialSpringStrength = 50f; // Base spring strength
    [SerializeField] private float springVelocityMultiplier = 10f; // Scales with XY speed
    [SerializeField] private float springDamper = 10f;
    [SerializeField] private float maxArmExtension = 2.0f; // Hard limit
    
    [Header("Z-Burst Settings")]
    [SerializeField] private float zBurstMultiplier = 50f; // Proportional to XY force
    [SerializeField] private float swingDetectionThreshold = 0.5f; // Speed threshold for new swing
    
    [Header("Physics Settings")]
    [SerializeField] private float customGravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 0.3f;
    [SerializeField] private float airDrag = 1f;
    [SerializeField] private float maxSpeed = 20f;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugLines = true;
    
    private Rigidbody rb;
    private float responsiveness;
    private bool isCombatModeActive = false;
    private float previousXYSpeed = 0f;
    private bool zBurstAppliedThisSwing = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        rb.useGravity = false;
        rb.linearDamping = airDrag;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        responsiveness = characterStrength / weaponWeight;
    }
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    
    private void Update()
    {
        isCombatModeActive = Input.GetKey(combatActivationKey);
        
        if (showDebugLines)
        {
            DrawDebugVisualization();
        }
    }
    
    private void FixedUpdate()
    {
        if (isCombatModeActive)
        {
            ApplyMouseForces();
            DetectAndApplyZBurst();
            ApplyRadialSpring();
            ApplyCustomGravity();
            EnforceMaxExtension();
            CapVelocity();
        }
    }
    
    private void ApplyMouseForces()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        mouseDelta *= 10f;
        
        if (mouseDelta.magnitude < 0.01f) return;
        
        float adjustedDeltaX = mouseDelta.x;
        float adjustedDeltaY = mouseDelta.y;
        
        if (compensateForAspectRatio && playerCamera != null)
        {
            adjustedDeltaY *= playerCamera.aspect;
        }
        adjustedDeltaY *= verticalSensitivityMultiplier;
        
        // Camera-relative XY forces
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;
        Vector3 xyForceDirection = (cameraRight * adjustedDeltaX + cameraUp * adjustedDeltaY).normalized;
        
        float forceMagnitude = mouseDelta.magnitude * responsiveness;
        Vector3 force = xyForceDirection * forceMagnitude;
        
        rb.AddForce(force, ForceMode.Force);
    }
    
    private void DetectAndApplyZBurst()
    {
        // Calculate current XY speed
        Vector3 xyVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentXYSpeed = xyVelocity.magnitude;
        
        // Detect swing start: speed went from low to high
        bool swingStarted = (previousXYSpeed < swingDetectionThreshold && currentXYSpeed >= swingDetectionThreshold);
        
        if (swingStarted && !zBurstAppliedThisSwing)
        {
            // Apply initial Z-burst outward from player
            Vector3 forwardDirection = forwardReference != null ? forwardReference.forward : transform.forward;
            float zBurstMagnitude = currentXYSpeed * zBurstMultiplier;
            Vector3 zBurstForce = forwardDirection * zBurstMagnitude;
            
            rb.AddForce(zBurstForce, ForceMode.Impulse);
            
            zBurstAppliedThisSwing = true;
            Debug.Log($"Z-Burst applied! Magnitude: {zBurstMagnitude:F1}");
        }
        
        // Reset flag when speed drops back down
        if (currentXYSpeed < swingDetectionThreshold)
        {
            zBurstAppliedThisSwing = false;
        }
        
        previousXYSpeed = currentXYSpeed;
    }
    
    private void ApplyRadialSpring()
    {
        if (shoulderTransform == null) return;
        
        // Calculate shoulder→wrist direction
        Vector3 shoulderToWrist = rb.position - shoulderTransform.position;
        float currentDistance = shoulderToWrist.magnitude;
        
        if (currentDistance < 0.01f) return; // Avoid division by zero
        
        Vector3 radialDirection = shoulderToWrist.normalized;
        
        // Calculate target position along radial line
        Vector3 targetPosition = shoulderTransform.position + radialDirection * targetArmExtension;
        
        // Spring force pulls toward target position
        Vector3 toTarget = targetPosition - rb.position;
        
        // Calculate current XY speed for proportional spring
        Vector3 xyVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentXYSpeed = xyVelocity.magnitude;
        
        // Spring strength proportional to XY speed
        float dynamicSpringStrength = radialSpringStrength + (currentXYSpeed * springVelocityMultiplier);
        
        Vector3 springForce = toTarget * dynamicSpringStrength;
        
        // Damping force
        Vector3 dampingForce = -rb.linearVelocity * springDamper;
        
        Vector3 totalSpringForce = springForce + dampingForce;
        
        rb.AddForce(totalSpringForce, ForceMode.Force);
    }
    
    private void EnforceMaxExtension()
    {
        if (shoulderTransform == null) return;
        
        Vector3 fromShoulder = rb.position - shoulderTransform.position;
        float currentExtension = fromShoulder.magnitude;
        
        if (currentExtension > maxArmExtension)
        {
            // Clamp to max extension
            Vector3 clampedPosition = shoulderTransform.position + fromShoulder.normalized * maxArmExtension;
            rb.position = clampedPosition;
            
            // Remove velocity component going further away
            Vector3 radialDirection = fromShoulder.normalized;
            float radialVelocity = Vector3.Dot(rb.linearVelocity, radialDirection);
            if (radialVelocity > 0)
            {
                rb.linearVelocity -= radialDirection * radialVelocity;
            }
        }
    }
    
    
    private void ApplyCustomGravity()
    {
        Vector3 gravity = Vector3.up * customGravity * gravityMultiplier;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
    
    private void CapVelocity()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    
    private void DrawDebugVisualization()
    {
        if (shoulderTransform == null) return;
        
        // Yellow line: Shoulder to wrist (current arm)
        Debug.DrawLine(shoulderTransform.position, rb.position, Color.yellow);
        
        if (isCombatModeActive)
        {
            // Calculate radial target position
            Vector3 shoulderToWrist = rb.position - shoulderTransform.position;
            if (shoulderToWrist.magnitude > 0.01f)
            {
                Vector3 radialDirection = shoulderToWrist.normalized;
                Vector3 targetPosition = shoulderTransform.position + radialDirection * targetArmExtension;
                
                // Cyan line: Shoulder to target position (spring target)
                Debug.DrawLine(shoulderTransform.position, targetPosition, Color.cyan);
                
                // Green cross: Target position marker
                Debug.DrawRay(targetPosition, Vector3.up * 0.1f, Color.green);
                Debug.DrawRay(targetPosition, Vector3.right * 0.1f, Color.green);
                Debug.DrawRay(targetPosition, Vector3.forward * 0.1f, Color.green);
                
                // Red line: Spring force direction
                Vector3 toTarget = targetPosition - rb.position;
                if (toTarget.magnitude > 0.01f)
                {
                    Debug.DrawRay(rb.position, toTarget.normalized * 0.3f, Color.red);
                }
            }
        }
    }
    
}