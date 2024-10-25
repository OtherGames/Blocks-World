using UnityEngine;
using System.Collections.Generic;

public class MeshUtility : MonoBehaviour
{
    public static MeshUtility Single;

    private void Awake()
    {
        Single = this;
    }

    public int chunkSize = 16;  // Размер чанка
    public int chunkHeight = 16;  // Высота чанка

    // Метод для упрощения чанка с учётом LOD
    public Mesh SimplifyChunkMesh(Mesh originalMesh, float mergeThreshold = 0.1f)
    {
        // Списки для новых вершин, треугольников и UV
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector2> newUVs = new List<Vector2>();

        // Карта для кластеров вершин
        Dictionary<Vector3, Vector3> vertexClusters = new Dictionary<Vector3, Vector3>();

        // Получаем исходные данные меша
        Vector3[] vertices = originalMesh.vertices;
        int[] triangles = originalMesh.triangles;
        Vector2[] uvs = originalMesh.uv;

        // Проходим по всем вершинам и объединяем их в кластеры
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 currentVertex = vertices[i];

            if (!IsEdgeVertex(currentVertex))
            {
                // Объединяем вершины, которые находятся близко друг к другу
                Vector3 clusterVertex = FindOrCreateCluster(vertexClusters, currentVertex, mergeThreshold);
                vertices[i] = clusterVertex; // Заменяем вершину на её кластер
            }
        }

        // Пройдем по треугольникам и создадим новый меш
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            // Проверяем, если треугольник не стал вырожденным (например, из-за объединения вершин)
            if (v1 != v2 && v2 != v3 && v1 != v3)
            {
                AddTriangle(newVertices, newTriangles, newUVs, v1, v2, v3, uvs[triangles[i]], uvs[triangles[i + 1]], uvs[triangles[i + 2]]);
            }
        }

        // Создаём новый меш с упрощённой геометрией
        Mesh simplifiedMesh = new Mesh();
        simplifiedMesh.vertices = newVertices.ToArray();
        simplifiedMesh.triangles = newTriangles.ToArray();
        simplifiedMesh.uv = newUVs.ToArray();
        simplifiedMesh.RecalculateNormals();

        return simplifiedMesh;
    }

    // Метод для создания или поиска кластера для вершины
    private Vector3 FindOrCreateCluster(Dictionary<Vector3, Vector3> vertexClusters, Vector3 vertex, float threshold)
    {
        foreach (var clusterVertex in vertexClusters.Keys)
        {
            if (Vector3.Distance(vertex, clusterVertex) < threshold)
            {
                // Если вершина достаточно близка к кластеру, возвращаем кластерную вершину
                return clusterVertex;
            }
        }

        // Если кластер не найден, создаём новый кластер
        vertexClusters[vertex] = vertex;
        return vertex;
    }

    // Проверяем, является ли вершина границей чанка
    private bool IsEdgeVertex(Vector3 vertex)
    {
        // Проверка, является ли вершина граничной
        return vertex.x == 0 || vertex.x == chunkSize - 1 ||
               vertex.y == 0 || vertex.y == chunkHeight - 1 ||
               vertex.z == 0 || vertex.z == chunkSize - 1;
    }

    // Добавляем треугольник в новый меш
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
