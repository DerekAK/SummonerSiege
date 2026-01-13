// z force seems to be especially inadequate when the ball is going in the negative z direction. 
// so maybe need to compensate by making it scale with z direction as well? 

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsHandController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private Transform centerConeTransform;
    [SerializeField] private Transform centerBoundingSphereTransform;
    [SerializeField] private Transform forwardReference;
    [SerializeField] private Camera playerCamera;
    
    [Header("Boundary Objects")]
    [SerializeField] private MeshCollider boundingSphereCollider;
    [SerializeField] private BoxCollider leftConeWall;
    [SerializeField] private BoxCollider rightConeWall;
    [SerializeField] private PhysicsMaterial boundaryPhysicsMaterial;
    
    [Header("Character Stats")]
    [SerializeField] private float characterStrength = 50f;
    [SerializeField] private float weaponWeight = 20f;
    
    [Header("Control Settings")]
    [SerializeField] private float sphereRadius = 2f;
    [SerializeField] private float maxConeAngle = 90f;
    [SerializeField] private float coneRotationOffset = 0f;
    [SerializeField] private KeyCode activationKey = KeyCode.Mouse1;
    [SerializeField] private float mouseSensitivity = 50f;
    [SerializeField] private bool compensateForAspectRatio = true;
    [SerializeField] private float verticalSensitivityMultiplier = 1.0f;
    
    [Header("Physics Settings")]
    [SerializeField] private float customGravity = -9.81f;
    [SerializeField] private float gravityMultiplier = 0.3f;
    [SerializeField] private float airDrag = 1f;
    [SerializeField] private float maxSpeed = 20f;
    
    [Header("Swing Detection Settings")]
    [SerializeField] private float swingDetectionAngleThreshold = 70f;
    [SerializeField] private float swingDetectionVelocityThreshold = 2f;
    [SerializeField] private float swingCooldownTime = 0.25f;
    
    [Header("Z-Force Settings")]
    [SerializeField] private float zForceMultiplier = 1.5f;
    [SerializeField] private AnimationCurve zForceRampCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float zForceRampDuration = 0.1f;
    [SerializeField] private float zForceLerpTowardsZeroDuration = 0.1f;
    
    private Rigidbody rb;
    
    // Swing detection tracking
    private Vector3 lastSwingXYDirection;
    private float lastSwingDetectionTime;
    private bool isRampingZForce;
    private float zForceRampStartTime;
    private float zForceBaseMagnitude;
    
    public Vector3 GetVelocity() => rb.linearVelocity;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        rb.useGravity = false;
        rb.mass = weaponWeight;
        rb.linearDamping = airDrag;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        lastSwingXYDirection = Vector3.zero;
        lastSwingDetectionTime = -swingCooldownTime;
    }
    
    private void Start()
    {

        InitializeBoundingSphere();

        UpdateConeWalls();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void InitializeBoundingSphere()
    {
        boundingSphereCollider.material = boundaryPhysicsMaterial;
        boundingSphereCollider.transform.position = centerBoundingSphereTransform.position;
        boundingSphereCollider.transform.localScale *= sphereRadius;
    }
    
    private void UpdateConeWalls()
    {
        if (centerConeTransform == null || forwardReference == null) return;
        
        Vector3 baseForward = forwardReference.forward;
        Quaternion offsetRotation = Quaternion.AngleAxis(coneRotationOffset, Vector3.up);
        Vector3 coneForward = offsetRotation * baseForward;
        
        float wallSize = sphereRadius * 4f;
        
        if (leftConeWall != null)
        {
            Vector3 leftEdge = Quaternion.AngleAxis(-maxConeAngle, Vector3.up) * coneForward;
            Vector3 leftWallNormal = Vector3.Cross(Vector3.up, leftEdge).normalized;
            
            Vector3 leftDirection = Vector3.Cross(leftWallNormal, Vector3.up).normalized;
            leftConeWall.transform.position = centerConeTransform.position + leftDirection * (wallSize / 2f);
            leftConeWall.transform.rotation = Quaternion.LookRotation(leftWallNormal);
            leftConeWall.size = new Vector3(wallSize, wallSize, 0.1f);
        }
        
        if (rightConeWall != null)
        {
            Vector3 rightEdge = Quaternion.AngleAxis(maxConeAngle, Vector3.up) * coneForward;
            Vector3 rightWallNormal = Vector3.Cross(rightEdge, Vector3.up).normalized;
            
            Vector3 rightDirection = Vector3.Cross(rightWallNormal, Vector3.up).normalized;
            rightConeWall.transform.position = centerConeTransform.position - rightDirection * (wallSize / 2f);
            rightConeWall.transform.rotation = Quaternion.LookRotation(rightWallNormal);
            rightConeWall.size = new Vector3(wallSize, wallSize, 0.1f);
        }
    }
    
    private void Update()
    {
        UpdateConeWalls();
    }
    
    private void FixedUpdate()
    {
        if (Input.GetKey(activationKey))
        {
            ApplyMouseForces();
            DetectSwing();
        }
        
        ApplyCustomGravity();
        ApplyZForceRamp();
        CapVelocity();
    }
    
    private void DetectSwing()
    {
        // Get XY velocity (ignore Z component)
        Vector3 xyVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
        float xySpeed = xyVelocity.magnitude;
        
        // Check velocity threshold
        if (xySpeed < swingDetectionVelocityThreshold)
        {
            return;
        }
        
        // Check cooldown
        if (Time.time - lastSwingDetectionTime < swingCooldownTime)
        {
            return;
        }
        
        Vector3 currentXYDirection = xyVelocity.normalized;
        
        // If we have a previous swing direction, check angle change
        if (lastSwingXYDirection != Vector3.zero)
        {
            float angleDifference = Vector3.Angle(lastSwingXYDirection, currentXYDirection);
            
            if (angleDifference > swingDetectionAngleThreshold)
            {
                // New swing detected!
                TriggerNewSwing(xySpeed);
            }
        }
        else
        {
            // First swing - always trigger
            TriggerNewSwing(xySpeed);
        }
        
        // Update tracking
        lastSwingXYDirection = currentXYDirection;
    }
    
    private void TriggerNewSwing(float xySpeed)
    {
        lastSwingDetectionTime = Time.time;
        isRampingZForce = true;
        zForceRampStartTime = Time.time;
        zForceBaseMagnitude = xySpeed * zForceMultiplier;
        
        Debug.Log($"New swing detected! XY Speed: {xySpeed:F2}, Z Force Base: {zForceBaseMagnitude:F2}");
    }
    
    private void ApplyZForceRamp()
    {
        if (!isRampingZForce)
        {
            return;
        }
        
        float elapsedTime = Time.time - zForceRampStartTime;
        
        if (elapsedTime >= zForceRampDuration)
        {
            isRampingZForce = false;
            Debug.Log("Z-Force ramp completed");
            return;
        }
        
        // Lerp Z velocity to zero during the ramp
        // Get current velocity
        Vector3 currentVelocity = rb.linearVelocity;
        float currentZVelocity = Vector3.Dot(currentVelocity, forwardReference.forward);
        
        // Lerp Z velocity toward zero (faster than the force ramp)
        if (elapsedTime < zForceLerpTowardsZeroDuration)
        {
            float lerpProgress = elapsedTime / zForceLerpTowardsZeroDuration;
            float targetZVelocity = Mathf.Lerp(currentZVelocity, 0f, lerpProgress);
            float zVelocityDelta = targetZVelocity - currentZVelocity;
            
            // Apply the Z velocity change
            rb.linearVelocity += forwardReference.forward * zVelocityDelta;
        }
        
        // Calculate ramp progress (0 to 1)
        float rampProgress = elapsedTime / zForceRampDuration;
        float curveValue = zForceRampCurve.Evaluate(rampProgress);
        
        // Calculate Z force magnitude for this frame
        float currentZForceMagnitude = zForceBaseMagnitude * curveValue;
        
        // Get FORWARD direction (pure Z-axis forward from player)
        Vector3 forwardDirection = forwardReference.forward;
        
        // Apply Z force in pure forward direction
        Vector3 zForce = forwardDirection * currentZForceMagnitude;
        
        Debug.Log($"Applying Z-Force - Progress: {rampProgress:F2}, Magnitude: {currentZForceMagnitude:F2}, Z Vel: {currentZVelocity:F2}");
        
        // Draw debug ray showing the force vector
        Debug.DrawRay(rb.position, zForce * 0.1f, Color.cyan, 0.1f);
        
        rb.AddForce(zForce, ForceMode.Force);
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
        
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;
        Vector3 xyForceDirection = (cameraRight * adjustedDeltaX + cameraUp * adjustedDeltaY).normalized;
        
        float responsiveness = characterStrength / weaponWeight;
        float forceMagnitude = mouseDelta.magnitude * mouseSensitivity * responsiveness;
        
        Vector3 force = xyForceDirection * forceMagnitude;
        
        rb.AddForce(force, ForceMode.Force);
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
    
    public void SetMaxConeAngle(float angle)
    {
        maxConeAngle = Mathf.Clamp(angle, 0f, 180f);
        UpdateConeWalls();
    }
}