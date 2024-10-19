using UnityEngine;
using System.Collections.Generic;

public class MeshUtility
{
    static Vector3 blockableVertexOffset = new Vector3(-0.5f, 0.5f, 0.5f);

    // Метод для объединения всех SubMesh и добавления к уже существующему мешу
    public static void CombineAndAppendMesh(
        Mesh subMeshToCombine,
        List<Vector3> vertices,
        List<int> triangulos,
        List<Vector2> uvs,
        Vector3 offset
    )
    { 
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();

        // Изначальное смещение индексов для базового меша
        int vertexOffset = vertices.Count;

        // Скопируем все уже существующие вершины в карту (vertexMap)
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!vertexMap.ContainsKey(vertices[i]))
            {
                vertexMap.Add(vertices[i], i);
            }
        }

        // Списки для хранения данных нового меша
        List<Vector3> combinedVertices = new List<Vector3>();
        List<Vector2> combinedUVs = new List<Vector2>();
        List<int> combinedIndices = new List<int>();

        // Проходим по всем SubMeshes в новом меше
        for (int subMeshIndex = 0; subMeshIndex < subMeshToCombine.subMeshCount; subMeshIndex++)
        {
            // Получаем вершины, нормали и UV для текущего подмеша
            Vector3[] subMeshVertices = subMeshToCombine.vertices;
            Vector2[] subMeshUVs = subMeshToCombine.uv;

            // Получаем индексы треугольников для текущего подмеша
            int[] subMeshIndices = subMeshToCombine.GetTriangles(subMeshIndex);

            // Обрабатываем каждый индекс треугольника
            for (int i = 0; i < subMeshIndices.Length; i++)
            {
                int originalIndex = subMeshIndices[i];
                Vector3 vertex = subMeshVertices[originalIndex];

                // Проверяем, существует ли уже эта вершина в нашем словаре (vertexMap)
                if (!vertexMap.ContainsKey(vertex))
                {
                    // Если вершина новая, добавляем её в карту и увеличиваем offset
                    vertices.Add(vertex + offset + blockableVertexOffset);
                    uvs.Add(subMeshUVs[originalIndex]);
                    vertexMap[vertex] = vertexOffset++;
                }

                // Добавляем скорректированный индекс в общий список индексов
                triangulos.Add(vertexMap[vertex]);
            }
        }
    }

    // Метод для объединения всех SubMesh в один Mesh
    public static Mesh CombineSubMeshes(Mesh originalMesh)
    {
        // Создаем новый объект Mesh
        Mesh combinedMesh = new Mesh();

        // Списки для хранения объединенных данных
        List<Vector3> combinedVertices = new List<Vector3>();
        List<Vector3> combinedNormals = new List<Vector3>();
        List<Vector2> combinedUVs = new List<Vector2>();
        List<int> combinedIndices = new List<int>();

        // Переменная для смещения индексов
        int vertexOffset = 0;

        // Проходим по всем SubMeshes
        for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
        {
            // Получаем вершины, нормали и UV для текущего подмеша
            Vector3[] subMeshVertices = originalMesh.vertices;
            Vector3[] subMeshNormals = originalMesh.normals;
            Vector2[] subMeshUVs = originalMesh.uv;

            // Добавляем вершины, нормали и UV к общему списку
            combinedVertices.AddRange(subMeshVertices);
            combinedNormals.AddRange(subMeshNormals);
            combinedUVs.AddRange(subMeshUVs);

            // Получаем индексы треугольников для текущего подмеша
            int[] subMeshIndices = originalMesh.GetTriangles(subMeshIndex);

            // Корректируем индексы (с учетом смещения)
            for (int i = 0; i < subMeshIndices.Length; i++)
            {
                combinedIndices.Add(subMeshIndices[i] + vertexOffset);
            }

            // Обновляем смещение для следующего подмеша
            vertexOffset += subMeshVertices.Length;
        }

        // Устанавливаем данные для нового объединенного меша
        combinedMesh.SetVertices(combinedVertices);
        combinedMesh.SetNormals(combinedNormals);
        combinedMesh.SetUVs(0, combinedUVs);
        combinedMesh.SetTriangles(combinedIndices, 0);

        // Обновляем границы меша
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
