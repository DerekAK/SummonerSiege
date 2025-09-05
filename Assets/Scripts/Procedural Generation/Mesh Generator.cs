using UnityEngine;

public static class MeshGenerator {
	public static MeshData GenerateTerrainMeshData(float[,] heightMap, float heightMultiplier, int levelOfDetail, BiomeSO2 biome) {
        int chunkSize = heightMap.GetLength(0);
        float topLeftX = (chunkSize - 1) / -2f;
        float topLeftZ = (chunkSize - 1) / 2f;

        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;

        int verticesPerChunkSide = (chunkSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerChunkSide, verticesPerChunkSide);
        int vertexIndex = 0;

        for (int y = 0; y < chunkSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < chunkSize; x += meshSimplificationIncrement) {

                AnimationCurve noiseSplineCurveCopy = new AnimationCurve (biome.NoiseSplineCurve.keys);

                float vertexY = noiseSplineCurveCopy.Evaluate(heightMap[x, y]) * heightMultiplier;

                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, vertexY, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)chunkSize, y / (float)chunkSize);

                if (x < chunkSize - 1 && y < chunkSize - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerChunkSide + 1, vertexIndex + verticesPerChunkSide);
                    meshData.AddTriangle(vertexIndex + verticesPerChunkSide + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;

    }
}

public class MeshData {
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;
	int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
	}

	public void AddTriangle(int a, int b, int c) {
		triangles [triangleIndex] = a;
		triangles [triangleIndex + 1] = b;
		triangles [triangleIndex + 2] = c;
		triangleIndex += 3;
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals ();
		return mesh;
	}

}