
// GreedyMesher_OpaqueTransparent.cs
// Greedy meshing that produces:
//  - single opaque mesh (all opaque faces merged into one Mesh/submesh -> 1 draw call)
//  - optional transparent mesh (separate GameObject/mesh rendered after opaque -> 1 draw call)
// Avoids creating material instances (uses provided materials).
// Keeps correct UVs by merging only faces with identical tileX/tileY/faceType/id.
//
// Usage:
//  - Drop into Assets/Scripts
//  - Assign baseOpaqueMaterial (atlas material) and optional transparentMaterial (with alpha blending)
//  - Attach ChunkBuilder_OpaqueTransparent to a GameObject with MeshFilter+MeshRenderer
//  - Run scene
//
// Notes:
//  - This preserves per-tile correct UV mapping (we only merge faces that have identical tile coords and face type)
//  - The opaque mesh will contain quads of many different textures but all use same atlas material (no material instancing)
//  - Transparent faces are collected separately and rendered in a child GameObject to ensure correct render order
//  - Performance: this reduces draw calls drastically for chunks composed of mixed tiles
//  - If you want to further reduce CPU, consider caching BlockUVS table, pooling MeshParts, or moving to Jobs/Burst

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public struct OpaqueTransparentResult
{
    public Mesh opaqueMesh;
    public Mesh transparentMesh; // may be null
    public Material opaqueMaterial;
    public Material transparentMaterial; // may be null (in which case opaqueMaterial is used)
}

public static class GreedyMesherOpaqueTransparent
{
    const int DEFAULT_ATLAS_SIZE = 16;
    static readonly Vector3 GlobalMeshOffset = new Vector3(-1f, 0f, 0f); // as requested

