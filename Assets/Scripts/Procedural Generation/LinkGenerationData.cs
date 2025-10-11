// LinkGenerationData.cs

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

// The output data from a link-finding job
public struct LinkPointData
{
    public float3 startPoint;
    public float3 endPoint;
    public int ruleIndex;
    public int agentTypeID; // Store this to update the correct bitmask
}

// A container to pass all necessary data from EndlessTerrain to the new generator
public class ChunkLinkRequest
{
    public int2 chunkCoord;
    public int lod;
    public Transform parentTransform;
    public NativeArray<float> densityField;
    public List<float3> priorityAnchors;
    public int3 chunkSize;
    public float isoLevel;
}