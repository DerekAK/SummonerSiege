using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    [Header("Model References")]
    [SerializeField] private GameObject ragdollGO;
    [SerializeField] private GameObject animatedGO;
    [SerializeField] private SkinnedMeshRenderer[] ragdollHeadRenderers;
    [SerializeField] private SkinnedMeshRenderer[] animatedHeadRenderers;
    private SkinnedMeshRenderer[] currentHeadSkinnedMeshRenderers;
    public SkinnedMeshRenderer[] CurrHeadRenderers => currentHeadSkinnedMeshRenderers;

    private SkinnedMeshRenderer[] ragdollRenderers;
    private SkinnedMeshRenderer[] animatedRenderers;
    private SynchronizedJoint[] synchronizedJoints;
    private Dictionary<SynchronizedJoint, JointDrive> originalSlerpDrives;

    private Vector3 animatedRigStartPosition;
    private Quaternion animatedRigStartRotation;
    
    private bool isInAnimationMode = true; // set to true so first call to enablephysicsmode will go through
    public bool IsInAnimationMode => isInAnimationMode;

    private void Awake()
    {
        CacheComponents();
    }
    
    private void Start()
    {
        animatedRigStartPosition = animatedGO.transform.localPosition;
        animatedRigStartRotation = animatedGO.transform.localRotation;
        
        // Start in physics mode
        EnablePhysicsMode();
    }

    private void FixedUpdate()
    {
       SyncJointsWithAnimation(); 
    }

    private void SyncJointsWithAnimation()
    {
        foreach (SynchronizedJoint joint in synchronizedJoints)
        {       
            joint.SyncJoint();
        }
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
    
    public void EnablePhysicsMode()
    {
        // Early return if already in physics mode
        if (!isInAnimationMode)
        {
            Debug.Log("Already in physics mode, ignoring EnablePhysicsMode call");
            return;
        }
        
        Debug.Log("Switching to Physics Mode");

        currentHeadSkinnedMeshRenderers = ragdollHeadRenderers;

        // Calculate how much the animated rig moved (root motion delta)
        Vector3 positionDelta = animatedGO.transform.localPosition - animatedRigStartPosition;
        Quaternion rotationDelta = animatedGO.transform.localRotation * Quaternion.Inverse(animatedRigStartRotation);

        // Apply that movement to the parent GameObject (transfer root motion)
        transform.position += transform.TransformDirection(positionDelta);
        transform.rotation = rotationDelta * transform.rotation;

        // Reset animated rig back to its starting local position
        animatedGO.transform.localPosition = animatedRigStartPosition;
        animatedGO.transform.localRotation = animatedRigStartRotation;

        // Sync physics rig to animated rig's final pose and swap renderers
        StartCoroutine(TransitionToPhysicsMode());
        
        // Set flag FIRST to prevent re-entry during coroutine
        isInAnimationMode = false;
    }
    
    public void EnableAnimationMode()
    {
        // Early return if already in animation mode
        if (isInAnimationMode)
        {
            Debug.Log("Already in animation mode, ignoring EnableAnimationMode call");
            return;
        }
        
        Debug.Log("Switching to Animation Mode");
        
        currentHeadSkinnedMeshRenderers = animatedHeadRenderers;

        // Store the animated rig's current local position/rotation as reference point
        animatedRigStartPosition = animatedGO.transform.localPosition;
        animatedRigStartRotation = animatedGO.transform.localRotation;
        
        // Sync physics rig to animated rig's current pose to prevent popping
        SyncPhysicsToAnimated();
        
        // Disable physics influence on the ragdoll
        DisableJointSprings();
        
        // Switch renderers to show animated model
        SetRenderersActive(ragdollRenderers, false);
        SetRenderersActive(animatedRenderers, true);
        
        // Set flag LAST to prevent re-entry
        isInAnimationMode = true;
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

    /// <summary>
    /// Coroutine to handle the transition back to physics mode
    /// </summary>
    private IEnumerator TransitionToPhysicsMode()
    {
        // Sync physics rig to match animated rig's final pose
        SyncPhysicsToAnimated();

        // Update joint targets to match the animated rig's pose
        UpdateJointTargets();

        // Force physics update
        Physics.SyncTransforms();

        // Restore springs immediately
        RestoreJointSprings();

        // Wait one fixed update for physics to stabilize
        yield return new WaitForFixedUpdate();

        // Swap renderers to show ragdoll
        SetRenderersActive(animatedRenderers, false);
        SetRenderersActive(ragdollRenderers, true);
        
        Debug.Log("Physics mode transition complete");
    }
    
    /// <summary>
    /// Updates all joint targets to match their corresponding animated bones
    /// </summary>
    private void UpdateJointTargets()
    {
        foreach (var syncJoint in synchronizedJoints)
        {
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