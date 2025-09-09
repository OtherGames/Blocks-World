using System;
using System.Collections.Generic;
using UnityEngine;

/*
 LODMeshGenerator.cs (v5)
 - Density-weighted color blending with sample stride optimization.
 - Additional behavior: after mesh geometry is generated, all vertices are shifted by -1 along X axis,
   as requested.
 - API: GenerateLODMesh(byte[,,] blocks, int lodLevel, Vector3Int chunkGlobalPosition)
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

        // Shift all vertices by -1 on X axis as requested.
        for (int vi = 0; vi < meshData.Vertices.Count; vi++)
        {
            Vector3 v = meshData.Vertices[vi];
            v.x += -1f;
            meshData.Vertices[vi] = v;
        }

        // Create mesh
        var mesh = new Mesh();
        if (meshData.Vertices.Count >= 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(meshData.Vertices);
        mesh.SetTriangles(meshData.Triangles, 0);

        // Smooth color blending per-vertex with sampling stride optimization.
        var colors = new List<Color32>(meshData.Vertices.Count);

        // Sampling radius: scale with step so higher LOD (bigger step) samples larger neighborhood.
        float radius = Mathf.Max(1f, step * 1.5f);

        // Optimization: sample stride equals the LOD step (reasonable default).
        int sampleStep = Math.Max(1, step);

        for (int i = 0; i < meshData.Vertices.Count; i++)
        {
            // note: vertices already shifted locally by -1 on X; when computing world position we still add chunkGlobalPosition
            Vector3 local = meshData.Vertices[i]; // in block units relative to chunk origin (shifted)
            Vector3 worldPos = new Vector3(chunkGlobalPosition.x + local.x, chunkGlobalPosition.y + local.y, chunkGlobalPosition.z + local.z);

            int minX = Mathf.FloorToInt(worldPos.x - radius);
            int maxX = Mathf.CeilToInt(worldPos.x + radius);
            int minY = Mathf.FloorToInt(worldPos.y - radius);
            int maxY = Mathf.CeilToInt(worldPos.y + radius);
            int minZ = Mathf.FloorToInt(worldPos.z - radius);
            int maxZ = Mathf.CeilToInt(worldPos.z + radius);

            float totalWeight = 0f;
            float r = radius;
            float accumR = 0f, accumG = 0f, accumB = 0f;

            // Iterate with stride to reduce sample count. This trades detail for speed.
            for (int bx = minX; bx <= maxX; bx += sampleStep)
            {
                for (int by = minY; by <= maxY; by += sampleStep)
                {
                    for (int bz = minZ; bz <= maxZ; bz += sampleStep)
                    {
                        Vector3 blockCenter = new Vector3(bx + 0.5f, by + 0.5f, bz + 0.5f);
                        float dist = Vector3.Distance(worldPos, blockCenter);
                        if (dist > r) continue;

                        // weight decreases linearly with distance (triangle kernel)
                        float weight = 1f - (dist / r);
                        if (weight <= 0f) continue;

                        byte id = GetBlockIDSafe(blocks, bx - chunkGlobalPosition.x, by - chunkGlobalPosition.y, bz - chunkGlobalPosition.z, bx, by, bz);
                        if (!IsSolid(id)) continue;

                        Color32 c32 = ColorForBlockID(id);
                        float cfR = c32.r / 255f;
                        float cfG = c32.g / 255f;
                        float cfB = c32.b / 255f;

                        accumR += cfR * weight;
                        accumG += cfG * weight;
                        accumB += cfB * weight;
                        totalWeight += weight;
                    }
                }
            }

            if (totalWeight <= 0f)
            {
                colors.Add(ColorForBlockID(0));
            }
            else
            {
                float inv = 1f / totalWeight;
                byte outR = (byte)Mathf.Clamp(Mathf.RoundToInt(accumR * inv * 255f), 0, 255);
                byte outG = (byte)Mathf.Clamp(Mathf.RoundToInt(accumG * inv * 255f), 0, 255);
                byte outB = (byte)Mathf.Clamp(Mathf.RoundToInt(accumB * inv * 255f), 0, 255);
                colors.Add(new Color32(outR, outG, outB, 255));
            }
        }

        mesh.SetColors(colors);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static bool IsSolid(byte id) => id != 0;

    private static Color32 ColorForBlockID(byte id)
    {
        if (id == 1) return new Color32(60, 180, 75, 255);    // grass
        if (id == 2) return new Color32(128, 128, 128, 255);  // stone
        return new Color32(115, 60, 20, 255);                 // brown/fallback
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
