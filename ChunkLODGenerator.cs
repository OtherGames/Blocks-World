using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��������� LOD-���� ��� ����� 16x16x16. 
/// ����: byte[,,] blocks (������ ������ ���� 16x16x16), lodLevel >= 0, chunkGlobalPosition � �������� �����������.
/// ���������� Mesh � ��������� � ��������� ����������� (0..16 �� ����), GameObject � �������� ���� Mesh ������ ����� transform.position == chunkGlobalPosition.
/// </summary>
public static class ChunkLODGenerator
{
    private const int CHUNK_SIZE = 16;
    private const float ISOLEVEL = 0.5f; // ����� ��� marching cubes

    // �������� ������ ���� (�����������)
    private static readonly int[,] vertexOffset = new int[,]
    {
        {0,0,0},{1,0,0},{1,1,0},{0,1,0},
        {0,0,1},{1,0,1},{1,1,1},{0,1,1}
    };

    // ������ ����� ��������� ��� ������� ����
    private static readonly int[,] edgeConnection = new int[,]
    {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    /// <summary>
    /// �������� �����: ���������� LOD Mesh.
    /// </summary>
    /// <param name="blocks">byte[16,16,16] � ������ ����� ������ ����� (0 = ������, !=0 = solid)</param>
    /// <param name="lodLevel">������� LOD: 0 = full res (1 ����/������), 1 = ��� 2, 2 = ��� 4 � �.�.</param>
    /// <param name="chunkGlobalPosition">���������� ������� ����� (� ������) � ��������� � transform.position �����</param>
    /// <returns>UnityEngine.Mesh</returns>
    public static Mesh GenerateChunkLOD(byte[,,] blocks, int lodLevel, Vector3Int chunkGlobalPosition)
    {
        if (blocks == null) throw new System.ArgumentNullException(nameof(blocks));
        if (blocks.GetLength(0) != CHUNK_SIZE || blocks.GetLength(1) != CHUNK_SIZE || blocks.GetLength(2) != CHUNK_SIZE)
            Debug.LogWarning("��������� blocks �������� 16x16x16");

        int step = 1 << lodLevel;                 // 1,2,4,8...
        int samplesPerAxis = CHUNK_SIZE / step + 1; // ����� sample-����� ����� ��� (nCells + 1)
        // sample grid spans [0..CHUNK_SIZE] � ����� = step (� �������� ��������)
        // ���������� � world-blocks: chunkGlobalPosition + localSamplePosition

        // ������������� ��������
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        // ��� ��� ������ �� ������ (����� �� �����������)
        // ����: (cubeX, cubeY, cubeZ, edgeIndex) -> vertexIndex
        var edgeVertexCache = new Dictionary<long, int>();

        // ��������� ������� ��� ��������� ����������� ����� ��� ���� �����
        long EdgeKey(int cx, int cy, int cz, int edgeIndex)
        {
            // ������� � 64-bit: low bits - coords + edgeIndex
            // coords �� ��������� 16, �� ��� ������������ ��������
            return (((long)cx & 0xffffL) << 48) | (((long)cy & 0xffffL) << 32) | (((long)cz & 0xffffL) << 16) | (long)edgeIndex;
        }

        // ������� ��������� ��������� (density) � ���� �����: 1.0 = solid, 0.0 = air.
        float SampleDensityAt(int gx, int gy, int gz)
        {
            // gx,gy,gz � ���������� �������� ���������� �����-����.
            // �� ������� ������������ ��������� ������ blocks, ���� ����� ������ �����.
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
                // ���������� � �������� ���������� �� ��������
                id = WorldGenerator.Inst.procedural.GetBlockID(gx, gy, gz);
            }

            return (id != 0) ? 1.0f : 0.0f;
        }

        // ������������ ������� �� �����
        Vector3 VertexInterp(Vector3 p1, Vector3 p2, float valp1, float valp2)
        {
            if (Mathf.Abs(ISOLEVEL - valp1) < 1e-6f) return p1;
            if (Mathf.Abs(ISOLEVEL - valp2) < 1e-6f) return p2;
            if (Mathf.Abs(valp1 - valp2) < 1e-6f) return p1;
            float mu = (ISOLEVEL - valp1) / (valp2 - valp1);
            return p1 + mu * (p2 - p1);
        }

        // ��������������� �������: �������� ������� ����� �������� ��������� (����������� ��������)
        float SampleDensityFloat(Vector3 worldPos)
        {
            // worldPos � ���������� �������� ����������� (float). ��� ������ ��������� ���������� ��������� integer sample.
            int x = Mathf.RoundToInt(worldPos.x);
            int y = Mathf.RoundToInt(worldPos.y);
            int z = Mathf.RoundToInt(worldPos.z);
            return SampleDensityAt(x, y, z);
        }

        // �������� �� ���� ����� ����� (cubeCount = samplesPerAxis-1 �� ������ ���)
        for (int cz = 0; cz < samplesPerAxis - 1; cz++)
        {
            for (int cy = 0; cy < samplesPerAxis - 1; cy++)
            {
                for (int cx = 0; cx < samplesPerAxis - 1; cx++)
                {
                    // ���������� � ��������� �������� �������� (�� 0 �� CHUNK_SIZE)
                    // ��� ����� ������ (cx,cy,cz) � (cx+1,cy+1,cz+1)
                    float[] cubeValue = new float[8];
                    Vector3[] cubePos = new Vector3[8];

                    for (int i = 0; i < 8; i++)
                    {
                        int vx = cx * step + vertexOffset[i, 0];
                        int vy = cy * step + vertexOffset[i, 1];
                        int vz = cz * step + vertexOffset[i, 2];

                        // ���������� ������� ���� � ������
                        int gx = chunkGlobalPosition.x + vx;
                        int gy = chunkGlobalPosition.y + vy;
                        int gz = chunkGlobalPosition.z + vz;

                        cubeValue[i] = SampleDensityAt(gx, gy, gz);
                        // ��������� ������� ������� � ����������� ����� (0..16)
                        cubePos[i] = new Vector3(vx, vy, vz);
                    }

                    // ��������� ������ ������������
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                        if (cubeValue[i] > ISOLEVEL) cubeIndex |= (1 << i);

                    // ���������� ������� ������� ����� (EdgeTable) �� MarchingCubesTables
                    int edgeFlags = MarchingCubesTables.EdgeTable[cubeIndex];
                    if (edgeFlags == 0) continue; // ��� �������������

                    // ��� ������� �����, ���� ����������� � ��������� �������
                    Vector3[] edgeVertex = new Vector3[12];
                    for (int e = 0; e < 12; e++)
                    {
                        if ((edgeFlags & (1 << e)) == 0) continue;

                        // ��������� �����������: ���� � �� �� �������� ������� ��� �������� �����
                        long key = EdgeKey(cx, cy, cz, e);
                        if (edgeVertexCache.TryGetValue(key, out int cachedIndex))
                        {
                            // ��� ������� ������� ��� ����� �����
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

                        // ��������� ������� � ������� (���� ������� � 0, ��������� �����)
                        int newIndex = verts.Count;
                        verts.Add(vert);
                        normals.Add(Vector3.zero);

                        edgeVertex[e] = vert;
                        edgeVertexCache[key] = newIndex;
                    }

                    // ������ ������ ������������ �� TriangleTable
                    for (int t = 0; t < 5; t++) // �������� 5 ������������� �� ���
                    {
                        int triIndex = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 0];
                        if (triIndex < 0) break;

                        int e0 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 0];
                        int e1 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 1];
                        int e2 = MarchingCubesTables.TriangleTable[cubeIndex, 3 * t + 2];

                        // ��� �������� ����� ����� ������ ������� �� ���� (�� ��������� ������ ��� ����������)
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

                        // ����� ���������� ������� ����� �� ��������
                    }
                }
            }
        }

        // ����������� �������: ��� ������� ������������ �������� ������� � ��������
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

        // ������ Mesh
        Mesh m = new Mesh();
        m.indexFormat = (verts.Count > 65535) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        // ������� ��� � ��������� ����������� (0..16). ���� ������ ������� (blockSize != 1), ������ �����.
        m.SetVertices(verts);
        m.SetNormals(normals);
        m.SetTriangles(indices, 0);
        m.RecalculateBounds();

        return m;
    }
}
