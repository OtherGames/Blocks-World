using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;
using UnityEngine.Events;

using System;
using System.Runtime.CompilerServices;

public class ChunckComponent
{
    public MeshRenderer renderer;
    public MeshFilter meshFilter;
    public MeshCollider collider;
    public Vector3 pos;
    public byte[,,] blocks;

    public NavMeshModifier navMeshModifier;
    public NavMeshSurface meshSurface;
    public ChunckState chunckState;

    public Dictionary<Vector3, NavMeshLink> links = new Dictionary<Vector3, NavMeshLink>();
    public Dictionary<Vector3Int, List<TurnBlockData>> turnedBlocks = new Dictionary<Vector3Int, List<TurnBlockData>>();// Словарь который хранит информацию о повернутых блоках  
    public List<Vector3Int> grassBlocks = new List<Vector3Int>();
    
    public int size;
    public bool blocksLoaded;

    public static UnityEvent<ChunckComponent> onChunckInit = new UnityEvent<ChunckComponent>();
    /// <summary>
    /// Chunck With Blocks
    /// </summary>
    public static UnityEvent<ChunckComponent> onBlocksSeted = new UnityEvent<ChunckComponent>();

    public ChunckComponent(int posX, int posY, int posZ)
    {
        size = WorldGenerator.size;
        blocks = new byte[size, size, size];
        pos.x = posX;
        pos.y = posY;
        pos.z = posZ;

        onChunckInit?.Invoke(this);
    }

    /// <summary>
    /// Не забываем перед вызовом сделать +1 по оси X
    /// </summary>
    /// <param name="blockPosition"></param>
    /// <returns></returns>
    public byte GetBlockID(Vector3 blockPosition)
    {
        var pos = renderer.transform.position;

        int xBlock = (int)(blockPosition.x - pos.x);
        int yBlock = (int)(blockPosition.y - pos.y);
        int zBlock = (int)(blockPosition.z - pos.z);

        //Debug.Log($"{xBlock}|{yBlock}|{zBlock}");
        byte blockID = blocks[xBlock, yBlock, zBlock];

        return blockID;
    }

    public void SetBlock(Vector3 localBlockPos, byte blockID)
    {
        int xIdx = (int)localBlockPos.x;
        int yIdx = (int)localBlockPos.y;
        int zIdx = (int)localBlockPos.z;

        blocks[xIdx, yIdx, zIdx] = blockID;
    }

    TurnBlockData turnBlockData;
    public void AddTurnBlock(Vector3Int blockLocalPos, float angle, RotationAxis axis)
    {
        turnBlockData.angle = angle;
        turnBlockData.axis = axis;

        if (turnedBlocks.ContainsKey(blockLocalPos))
        {
            turnedBlocks[blockLocalPos].Add(turnBlockData);
        }
        else
        {
            turnedBlocks.Add(blockLocalPos, new List<TurnBlockData>() { turnBlockData });
        }
    }

    public void AddTurnBlock(Vector3Int blockLocalPos, TurnBlockData[] turnsData)
    {
        var turnsBlockData = turnedBlocks[blockLocalPos];
        turnsBlockData.Clear();
        foreach (var item in turnsData)
        {
            turnsBlockData.Add(item);
        }

        if (turnedBlocks.ContainsKey(blockLocalPos))
        {
            
            turnedBlocks[blockLocalPos] = turnsBlockData;
        }
        else
        {
            turnedBlocks.Add(blockLocalPos, turnsBlockData);
        }
    }

}

public enum ChunckState : byte
{
    Generated     = 0,
    NotGenerated  = 1,
}

[System.Serializable]
public struct TurnBlockData
{
    public float angle;
    public RotationAxis axis;
}

[JsonObject]
public class ChunckData
{
    [JsonProperty]
    public List<JsonBlockData> changedBlocks;
    [JsonProperty]
    public List<UserChunckData> usersChangedBlocks;
    [JsonProperty]
    public byte[,,] blocks;
    [JsonProperty]
    public List<JsonTurnedBlock> turnedBlocks;

    [JsonConstructor]
    private ChunckData() { }

    public ChunckData(ChunckComponent chunck)
    {
        changedBlocks = new List<JsonBlockData>();
        usersChangedBlocks = new List<UserChunckData>();
        turnedBlocks = new List<JsonTurnedBlock>();
        blocks = chunck.blocks;
        //blocks = new List<List<byte>>();

        //for (int x = 0; x < WorldGenerator.size; x++)
        //{
        //    for (int y = 0; y < WorldGenerator.size; y++)
        //    {
        //        if (x < blocks.Count)
        //        {
        //            if (y < blocks[x].Count)
        //            {

        //            }
        //            else
        //            {
        //                blocks[x].Add(chunck.blocks[x, y, 0]);
        //            }
        //        }
        //        else
        //        {
        //            blocks.Add(new List<byte>());
        //            blocks[x].Add(chunck.blocks[x, y, 0]);
        //        }
        //    }
        //}
    }

    // ========== Эксперименты работы со структурами ===============
    //public JsonTurnedBlock notFoundTurnedBlock = default;

    ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public ref JsonTurnedBlock GetTurnedBlock (Vector3 pos)
    //{
    //    for (int i = 0; i < turnedBlocks.Length; i++)
    //    {
    //        if (turnedBlocks[i].pos.Equals(pos))
    //        {
    //            return ref turnedBlocks[i];
    //        }
    //    }

    //    return ref notFoundTurnedBlock;
    //}

    //public void AddTurnedBlock(JsonTurnedBlock turnedBlock)
    //{

    //}
    // ==========================================================

    [JsonObject]
    public class UserChunckData
    {
        [JsonProperty]
        public string userName;
        [JsonProperty]
        public List<JsonBlockData> changedBlocks = new List<JsonBlockData>();

        [JsonConstructor]
        public UserChunckData() { }
    }

    [JsonObject]
    public class JsonBlockData
    {
        public float posX;
        public float posY;
        public float posZ;
        public byte blockId;

        [JsonIgnore]
        Vector3 pos;

        public JsonBlockData(Vector3 pos, byte blockId)
        {
            posX = pos.x;
            posY = pos.y;
            posZ = pos.z;
            this.blockId = blockId;
        }

        [JsonIgnore]
        public Vector3 Pos
        {
            get
            {
                if (pos == Vector3.zero)
                {
                    pos.x = posX;
                    pos.y = posY;
                    pos.z = posZ;
                }

                return pos;
            }
        }

        [JsonConstructor]
        public JsonBlockData() { }
    }

    [JsonObject]
    public struct JsonTurnedBlock
    {
        public float posX;
        public float posY;
        public float posZ;

        public TurnBlockData[] turnsBlockData;

        //[JsonConstructor]
        //private JsonTurnedBlock(Vector3 localPos, TurnBlockData turnBlockData)
        //{
        //    pos = localPos;
        //    angle = turnBlockData.angle;
        //    axis = turnBlockData.axis;
        //}

        public JsonTurnedBlock(Vector3 localPos, TurnBlockData[] turns)
        {
            posX = localPos.x;
            posY = localPos.y;
            posZ = localPos.z;
            turnsBlockData = turns;

            pos = default;
        }

        [JsonIgnore]
        Vector3 pos;
        [JsonIgnore]
        public Vector3 Pos
        {
            get
            {
                if (pos == default)
                {
                    pos.x = posX;
                    pos.y = posY;
                    pos.z = posZ;
                }

                return pos;
            }
        }
    }
}
