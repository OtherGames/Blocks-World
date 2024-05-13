using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class FindBlockSystem : MonoBehaviour
{
    public int speedCalculation = 10;
    public static FindBlockSystem Instance;

    NearBlockQueueData currentData;
    public List<NearBlockQueueData> queueData = new List<NearBlockQueueData>();

    [Serializable]
    public class NearBlockQueueData
    {
        public int dist = 1;
        public Vector3 checkingPos;
        public Vector3 originBlockingPos;
        //List<GameObject> ebosos = new List<GameObject>();
        public List<Vector3> foundBlocks = new List<Vector3>();
        public List<Vector3> exclude;
        public Action<BlockData> resultCallback;
        public Action findCallback;

        public NearBlockQueueData(Vector3 origin, List<Vector3> exclude, Action findCallback, Action<BlockData> resultCallback)
        {
            originBlockingPos = origin;
            this.exclude = exclude;
            this.resultCallback = resultCallback;
            this.findCallback = findCallback;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    //public BlockData GetNearBlockByUpPlane(Vector3 origin)
    //{
        
    //    //foreach (var item in ebosos)
    //    //{
    //    //    Destroy(item);
    //    //}

    //    while (foundBlocks.Count == 0)
    //    {

    //        for (int x = -dist; x <= dist; x++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(x, y, -dist, origin);
    //            }
    //        }

    //        for (int x = -dist; x <= dist; x++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(x, y, dist, origin);
    //            }
    //        }

    //        for (int z = -dist; z <= dist; z++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(dist, y, z, origin);
    //            }
    //        }

    //        for (int z = -dist; z <= dist; z++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(-dist, y, z, origin);
    //            }
    //        }

    //        dist++;
    //    }

        
    //    float minDist = float.MaxValue;
    //    Vector3 minDistPos = default;
    //    foreach (var pos in foundBlocks)
    //    {
    //        var distance = Vector3.Distance(pos, origin);
    //        if(minDist > distance)
    //        {
    //            minDist = distance;
    //            minDistPos = pos;
    //        }
    //    }

    //    dist = 0;
    //    foundBlocks.Clear();

    //    return new BlockData()
    //    {
    //        ID = WorldGenerator.Inst.GetBlockID(minDistPos),
    //        pos = minDistPos,
    //    };
    //}

    //public BlockData GetNearBlockByUpPlane(Vector3 origin, List<Vector3> exclude)
    //{
    //    //foreach (var item in ebosos)
    //    //{
    //    //    Destroy(item);
    //    //}

    //    originBlockingPos = origin.ToGlobalBlockPos();

    //    //List<ChunckComponent> chuncks = new List<ChunckComponent>();
    //    //foreach (var pos in exclude)
    //    //{
    //    //    chuncks.Add(WorldGenerator.Inst.SetBlock(pos, 11));
    //    //    print($"{pos} ===== {checkingPos} =-=-=- {pos == checkingPos}");
    //    //}
    //    //foreach (var item in chuncks)
    //    //{
    //    //    WorldGenerator.Inst.UpdateChunckMesh(item);
    //    //}
    //    dist = 1;

    //    while (foundBlocks.Count == 0)
    //    {
    //        for (int x = -dist; x <= dist; x++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(x, y, -dist, originBlockingPos, exclude);
    //            }
    //        }

    //        for (int x = -dist; x <= dist; x++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(x, y, dist, originBlockingPos, exclude);
    //            }
    //        }

    //        for (int z = -dist; z <= dist; z++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(dist, y, z, originBlockingPos, exclude);
    //            }
    //        }

    //        for (int z = -dist; z <= dist; z++)
    //        {
    //            for (int y = 0; y <= dist; y++)
    //            {
    //                CheckPos(-dist, y, z, originBlockingPos, exclude);
    //            }
    //        }

    //        dist++;
    //    }


    //    float minDist = float.MaxValue;
    //    Vector3 minDistPos = default;
    //    //print(origin);
    //    foreach (var pos in foundBlocks)
    //    {
    //        var distance = Vector3.Distance(pos - (Vector3.right * 0.5f) + (Vector3.forward * 0.5f), origin);
    //        if (minDist > distance)
    //        {
    //            minDist = distance;
    //            minDistPos = pos;
    //        }
    //    }

    //    dist = 0;
    //    foundBlocks.Clear();

    //    return new BlockData()
    //    {
    //        ID = WorldGenerator.Inst.GetBlockID(minDistPos),
    //        pos = minDistPos,
    //    };
    //}

    public void GetNearUpperBlock(Vector3 origin, List<Vector3> exclude, Action<BlockData> resultCallback)
    {
        NearBlockQueueData findBlockData = new NearBlockQueueData
        (
            origin.ToGlobalBlockPos(),
            exclude,
            NearUpperBlock,
            resultCallback
        );

        queueData.Add(findBlockData);

        if (currentData == null)
        {
            currentData = findBlockData;
            StartCoroutine(StartAsyncFindNearBlock());
        }

        void NearUpperBlock()
        {
            float maxHeight = 0;
            Vector3 upperBlockPos = default;

            foreach (var pos in currentData.foundBlocks)
            {
                var height = pos.y;
                if (height > maxHeight)
                {
                    maxHeight = height;
                    upperBlockPos = pos;
                }
            }

            currentData.dist = 0;
            currentData.foundBlocks.Clear();

            resultCallback(new BlockData()
            {
                ID = WorldGenerator.Inst.GetBlockID(upperBlockPos),
                pos = upperBlockPos,
            });
        }
    }

    IEnumerator StartAsyncFindNearBlock()
    {
        int iteration = 0;

        currentData.dist = 1;
        var dist = currentData.dist;

        while (currentData.foundBlocks.Count == 0)
        {
            for (int x = -dist; x <= dist; x++)
            {
                for (int y = 0; y <= dist; y++)
                {
                    CheckPos(x, y, -dist, currentData.originBlockingPos, currentData.exclude);
                }
            }

            for (int x = -dist; x <= dist; x++)
            {
                for (int y = 0; y <= dist; y++)
                {
                    CheckPos(x, y, dist, currentData.originBlockingPos, currentData.exclude);
                }
            }

            for (int z = -dist; z <= dist; z++)
            {
                for (int y = 0; y <= dist; y++)
                {
                    CheckPos(dist, y, z, currentData.originBlockingPos, currentData.exclude);
                }
            }

            for (int z = -dist; z <= dist; z++)
            {
                for (int y = 0; y <= dist; y++)
                {
                    CheckPos(-dist, y, z, currentData.originBlockingPos, currentData.exclude);
                }
            }

            for (int x = -dist; x <= dist; x++)
            {
                for (int z = -dist; z <= dist; z++)
                {
                    CheckPos(x, dist, z, currentData.originBlockingPos, currentData.exclude);
                }
            }

            dist++;
            iteration++;

            if (iteration > speedCalculation / Time.deltaTime)
            {
                iteration = 0;
                yield return null;
            }
        }

        currentData.findCallback();

        queueData.Remove(currentData);

        if (queueData.Count > 0)
        {
            currentData = queueData[0];
            StartCoroutine(StartAsyncFindNearBlock());
        }
        else
        {
            currentData = null;
        }
    }

    void CheckPos(int x, int y, int z, Vector3 origin)
    {
        currentData.checkingPos.x = x;
        currentData.checkingPos.y = y;
        currentData.checkingPos.z = z;
        currentData.checkingPos += origin + Vector3.up + (Vector3.right * 2);

        var blockID = WorldGenerator.Inst.GetBlockID(currentData.checkingPos);
        if (blockID > 0)
        {
            currentData.foundBlocks.Add(currentData.checkingPos);
        }

        //var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //go.transform.position = checkingPos;
        //ebosos.Add(go);
    }

    void CheckPos(int x, int y, int z, Vector3 origin, List<Vector3> exclude)
    {
        currentData.checkingPos.x = x;
        currentData.checkingPos.y = y;
        currentData.checkingPos.z = z;
        currentData.checkingPos += origin + Vector3.up + Vector3.right;

        //List<ChunckComponent> chuncks = new List<ChunckComponent>();
        //foreach (var pos in exclude)
        //{
        //    chuncks.Add(WorldGenerator.Inst.SetBlock(pos, 11));
        //    print($"{pos} ===== {checkingPos} =-=-=- {pos == checkingPos}");
        //}
        //foreach (var item in chuncks)
        //{
        //    WorldGenerator.Inst.UpdateChunckMesh(item);
        //}

        var blockID = WorldGenerator.Inst.GetBlockID(currentData.checkingPos);
        if (blockID > 0)
        {
            foreach (var pos in currentData.exclude)
            {
                //print($"{pos} ### {currentData.checkingPos}");
                if (pos.Equals(currentData.checkingPos))
                {
                    //print($"{pos} ### {currentData.checkingPos} есть исключение");
                    return;
                }
            }

            currentData.foundBlocks.Add(currentData.checkingPos);
        }


        //WorldGenerator.Inst.SetBlockAndUpdateChunck(checkingPos, 11);


        //var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //go.transform.position = checkingPos - (Vector3.right * 0.5f) + (Vector3.forward * 0.5f);
        //go.transform.localScale = Vector3.one * 0.3f;
        //ebosos.Add(go);
    }
}