    // Generate meshes: opaqueMesh (always produced, even if empty), transparentMesh (null if none)
    // baseOpaqueMaterial: material that samples the atlas (used for opaque)
    // transparentMaterial: optional material to render transparent mesh (if null, baseOpaqueMaterial is reused)
    public static OpaqueTransparentResult GenerateMeshes(byte[,,] blocks,
                                                         Material baseOpaqueMaterial,
                                                         Material transparentMaterial,
                                                         int sizeX = 16, int sizeY = 16, int sizeZ = 16,
                                                         float blockSize = 1f,
                                                         int atlasSize = DEFAULT_ATLAS_SIZE,
                                                         HashSet<byte> transparentIDs = null)
    {
        if (blocks == null) throw new ArgumentNullException(nameof(blocks));
        if (baseOpaqueMaterial == null) throw new ArgumentNullException(nameof(baseOpaqueMaterial));

        // Lists for final merged geometry
        var opaqueVerts = new List<Vector3>();
        var opaqueTris = new List<int>();
        var opaqueUVs = new List<Vector2>();

        var transpVerts = new List<Vector3>();
        var transpTris = new List<int>();
        var transpUVs = new List<Vector2>();

        int[] dims = new int[3] { sizeX, sizeY, sizeZ };

        int[] x = new int[3];
        int[] q = new int[3];
        int[] du = new int[3];
        int[] dv = new int[3];

        // We pack a mask int encoding tileX(8), tileY(8), faceType(8), id(8)
        // layout: (tileX<<24) | (tileY<<16) | (faceType<<8) | id
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;
            int maxU = dims[u];
            int maxV = dims[v];

            int[] mask = new int[maxU * maxV]; // 0 means no face
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

                        bool aNonZero = (a != 0);
                        bool bNonZero = (b != 0);

                        int idx = q[u] + q[v] * maxU;

                        if (aNonZero == bNonZero)
                        {
                            mask[idx] = 0;
                            signMask[idx] = 0;
                        }
                        else
                        {
                            // face belongs to the solid side (a or b)
                            byte id = (aNonZero ? a : b);
                            sbyte sgn = (aNonZero ? (sbyte)+1 : (sbyte)-1);
                            signMask[idx] = sgn;

                            // which tile and face type to use
                            BlockUVS buv = BlockUVS.GetBlock(id);
                            int tileX = 0, tileY = 0;
                            int faceType = 0;
                            if (d == 1)
                            {
                                if (sgn == 1) { tileX = buv.TextureX; tileY = buv.TextureY; faceType = 1; } // top
                                else { tileX = buv.TextureXBottom; tileY = buv.TextureYBottom; faceType = 2; } // bottom
                            }
                            else
                            {
                                tileX = buv.TextureXSide; tileY = buv.TextureYSide; faceType = 0; // side
                            }

                            // pack into mask: keep tileX(8),tileY(8),faceType(8),id(8)
                            int packed = (tileX << 24) | (tileY << 16) | (faceType << 8) | id;
                            mask[idx] = packed;
                        }
                    }
                }

                // greedy merge on mask
                for (int j = 0; j < maxV; j++)
                {
                    for (int i = 0; i < maxU; )
                    {
                        int idx = i + j * maxU;
                        if (mask[idx] == 0) { i++; continue; }

                        int packed = mask[idx];
                        sbyte sgn = signMask[idx];

                        int w;
                        for (w = 1; i + w < maxU && mask[idx + w] == packed && signMask[idx + w] == sgn; w++) { }

                        int h;
                        bool stop = false;
                        for (h = 1; j + h < maxV; h++)
                        {
                            for (int k = 0; k < w; k++)
                            {
                                int idx2 = (i + k) + (j + h) * maxU;
                                if (mask[idx2] != packed || signMask[idx2] != sgn) { stop = true; break; }
                            }
                            if (stop) break;
                        }

                        // position and extents
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

                        // unpack packed
                        int tileX = (packed >> 24) & 0xFF;
                        int tileY = (packed >> 16) & 0xFF;
                        int faceType = (packed >> 8) & 0xFF;
                        byte id = (byte)(packed & 0xFF);

                        // compute atlas UVs for this tile
                        float tileU = 1f / atlasSize;
                        float tileV = 1f / atlasSize;
                        float uMin = tileX * tileU;
                        float vMin = tileY * tileV;
                        float uMax = uMin + tileU;
                        float vMax = vMin + tileV;

                        // Determine whether this face belongs to opaque or transparent mesh
                        bool isTransparent = (transparentIDs != null && transparentIDs.Contains(id));

                        // Add quad to appropriate lists
                        if (isTransparent)
                        {
                            int baseVert = transpVerts.Count;
                            if (windingCorrect)
                            {
                                transpVerts.Add(v0);
                                transpVerts.Add(v1);
                                transpVerts.Add(v2);
                                transpVerts.Add(v3);

                                transpTris.Add(baseVert + 0);
                                transpTris.Add(baseVert + 1);
                                transpTris.Add(baseVert + 2);
                                transpTris.Add(baseVert + 0);
                                transpTris.Add(baseVert + 2);
                                transpTris.Add(baseVert + 3);

                                transpUVs.Add(new Vector2(uMin, vMin));
                                transpUVs.Add(new Vector2(uMax, vMin));
                                transpUVs.Add(new Vector2(uMax, vMax));
                                transpUVs.Add(new Vector2(uMin, vMax));
                            }
                            else
                            {
                                transpVerts.Add(v0);
                                transpVerts.Add(v3);
                                transpVerts.Add(v2);
                                transpVerts.Add(v1);

                                transpTris.Add(baseVert + 0);
                                transpTris.Add(baseVert + 1);
                                transpTris.Add(baseVert + 2);
                                transpTris.Add(baseVert + 0);
                                transpTris.Add(baseVert + 2);
                                transpTris.Add(baseVert + 3);

                                transpUVs.Add(new Vector2(uMin, vMin));
                                transpUVs.Add(new Vector2(uMin, vMax));
                                transpUVs.Add(new Vector2(uMax, vMax));
                                transpUVs.Add(new Vector2(uMax, vMin));
                            }
                        }
                        else
                        {
                            int baseVert = opaqueVerts.Count;
                            if (windingCorrect)
                            {
                                opaqueVerts.Add(v0);
                                opaqueVerts.Add(v1);
                                opaqueVerts.Add(v2);
                                opaqueVerts.Add(v3);

                                opaqueTris.Add(baseVert + 0);
                                opaqueTris.Add(baseVert + 1);
                                opaqueTris.Add(baseVert + 2);
                                opaqueTris.Add(baseVert + 0);
                                opaqueTris.Add(baseVert + 2);
                                opaqueTris.Add(baseVert + 3);

                                opaqueUVs.Add(new Vector2(uMin, vMin));
                                opaqueUVs.Add(new Vector2(uMax, vMin));
                                opaqueUVs.Add(new Vector2(uMax, vMax));
                                opaqueUVs.Add(new Vector2(uMin, vMax));
                            }
                            else
                            {
                                opaqueVerts.Add(v0);
                                opaqueVerts.Add(v3);
                                opaqueVerts.Add(v2);
                                opaqueVerts.Add(v1);

                                opaqueTris.Add(baseVert + 0);
                                opaqueTris.Add(baseVert + 1);
                                opaqueTris.Add(baseVert + 2);
                                opaqueTris.Add(baseVert + 0);
                                opaqueTris.Add(baseVert + 2);
                                opaqueTris.Add(baseVert + 3);

                                opaqueUVs.Add(new Vector2(uMin, vMin));
                                opaqueUVs.Add(new Vector2(uMin, vMax));
                                opaqueUVs.Add(new Vector2(uMax, vMax));
                                opaqueUVs.Add(new Vector2(uMax, vMin));
                            }
                        }

                        // zero out mask cells we consumed
                        for (int hh = 0; hh < h; hh++)
                        {
                            for (int ww = 0; ww < w; ww++)
                            {
                                int idx2 = (i + ww) + (j + hh) * maxU;
                                mask[idx2] = 0;
                                signMask[idx2] = 0;
                            }
                        }

                        i += w;
                    }
                }
            }
        }

        // apply global mesh offset to vertices
        for (int i = 0; i < opaqueVerts.Count; i++) opaqueVerts[i] += GlobalMeshOffset;
        for (int i = 0; i < transpVerts.Count; i++) transpVerts[i] += GlobalMeshOffset;

        // create meshes
        Mesh opaqueMesh = new Mesh();
        if (opaqueVerts.Count >= 65535) opaqueMesh.indexFormat = IndexFormat.UInt32;
        opaqueMesh.SetVertices(opaqueVerts);
        opaqueMesh.SetUVs(0, opaqueUVs);
        opaqueMesh.SetTriangles(opaqueTris, 0);
        opaqueMesh.RecalculateNormals();
        opaqueMesh.RecalculateBounds();
        opaqueMesh.UploadMeshData(false);

        Mesh transparentMesh = null;
        if (transpVerts.Count > 0)
        {
            transparentMesh = new Mesh();
            if (transpVerts.Count >= 65535) transparentMesh.indexFormat = IndexFormat.UInt32;
            transparentMesh.SetVertices(transpVerts);
            transparentMesh.SetUVs(0, transpUVs);
            transparentMesh.SetTriangles(transpTris, 0);
            transparentMesh.RecalculateNormals();
            transparentMesh.RecalculateBounds();
            transparentMesh.UploadMeshData(false);
        }

        var result = new OpaqueTransparentResult();
        result.opaqueMesh = opaqueMesh;
        result.transparentMesh = transparentMesh;
        result.opaqueMaterial = baseOpaqueMaterial;
        result.transparentMaterial = transparentMaterial != null ? transparentMaterial : baseOpaqueMaterial;
        return result;
    }

    static byte GetBlockSafe(byte[,,] blocks, int x, int y, int z, int[] dims)
    {
        if (x < 0 || y < 0 || z < 0) return 0;
        if (x >= dims[0] || y >= dims[1] || z >= dims[2]) return 0;
        return blocks[x, y, z];
    }
}

