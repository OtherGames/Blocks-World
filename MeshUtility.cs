using UnityEngine;
using System.Collections.Generic;

public class MeshUtility
{
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
