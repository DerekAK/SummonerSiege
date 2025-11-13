using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    [Header("Model References")]
    [SerializeField] private GameObject ragdollGO;
    [SerializeField] private GameObject animatedGO;

    [Header("Spring Transition Settings")]
    [SerializeField] private float springRestoreTime = 0.2f; // Time to restore springs
    private Coroutine springRestoreCoroutine;
    
    private SkinnedMeshRenderer[] ragdollRenderers;
    private SkinnedMeshRenderer[] animatedRenderers;
    private SynchronizedJoint[] synchronizedJoints;
    private Dictionary<SynchronizedJoint, JointDrive> originalSlerpDrives;

    private Vector3 animatedRigStartPosition;
    private Quaternion animatedRigStartRotation;
    
    private bool isInAnimationMode = false;
    public bool IsInAnimationMode => isInAnimationMode;
    private bool firstTimeEnabling = true;

    private void Awake()
    {
        CacheComponents();
    }
    
    private void Start()
    {
        animatedRigStartPosition = animatedGO.transform.localPosition;
        animatedRigStartRotation = animatedGO.transform.localRotation;
        // start in physics mode
        EnablePhysicsMode();
        firstTimeEnabling = false;
    }
    
    private void CacheComponents()
    {
        // Cache renderers
        ragdollRenderers = ragdollGO.GetComponentsInChildren<SkinnedMeshRenderer>();
        animatedRenderers = animatedGO.GetComponentsInChildren<SkinnedMeshRenderer>();
        
        // Cache synchronized joints
        synchronizedJoints = ragdollGO.GetComponentsInChildren<SynchronizedJoint>();
        
        // Store original joint drives
        originalSlerpDrives = new Dictionary<SynchronizedJoint, JointDrive>();
        foreach (var syncJoint in synchronizedJoints)
        {
            ConfigurableJoint joint = syncJoint.GetComponent<ConfigurableJoint>();
            if (joint != null)
            {
                originalSlerpDrives[syncJoint] = joint.slerpDrive;
            }
        }
        
        Debug.Log($"PhysicsManager cached {ragdollRenderers.Length} ragdoll renderers, " +
                  $"{animatedRenderers.Length} animated renderers, " +
                  $"{synchronizedJoints.Length} synchronized joints");
    }
    
    public void EnableAnimationMode()
    {
        if (isInAnimationMode) return;
        
        // 1. Store the animated rig's current local position/rotation
        animatedRigStartPosition = animatedGO.transform.localPosition;
        animatedRigStartRotation = animatedGO.transform.localRotation;
        
        // 2. Sync physics rig to animated rig's current pose
        SyncPhysicsToAnimated();
        
        // 3. Disable physics influence
        DisableJointSprings();
        
        // 4. Switch renderers
        SetRenderersActive(ragdollRenderers, false);
        SetRenderersActive(animatedRenderers, true);
        
        isInAnimationMode = true;
        Debug.Log("Switched to Animation Mode");
    }

    public void EnablePhysicsMode()
    {
        if (!isInAnimationMode && synchronizedJoints != null && !firstTimeEnabling) return;

        if (springRestoreCoroutine != null)
        {
            StopCoroutine(springRestoreCoroutine);
        }

        // 1. Calculate how much the animated rig moved (root motion delta)
        Vector3 positionDelta = animatedGO.transform.localPosition - animatedRigStartPosition;
        Quaternion rotationDelta = animatedGO.transform.localRotation * Quaternion.Inverse(animatedRigStartRotation);

        // 2. Apply that movement to the parent GameObject
        transform.position += transform.TransformDirection(positionDelta);
        transform.rotation = rotationDelta * transform.rotation;

        // 3. Reset animated rig back to its starting local position
        animatedGO.transform.localPosition = animatedRigStartPosition;
        animatedGO.transform.localRotation = animatedRigStartRotation;

        // 4. Sync physics rig to animated rig's final pose
        StartCoroutine(SwapRenderersAtEndOfFrame());

        isInAnimationMode = false;
    }

    /// <summary>
    /// Disables all joint springs so they don't influence the physics rig during animation
    /// </summary>
    private void DisableJointSprings()
    {
        JointDrive zeroDrive = new JointDrive
        {
            positionSpring = 0f,
            positionDamper = 0f,
            maximumForce = 0f
        };

        foreach (var syncJoint in synchronizedJoints)
        {
            ConfigurableJoint joint = syncJoint.GetComponent<ConfigurableJoint>();
            if (joint != null)
            {
                joint.slerpDrive = zeroDrive;
            }
        }
    }
    
    
    /// <summary>
    /// Restores original joint spring values for physics mode
    /// </summary>
    private void RestoreJointSprings()
    {
        foreach (var syncJoint in synchronizedJoints)
        {
            ConfigurableJoint joint = syncJoint.GetComponent<ConfigurableJoint>();
            if (joint != null && originalSlerpDrives.ContainsKey(syncJoint))
            {
                joint.slerpDrive = originalSlerpDrives[syncJoint];
            }
        }
    }

    /// <summary>
    /// Syncs physics rig bones to match animated rig's current pose
    /// This prevents visual "popping" when switching between modes
    /// </summary>
    private void SyncPhysicsToAnimated()
    {
        foreach (var syncJoint in synchronizedJoints)
        {
            Transform physicsTransform = syncJoint.transform;
            Transform targetTransform = syncJoint.TargetTransform;

            if (targetTransform == null) continue;

            Rigidbody rb = physicsTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Teleport the rigidbody (doesn't add velocity)
                rb.position = targetTransform.position;
                rb.rotation = targetTransform.rotation;

                // Zero out velocities to prevent unwanted movement
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                // Fallback for bones without rigidbodies
                physicsTransform.position = targetTransform.position;
                physicsTransform.rotation = targetTransform.rotation;
            }
        }
    }

    private IEnumerator SwapRenderersAtEndOfFrame()
    {
        // 1. Sync physics to animated
        SyncPhysicsToAnimated();

        // 2. UPDATE JOINT TARGETS to match the animated rig's pose
        UpdateJointTargets();

        // 3. Force physics update
        Physics.SyncTransforms();

        // 4. Restore springs immediately
        RestoreJointSprings();

        // 5. Wait one fixed update
        yield return new WaitForFixedUpdate();

        // 6. Swap renderers
        SetRenderersActive(animatedRenderers, false);
        SetRenderersActive(ragdollRenderers, true);
    }
    
    private void UpdateJointTargets()
    {
        foreach (var syncJoint in synchronizedJoints)
        {
            // This calls your existing SyncJoint method which sets the targetRotation
            syncJoint.SyncJoint();
        }
    }
    
    /// <summary>
    /// Helper method to enable/disable all renderers in an array
    /// </summary>
    private void SetRenderersActive(SkinnedMeshRenderer[] renderers, bool active)
    {
        foreach (var renderer in renderers)
        {
            renderer.enabled = active;
        }
    }
}