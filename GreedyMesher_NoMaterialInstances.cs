
// GreedyMesher_NoMaterialInstances.cs
// Patch: avoid creating new Material instances per submesh.
// Instead reuse the same baseMaterial so Unity doesn't create unique material instances
// (which breaks batching and explodes drawcalls).
//
// This is a minimal, targeted patch for the file you provided.
// It keeps submesh grouping by tile/face, but uses the same material reference for every submesh.
// To further reduce drawcalls you must merge submeshes that share the same material into one submesh
// (or use Texture2DArray / custom shader). If you want that, I can patch it too.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct MeshAndMaterials
{
    public Mesh mesh;
    public Material[] materials;
}

struct MatKey : IEquatable<MatKey>
{
    public int tileX, tileY;
    public byte faceType; // 0=side,1=top,2=bottom

    public MatKey(int tx, int ty, byte ft) { tileX = tx; tileY = ty; faceType = ft; }
    public bool Equals(MatKey other) => tileX == other.tileX && tileY == other.tileY && faceType == other.faceType;
    public override bool Equals(object obj) => obj is MatKey other && Equals(other);
    public override int GetHashCode() => ((tileX * 397) ^ tileY) * 31 + faceType;
}

class MeshPart
{
    public List<Vector3> verts = new List<Vector3>();
    public List<int> tris = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();
}

public static class GreedyMesherWithCorrectUVAndOffset_NoMaterialInstances
{
    const int DEFAULT_ATLAS_SIZE = 16;
    static readonly Vector3 GlobalMeshOffset = new Vector3(-1f, 0f, 0f);

    public static MeshAndMaterials GenerateMesh(byte[,,] blocks,
                                               Material baseMaterial,
                                               int sizeX = 16, int sizeY = 16, int sizeZ = 16,
                                               float blockSize = 1f,
                                               int atlasSize = DEFAULT_ATLAS_SIZE,
                                               HashSet<byte> transparentBlocks = null)
    {
        if (baseMaterial == null) throw new ArgumentNullException(nameof(baseMaterial));

        MeshPart[] partsArray;
        List<MatKey> keys;
        BuildParts(blocks, sizeX, sizeY, sizeZ, blockSize, atlasSize, out partsArray, out keys, transparentBlocks);

        List<Vector3> allVerts = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        List<int[]> subTriangles = new List<int[]>();

        for (int i = 0; i < partsArray.Length; i++)
        {
            var p = partsArray[i];
            int vertOffset = allVerts.Count;
            allVerts.AddRange(p.verts);
            allUVs.AddRange(p.uvs);

            int[] tris = new int[p.tris.Count];
            for (int t = 0; t < p.tris.Count; t++)
                tris[t] = p.tris[t] + vertOffset;
            subTriangles.Add(tris);
        }

        // apply global offset
        for (int vi = 0; vi < allVerts.Count; vi++) allVerts[vi] += GlobalMeshOffset;

        Mesh mesh = new Mesh();
        if (allVerts.Count >= 65535) mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(allVerts);
        mesh.SetUVs(0, allUVs);
        mesh.subMeshCount = subTriangles.Count;
        for (int si = 0; si < subTriangles.Count; si++)
        {
            mesh.SetTriangles(subTriangles[si], si);
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);

        // *** PATCH: reuse the same baseMaterial instance for every submesh ***
        Material[] materials = new Material[partsArray.Length];
        for (int i = 0; i < partsArray.Length; i++)
        {
            materials[i] = baseMaterial; // <<--- no new Material(...) here
        }

        return new MeshAndMaterials() { mesh = mesh, materials = materials };
    }

