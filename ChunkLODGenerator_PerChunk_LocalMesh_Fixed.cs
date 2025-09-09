
// ChunkLODGenerator_PerChunk_LocalMesh_Fixed.cs
// Corrected per-chunk LOD generator: fixes origin multiplication bug and ensures mesh vertices align with chunk position.
// Produces a mesh LOCAL to the chunk origin by default (so assign mesh to a GameObject positioned at chunkWorldPosition).
//
// Key fixes:
// - World-space vertex = scalarOriginBlocks + ix * cellSize  (previously scalarOriginBlocks was multiplied by cellSize)
// - Edge keys use global cell coordinates (scalarOriginBlocks / cellSize + cellIndex) to ensure identical keys across chunks
// - Mesh returned is LOCAL to chunk origin (vertices in 0..CHUNK_SIZE blocks range), avoiding double-transforms
//
// Usage:
// Mesh mesh = ChunkLODGenerator_PerChunk_LocalMesh_Fixed.GenerateLODMesh(localBlocks, lodLevel, chunkWorldPosition);
//
// localBlocks: optional byte[16,16,16] array (pass null to always use global getter).
// lodLevel: 0,1,2...
// chunkWorldPosition: world block origin of the chunk (must be multiple of 16).
//
// Requirements:
// - MarchingCubesTables.EdgeTable (int[]) and MarchingCubesTables.TriangleTable (int[,]) exist.
// - WorldGenerator.Inst.procedural.GetBlockID(int x,int y,int z) exists.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class ChunkLODGenerator_PerChunk_LocalMesh_Fixed
{
    const int CHUNK_SIZE = 16;
    const float SNAP_WORLD_STEP = 1e-5f;

    // Generate mesh. If returnWorldSpaceMesh==false (default), vertices are local to chunk origin.
    public static Mesh GenerateLODMesh(byte[,,] localBlocks, int lodLevel, Vector3Int chunkWorldPosition, bool returnWorldSpaceMesh = false)
    {
        if (localBlocks != null)
        {
            if (localBlocks.GetLength(0) != CHUNK_SIZE || localBlocks.GetLength(1) != CHUNK_SIZE || localBlocks.GetLength(2) != CHUNK_SIZE)
                throw new ArgumentException($"localBlocks must be {CHUNK_SIZE}x{CHUNK_SIZE}x{CHUNK_SIZE} or null.");
        }

        // chunkWorldPosition must be the block-space origin of the chunk (e.g., multiples of 16).
        Vector3Int chunkOriginBlocks = chunkWorldPosition;

        int factor = 1 << Math.Max(0, lodLevel);
        if (factor > CHUNK_SIZE) factor = CHUNK_SIZE;

        int cellCount = CHUNK_SIZE / factor;
        int sampleSize = cellCount + 1;

        Vector3Int scalarOriginBlocks = chunkOriginBlocks;

        float[,,] scalar = new float[sampleSize, sampleSize, sampleSize];

        // Sampling: aggregate blocks in inclusive range [worldSample, worldSample + factor - 1]
        for (int sx = 0; sx < sampleSize; sx++)
        for (int sy = 0; sy < sampleSize; sy++)
        for (int sz = 0; sz < sampleSize; sz++)
        {
            int worldSampleX = scalarOriginBlocks.x + sx * factor;
            int worldSampleY = scalarOriginBlocks.y + sy * factor;
            int worldSampleZ = scalarOriginBlocks.z + sz * factor;

            int solid = 0;
            int total = 0;

            for (int dx = 0; dx < factor; dx++)
            for (int dy = 0; dy < factor; dy++)
            for (int dz = 0; dz < factor; dz++)
            {
                int gx = worldSampleX + dx;
                int gy = worldSampleY + dy;
                int gz = worldSampleZ + dz;

                int id = 0;
                if (localBlocks != null)
                {
                    int lx = gx - chunkOriginBlocks.x;
                    int ly = gy - chunkOriginBlocks.y;
                    int lz = gz - chunkOriginBlocks.z;
                    if (lx >= 0 && lx < CHUNK_SIZE && ly >= 0 && ly < CHUNK_SIZE && lz >= 0 && lz < CHUNK_SIZE)
                    {
                        id = localBlocks[lx, ly, lz];
                    }
                    else
                    {
                        id = WorldGenerator.Inst.procedural.GetBlockID(gx, gy, gz);
                    }
                }
                else
                {
                    id = WorldGenerator.Inst.procedural.GetBlockID(gx, gy, gz);
                }

                if (id != 0) solid++;
                total++;
            }

            scalar[sx, sy, sz] = total > 0 ? (float)solid / total : 0f;
        }

        float iso = 0.5f;
        float cellSize = factor;

        // Build world-space mesh using correct math
        Mesh meshWorld = MarchingCubes_Global.BuildMeshFromScalarField_WorldOrigin(scalar, iso, cellSize, scalarOriginBlocks);

        // Snap vertices to remove tiny FP differences
        SnapMeshVertices(meshWorld, SNAP_WORLD_STEP);

        if (returnWorldSpaceMesh)
        {
            meshWorld.RecalculateBounds();
            meshWorld.RecalculateNormals();
            return meshWorld;
        }

        // Convert to chunk-local coordinates by subtracting chunkOriginBlocks (block units)
        Vector3[] vertsWorld = meshWorld.vertices;
        Vector3[] vertsLocal = new Vector3[vertsWorld.Length];
        Vector3 originOffset = new Vector3(chunkOriginBlocks.x, chunkOriginBlocks.y, chunkOriginBlocks.z);
        for (int i = 0; i < vertsWorld.Length; i++)
            vertsLocal[i] = vertsWorld[i] - originOffset;

        Mesh meshLocal = new Mesh();
        if (vertsLocal.Length >= 65534) meshLocal.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshLocal.vertices = vertsLocal;
        meshLocal.triangles = meshWorld.triangles;
        meshLocal.RecalculateBounds();
        meshLocal.RecalculateNormals();
        return meshLocal;
    }

    static void SnapMeshVertices(Mesh mesh, float step)
    {
        if (step <= 0f) return;
        Vector3[] v = mesh.vertices;
        for (int i = 0; i < v.Length; i++)
        {
            v[i].x = Mathf.Round(v[i].x / step) * step;
            v[i].y = Mathf.Round(v[i].y / step) * step;
            v[i].z = Mathf.Round(v[i].z / step) * step;
        }
        mesh.vertices = v;
    }
}

