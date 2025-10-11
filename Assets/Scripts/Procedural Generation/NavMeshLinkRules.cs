// NavMeshLinkRules.cs

using System;
using UnityEngine;

// NOTE: All three rule structs are placed in this single file for organization.

[Serializable]
public struct NavMeshCliffRule
{
    [Header("Detection")]
    [Tooltip("The minimum vertical drop distance to be considered for this link type.")]
    public float minVerticalDistance;
    [Tooltip("The maximum vertical drop distance for this link type.")]
    public float maxVerticalDistance;
    [Tooltip("How far from a potential landing spot the system should search for a valid surface (in voxels).")]
    [Range(0, 10)]
    public int landingSearchRadius;

    [Header("Link Properties")]
    [Tooltip("The Agent Type ID this link is for. (0=Humanoid)")]
    public int agentTypeID;
    public bool bidirectional;
    [Tooltip("Cost multiplier for pathfinding. -1 to not override.")]
    public float costOverride;
}

[Serializable]
public struct NavMeshGapRule
{
    [Header("Detection")]
    [Tooltip("The minimum horizontal distance to be considered for this link type.")]
    public float minHorizontalDistance;
    [Tooltip("The maximum horizontal distance for this link type.")]
    public float maxHorizontalDistance;
    [Tooltip("The maximum allowed vertical height difference between the start and end of the gap.")]
    public float maxVerticalDisplacement;
    [Tooltip("How far from a potential landing spot the system should search for a valid surface (in voxels).")]
    [Range(0, 10)]
    public int landingSearchRadius;

    [Header("Link Properties")]
    public int agentTypeID;
    public bool bidirectional;
    public float costOverride;
}

[Serializable]
public struct NavMeshSlopeRule
{
    [Header("Detection")]
    [Tooltip("The minimum slope angle (in degrees) to be considered unwalkable.")]
    [Range(1, 90)]
    public float minSlopeAngle;
    [Tooltip("The maximum slope angle (in degrees).")]
    [Range(1, 90)]
    public float maxSlopeAngle;
    [Tooltip("The minimum length of the steep path for a link to be created.")]
    public float minLinkLength;
    [Tooltip("The maximum length of the steep path.")]
    public float maxLinkLength;

    [Header("Link Properties")]
    public int agentTypeID;
    public bool bidirectional;
    public float costOverride;
}