using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���������� ������ lightmap ��� �����.
/// ������������ ������� ��������������� � ������ ���� skylight-seed �� �������.
/// �������� IsOpaque/GetLightEmission ��� ���� ID.
/// </summary>
public static class VoxelLighting
{
    public const byte MAX_LIGHT = 15;

    // ������� ������ ����� ����������� (������������). ��� ������ � ��� ������.
    public const int SKY_SCAN_CHUNKS_UP = 4;

    // ����������� �� ������������� ���������� ����/����� �� ����� (� ������)
    // maxUpBlocks = size * SKY_SCAN_CHUNKS_UP
    struct QItem { public Vector3Int pos; public byte light; public QItem(Vector3Int p, byte l) { pos = p; light = l; } }

    // ---- ������� ���� ����� ----
    static bool IsOpaque(byte blockID)
    {
        if (blockID == 0) return false;
        // ������: id==2 � ������ (���������)
        //if (blockID == 2) return false;
        return true;
    }

    static byte GetLightEmission(byte blockID)
    {
        if (blockID == 5) return 14; // �����
        if (blockID == 6) return 15; // �����
        return 0;
    }
    // ---------------------------

    public static byte[,,] ComputeChunkLightmap(ChunckComponent chunk)
    {
        int size = chunk.size;
        Vector3Int chunkOrigin = new Vector3Int(Mathf.FloorToInt(chunk.pos.x), Mathf.FloorToInt(chunk.pos.y), Mathf.FloorToInt(chunk.pos.z));

        byte[,,] localLight = new byte[size, size, size];

        var lightMap = new Dictionary<Vector3Int, byte>(new Vector3IntComparer());
        var q = new Queue<QItem>();

        // �������������� ���� � ������������� "������" � ����������� ���.
        int scanUpBlocks = size * SKY_SCAN_CHUNKS_UP;
        int minX = chunkOrigin.x - MAX_LIGHT;
        int maxX = chunkOrigin.x + size + MAX_LIGHT;
        int minZ = chunkOrigin.z - MAX_LIGHT;
        int maxZ = chunkOrigin.z + size + MAX_LIGHT;
        int minY = chunkOrigin.y - MAX_LIGHT; // ������� ����
        int maxY = chunkOrigin.y + size + scanUpBlocks; // ����� ������������

        // ��������� ������� �������� ��������� � ����
        bool InBounds(Vector3Int p)
        {
            if (p.x < minX || p.x > maxX) return false;
            if (p.z < minZ || p.z > maxZ) return false;
            if (p.y < minY || p.y > maxY) return false;
            return true;
        }

        // 1) �������� ������ ����� (� ������? � ������ ������, �������� ����� ����� "����������" ����� safe-get)
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    Vector3Int gpos = chunkOrigin + new Vector3Int(x, y, z);
                    byte id = SafeGetBlockID(gpos);
                    byte em = GetLightEmission(id);
                    if (em > 0 && InBounds(gpos))
                    {
                        SetLightAndEnqueue(lightMap, q, gpos, em);
                    }
                }

        // 2) skylight: ��� ������ ������� lx,lz ������ 1 seed ������ ����-��������� (�� ��������� ��� Y)
        for (int lx = 0; lx < size; lx++)
            for (int lz = 0; lz < size; lz++)
            {
                int gx = chunkOrigin.x + lx;
                int gz = chunkOrigin.z + lz;

                // ��������� ������ ���� � �������� maxY..(min check)
                int foundOpaqueY = int.MinValue;
                // �������� ������ ������������� ���������
                for (int gy = maxY; gy >= minY; gy--)
                {
                    var pos = new Vector3Int(gx, gy, gz);
                    byte id = SafeGetBlockID(pos);
                    if (id != 0 && IsOpaque(id))
                    {
                        foundOpaqueY = gy;
                        break;
                    }
                }

                int seedY;
                if (foundOpaqueY == int.MinValue)
                {
                    // ������������ �� ������� � ����-��������� � �������, ��� ����� ����.
                    seedY = maxY;
                }
                else
                {
                    seedY = foundOpaqueY + 1;
                    if (seedY > maxY) seedY = maxY;
                }

                var seedPos = new Vector3Int(gx, seedY, gz);
                if (InBounds(seedPos))
                {
                    // ������ ���� seed �� �������
                    SetLightAndEnqueue(lightMap, q, seedPos, MAX_LIGHT);
                }
            }

        // 3) BFS ��������������� �����, �� ������ � �������� InBounds
        Vector3Int[] neigh = new[]{
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        while (q.Count > 0)
        {
            var it = q.Dequeue();
            var cur = it.pos;
            byte curLight = it.light;

            // ���� �������� � ����� ���������� � ���������� ����������
            if (lightMap.TryGetValue(cur, out byte stored) && stored != curLight) continue;

            foreach (var d in neigh)
            {
                Vector3Int nb = cur + d;
                if (!InBounds(nb)) continue;

                byte blockId = SafeGetBlockID(nb);
                if (IsOpaque(blockId)) continue; // ���������

                byte newLight = (byte)(curLight == 0 ? 0 : curLight - 1);
                if (newLight == 0) continue;

                if (!lightMap.TryGetValue(nb, out byte existing) || newLight > existing)
                {
                    lightMap[nb] = newLight;
                    q.Enqueue(new QItem(nb, newLight));
                }
            }
        }

        // 4) �������� �������� � ��������� ����� �����
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                for (int z = 0; z < size; z++)
                {
                    var g = chunkOrigin + new Vector3Int(x, y, z);
                    if (lightMap.TryGetValue(g, out byte v)) localLight[x, y, z] = v;
                    else localLight[x, y, z] = 0;
                }

        return localLight;
    }

    static void SetLightAndEnqueue(Dictionary<Vector3Int, byte> map, Queue<QItem> q, Vector3Int pos, byte light)
    {
        if (light == 0) return;
        if (!map.TryGetValue(pos, out byte exist) || light > exist)
        {
            map[pos] = light;
            q.Enqueue(new QItem(pos, light));
        }
    }

    // ���������� ������ � ���������� 0 ��� �������������/��������� ����
    static byte SafeGetBlockID(Vector3Int globalPos)
    {
        try
        {
            var wg = WorldGenerator.Inst;
            if (wg.HasChunck(globalPos, out var key))
            {
                var chunk = wg.chuncks[key];
                return chunk.GetBlockID(globalPos);
            }
            else
            {
                return 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    class Vector3IntComparer : IEqualityComparer<Vector3Int>
    {
        public bool Equals(Vector3Int a, Vector3Int b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public int GetHashCode(Vector3Int v) => ((v.x * 73856093) ^ (v.y * 19349663) ^ (v.z * 83492791));
    }
}