    static void BuildParts(byte[,,] blocks, int sizeX, int sizeY, int sizeZ, float blockSize, int atlasSize,
                           out MeshPart[] partsArray, out List<MatKey> keysOut,
                           HashSet<byte> transparentBlocks = null)
    {
        int[] dims = new int[3] { sizeX, sizeY, sizeZ };
        var parts = new Dictionary<MatKey, MeshPart>();

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

            byte[] mask = new byte[maxU * maxU]; // allocate at least, will be re-sized below if needed
            mask = new byte[maxU * maxV];
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

                        bool aSolid = (a != 0) && (transparentBlocks == null || !transparentBlocks.Contains(a));
                        bool bSolid = (b != 0) && (transparentBlocks == null || !transparentBlocks.Contains(b));

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

                for (int j = 0; j < maxV; j++)
                {
                    for (int i = 0; i < maxU; )
                    {
                        int idx = i + j * maxU;
                        if (mask[idx] == 0) { i++; continue; }

                        byte id = mask[idx];
                        sbyte sgn = signMask[idx];

                        int w;
                        for (w = 1; i + w < maxU && mask[idx + w] == id && signMask[idx + w] == sgn; w++) { }

                        int h;
                        bool stop = false;
                        for (h = 1; j + h < maxV; h++)
                        {
                            for (int k = 0; k < w; k++)
                            {
                                int idx2 = (i + k) + (j + h) * maxU;
                                if (mask[idx2] != id || signMask[idx2] != sgn) { stop = true; break; }
                            }
                            if (stop) break;
                        }

                        // Position and extents
                        x[d] = q[d] + (sgn == 1 ? 1 : 0);
                        // fixes for negative-facing faces: align with grid
                        if (sgn == -1 && (d == 0 || d == 1 || d == 2))
                        {
                            x[d] += 1;
                        }
                        x[u] = i;
                        x[v] = j;

                        du[0] = du[1] = du[2] = 0;
                        dv[0] = dv[1] = dv[2] = 0;
                        du[u] = w;
                        dv[v] = h;

                        Vector3 pos = new Vector3(x[0], x[1], x[2]) * blockSize;
                        Vector3 vdu = new Vector3(du[0], du[1], du[2]) * blockSize;
                        Vector3 vdv = new Vector3(dv[0], dv[1], dv[2]) * blockSize;

                        Vector3 expectedNormal = Vector3.zero;
                        if (d == 0) expectedNormal = new Vector3(sgn, 0, 0);
                        else if (d == 1) expectedNormal = new Vector3(0, sgn, 0);
                        else expectedNormal = new Vector3(0, 0, sgn);

                        Vector3 v0 = pos;
                        Vector3 v1 = pos + vdu;
                        Vector3 v2 = pos + vdu + vdv;
                        Vector3 v3 = pos + vdv;

                        Vector3 cross = Vector3.Cross(v1 - v0, v2 - v0);
                        bool windingCorrect = Vector3.Dot(cross, expectedNormal) >= 0f;

                        BlockUVS buv = BlockUVS.GetBlock(id);
                        int tileX = 0, tileY = 0;
                        byte faceType = 0;
                        if (d == 1)
                        {
                            if (sgn == 1) { tileX = buv.TextureX; tileY = buv.TextureY; faceType = 1; }
                            else { tileX = buv.TextureXBottom; tileY = buv.TextureYBottom; faceType = 2; }
                        }
                        else
                        {
                            tileX = buv.TextureXSide; tileY = buv.TextureYSide; faceType = 0;
                        }

                        var key = new MatKey(tileX, tileY, faceType);
                        if (!parts.TryGetValue(key, out MeshPart part))
                        {
                            part = new MeshPart();
                            parts[key] = part;
                        }

                        int baseVert = part.verts.Count;

                        // Calculate atlas UV coordinates for single tile (do not stretch across atlas)
                        float tileU = 1f / atlasSize;
                        float tileV = 1f / atlasSize;
                        float uMin = tileX * tileU;
                        float vMin = tileY * tileV;
                        float uMax = uMin + tileU;
                        float vMax = vMin + tileV;

                        if (windingCorrect)
                        {
                            part.verts.Add(v0);
                            part.verts.Add(v1);
                            part.verts.Add(v2);
                            part.verts.Add(v3);

                            part.tris.Add(baseVert + 0);
                            part.tris.Add(baseVert + 1);
                            part.tris.Add(baseVert + 2);
                            part.tris.Add(baseVert + 0);
                            part.tris.Add(baseVert + 2);
                            part.tris.Add(baseVert + 3);

                            // UVs mapped to single tile region (stretched if quad larger than 1x1)
                            part.uvs.Add(new Vector2(uMin, vMin));
                            part.uvs.Add(new Vector2(uMax, vMin));
                            part.uvs.Add(new Vector2(uMax, vMax));
                            part.uvs.Add(new Vector2(uMin, vMax));
                        }
                        else
                        {
                            part.verts.Add(v0);
                            part.verts.Add(v3);
                            part.verts.Add(v2);
                            part.verts.Add(v1);

                            part.tris.Add(baseVert + 0);
                            part.tris.Add(baseVert + 1);
                            part.tris.Add(baseVert + 2);
                            part.tris.Add(baseVert + 0);
                            part.tris.Add(baseVert + 2);
                            part.tris.Add(baseVert + 3);

                            part.uvs.Add(new Vector2(uMin, vMin));
                            part.uvs.Add(new Vector2(uMin, vMax));
                            part.uvs.Add(new Vector2(uMax, vMax));
                            part.uvs.Add(new Vector2(uMax, vMin));
                        }

                        // clear mask
                        for (int hh = 0; hh < h; hh++)
                            for (int ww = 0; ww < w; ww++)
                            {
                                int idx2 = (i + ww) + (j + hh) * maxU;
                                mask[idx2] = 0;
                                signMask[idx2] = 0;
                            }

                        i += w;
                    }
                }
            }
        }

        keysOut = new List<MatKey>(parts.Count);
        var values = new List<MeshPart>(parts.Count);
        foreach (var kv in parts)
        {
            keysOut.Add(kv.Key);
            values.Add(kv.Value);
        }
        partsArray = values.ToArray();
    }

    static byte GetBlockSafe(byte[,,] blocks, int x, int y, int z, int[] dims)
    {
        if (x < 0 || y < 0 || z < 0) return 0;
        if (x >= dims[0] || y >= dims[1] || z >= dims[2]) return 0;
        return blocks[x, y, z];
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkMeshBuilder_NoMaterialInstances : MonoBehaviour
{
    public Material baseMaterial; // assign atlas material
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public float blockSize = 1f;
    public int atlasSize = 16;
    public Vector3 meshOffset = new Vector3(-1f, 0f, 0f); // global mesh shift along X = -1

    public byte[] transparentIDs;

    void Start()
    {
        if (baseMaterial == null)
        {
            Debug.LogError("Assign baseMaterial that uses your atlas texture.");
            return;
        }

        byte[,,] blocks = new byte[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
                for (int y = 0; y < sizeY; y++)
                    blocks[x, y, z] = (y == 0) ? (byte)2 : (byte)0;

        for (int x = 4; x < 8; x++)
            for (int z = 4; z < 8; z++)
                blocks[x, 0, z] = 90;

        HashSet<byte> transparent = null;
        if (transparentIDs != null && transparentIDs.Length > 0)
            transparent = new HashSet<byte>(transparentIDs);

        MeshAndMaterials mm = GreedyMesherWithCorrectUVAndOffset_NoMaterialInstances.GenerateMesh(blocks, baseMaterial, sizeX, sizeY, sizeZ, blockSize, atlasSize, transparent);

        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        mf.sharedMesh = mm.mesh;
        mr.sharedMaterials = mm.materials;
    }
}
