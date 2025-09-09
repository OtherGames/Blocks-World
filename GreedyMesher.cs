// GreedyMesher.cs
// Greedy meshing + padding рамка, но рамка состоит из отдельных 1x1 блоков (freeze border).
// —одержит global mesh offset X = -1.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class GreedyMesher
{
    const int ATLAS_SIZE = 16;
    const float TextureOffset = 1f / ATLAS_SIZE;
    static readonly Vector3 GlobalMeshOffset = new Vector3(-1f, 0f, 0f);

    public static Mesh[] GenerateMeshes(byte[,,] blocks,
                                        int sizeX = 16, int sizeY = 16, int sizeZ = 16,
                                        float blockSize = 1f,
                                        HashSet<byte> transparentBlocks = null,
                                        int padding = 0)
    {
        if (blocks == null) throw new ArgumentNullException(nameof(blocks));

        Mesh opaque = GenerateForPredicate(blocks, sizeX, sizeY, sizeZ, blockSize,
                                           (b) => b != 0 && (transparentBlocks == null || !transparentBlocks.Contains(b)),
                                           padding);
        Mesh transparent = null;
        if (transparentBlocks != null && transparentBlocks.Count > 0)
        {
            transparent = GenerateForPredicate(blocks, sizeX, sizeY, sizeZ, blockSize,
                                               (b) => b != 0 && transparentBlocks.Contains(b),
                                               padding);
        }

        if (transparent == null) return new[] { opaque };
        return new[] { opaque, transparent };
    }

    // ƒобавл€ет квад в списки, учитывает winding и UV.
    static void AddQuad(List<Vector3> verts, List<int> tris, List<Vector2> uvs,
                        Vector3 v0, Vector3 vdu, Vector3 vdv,
                        Vector3 expectedNormal,
                        int tileX, int tileY, int tileW, int tileH)
    {
        Vector3 v1 = v0 + vdu;
        Vector3 v2 = v0 + vdu + vdv;
        Vector3 v3 = v0 + vdv;

        Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
        bool windingCorrect = Vector3.Dot(cross, expectedNormal) >= 0f;

        int vertStart = verts.Count;
        if (windingCorrect)
        {
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            tris.Add(vertStart + 0); tris.Add(vertStart + 1); tris.Add(vertStart + 2);
            tris.Add(vertStart + 0); tris.Add(vertStart + 2); tris.Add(vertStart + 3);

            float uMin = tileX * TextureOffset;
            float vMin = tileY * TextureOffset;
            float uMax = uMin + tileW * TextureOffset;
            float vMax = vMin + tileH * TextureOffset;

            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMax, vMin));
            uvs.Add(new Vector2(uMax, vMax));
            uvs.Add(new Vector2(uMin, vMax));
        }
        else
        {
            // flipped winding
            verts.Add(v0); verts.Add(v3); verts.Add(v2); verts.Add(v1);
            tris.Add(vertStart + 0); tris.Add(vertStart + 1); tris.Add(vertStart + 2);
            tris.Add(vertStart + 0); tris.Add(vertStart + 2); tris.Add(vertStart + 3);

            float uMin = tileX * TextureOffset;
            float vMin = tileY * TextureOffset;
            float uMax = uMin + tileW * TextureOffset;
            float vMax = vMin + tileH * TextureOffset;

            uvs.Add(new Vector2(uMin, vMin));
            uvs.Add(new Vector2(uMin, vMax));
            uvs.Add(new Vector2(uMax, vMax));
            uvs.Add(new Vector2(uMax, vMin));
        }
    }

    static byte GetBlockSafe(byte[,,] blocks, int x, int y, int z, int[] dims)
    {
        if (x < 0 || y < 0 || z < 0) return 0;
        if (x >= dims[0] || y >= dims[1] || z >= dims[2]) return 0;
        return blocks[x, y, z];
    }

    static Mesh GenerateForPredicate(byte[,,] blocks, int sizeX, int sizeY, int sizeZ, float blockSize, Func<byte, bool> predicate, int padding)
    {
        int[] dims = new int[3] { sizeX, sizeY, sizeZ };
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();

        int[] x = new int[3];
        int[] q = new int[3];
        int[] du = new int[3];
        int[] dv = new int[3];

        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;
            int maxU = dims[u];
            int maxV = dims[v];

            byte[] mask = new byte[maxU * maxV];
            sbyte[] signMask = new sbyte[maxU * maxV];

            for (q[d] = -1; q[d] < dims[d]; q[d]++)
            {
                Array.Clear(mask, 0, mask.Length);
                Array.Clear(signMask, 0, signMask.Length);

                for (q[u] = 0; q[u] < dims[u]; q[u]++)
                {
                    for (q[v] = 0; q[v] < dims[v]; q[v]++)
                    {
                        x[0] = q[0]; x[1] = q[1]; x[2] = q[2];

                        byte a = 0, b = 0;
                        if (q[d] >= 0)
                        {
                            x[d] = q[d];
                            a = GetBlockSafe(blocks, x[0], x[1], x[2], dims);
                        }
                        if (q[d] < dims[d] - 1)
                        {
                            x[d] = q[d] + 1;
                            b = GetBlockSafe(blocks, x[0], x[1], x[2], dims);
                        }

                        bool aSolid = predicate?.Invoke(a) ?? (a != 0);
                        bool bSolid = predicate?.Invoke(b) ?? (b != 0);

                        int idx = q[u] + q[v] * maxU;

                        if (aSolid == bSolid)
                        {
                            mask[idx] = 0;
                            signMask[idx] = 0;
                        }
                        else if (aSolid && !bSolid)
                        {
                            mask[idx] = a;
                            signMask[idx] = +1;
                        }
                        else
                        {
                            mask[idx] = b;
                            signMask[idx] = -1;
                        }
                    }
                }

                // сделать копию дл€ проходов и массив дл€ заморозки рамки
                byte[] passMask = new byte[mask.Length];
                sbyte[] passSign = new sbyte[signMask.Length];
                byte[] frozen = new byte[mask.Length]; // 1 = freeze (обрабатывать как 1x1), 0 = обычный
                Array.Copy(mask, passMask, mask.Length);
                Array.Copy(signMask, passSign, signMask.Length);

                // PASS A: строим внутренние квадраты с padding, и помечаем внешнюю рамку как frozen
                if (padding > 0)
                {
                    for (int j = 0; j < maxV; j++)
                    {
                        for (int i = 0; i < maxU;)
                        {
                            int idx = i + j * maxU;
                            if (passMask[idx] == 0) { i++; continue; }

                            int startI = i;
                            byte id = passMask[idx];
                            sbyte sgn = passSign[idx];

                            // ширина
                            int w;
                            for (w = 1; i + w < maxU && passMask[idx + w] == id && passSign[idx + w] == sgn; w++) { }

                            // высота
                            int h;
                            bool stop = false;
                            for (h = 1; j + h < maxV; h++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    int idx2 = (i + k) + (j + h) * maxU;
                                    if (passMask[idx2] != id || passSign[idx2] != sgn) { stop = true; break; }
                                }
                                if (stop) break;
                            }

                            int iw = w - 2 * padding;
                            int ih = h - 2 * padding;

                            if (iw > 0 && ih > 0)
                            {
                                int iu = startI + padding;
                                int jv = j + padding;

                                x[d] = q[d] + (sgn == 1 ? 1 : 0);
                                if (sgn == -1 && (d == 0 || d == 1 || d == 2)) x[d] += 1;
                                x[u] = iu;
                                x[v] = jv;

                                du[0] = du[1] = du[2] = 0;
                                dv[0] = dv[1] = dv[2] = 0;
                                du[u] = iw;
                                dv[v] = ih;

                                Vector3 pos = new Vector3(x[0], x[1], x[2]) * blockSize;
                                Vector3 vdu = new Vector3(du[0], du[1], du[2]) * blockSize;
                                Vector3 vdv = new Vector3(dv[0], dv[1], dv[2]) * blockSize;
                                Vector3 expectedNormal = (d == 0) ? new Vector3(sgn, 0, 0) : (d == 1) ? new Vector3(0, sgn, 0) : new Vector3(0, 0, sgn);

                                BlockUVS buv = BlockUVS.GetBlock(id);
                                int tileX = (d == 1) ? ((sgn == 1) ? buv.TextureX : buv.TextureXBottom) : buv.TextureXSide;
                                int tileY = (d == 1) ? ((sgn == 1) ? buv.TextureY : buv.TextureYBottom) : buv.TextureYSide;

                                AddQuad(verts, tris, uvs, pos, vdu, vdv, expectedNormal, tileX, tileY, iw, ih);

                                // очищаем внутреннюю область
                                for (int yy = 0; yy < ih; yy++)
                                    for (int xx = 0; xx < iw; xx++)
                                    {
                                        int idx2 = (iu + xx) + (jv + yy) * maxU;
                                        passMask[idx2] = 0;
                                        passSign[idx2] = 0;
                                    }

                                // помечаем рамку (внешний rect startI..startI+w-1, j..j+h-1) как frozen
                                for (int yy = 0; yy < h; yy++)
                                    for (int xx = 0; xx < w; xx++)
                                    {
                                        int gx = startI + xx;
                                        int gy = j + yy;
                                        int idx2 = gx + gy * maxU;
                                        // только если клетка реально принадлежала этому id (mask), и не внутренн€€
                                        if (mask[idx2] == id && !(xx >= padding && xx < padding + iw && yy >= padding && yy < padding + ih))
                                        {
                                            frozen[idx2] = 1;
                                        }
                                    }

                                // не перескакиваем всю рамку Ч идЄм от startI+1, чтобы лева€ рамка не пропала
                                i = startI + 1;
                            }
                            else
                            {
                                i = startI + 1;
                            }
                        }
                    }
                }

                // PASS B: обычный greedy по незамороженным клеткам; frozen клетки -> 1x1 quads
                for (int j = 0; j < maxV; j++)
                {
                    for (int i = 0; i < maxU;)
                    {
                        int idx = i + j * maxU;
                        if (passMask[idx] == 0) { i++; continue; }

                        // если заморожена Ч делаем 1x1 quad
                        if (frozen[idx] == 1)
                        {
                            byte id = passMask[idx];
                            sbyte sgn = passSign[idx];

                            x[d] = q[d] + (sgn == 1 ? 1 : 0);
                            if (sgn == -1 && (d == 0 || d == 1 || d == 2)) x[d] += 1;
                            x[u] = i;
                            x[v] = j;

                            du[0] = du[1] = du[2] = 0;
                            dv[0] = dv[1] = dv[2] = 0;
                            du[u] = 1; dv[v] = 1;

                            Vector3 pos = new Vector3(x[0], x[1], x[2]) * blockSize;
                            Vector3 vdu = new Vector3(du[0], du[1], du[2]) * blockSize;
                            Vector3 vdv = new Vector3(dv[0], dv[1], dv[2]) * blockSize;

                            Vector3 expectedNormal = (d == 0) ? new Vector3(sgn, 0, 0) : (d == 1) ? new Vector3(0, sgn, 0) : new Vector3(0, 0, sgn);

                            BlockUVS buv = BlockUVS.GetBlock(id);
                            int tileX = (d == 1) ? ((sgn == 1) ? buv.TextureX : buv.TextureXBottom) : buv.TextureXSide;
                            int tileY = (d == 1) ? ((sgn == 1) ? buv.TextureY : buv.TextureYBottom) : buv.TextureYSide;

                            AddQuad(verts, tris, uvs, pos, vdu, vdv, expectedNormal, tileX, tileY, 1, 1);

                            passMask[idx] = 0;
                            passSign[idx] = 0;
                            frozen[idx] = 0;

                            i = i + 1;
                            continue;
                        }

                        // стандартный greedy (не frozen)
                        int startI = i;
                        byte id2 = passMask[idx];
                        sbyte sgn2 = passSign[idx];

                        int w2;
                        for (w2 = 1; i + w2 < maxU && passMask[idx + w2] == id2 && passSign[idx + w2] == sgn2 && frozen[idx + w2] == 0; w2++) { }

                        int h2;
                        bool stop2 = false;
                        for (h2 = 1; j + h2 < maxV; h2++)
                        {
                            for (int k = 0; k < w2; k++)
                            {
                                int idx2 = (i + k) + (j + h2) * maxU;
                                if (passMask[idx2] != id2 || passSign[idx2] != sgn2 || frozen[idx2] != 0) { stop2 = true; break; }
                            }
                            if (stop2) break;
                        }

                        x[d] = q[d] + (sgn2 == 1 ? 1 : 0);
                        if (sgn2 == -1 && (d == 0 || d == 1 || d == 2)) x[d] += 1;
                        x[u] = startI;
                        x[v] = j;

                        du[0] = du[1] = du[2] = 0;
                        dv[0] = dv[1] = dv[2] = 0;
                        du[u] = w2;
                        dv[v] = h2;

                        Vector3 posB = new Vector3(x[0], x[1], x[2]) * blockSize;
                        Vector3 vduB = new Vector3(du[0], du[1], du[2]) * blockSize;
                        Vector3 vdvB = new Vector3(dv[0], dv[1], dv[2]) * blockSize;
                        Vector3 expectedNormalB = (d == 0) ? new Vector3(sgn2, 0, 0) : (d == 1) ? new Vector3(0, sgn2, 0) : new Vector3(0, 0, sgn2);

                        BlockUVS buvB = BlockUVS.GetBlock(id2);
                        int tileXB = (d == 1) ? ((sgn2 == 1) ? buvB.TextureX : buvB.TextureXBottom) : buvB.TextureXSide;
                        int tileYB = (d == 1) ? ((sgn2 == 1) ? buvB.TextureY : buvB.TextureYBottom) : buvB.TextureYSide;

                        AddQuad(verts, tris, uvs, posB, vduB, vdvB, expectedNormalB, tileXB, tileYB, w2, h2);

                        for (int yy = 0; yy < h2; yy++)
                            for (int xx = 0; xx < w2; xx++)
                            {
                                int idx2 = (startI + xx) + (j + yy) * maxU;
                                passMask[idx2] = 0;
                                passSign[idx2] = 0;
                            }

                        i = startI + 1;
                    }
                }
            } // end slices
        } // end axes

        // apply global offset
        for (int vi = 0; vi < verts.Count; vi++) verts[vi] += GlobalMeshOffset;

        // build mesh
        Mesh mesh = new Mesh();
        if (verts.Count >= 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);

        return mesh;
    }
}

/*// ѕример использовани€ дл€ быстрого теста
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkMeshBuilder : MonoBehaviour
{
    public Material material;
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public float blockSize = 1f;
    public int padding = 1;

    void Start()
    {
        byte[,,] blocks = new byte[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
                for (int y = 0; y < sizeY; y++)
                    blocks[x, y, z] = (y == 0) ? (byte)1 : (byte)0;

        HashSet<byte> transparent = new HashSet<byte>() { };

        Mesh[] meshes = GreedyMesher.GenerateMeshes(blocks, sizeX, sizeY, sizeZ, blockSize, transparent, padding);
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (material != null) mr.sharedMaterial = material;

        mf.sharedMesh = meshes[0];
    }
}/**/
