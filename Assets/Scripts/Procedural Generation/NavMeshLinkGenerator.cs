using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

public static class NavMeshLinkGenerator
{
    // A coroutine to find and create NavMesh links for a specific chunk.
    public static IEnumerator GenerateLinksForChunk(Transform linkParent, EndlessTerrain.TerrainChunk chunk, AgentNavigationProfileSO[] agentProfiles)
    {
        if (chunk == null || chunk.IsDestroyed || agentProfiles == null || agentProfiles.Length == 0)
        {
            yield break;
        }

        // A list to hold all objects flagged as priority targets for linking.
        var priorityTargets = new List<(GameObject, PlaceableObject)>();
        foreach (var objTuple in chunk.ActivePlaceableObjects)
        {
            if (objTuple.Item2 != null && objTuple.Item2.isPriorityLinkTarget)
            {
                priorityTargets.Add(objTuple);
            }
        }

        if (priorityTargets.Count == 0)
        {
            Debug.LogError("Priority targets for navmeshlinks are empty!");
            yield break;
        }

        // Define how many points to check around each object. 8 points = every 45 degrees.
        const int pointsToSampleAroundObject = 8;
        // The distance from the object's pivot to sample for a ground connection point.
        const float sampleRadius = 3.0f; 

        foreach (var target in priorityTargets)
        {
            if (chunk.IsDestroyed) yield break;

            GameObject placedObject = target.Item1;
            PlaceableObject config = target.Item2;
            
            // The 'end point' of our potential link, located on the object itself.
            Vector3 objectLinkPoint = placedObject.transform.TransformPoint(config.linkAnchorOffset);

            for (int i = 0; i < pointsToSampleAroundObject; i++)
            {
                if (chunk.IsDestroyed) yield break;

                // Calculate a sample point on the ground around the object.
                float angle = i * (360f / pointsToSampleAroundObject);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 sampleOrigin = placedObject.transform.position + direction * sampleRadius;
                sampleOrigin.y += 5; // Start the raycast from above to ensure it hits the ground.

                // Find a valid point on the NavMesh below our sample origin.
                if (TryFindNavMeshPoint(sampleOrigin, 10f, out NavMeshHit groundHit))
                {
                    Vector3 groundLinkPoint = groundHit.position;

                    // Check if there's a clear path. If there isn't, a link might be needed.
                    if (NavMesh.Raycast(groundLinkPoint, objectLinkPoint, out NavMeshHit _, NavMesh.AllAreas))
                    {
                        // Calculate the link's properties.
                        Vector3 offset = objectLinkPoint - groundLinkPoint;
                        float horizontalDist = new Vector2(offset.x, offset.z).magnitude;
                        float verticalDist = Mathf.Abs(offset.y);

                        // Check this potential link against all agent profiles.
                        foreach (var profile in agentProfiles)
                        {
                            if (chunk.IsDestroyed) yield break;

                            // Does this agent have the ability to make this jump/climb?
                            bool canTraverse = (verticalDist <= profile.maxClimbDistance && horizontalDist <= profile.maxJumpDistance) ||
                                               (verticalDist <= profile.maxFallHeight && horizontalDist <= profile.maxJumpDistance);

                            if (canTraverse)
                            {
                                // Create the link GameObject.
                                GameObject linkObject = new GameObject($"NavMeshLink_{placedObject.name}");
                                linkObject.transform.SetParent(linkParent);
                                // MODIFIED: Position the link object at the start point.
                                linkObject.transform.position = groundLinkPoint;
                                var link = linkObject.AddComponent<NavMeshLink>();

                                // Configure the link's start and end points.
                                link.startPoint = placedObject.transform.InverseTransformPoint(groundLinkPoint);
                                link.endPoint = placedObject.transform.InverseTransformPoint(objectLinkPoint);
                                link.width = 1.5f;
                                link.bidirectional = true;

                                chunk.AddGeneratedLink(link);
                                
                                // One link is enough for this spot, move to the next sample point.
                                break; 
                            }
                        }
                    }
                }
            }
            // Yielding allows the main thread to continue, preventing the game from freezing.
            yield return null; 
        }
    }

    // Helper function to find the closest valid point on a NavMesh within a certain range.
    private static bool TryFindNavMeshPoint(Vector3 origin, float range, out NavMeshHit hit)
    {
        // First, raycast down to find the actual ground surface.
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit raycastHit, range * 2))
        {
            // Then, find the closest point on the NavMesh to where our raycast hit.
            if (NavMesh.SamplePosition(raycastHit.point, out hit, 2.0f, NavMesh.AllAreas))
            {
                return true;
            }
        }
        hit = default;
        return false;
    }
}
