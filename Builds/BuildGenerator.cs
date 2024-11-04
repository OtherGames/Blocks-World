using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BuildGenerator
{
    static List<ChunckComponent> chuncksBeUpdate = new List<ChunckComponent>();

    public static BuildedData Build(TextAsset textAsset, Vector3 position, bool createTrigger)
    {
        var result = new BuildedData();
       
        var data = JsonUtility.FromJson<BuildBlocks>(textAsset.text);
        var blocks = data.blocks;

        int minX = Mathf.RoundToInt(blocks.Min(b => b.pos.x));
        int maxX = Mathf.RoundToInt(blocks.Max(b => b.pos.x));
        int xSize = maxX - minX + 1;

        int minY = Mathf.RoundToInt(blocks.Min(b => b.pos.y));
        int maxY = Mathf.RoundToInt(blocks.Max(b => b.pos.y));
        int ySize = maxY - minY + 1;

        int minZ = Mathf.RoundToInt(blocks.Min(b => b.pos.z));
        int maxZ = Mathf.RoundToInt(blocks.Max(b => b.pos.z));
        int ZSize = maxZ - minZ + 1;

        byte[,,] topology = new byte[xSize, ySize, ZSize];

        foreach (var b in blocks)
        {
            int iX = Mathf.RoundToInt(b.pos.x) - minX;
            int iY = Mathf.RoundToInt(b.pos.y) - minY;
            int iZ = Mathf.RoundToInt(b.pos.z) - minZ;

            topology[iX, iY, iZ] = b.ID;
        }

        List<Vector3> globalBlockPoses = new List<Vector3>();

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < ZSize; z++)
                {
                    var globalBlockPos = position;
                    globalBlockPos.x += x;
                    globalBlockPos.y += y;
                    globalBlockPos.z += z;

                    var chunck = WorldGenerator.Inst.GetChunk(globalBlockPos);
                    var pos = chunck.renderer.transform.position;
                    int xBlock = (int)(globalBlockPos.x - pos.x);// + 1;
                    int yBlock = (int)(globalBlockPos.y - pos.y);
                    int zBlock = (int)(globalBlockPos.z - pos.z);
                    //print($"{xBlock} {yBlock} {zBlock}");
                    var blockID = topology[x, y, z];
                    chunck.blocks[xBlock, yBlock, zBlock] = blockID;

                    if(blockID > 0)
                    {
                        globalBlockPoses.Add(globalBlockPos);
                    }

                    if (!chuncksBeUpdate.Contains(chunck))
                        chuncksBeUpdate.Add(chunck);

                }
            }
        }

        foreach (var chunck in chuncksBeUpdate)
        {
            var otherMesh = WorldGenerator.Inst.UpdateMesh(chunck);
            chunck.meshFilter.mesh = otherMesh;
            //chunck.collider.sharedMesh = otherMesh;
        }
        chuncksBeUpdate.Clear();

        result.globalBlockPoses = globalBlockPoses;
        //var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //go.transform.position = position;

        if (createTrigger)
        {
            var colliderPos = new Vector3
            {
                x = Mathf.FloorToInt(position.x),
                y = Mathf.FloorToInt(position.y),
                z = Mathf.FloorToInt(position.z)
            };

            var holder = new GameObject($"Collider {textAsset.name}");
            result.triiger = holder;
            holder.transform.position = colliderPos;
            holder.layer = 10;
            var collider = holder.AddComponent<BoxCollider>();
            holder.AddComponent<TriggerSystem>();
            collider.isTrigger = true;
            var colliderSize = collider.size;
            colliderSize.x = xSize;
            colliderSize.y = ySize;
            colliderSize.z = ZSize;
            collider.size = colliderSize + (Vector3.one * 2);
            var colliderCenter = collider.center;
            colliderCenter.x = (float)xSize / 2 - 1;
            colliderCenter.y = (float)ySize / 2;
            colliderCenter.z = (float)ZSize / 2;
            collider.center = colliderCenter;
        }


        return result;
    }

    public class BuildedData
    {
        public List<Vector3> globalBlockPoses;
        public GameObject triiger;
    }

    [System.Serializable]
    public class BuildBlocks
    {
        public List<BlockData> blocks;
    }
}
