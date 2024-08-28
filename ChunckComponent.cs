using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;
using UnityEngine.Events;

public class ChunckComponent
{
    public MeshRenderer renderer;
    public MeshFilter meshFilter;
    public MeshCollider collider;
    public Vector3 pos;

    public byte[,,] blocks;

    public NavMeshModifier navMeshModifier;
    public NavMeshSurface meshSurface;

    public Dictionary<Vector3, NavMeshLink> links = new Dictionary<Vector3, NavMeshLink>();
    public List<Vector3Int> grassBlocks = new List<Vector3Int>();

    public bool blocksLoaded;

    public static UnityEvent<ChunckComponent> onChunckInit = new UnityEvent<ChunckComponent>();
    /// <summary>
    /// Chunck With Blocks
    /// </summary>
    public static UnityEvent<ChunckComponent> onBlocksSeted = new UnityEvent<ChunckComponent>();

    public ChunckComponent(int posX, int posY, int posZ)
    {
        var size = WorldGenerator.size;
        blocks = new byte[size, size, size];
        pos = new Vector3(posX, posY, posZ);

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
        //blocks[xBlock, yBlock, zBlock] = 0;

        //var mesh = WorldGenerator.Inst.UpdateMesh(this);//, (int)pos.x, (int)pos.y, (int)pos.z);
        //meshFilter.mesh = mesh;
        //collider.sharedMesh = mesh;

        return blockID;
    }
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

    [JsonConstructor]
    private ChunckData() { }

    public ChunckData(ChunckComponent chunck)
    {
        changedBlocks = new List<JsonBlockData>();
        usersChangedBlocks = new List<UserChunckData>();
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
}