// MarchingCubes_Global: builds mesh in world-space using scalarOriginBlocks (in block units).
public static class MarchingCubes_Global
{
    static readonly int[,] VertexOffset = new int[8,3] {
        {0,0,0},{1,0,0},{1,1,0},{0,1,0},
        {0,0,1},{1,0,1},{1,1,1},{0,1,1}
    };

    static readonly int[,] EdgeConnection = new int[12,2] {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    // scalarOriginBlocks: world block coordinate for scalar[0,0,0]
    public static Mesh BuildMeshFromScalarField_WorldOrigin(float[,,] scalar, float iso, float cellSize, Vector3Int scalarOriginBlocks)
    {
        int nx = scalar.GetLength(0) - 1;
        int ny = scalar.GetLength(1) - 1;
        int nz = scalar.GetLength(2) - 1;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        Dictionary<long,int> edgeVertexMap = new Dictionary<long,int>();

        int cellSizeInt = Math.Max(1, (int)cellSize);

        // base cell indices in cell-space (global)
        int baseCellX = Mathf.FloorToInt((float)scalarOriginBlocks.x / cellSizeInt);
        int baseCellY = Mathf.FloorToInt((float)scalarOriginBlocks.y / cellSizeInt);
        int baseCellZ = Mathf.FloorToInt((float)scalarOriginBlocks.z / cellSizeInt);

        long EdgeKeyCell(int cx, int cy, int cz, int edgeIndex)
        {
            // pack into 64-bit key: 21 bits per coord + edgeIndex in low bits
            long key = (((long)(cx & 0x1FFFFF)) << 43) | (((long)(cy & 0x1FFFFF)) << 22) | (((long)(cz & 0x3FFFFF)));
            key ^= (long)edgeIndex;
            return key;
        }

        for (int x = 0; x < nx; x++)
        for (int y = 0; y < ny; y++)
        for (int z = 0; z < nz; z++)
        {
            float[] val = new float[8];
            Vector3[] pos = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                int ix = x + VertexOffset[i,0];
                int iy = y + VertexOffset[i,1];
                int iz = z + VertexOffset[i,2];
                val[i] = scalar[ix, iy, iz];
                // CORRECT: world position = scalarOriginBlocks + ix * cellSize
                pos[i] = new Vector3(scalarOriginBlocks.x + ix * cellSize, scalarOriginBlocks.y + iy * cellSize, scalarOriginBlocks.z + iz * cellSize);
            }

            int cubeIndex = 0;
            for (int i = 0; i < 8; i++) if (val[i] < iso) cubeIndex |= (1 << i);

            int edges = MarchingCubesTables.EdgeTable[cubeIndex];
            if (edges == 0) continue;

            Vector3[] edgeVerts = new Vector3[12];
            for (int e = 0; e < 12; e++)
            {
                if ((edges & (1 << e)) != 0)
                {
                    int a = EdgeConnection[e,0];
                    int b = EdgeConnection[e,1];
                    float va = val[a];
                    float vb = val[b];
                    float denom = (vb - va);
                    float t = Mathf.Abs(denom) > 1e-9f ? (iso - va) / denom : 0.5f;
                    t = Mathf.Clamp01(t);
                    Vector3 p = Vector3.Lerp(pos[a], pos[b], t);
                    edgeVerts[e] = p;
                }
            }

            int cellGX = baseCellX + x;
            int cellGY = baseCellY + y;
            int cellGZ = baseCellZ + z;

            for (int ti = 0; ti < 16; ti += 3)
            {
                int ea = MarchingCubesTables.TriangleTable[cubeIndex, ti];
                if (ea < 0) break;
                int eb = MarchingCubesTables.TriangleTable[cubeIndex, ti+1];
                int ec = MarchingCubesTables.TriangleTable[cubeIndex, ti+2];

                int[] idx = new int[3];
                int[] edgesForVert = new int[3] { ea, eb, ec };
                for (int vi = 0; vi < 3; vi++)
                {
                    int edgeIdx = edgesForVert[vi];
                    long key = EdgeKeyCell(cellGX, cellGY, cellGZ, edgeIdx);
                    if (!edgeVertexMap.TryGetValue(key, out int vIdx))
                    {
                        vIdx = verts.Count;
                        verts.Add(edgeVerts[edgeIdx]);
                        edgeVertexMap.Add(key, vIdx);
                    }
                    idx[vi] = vIdx;
                }
                tris.Add(idx[0]);
                tris.Add(idx[1]);
                tris.Add(idx[2]);
            }
        }

        Mesh mesh = new Mesh();
        if (verts.Count >= 65534) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        return mesh;
    }
}
