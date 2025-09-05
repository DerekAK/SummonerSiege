using Unity.Collections;
using Unity.Mathematics;

public static class MarchingCubes
{
    public static void March(float3 position, float isoLevel, int step, NativeArray<float> cubeDensities, NativeList<float3> vertices, NativeList<int> triangles, NativeArray<float3> edgeVertices)
    {
        int cubeIndex = 0;
        if (cubeDensities[0] < isoLevel) cubeIndex |= 1;
        if (cubeDensities[1] < isoLevel) cubeIndex |= 2;
        if (cubeDensities[2] < isoLevel) cubeIndex |= 4;
        if (cubeDensities[3] < isoLevel) cubeIndex |= 8;
        if (cubeDensities[4] < isoLevel) cubeIndex |= 16;
        if (cubeDensities[5] < isoLevel) cubeIndex |= 32;
        if (cubeDensities[6] < isoLevel) cubeIndex |= 64;
        if (cubeDensities[7] < isoLevel) cubeIndex |= 128;

        if (MarchingCubesTables.edgeTable[cubeIndex] == 0)
        {
            return;
        }

        if ((MarchingCubesTables.edgeTable[cubeIndex] & 1) != 0)
            edgeVertices[0] = InterpolateVertex(position + cornerOffsets[0] * step, position + cornerOffsets[1] * step, cubeDensities[0], cubeDensities[1], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 2) != 0)
            edgeVertices[1] = InterpolateVertex(position + cornerOffsets[1] * step, position + cornerOffsets[2] * step, cubeDensities[1], cubeDensities[2], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 4) != 0)
            edgeVertices[2] = InterpolateVertex(position + cornerOffsets[2] * step, position + cornerOffsets[3] * step, cubeDensities[2], cubeDensities[3], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 8) != 0)
            edgeVertices[3] = InterpolateVertex(position + cornerOffsets[3] * step, position + cornerOffsets[0] * step, cubeDensities[3], cubeDensities[0], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 16) != 0)
            edgeVertices[4] = InterpolateVertex(position + cornerOffsets[4] * step, position + cornerOffsets[5] * step, cubeDensities[4], cubeDensities[5], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 32) != 0)
            edgeVertices[5] = InterpolateVertex(position + cornerOffsets[5] * step, position + cornerOffsets[6] * step, cubeDensities[5], cubeDensities[6], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 64) != 0)
            edgeVertices[6] = InterpolateVertex(position + cornerOffsets[6] * step, position + cornerOffsets[7] * step, cubeDensities[6], cubeDensities[7], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 128) != 0)
            edgeVertices[7] = InterpolateVertex(position + cornerOffsets[7] * step, position + cornerOffsets[4] * step, cubeDensities[7], cubeDensities[4], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 256) != 0)
            edgeVertices[8] = InterpolateVertex(position + cornerOffsets[0] * step, position + cornerOffsets[4] * step, cubeDensities[0], cubeDensities[4], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 512) != 0)
            edgeVertices[9] = InterpolateVertex(position + cornerOffsets[1] * step, position + cornerOffsets[5] * step, cubeDensities[1], cubeDensities[5], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 1024) != 0)
            edgeVertices[10] = InterpolateVertex(position + cornerOffsets[2] * step, position + cornerOffsets[6] * step, cubeDensities[2], cubeDensities[6], isoLevel);
        if ((MarchingCubesTables.edgeTable[cubeIndex] & 2048) != 0)
            edgeVertices[11] = InterpolateVertex(position + cornerOffsets[3] * step, position + cornerOffsets[7] * step, cubeDensities[3], cubeDensities[7], isoLevel);

        int triTableIndex = cubeIndex * 16;
        for (int i = 0; MarchingCubesTables.flatTriTable[triTableIndex + i] != -1; i += 3)
        {
            int baseVertexIndex = vertices.Length;

            vertices.Add(edgeVertices[MarchingCubesTables.flatTriTable[triTableIndex + i + 2]]);
            vertices.Add(edgeVertices[MarchingCubesTables.flatTriTable[triTableIndex + i + 1]]);
            vertices.Add(edgeVertices[MarchingCubesTables.flatTriTable[triTableIndex + i]]);
            
            triangles.Add(baseVertexIndex);
            triangles.Add(baseVertexIndex + 1);
            triangles.Add(baseVertexIndex + 2);
        }
    }
    
    private static float3 InterpolateVertex(float3 p1, float3 p2, float d1, float d2, float isoLevel)
    {
        if (math.abs(isoLevel - d1) < 0.00001f) return p1;
        if (math.abs(isoLevel - d2) < 0.00001f) return p2;
        if (math.abs(d1 - d2) < 0.00001f) return p1;

        float mu = (isoLevel - d1) / (d2 - d1);
        return p1 + mu * (p2 - p1);
    }
    
    private static readonly float3[] cornerOffsets = {
        new float3(0, 0, 0), new float3(1, 0, 0), new float3(1, 0, 1), new float3(0, 0, 1),
        new float3(0, 1, 0), new float3(1, 1, 0), new float3(1, 1, 1), new float3(0, 1, 1)
    };
}