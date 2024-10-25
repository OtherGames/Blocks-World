using UnityEngine;
using System.Collections.Generic;

public class MeshUtility : MonoBehaviour
{
    public static MeshUtility Single;

    private void Awake()
    {
        Single = this;
    }

    public int chunkSize = 16;  // ������ �����
    public int chunkHeight = 16;  // ������ �����

    // ����� ��� ��������� ����� � ������ LOD
    public Mesh SimplifyChunkMesh(Mesh originalMesh, float mergeThreshold = 0.1f)
    {
        // ������ ��� ����� ������, ������������� � UV
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector2> newUVs = new List<Vector2>();

        // ����� ��� ��������� ������
        Dictionary<Vector3, Vector3> vertexClusters = new Dictionary<Vector3, Vector3>();

        // �������� �������� ������ ����
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] uvs = originalMesh.uv;

        // �������� �� ���� �������� � ���������� �� � ��������
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 currentVertex = vertices[i];

            if (!IsEdgeVertex(currentVertex))
            {
                // ���������� �������, ������� ��������� ������ ���� � �����
                Vector3 clusterVertex = FindOrCreateCluster(vertexClusters, currentVertex, mergeThreshold);
                vertices[i] = clusterVertex; // �������� ������� �� � �������
            }
        }

        // ������� �� ������������� � �������� ����� ���
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            // ���������, ���� ����������� �� ���� ����������� (��������, ��-�� ����������� ������)
            if (v1 != v2 && v2 != v3 && v1 != v3)
            {
                AddTriangle(newVertices, newTriangles, newUVs, v1, v2, v3, uvs[triangles[i]], uvs[triangles[i + 1]], uvs[triangles[i + 2]]);
            }
        }

        // ������ ����� ��� � ���������� ����������
        Mesh simplifiedMesh = new Mesh();
        simplifiedMesh.vertices = newVertices.ToArray();
        simplifiedMesh.triangles = newTriangles.ToArray();
        simplifiedMesh.uv = newUVs.ToArray();
        simplifiedMesh.RecalculateNormals();

        return simplifiedMesh;
    }

    // ����� ��� �������� ��� ������ �������� ��� �������
    private Vector3 FindOrCreateCluster(Dictionary<Vector3, Vector3> vertexClusters, Vector3 vertex, float threshold)
    {
        foreach (var clusterVertex in vertexClusters.Keys)
        {
            if (Vector3.Distance(vertex, clusterVertex) < threshold)
            {
                // ���� ������� ���������� ������ � ��������, ���������� ���������� �������
                return clusterVertex;
            }
        }

        // ���� ������� �� ������, ������ ����� �������
        vertexClusters[vertex] = vertex;
        return vertex;
    }

    // ���������, �������� �� ������� �������� �����
    private bool IsEdgeVertex(Vector3 vertex)
    {
        // ��������, �������� �� ������� ���������
        return vertex.x == 0 || vertex.x == chunkSize - 1 ||
               vertex.y == 0 || vertex.y == chunkHeight - 1 ||
               vertex.z == 0 || vertex.z == chunkSize - 1;
    }

    // ��������� ����������� � ����� ���
    private void AddTriangle(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
        Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        int index = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    //=======================================================================
    static Vector3 blockableVertexOffset = new Vector3(-0.5f, 0.5f, 0.5f);

    // ����� ��� ����������� ���� SubMesh � ���������� � ��� ������������� ����
    public static void CombineAndAppendMesh(
        Mesh subMeshToCombine,
        List<Vector3> vertices,
        List<int> triangulos,
        List<Vector2> uvs,
        Vector3 offset
    )
    { 
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        // ����������� �������� �������� ��� �������� ����
        int vertexOffset = vertices.Count;

        // ��������� ��� ��� ������������ ������� � ����� (vertexMap)
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!vertexMap.ContainsKey(vertices[i]))
            {
                vertexMap.Add(vertices[i], i);
            }
        }

        // ������ ��� �������� ������ ������ ����
        List<Vector3> combinedVertices = new List<Vector3>();
        List<Vector2> combinedUVs = new List<Vector2>();
        List<int> combinedIndices = new List<int>();

        // �������� �� ���� SubMeshes � ����� ����
        for (int subMeshIndex = 0; subMeshIndex < subMeshToCombine.subMeshCount; subMeshIndex++)
        {
            // �������� �������, ������� � UV ��� �������� �������
            Vector3[] subMeshVertices = subMeshToCombine.vertices;
            Vector2[] subMeshUVs = subMeshToCombine.uv;

            // �������� ������� ������������� ��� �������� �������
            int[] subMeshIndices = subMeshToCombine.GetTriangles(subMeshIndex);

            // ������������ ������ ������ ������������
            for (int i = 0; i < subMeshIndices.Length; i++)
            {
                int originalIndex = subMeshIndices[i];
                Vector3 vertex = subMeshVertices[originalIndex];

                // ���������, ���������� �� ��� ��� ������� � ����� ������� (vertexMap)
                if (!vertexMap.ContainsKey(vertex))
                {
                    // ���� ������� �����, ��������� � � ����� � ����������� offset
                    vertices.Add(vertex + offset + blockableVertexOffset);
                    uvs.Add(subMeshUVs[originalIndex]);
                    vertexMap[vertex] = vertexOffset++;
                }

                // ��������� ����������������� ������ � ����� ������ ��������
                triangulos.Add(vertexMap[vertex]);
            }
        }
    }

    // ����� ��� ����������� ���� SubMesh � ���� Mesh
    public static Mesh CombineSubMeshes(Mesh originalMesh)
    {
        // ������� ����� ������ Mesh
        Mesh combinedMesh = new Mesh();

        // ������ ��� �������� ������������ ������
        List<Vector3> combinedVertices = new List<Vector3>();
        List<Vector3> combinedNormals = new List<Vector3>();
        List<Vector2> combinedUVs = new List<Vector2>();
        List<int> combinedIndices = new List<int>();

        // ���������� ��� �������� ��������
        int vertexOffset = 0;

        // �������� �� ���� SubMeshes
        for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
        {
            // �������� �������, ������� � UV ��� �������� �������
            Vector3[] subMeshVertices = originalMesh.vertices;
            Vector3[] subMeshNormals = originalMesh.normals;
            Vector2[] subMeshUVs = originalMesh.uv;

            // ��������� �������, ������� � UV � ������ ������
            combinedVertices.AddRange(subMeshVertices);
            combinedNormals.AddRange(subMeshNormals);
            combinedUVs.AddRange(subMeshUVs);

            // �������� ������� ������������� ��� �������� �������
            int[] subMeshIndices = originalMesh.GetTriangles(subMeshIndex);

            // ������������ ������� (� ������ ��������)
            for (int i = 0; i < subMeshIndices.Length; i++)
            {
                combinedIndices.Add(subMeshIndices[i] + vertexOffset);
            }

            // ��������� �������� ��� ���������� �������
            vertexOffset += subMeshVertices.Length;
        }

        // ������������� ������ ��� ������ ������������� ����
        combinedMesh.SetVertices(combinedVertices);
        combinedMesh.SetNormals(combinedNormals);
        combinedMesh.SetUVs(0, combinedUVs);
        combinedMesh.SetTriangles(combinedIndices, 0);

        // ��������� ������� ����
        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();

        return combinedMesh;
    }

    private static void JustAddMesh(Mesh mesh, List<Vector3> vertices,
        List<int> triangulos,
        List<Vector2> uvs, Vector3 offset)
    {
        foreach (var triangle in mesh.triangles)
        {
            triangulos.Add(triangle + vertices.Count);
        }
        var meshVertices = mesh.vertices;
        //meshVertices = RotationUtility.RotatePoints(meshVertices, 90, RotationUtility.Axis.X);

        foreach (var vrtx in meshVertices)
        {
            vertices.Add(vrtx + offset + blockableVertexOffset);
        }
        foreach (var item in mesh.uv)
        {
            uvs.Add(item);
        }
    }
}
