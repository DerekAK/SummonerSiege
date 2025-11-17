using UnityEngine;

public static class ConfigurableJointExtensions {
	/// <summary>
	/// Sets a joint's targetRotation to match a given local rotation.
	/// The joint transform's local rotation must be cached on Start and passed into this method.
	/// </summary>
	public static void SetTargetRotationLocal (this ConfigurableJoint joint, Quaternion targetLocalRotation, Quaternion startLocalRotation)
	{
		if (joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotationLocal should not be used with joints that are configured in world space. For world space joints, use SetTargetRotation.", joint);
		}
		SetTargetRotationInternal (joint, targetLocalRotation, startLocalRotation, Space.Self);
	}
	
	/// <summary>
	/// Sets a joint's targetRotation to match a given world rotation.
	/// The joint transform's world rotation must be cached on Start and passed into this method.
	/// </summary>
	public static void SetTargetRotation (this ConfigurableJoint joint, Quaternion targetWorldRotation, Quaternion startWorldRotation)
	{
		if (!joint.configuredInWorldSpace) {
			Debug.LogError ("SetTargetRotation must be used with joints that are configured in world space. For local space joints, use SetTargetRotationLocal.", joint);
		}
		SetTargetRotationInternal (joint, targetWorldRotation, startWorldRotation, Space.World);
	}
	
	static void SetTargetRotationInternal (ConfigurableJoint joint, Quaternion targetRotation, Quaternion startRotation, Space space)
    {
        // Calculate the rotation expressed by the joint's axis and secondary axis
        var right = joint.axis.normalized;
        var forward = Vector3.Cross (joint.axis, joint.secondaryAxis).normalized;
        var up = Vector3.Cross (forward, right).normalized;
        
        // Safety check for degenerate cases
        if (forward.sqrMagnitude < 0.01f || up.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning($"Degenerate joint axes on {joint.name}. Skipping rotation update.");
            return;
        }
        
        Quaternion worldToJointSpace = Quaternion.LookRotation (forward, up);
        
        // Transform into world space
        Quaternion resultRotation = Quaternion.Inverse (worldToJointSpace);
        
        // Counter-rotate and apply the new local rotation.
        if (space == Space.World) {
            resultRotation *= startRotation * Quaternion.Inverse (targetRotation);
        } else {
            resultRotation *= Quaternion.Inverse (targetRotation) * startRotation;
        }
        
        // Transform back into joint space
        resultRotation *= worldToJointSpace;
        
        // CRITICAL: Normalize before setting to prevent invalid quaternions
        resultRotation.Normalize();
        
        // Additional safety check
        if (float.IsNaN(resultRotation.x) || float.IsNaN(resultRotation.y) || 
            float.IsNaN(resultRotation.z) || float.IsNaN(resultRotation.w))
        {
            Debug.LogError($"NaN in targetRotation calculation for {joint.name}. Using identity.");
            return;
        }
        
        joint.targetRotation = resultRotation;
    }
}