// AgentNavigationProfileSO.cs
using UnityEngine;

// The AgentType enum has been removed.

[CreateAssetMenu(fileName = "NewAgentNavProfile", menuName = "Scriptable Objects/Procedural/Agent Navigation Profile")]
public class AgentNavigationProfileSO : ScriptableObject
{
    // This object is now purely a data container for an agent's physical capabilities.
    [Header("Traversal Capabilities")]
    [Tooltip("The maximum vertical distance this agent can drop down.")]
    public float maxFallHeight = 5f;

    [Tooltip("The maximum horizontal distance this agent can jump across a gap.")]
    public float maxJumpDistance = 4f;

    [Tooltip("The steepest slope (in degrees) this agent can scramble up/down.")]
    public float maxSlopeAngle = 60f;
    public float maxClimbDistance;
}