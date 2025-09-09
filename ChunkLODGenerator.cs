using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Генератор LOD-меша для чанка 16x16x16. 
/// Вход: byte[,,] blocks (размер должен быть 16x16x16), lodLevel >= 0, chunkGlobalPosition в блоковых координатах.
/// Возвращает Mesh с вершинами в локальных координатах (0..16 по осям), GameObject у которого этот Mesh должен иметь transform.position == chunkGlobalPosition.
/// </summary>
public static class ChunkLODGenerator
{
    private const int CHUNK_SIZE = 16;
    private const float ISOLEVEL = 0.5f; // порог для marching cubes

    // Смещения вершин куба (стандартные)
    private static readonly int[,] vertexOffset = new int[,]
    {
        {0,0,0},{1,0,0},{1,1,0},{0,1,0},
        {0,0,1},{1,0,1},{1,1,1},{0,1,1}
    };

    // Каждое ребро соединяет две вершины куба
    private static readonly int[,] edgeConnection = new int[,]
    {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    /// <summary>
    /// Основной метод: генерирует LOD Mesh.
    /// </summary>
    /// <param name="blocks">byte[16,16,16] — данные блока внутри чанка (0 = воздух, !=0 = solid)</param>
    /// <param name="lodLevel">уровень LOD: 0 = full res (1 блок/ячейка), 1 = шаг 2, 2 = шаг 4 и т.д.</param>
    /// <param name="chunkGlobalPosition">глобальная позиция чанка (в блоках) — совпадает с transform.position чанка</param>
    /// <returns>UnityEngine.Mesh</returns>
    public static Mesh GenerateChunkLOD(byte[,,] blocks, int lodLevel, Vector3Int chunkGlobalPosition)
    {
        if (blocks == null) throw new System.ArgumentNullException(nameof(blocks));
        if (blocks.GetLength(0) != CHUNK_SIZE || blocks.GetLength(1) != CHUNK_SIZE || blocks.GetLength(2) != CHUNK_SIZE)
            Debug.LogWarning("Ожидается blocks размером 16x16x16");

        int step = 1 << lodLevel;                 // 1,2,4,8...
        int samplesPerAxis = CHUNK_SIZE / step + 1; // число sample-узлов вдоль оси (nCells + 1)
        // sample grid spans [0..CHUNK_SIZE] с шагом = step (в блоковых единицах)
        // координаты в world-blocks: chunkGlobalPosition + localSamplePosition

        // предвыделение массивов
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        // кеш для вершин на ребрах (чтобы не дублировать)
        // ключ: (cubeX, cubeY, cubeZ, edgeIndex) -> vertexIndex
        var edgeVertexCache = new Dictionary<long, int>();

        // Локальная функция для генерации уникального ключа для кеша ребра
        long EdgeKey(int cx, int cy, int cz, int edgeIndex)
        {
            // упакуем в 64-bit: low bits - coords + edgeIndex
            // coords не превышают 16, но для безопасности умножаем
            return (((long)cx & 0xffffL) << 48) | (((long)cy & 0xffffL) << 32) | (((long)cz & 0xffffL) << 16) | (long)edgeIndex;
        }

        // функция получения плотности (density) в узле сетки: 1.0 = solid, 0.0 = air.
        float SampleDensityAt(int gx, int gy, int gz)
        {
            // gx,gy,gz — глобальные блоковые координаты точки-узла.
            // Мы пробуем использовать локальный массив blocks, если точка внутри чанка.
            int localX = gx - chunkGlobalPosition.x;
            int localY = gy - chunkGlobalPosition.y;
            int localZ = gz - chunkGlobalPosition.z;

            int id = 0;
            if (localX >= 0 && localX < CHUNK_SIZE && localY >= 0 && localY < CHUNK_SIZE && localZ >= 0 && localZ < CHUNK_SIZE)
            {
                id = blocks[localX, localY, localZ];
            }
            else
            {
                // обращаемся к мировому генератору за соседями
                id = WorldGenerator.Inst.procedural.GetBlockID(gx, gy, gz);
            }

            return (id != 0) ? 1.0f : 0.0f;
        }

        // Интерполяция вершины на ребре
        Vector3 VertexInterp(Vector3 p1, Vector3 p2, float valp1, float valp2)
        {
            if (Mathf.Abs(ISOLEVEL - valp1) < 1e-6f) return p1;
            if (Mathf.Abs(ISOLEVEL - valp2) < 1e-6f) return p2;
            if (Mathf.Abs(valp1 - valp2) < 1e-6f) return p1;
            float mu = (ISOLEVEL - valp1) / (valp2 - valp1);
            return p1 + mu * (p2 - p1);
        }

        // вспомогательная функция: получить нормаль через градиент плотности (центральная разность)
        float SampleDensityFloat(Vector3 worldPos)
        {
            // worldPos в глобальных блоковых координатах (float). Для оценки градиента используем ближайшие integer sample.
            int x = Mathf.RoundToInt(worldPos.x);
            int y = Mathf.RoundToInt(worldPos.y);
            int z = Mathf.RoundToInt(worldPos.z);
            return SampleDensityAt(x, y, z);
        }

        // Проходим по всем кубам сетки (cubeCount = samplesPerAxis-1 по каждой оси)
        for (int cz = 0; cz < samplesPerAxis - 1; cz++)
        {
            for (int cy = 0; cy < samplesPerAxis - 1; cy++)
            {
                for (int cx = 0; cx < samplesPerAxis - 1; cx++)
                {
                    // координаты в локальных блоковых единицах (от 0 до CHUNK_SIZE)
                    // куб между узлами (cx,cy,cz) и (cx+1,cy+1,cz+1)
                    float[] cubeValue = new float[8];
                    Vector3[] cubePos = new Vector3[8];

                    for (int i = 0; i < 8; i++)
                    {
                        int vx = cx * step + vertexOffset[i, 0];
                        int vy = cy * step + vertexOffset[i, 1];
                        int vz = cz * step + vertexOffset[i, 2];

                        // глобальная позиция узла в блоках
                        int gx = chunkGlobalPosition.x + vx;
                        int gy = chunkGlobalPosition.y + vy;
                        int gz = chunkGlobalPosition.z + vz;

                        cubeValue[i] = SampleDensityAt(gx, gy, gz);
                        // локальная позиция вершины в координатах чанка (0..16)
                        cubePos[i] = new Vector3(vx, vy, vz);
                    }

                    // вычисляем индекс конфигурации
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                        if (cubeValue[i] > ISOLEVEL) cubeIndex |= (1 << i);

                    // Используем готовую таблицу ребер (EdgeTable) от MarchingCubesTables
                    int edgeFlags = MarchingCubesTables.EdgeTable[cubeIndex];
                    if (edgeFlags == 0) continue; // нет треугольников

                    // для каждого ребра, если пересечение — вычислить вершину
                    Vector3[] edgeVertex = new Vector3[12];
                    for (int e = 0; e < 12; e++)
                    {
                        if ((edgeFlags & (1 << e)) == 0) continue;

                        // возможное кеширование: одна и та же реберная вершина для соседних кубов
                        long key = EdgeKey(cx, cy, cz, e);
                        if (edgeVertexCache.TryGetValue(key, out int cachedIndex))
                        {
                            // уже создана вершина для этого ребра
                            edgeVertex[e] = verts[cachedIndex];
                            continue;
                        }

                        int v1 = edgeConnection[e, 0];
                        int v2 = edgeConnection[e, 1];

                        Vector3 p1 = cubePos[v1];
                        Vector3 p2 = cubePos[v2];
                        float valp1 = cubeValue[v1];
                        float valp2 = cubeValue[v2];

                        Vector3 vert = VertexInterp(p1, p2, valp1, valp2);

                        // добавляем вершину и нормаль (пока нормаль в 0, посчитаем позже)
                        int newIndex = verts.Count;
                        verts.Add(vert);
                        normals.Add(Vector3.zero);

                        edgeVertex[e] = vert;
                        edgeVertexCache[key] = newIndex;
                    }

                    // теперь создаём треугольники по TriangleTable
                    for (int t = 0; t < 5; t++) // максимум 5 треугольников на куб
                    {
                        int triIndex = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 0];
                        if (triIndex < 0) break;

                        int e0 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 0];
                        int e1 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 1];
                        int e2 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 2];

                        // для индексов ребер найдём индекс вершины из кеша (мы сохранили индекс при добавлении)
                        int GetIndexForEdge(int cubeX, int cubeY, int cubeZ, int edge)
                        {
                            long k = EdgeKey(cubeX, cubeY, cubeZ, edge);
                            return edgeVertexCache[k];
                        }

                        int i0 = GetIndexForEdge(cx, cy, cz, e0);
                        int i1 = GetIndexForEdge(cx, cy, cz, e1);
                        int i2 = GetIndexForEdge(cx, cy, cz, e2);

                        indices.Add(i0);
                        indices.Add(i1);
                        indices.Add(i2);

                        // можно рассчитать нормали позже по индексам
                    }
                }
            }
        }

        // пересчитать нормали: для каждого треугольника добавить нормаль к вершинам
        for (int i = 0; i < indices.Count; i += 3)
        {
            Vector3 v0 = verts[indices[i + 0]];
            Vector3 v1 = verts[indices[i + 1]];
            Vector3 v2 = verts[indices[i + 2]];
            Vector3 n = Vector3.Cross(v1 - v0, v2 - v0);
            if (n.sqrMagnitude > 1e-12f) n.Normalize();
            normals[indices[i + 0]] += n;
            normals[indices[i + 1]] += n;
            normals[indices[i + 2]] += n;
        }

        for (int i = 0; i < normals.Count; i++)
        {
            normals[i] = normals[i].normalized;
        }

        // Создаём Mesh
        Mesh m = new Mesh();
        m.indexFormat = (verts.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        // Вершины уже в локальных координатах (0..16). Если хочешь масштаб (blockSize != 1), умножь здесь.
        m.SetVertices(verts);
        m.SetNormals(normals);
        m.SetTriangles(indices, 0);
        m.RecalculateBounds();

        return m;
    }
}
