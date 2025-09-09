using System.Collections.Generic;
using UnityEngine;

/* MarchingCubes.cs - unchanged from earlier version (geometry only).
   It emits vertices and triangles; color assignment is done after in LODMeshGenerator.
*/

public static class MarchingCubes
{
    public struct MeshData
    {
        public List<Vector3> Vertices;
        public List<int> Triangles;
    }

    private static readonly Vector3Int[] CornerOffset = new Vector3Int[8]
    {
        new Vector3Int(0,0,0),
        new Vector3Int(1,0,0),
        new Vector3Int(1,0,1),
        new Vector3Int(0,0,1),
        new Vector3Int(0,1,0),
        new Vector3Int(1,1,0),
        new Vector3Int(1,1,1),
        new Vector3Int(0,1,1)
    };

    private static readonly int[,] EdgeConnection = new int[12,2]
    {
        {0,1},
        {1,2},
        {2,3},
        {3,0},
        {4,5},
        {5,6},
        {6,7},
        {7,4},
        {0,4},
        {1,5},
        {2,6},
        {3,7}
    };

    public static MeshData GenerateMesh(float[,,] density, int step, Vector3Int chunkGlobalPosition)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();

        int nx = density.GetLength(0);
        int ny = density.GetLength(1);
        int nz = density.GetLength(2);

        float iso = LODMeshGenerator.ISO_LEVEL;

        for (int x = 0; x < nx - 1; x++)
        {
            for (int y = 0; y < ny - 1; y++)
            {
                for (int z = 0; z < nz - 1; z++)
                {
                    float[] cube = new float[8];
                    Vector3[] cornerPos = new Vector3[8];
                    for (int i = 0; i < 8; i++)
                    {
                        int cx = x + CornerOffset[i].x;
                        int cy = y + CornerOffset[i].y;
                        int cz = z + CornerOffset[i].z;
                        cube[i] = density[cx, cy, cz];

                        cornerPos[i] = new Vector3(cx * step, cy * step, cz * step);
                    }

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                        if (cube[i] > iso) cubeIndex |= 1 << i;

                    if (MarchingCubesTables.EdgeTable[cubeIndex] == 0) continue;

                    Vector3[] edgeVertex = new Vector3[12];
                    for (int i = 0; i < 12; i++)
                    {
                        if ((MarchingCubesTables.EdgeTable[cubeIndex] & (1 << i)) != 0)
                        {
                            int a = EdgeConnection[i, 0];
                            int b = EdgeConnection[i, 1];
                            edgeVertex[i] = VertexInterp(iso, cornerPos[a], cornerPos[b], cube[a], cube[b]);
                        }
                    }

                    for (int i = 0; MarchingCubesTables.TriangleTable[cubeIndex, i] != -1; i += 3)
                    {
                        int e0 = MarchingCubesTables.TriangleTable[cubeIndex, i];
                        int e1 = MarchingCubesTables.TriangleTable[cubeIndex, i + 1];
                        int e2 = MarchingCubesTables.TriangleTable[cubeIndex, i + 2];

                        int baseIndex = verts.Count;
                        verts.Add(edgeVertex[e0]);
                        verts.Add(edgeVertex[e1]);
                        verts.Add(edgeVertex[e2]);

                        tris.Add(baseIndex);
                        tris.Add(baseIndex + 1);
                        tris.Add(baseIndex + 2);
                    }
                }
            }
        }

        return new MeshData { Vertices = verts, Triangles = tris };
    }

    private static Vector3 VertexInterp(float iso, Vector3 p1, Vector3 p2, float valp1, float valp2)
    {
        if (Mathf.Abs(iso - valp1) < 0.00001f) return p1;
        if (Mathf.Abs(iso - valp2) < 0.00001f) return p2;
        if (Mathf.Abs(valp1 - valp2) < 0.00001f) return p1;
        float mu = (iso - valp1) / (valp2 - valp1);
        return p1 + mu * (p2 - p1);
    }
}
