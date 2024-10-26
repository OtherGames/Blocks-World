using UnityEngine;
using System.Collections.Generic;

public static class MeshOptimizer
{
    public static Mesh OptimizeMesh(Mesh originalMesh, float mergeDistance = 1f)
    {
        // ������� ����� ���
        Mesh optimizedMesh = new Mesh
        {
            name = originalMesh.name + "_LOD"
        };

        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] originalUV = originalMesh.uv;

        List<Vector3> newVertices = new List<Vector3>();
        List<Vector2> newUV = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        // ���������� ������� ��� ������������ ���������� ������
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        // ���������� ��� ������������
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int index0 = triangles[i];
            int index1 = triangles[i + 1];
            int index2 = triangles[i + 2];

            Vector3 v0 = vertices[index0];
            Vector3 v1 = vertices[index1];
            Vector3 v2 = vertices[index2];

            // �������� �� ������� ������������
            if (IsEdgeVertex(v0, mergeDistance) || IsEdgeVertex(v1, mergeDistance) || IsEdgeVertex(v2, mergeDistance))
            {
                AddVertex(v0, vertexMap, newVertices, newUV, originalUV[index0]);
                AddVertex(v1, vertexMap, newVertices, newUV, originalUV[index1]);
                AddVertex(v2, vertexMap, newVertices, newUV, originalUV[index2]);

                // ��������� ������� ��� ������ ������� �������������
                newTriangles.Add(vertexMap[v0]);
                newTriangles.Add(vertexMap[v1]);
                newTriangles.Add(vertexMap[v2]);
            }
        }

        // �������� �� ��������� �������������
        if (newTriangles.Count % 3 != 0 || newTriangles.Count < 3)
        {
            Debug.LogError("���������� ������������� �� �������� ��� �������� ����������������� ����.");
            return originalMesh; // ���������� ������������ ���, ���� �������� ������
        }

        optimizedMesh.vertices = newVertices.ToArray();
        optimizedMesh.triangles = newTriangles.ToArray();
        optimizedMesh.uv = newUV.ToArray();

        optimizedMesh.RecalculateNormals();
        optimizedMesh.RecalculateBounds();

        return optimizedMesh;
    }

    private static void AddVertex(Vector3 vertex, Dictionary<Vector3, int> vertexMap, List<Vector3> newVertices, List<Vector2> newUV, Vector2 uv)
    {
        // ���������, ���������� �� �������
        if (!vertexMap.ContainsKey(vertex))
        {
            vertexMap[vertex] = newVertices.Count;
            newVertices.Add(vertex);
            newUV.Add(uv);
        }
    }

    private static bool IsEdgeVertex(Vector3 vertex, float chunkSize)
    {
        // ����������, �������� �� ������� ������� (�� ������� �����)
        return Mathf.Approximately(vertex.x, 0) || Mathf.Approximately(vertex.x, chunkSize) ||
               Mathf.Approximately(vertex.y, 0) || Mathf.Approximately(vertex.y, chunkSize) ||
               Mathf.Approximately(vertex.z, 0) || Mathf.Approximately(vertex.z, chunkSize);
    }
}