// Simple component to build chunk and assign meshes/materials.
// Opaque mesh is assigned to this object's MeshFilter/MeshRenderer.
// Transparent mesh (if any) is created as a child GameObject and rendered after the opaque.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkBuilder_OpaqueTransparent : MonoBehaviour
{
    public Material baseOpaqueMaterial;
    public Material transparentMaterial; // optional (set to a transparent material)
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public float blockSize = 1f;
    public int atlasSize = 16;
    public Vector3 meshOffset = new Vector3(-1f, 0f, 0f); // user requested global shift
    public byte[] transparentIDs;

    void Start()
    {
        if (baseOpaqueMaterial == null)
        {
            Debug.LogError("Assign baseOpaqueMaterial that uses your atlas texture.");
            return;
        }

        // sample block data: flat plane at y==0 id=2, small patch id=90
        byte[,,] blocks = new byte[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
                for (int y = 0; y < sizeY; y++)
                    blocks[x, y, z] = (y == 0) ? (byte)2 : (byte)0;

        for (int x = 4; x < 8; x++)
            for (int z = 4; z < 8; z++)
                blocks[x, 0, z] = 90;

        HashSet<byte> transparentSet = null;
        if (transparentIDs != null && transparentIDs.Length > 0) transparentSet = new HashSet<byte>(transparentIDs);

        // Use overload that allows passing transparentIDs
        var res = GreedyMesherOpaqueTransparent.GenerateMeshes(blocks, baseOpaqueMaterial, transparentMaterial, sizeX, sizeY, sizeZ, blockSize, atlasSize, transparentSet);

        // Assign opaque mesh
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        mf.sharedMesh = res.opaqueMesh;
        mr.sharedMaterial = res.opaqueMaterial; // reuse provided material (no new instances)

        // If transparent mesh exists, create child object and render after opaque
        if (res.transparentMesh != null)
        {
            GameObject go = new GameObject("TransparentMesh");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var mf2 = go.AddComponent<MeshFilter>();
            var mr2 = go.AddComponent<MeshRenderer>();
            mf2.sharedMesh = res.transparentMesh;
            mr2.sharedMaterial = res.transparentMaterial;
            // Ensure transparent material uses transparent rendering (user responsibility)
        }
    }
}
