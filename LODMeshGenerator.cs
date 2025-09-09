using System;
using System.Collections.Generic;
using UnityEngine;

/*
 LODMeshGenerator.cs (vertex-color version)
 - Same API as before: GenerateLODMesh(byte[,,] blocks, int lodLevel, Vector3Int chunkGlobalPosition)
 - After generating geometry via MarchingCubes, this version assigns a vertex color for every vertex
   by sampling the nearest block ID at the vertex world position and mapping it to a color palette.
 - Uses Color32 lists for lower memory and faster upload.
*/

public static class LODMeshGenerator
{
    public const float ISO_LEVEL = 0.5f;

    public static Mesh GenerateLODMesh(byte[,,] blocks, int lodLevel, Vector3Int chunkGlobalPosition)
    {
        if (blocks == null) throw new ArgumentNullException(nameof(blocks));
        int sizeX = blocks.GetLength(0);
        int sizeY = blocks.GetLength(1);
        int sizeZ = blocks.GetLength(2);

        int step = 1 << Math.Max(0, lodLevel);
        step = Math.Min(step, Math.Max(Math.Max(sizeX, sizeY), sizeZ));

        int gx = Mathf.FloorToInt((float)sizeX / step) + 1;
        int gy = Mathf.FloorToInt((float)sizeY / step) + 1;
        int gz = Mathf.FloorToInt((float)sizeZ / step) + 1;

        float[,,] density = new float[gx, gy, gz];

        for (int ix = 0; ix < gx; ix++)
        {
            for (int iy = 0; iy < gy; iy++)
            {
                for (int iz = 0; iz < gz; iz++)
                {
                    int worldX = chunkGlobalPosition.x + ix * step;
                    int worldY = chunkGlobalPosition.y + iy * step;
                    int worldZ = chunkGlobalPosition.z + iz * step;

                    int countSolid = 0;
                    int countTotal = 0;

                    for (int sx = 0; sx < step; sx++)
                    {
                        for (int sy = 0; sy < step; sy++)
                        {
                            for (int sz = 0; sz < step; sz++)
                            {
                                int bx = worldX + sx;
                                int by = worldY + sy;
                                int bz = worldZ + sz;
                                byte id = GetBlockIDSafe(blocks, bx - chunkGlobalPosition.x, by - chunkGlobalPosition.y, bz - chunkGlobalPosition.z, bx, by, bz);
                                if (IsSolid(id)) countSolid++;
                                countTotal++;
                            }
                        }
                    }

                    float d = (countTotal == 0) ? 0f : (float)countSolid / (float)countTotal;
                    density[ix, iy, iz] = d;
                }
            }
        }

        var meshData = MarchingCubes.GenerateMesh(density, step, chunkGlobalPosition);

        // Create mesh
        var mesh = new Mesh();
        if (meshData.Vertices.Count >= 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(meshData.Vertices);
        mesh.SetTriangles(meshData.Triangles, 0);

        // Assign vertex colors by sampling block IDs near each vertex.
        var colors = new List<Color32>(meshData.Vertices.Count);
        for (int i = 0; i < meshData.Vertices.Count; i++)
        {
            // world position of vertex (chunkLocal + chunk origin in blocks)
            Vector3 local = meshData.Vertices[i]; // in block units relative to chunk origin
            int wx = Mathf.RoundToInt(chunkGlobalPosition.x + local.x);
            int wy = Mathf.RoundToInt(chunkGlobalPosition.y + local.y);
            int wz = Mathf.RoundToInt(chunkGlobalPosition.z + local.z);

            byte id = GetBlockIDSafe(blocks, wx - chunkGlobalPosition.x, wy - chunkGlobalPosition.y, wz - chunkGlobalPosition.z, wx, wy, wz);
            colors.Add(ColorForBlockID(id));
        }

        mesh.SetColors(colors);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static bool IsSolid(byte id) => id != 0;

    // Map block id to a color. Change constants to taste.
    private static Color32 ColorForBlockID(byte id)
    {
        // The user specified: grass ID = 1, stone ID = 2, else brown.
        if (id == 1) return new Color32(60, 180, 75, 255);    // green-ish (0.235,0.706,0.294)
        if (id == 2) return new Color32(80, 80, 80, 255);  // gray
        if (id == 14) return new Color32(128, 128, 128, 255);
        return new Color32(150, 150, 150, 255);
        return new Color32(115, 60, 20, 255);                 // brown
    }

    private static byte GetBlockIDSafe(byte[,,] blocks, int localX, int localY, int localZ, int worldX, int worldY, int worldZ)
    {
        int sizeX = blocks.GetLength(0);
        int sizeY = blocks.GetLength(1);
        int sizeZ = blocks.GetLength(2);

        if (localX >= 0 && localX < sizeX && localY >= 0 && localY < sizeY && localZ >= 0 && localZ < sizeZ)
        {
            return blocks[localX, localY, localZ];
        }
        else
        {
            try
            {
                if (WorldGenerator.Inst != null && WorldGenerator.Inst.procedural != null)
                {
                    return WorldGenerator.Inst.procedural.GetBlockID(worldX, worldY, worldZ);
                }
            }
            catch (Exception) { }
        }
        return 0;
    }
}


public static class MeshSmoothing
{
    // smoothingAngleDeg Ч угловой порог в градусах (например 60f)
    public static void SmoothNormalsWithAngle(Mesh mesh, float smoothingAngleDeg)
    {
        if (mesh == null) return;

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        int triCount = tris.Length / 3;

        // 1) вычислим нормали граней и (опционально) площадь грани (дл€ взвешивани€)
        Vector3[] faceNormals = new Vector3[triCount];
        float[] faceAreas = new float[triCount];

        for (int i = 0; i < triCount; i++)
        {
            int i0 = tris[i * 3 + 0];
            int i1 = tris[i * 3 + 1];
            int i2 = tris[i * 3 + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            Vector3 fn = Vector3.Cross(v1 - v0, v2 - v0);
            float area = fn.magnitude * 0.5f;
            if (fn.sqrMagnitude > 1e-9f) fn.Normalize();
            else fn = Vector3.up;

            faceNormals[i] = fn;
            faceAreas[i] = area;
        }

        // 2) дл€ каждой вершины список индексов граней, в которых она участвует
        Dictionary<int, List<int>> vertToFaces = new Dictionary<int, List<int>>();
        for (int i = 0; i < triCount; i++)
        {
            for (int k = 0; k < 3; k++)
            {
                int vi = tris[i * 3 + k];
                if (!vertToFaces.TryGetValue(vi, out var list))
                {
                    list = new List<int>();
                    vertToFaces[vi] = list;
                }
                list.Add(i);
            }
        }

        // 3) порог дл€ сравнени€ по скал€рному произведению
        float cosThreshold = Mathf.Cos(smoothingAngleDeg * Mathf.Deg2Rad);

        // 4) дл€ каждой вершины усредн€ем нормали соседних граней, но только те, что внутри углового порога
        Vector3[] newNormals = new Vector3[verts.Length];
        for (int vi = 0; vi < verts.Length; vi++)
        {
            if (!vertToFaces.TryGetValue(vi, out var adjFaces) || adjFaces.Count == 0)
            {
                newNormals[vi] = Vector3.up;
                continue;
            }

            Vector3 sum = Vector3.zero;

            // ЅерЄм каждую соседнюю грань как "референс" и добавл€ем все грани,
            // угол между которыми <= порога. Ёто даЄт корректное разделение на "группы" углов.
            // Ќа практике можно упростить, но этот способ часто даЄт ожидаемый результат.
            foreach (int f in adjFaces)
            {
                Vector3 fnRef = faceNormals[f];
                // собираем грани, близкие к fnRef
                Vector3 partial = Vector3.zero;
                float weightSum = 0f;
                foreach (int g in adjFaces)
                {
                    float dot = Vector3.Dot(fnRef, faceNormals[g]);
                    if (dot >= cosThreshold) // включаем грань в усреднение
                    {
                        // взвешивание по площади Ч даЄт более корректные результаты
                        partial += faceNormals[g] * faceAreas[g];
                        weightSum += faceAreas[g];
                    }
                }
                if (weightSum > 0f) sum += partial / weightSum;
            }

            if (sum.sqrMagnitude > 1e-6f) newNormals[vi] = sum.normalized;
            else newNormals[vi] = Vector3.up;
        }

        mesh.normals = newNormals;
    }
}

