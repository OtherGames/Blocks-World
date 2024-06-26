using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;
using static BLOCKS;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] LayerMask navMeshLayer;
    [SerializeField] int viewChunck = 5;
    [SerializeField] float navMeshVoxelSize = 0.18f;
    public ProceduralGeneration procedural;
    public Material mat;

    public Dictionary<Vector3Int, ChunckComponent> chuncks = new();

    public const int size = 16;
    public const int noiseScale = 100;
    public const float TextureOffset = 1f / 16f;
    public const float landThresold = 0.11f;
    public const float smallRockThresold = 0.8f;

    Dictionary<BlockSide, List<Vector3>> blockVerticesSet;
    Dictionary<BlockSide, List<int>> blockTrianglesSet;
    List<ChunckComponent> deferredCreateLinksChuncks = new List<ChunckComponent>();

    readonly List<Vector3> vertices = new();
    readonly List<int> triangulos = new();
    readonly List<Vector2> uvs = new();

    [SerializeField]
    List<Transform> players = new();

    public static WorldGenerator Inst { get; set; }
    public static UnityEvent onReady = new UnityEvent();
    public static UnityEvent<BlockData> onBlockPick = new UnityEvent<BlockData>();
    public static UnityEvent<BlockData> onBlockPlace = new UnityEvent<BlockData>();

    private void Awake()
    {
        Inst = this;
    }

    private void Start()
    {
        blockVerticesSet = new Dictionary<BlockSide, List<Vector3>>();
        blockTrianglesSet = new Dictionary<BlockSide, List<int>>();

        DictionaryInits();
        InitTriangulos();

        onReady?.Invoke();
    }

    public void AddPlayer(Transform player)
    {
        players.Add(player);
    }

    private void Update()
    {
        
        DynamicCreateChunck();
    }

    void DynamicCreateChunck()
    {
        var viewDistance = viewChunck * size;

        foreach (var player in players)
        {
            if (!player)
            {
                continue;// TO DO
            }

            var pos = player.transform.position.ToGlobalRoundBlockPos();

            var primary = GetChunk(pos + (Vector3.down * (size + 3)), out var key);
            if (primary == null)
            {
                key *= size;
                CreateChunck(key.x, key.y, key.z);
                //continue;
            }

            primary = GetChunk(pos + (Vector3.down * (size / 2)), out key);
            if (primary == null)
            {
                key *= size;
                CreateChunck(key.x, key.y, key.z);
                //continue;
            }

            primary = GetChunk(pos, out key);
            if (primary == null)
            {
                key *= size;
                CreateChunck(key.x, key.y, key.z);
                //continue;
            }



            //primary = GetChunk(pos + (Vector3.forward * (size/2)), out key);
            //if (primary == null)
            //{
            //    key *= size;
            //    CreateChunck(key.x, key.y, key.z);
            //    continue;
            //}

            for (float x = -viewDistance + pos.x; x < viewDistance + pos.x; x += size)
            {
                for (float y = -viewDistance + pos.y; y < viewDistance + pos.y; y += size)
                {
                    for (float z = -viewDistance + pos.z; z < viewDistance + pos.z; z += size)
                    {
                        var checkingPos = new Vector3(x, y, z);
                        var chunck = GetChunk(checkingPos, out var chunckKey);

                        if (chunck == null)
                        {
                            chunckKey *= size;
                            CreateChunck(chunckKey.x, chunckKey.y, chunckKey.z);
                            return;
                        }
                    }
                }
            }
        }
    }

    public ChunckComponent CreateChunck(int posX, int posY, int posZ)
    {
        vertices?.Clear();
        triangulos?.Clear();
        uvs?.Clear();

        var chunck = new ChunckComponent(posX, posY, posZ);

        if (!chunck.blocksLoaded)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        chunck.blocks[x, y, z] = procedural.GetBlockID(x + posX, y + posY, z + posZ);//GeneratedBlockID(x + posX, y + posY, z + posZ);
                        if (chunck.blocks[x, y, z] == DIRT && procedural.GetBlockID(x + posX, y + posY + 1, z + posZ) == 0)
                        {
                            chunck.blocks[x, y, z] = 1;
                        }
                    }
                }
            }
        }

        ChunckComponent.onBlocksSeted?.Invoke(chunck);

        var mesh = GenerateMesh(chunck, posX, posY, posZ);

        var chunckGO = new GameObject($"Chunck {posX} {posY} {posZ}");
        var renderer = chunckGO.AddComponent<MeshRenderer>();
        var meshFilter = chunckGO.AddComponent<MeshFilter>();
        var collider = chunckGO.AddComponent<MeshCollider>();
        renderer.material = mat;
        meshFilter.mesh = mesh;
        collider.sharedMesh = mesh;
        chunckGO.transform.position = new Vector3(posX, posY, posZ);
        chunckGO.transform.SetParent(transform, false);

        chunck.renderer = renderer;
        chunck.meshFilter = meshFilter;
        chunck.collider = collider;

        //chunck.navMeshModifier = chunckGO.AddComponent<NavMeshModifier>();
        chunck.meshSurface = chunckGO.AddComponent<NavMeshSurface>();
        chunck.meshSurface.layerMask = navMeshLayer;
        chunck.meshSurface.collectObjects = CollectObjects.Children;
        chunck.meshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        chunck.meshSurface.overrideVoxelSize = true;
        chunck.meshSurface.voxelSize = navMeshVoxelSize;
        chunck.meshSurface.overrideTileSize = true;
        chunck.meshSurface.tileSize = 128;//64;
        chunck.meshSurface.minRegionArea = 0.3f;
        //chunck.meshSurface
       
        StartCoroutine(DelayableBuildNavMesh(chunck));
            //chunck.meshSurface.BuildNavMesh();
        
        
        //UpdateNavMesh(chunck.meshSurface.navMeshData);
        
        //count++;
        //print(count);
        chunckGO.layer = 7;

        chuncks.Add(new(posX/size, posY/size, posZ/size), chunck);

        return chunck;
    }

    IEnumerator DelayableBuildNavMesh(ChunckComponent chunck)
    {
        var navMeshSurface = chunck.meshSurface;

        if (vertices.Count > 0)
        {
            yield return new WaitForSeconds(0.1f);

            navMeshSurface.BuildNavMesh();

            CreateLinks(chunck);
        }
    }

    IEnumerator DelayableUpdateNavMesh(ChunckComponent chunck)
    {
        var navMeshSurface = chunck.meshSurface;

        if (vertices.Count > 0)
        {
            yield return new WaitForSeconds(0.1f);

            if (navMeshSurface.navMeshData)
            {
                yield return navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
            }
            else
            {
                navMeshSurface.BuildNavMesh();
            }

            CreateLinks(chunck);
        }
    }

    Vector3 checkPosUp1, checkPosUp2, blockGlobalPos, linkPos;
    void CreateLinks(ChunckComponent chunck)
    {
        Vector3 camPos;
        
        camPos = Camera.main.transform.position;
        
        
        if (Vector3.Distance(camPos, chunck.pos) > size * 10)
        {
            deferredCreateLinksChuncks.Add(chunck);
            return;
        }

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                blockGlobalPos = chunck.pos;
                blockGlobalPos.x += x;
                blockGlobalPos.y += size - 1;
                blockGlobalPos.z += z;

                checkPosUp1 = blockGlobalPos;
                checkPosUp1.y += 1;

                if (chunck.blocks[x, size - 1, z] != 0 && GetBlockID(checkPosUp1) == 0)
                {
                    linkPos = blockGlobalPos + Vector3.up;

                    if (IsNotEmptyBackSide(blockGlobalPos) && !IsNotEmptyBackSide(blockGlobalPos, 2))
                    {
                        linkPos.x -= 0.5f;
                        linkPos.z -= 0.5f;

                        if (!chunck.links.ContainsKey(linkPos))
                        {
                            startPoint.z = -0.3f;
                            startPoint.y = 1;
                            endPoint.z = 1.3f;
                            endPoint.y = 0;

                            var link = CreateNavMeshLink(
                                chunck,
                                linkPos,
                                Quaternion.identity,
                                startPoint,
                                endPoint
                            );
                            link.name = link.name.Insert(0, "H - ");
                        }
                    }

                    //if (GetBlockID(blockGlobalPos + Vector3.up + Vector3.forward) != 0)
                    //{
                    //    linkPos.x -= 0.5f;
                    //    linkPos.z += 1;

                    //    if (!chunck.links.ContainsKey(linkPos))
                    //    {
                    //        startPoint.z = -0.3f;
                    //        startPoint.y = 0;
                    //        endPoint.z = 1.3f;
                    //        endPoint.y = 1;

                    //        CreateNavMeshLink(
                    //            chunck,
                    //            linkPos,
                    //            Quaternion.identity,
                    //            startPoint,
                    //            endPoint
                    //        );
                    //    }
                    //}
                }
            }
        }


        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                blockGlobalPos = chunck.pos;
                blockGlobalPos.x += x;
                blockGlobalPos.y += y;

                checkPosUp1 = blockGlobalPos;
                checkPosUp2 = blockGlobalPos;
                checkPosUp1.y += 1;
                checkPosUp2.y += 2;

                linkPos = blockGlobalPos + Vector3.up;
                linkPos.x -= 0.5f;

                if (chunck.blocks[x, y, 0] != 0 && GetBlockID(checkPosUp1) == 0 && GetBlockID(checkPosUp2) == 0 && !IsNotEmptyBackSide(blockGlobalPos, 2))
                {
                    var createLinkAvailable = false;
                    if (IsNotEmptyBackSide(blockGlobalPos) && !IsNotEmptyBackSide(blockGlobalPos, 3))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = 1;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                    }
                    else if (IsEmptyBackSide(blockGlobalPos) && !IsNotEmptyBackSide(blockGlobalPos) && IsNotEmptyBackSide(blockGlobalPos, -1))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = -1;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                    }
                    else if (!IsEmptyBackSide(blockGlobalPos) && !IsNotEmptyBackSide(blockGlobalPos))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = 0;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                    }

                    if (createLinkAvailable)
                    {
                        if (!chunck.links.ContainsKey(linkPos))
                        {
                            CreateNavMeshLink(chunck, linkPos, Quaternion.identity, startPoint, endPoint);
                        }
                        else
                        {
                            ChangeNavMeshLink(chunck.links[linkPos]);
                        }
                    }
                    else
                    {
                        if (chunck.links.ContainsKey(linkPos))
                        {
                            Destroy(chunck.links[linkPos].gameObject);
                            chunck.links.Remove(linkPos);
                        }
                    }
                }
                else
                {
                    if (chunck.links.ContainsKey(linkPos))
                    {
                        Destroy(chunck.links[linkPos].gameObject);
                        chunck.links.Remove(linkPos);
                    }
                }
            }
        }

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                blockGlobalPos = chunck.pos;
                blockGlobalPos.z += z;
                blockGlobalPos.y += y;

                checkPosUp1 = blockGlobalPos;
                checkPosUp2 = blockGlobalPos;
                checkPosUp1.y += 1;
                checkPosUp2.y += 2;

                linkPos = blockGlobalPos + Vector3.up + Vector3.left;
                linkPos.z += 0.5f;

                if (chunck.blocks[0, y, z] != 0 && GetBlockID(checkPosUp1) == 0 && GetBlockID(checkPosUp2) == 0 && !IsNotEmptyLeftSide(blockGlobalPos, 2))
                {
                    var createLinkAvailable = false;

                    if (IsNotEmptyLeftSide(blockGlobalPos) && !IsNotEmptyLeftSide(blockGlobalPos, 3))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = 1;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                    }
                    else if (IsEmptyLeftSide(blockGlobalPos) && !IsNotEmptyLeftSide(blockGlobalPos) && IsNotEmptyLeftSide(blockGlobalPos, -1))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = -1;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                        //CreateNavMeshLink(chunck, linkPos, Quaternion.Euler(0, 90, 0), startPoint, endPoint);

                    }
                    else if(!IsEmptyLeftSide(blockGlobalPos) && !IsNotEmptyLeftSide(blockGlobalPos))
                    {
                        startPoint.z = -0.53f;
                        startPoint.y = 0;
                        endPoint.z = 0.53f;
                        endPoint.y = 0;
                        createLinkAvailable = true;
                        //CreateNavMeshLink(chunck, linkPos, Quaternion.Euler(0, 90, 0));
                    }

                    if (createLinkAvailable)
                    {
                        if (!chunck.links.ContainsKey(linkPos))
                        {
                            CreateNavMeshLink(chunck, linkPos, Quaternion.Euler(0, 90, 0), startPoint, endPoint);
                        }
                        else
                        {
                            ChangeNavMeshLink(chunck.links[linkPos]);
                        }
                    }
                    else
                    {
                        if (chunck.links.ContainsKey(linkPos))
                        {
                            Destroy(chunck.links[linkPos].gameObject);
                            chunck.links.Remove(linkPos);
                        }
                    }
                }
                else
                {
                    if (chunck.links.ContainsKey(linkPos))
                    {
                        Destroy(chunck.links[linkPos].gameObject);
                        chunck.links.Remove(linkPos);
                    }
                }
            }
        }
    }

    bool IsEmptyBackSide(Vector3 globalBlockPos, int height = 0)
    {
        return GetBlockID(globalBlockPos + Vector3.back + (Vector3.up * height)) == 0;
    }

    bool IsNotEmptyBackSide(Vector3 globalBlockPos, int height = 1)
    {
        return GetBlockID(globalBlockPos + Vector3.back + (Vector3.up * height)) != 0;
    }

    bool IsEmptyLeftSide(Vector3 globalBlockPos)
    {
        return GetBlockID(globalBlockPos + Vector3.left) == 0;
    }

    bool IsNotEmptyLeftSide(Vector3 globalBlockPos, int height = 1)
    {
        return GetBlockID(globalBlockPos + Vector3.left + (Vector3.up * height)) != 0;
    }

    void ChangeNavMeshLink(NavMeshLink navMeshLink)
    {
        navMeshLink.startPoint = startPoint;
        navMeshLink.endPoint = endPoint;
    }

    Vector3 startPoint, endPoint;
    void CreateNavMeshLink(ChunckComponent chunck, Vector3 pos, Quaternion rotation)
    {
        startPoint.z = -0.53f;
        startPoint.y = 0;
        endPoint.z = 0.53f;
        endPoint.y = 0;
        CreateNavMeshLink(chunck, pos, rotation, startPoint, endPoint);
    }

    GameObject CreateNavMeshLink(ChunckComponent chunck, Vector3 pos, Quaternion rotation, Vector3 startPoint, Vector3 endPoint)
    {
        var link = new GameObject($"Link {pos.x} {pos.y} {pos.z}");
        link.transform.SetPositionAndRotation(pos, rotation);
        var navMeshLink = link.AddComponent<NavMeshLink>();
        navMeshLink.width = 0.9f;
        navMeshLink.autoUpdate = true;
        //var startPoint = navMeshLink.startPoint;
        //var endPoint = navMeshLink.endPoint;
        //startPoint.z = -0.53f;
        //endPoint.z = 0.53f;
        //startPoint.y = startY;
        //endPoint.y = endY;
        navMeshLink.startPoint = startPoint;
        navMeshLink.endPoint = endPoint;

        link.transform.SetParent(chunck.renderer.transform);

        chunck.links.Add(pos, navMeshLink);

        return link;
    }


    public void UpdateChunckMesh(ChunckComponent chunck)
    {
        var otherMesh = UpdateMesh(chunck);
        chunck.meshFilter.mesh = otherMesh;
        chunck.collider.sharedMesh = otherMesh;
    }

    public ChunckComponent GetChunk(Vector3 globalPosBlock, out Vector3Int chunckKey)
    {
        int xIdx = Mathf.FloorToInt(globalPosBlock.x / size);
        int zIdx = Mathf.FloorToInt(globalPosBlock.z / size);
        int yIdx = Mathf.FloorToInt(globalPosBlock.y / size);

        chunckKey = new Vector3Int(xIdx, yIdx, zIdx);

        if (chuncks.ContainsKey(chunckKey))
        {
            return chuncks[chunckKey];
        }

        return null;
    }

    public ChunckComponent GetChunk(Vector3 globalPosBlock)
    {
        int xIdx = Mathf.FloorToInt(globalPosBlock.x / size);
        int zIdx = Mathf.FloorToInt(globalPosBlock.z / size);
        int yIdx = Mathf.FloorToInt(globalPosBlock.y / size);

        var key = new Vector3Int(xIdx, yIdx, zIdx);
        if (chuncks.ContainsKey(key))
        {
            return chuncks[key];
        }

        key *= size;
        return CreateChunck(key.x, key.y, key.z);
    }

    public byte GetBlockID(Vector3 globalPos)
    {
        var chunck = GetChunk(globalPos);
        return chunck.GetBlockID(globalPos);
    }

    public void SetBlock(Vector3 globalPos, ChunckComponent chunck, byte blockID)
    {
        var pos = chunck.renderer.transform.position;
        int xBlock = (int)(globalPos.x - pos.x);
        int yBlock = (int)(globalPos.y - pos.y);
        int zBlock = (int)(globalPos.z - pos.z);
        //print($"{xBlock} {yBlock} {zBlock}");
        chunck.blocks[xBlock, yBlock, zBlock] = blockID;
    }

    public ChunckComponent SetBlock(Vector3 globalPos, byte blockID)
    {
        var chunck = GetChunk(globalPos);
        var pos = chunck.renderer.transform.position;
        int xBlock = (int)(globalPos.x - pos.x);
        int yBlock = (int)(globalPos.y - pos.y);
        int zBlock = (int)(globalPos.z - pos.z);
        //print($"{xBlock} {yBlock} {zBlock}");
        chunck.blocks[xBlock, yBlock, zBlock] = blockID;

        return chunck;
    }

    public void SetBlockAndUpdateChunck(Vector3 globalPos, byte blockID)
    {
        var chunck = GetChunk(globalPos);
        var pos = chunck.renderer.transform.position;
        int xBlock = (int)(globalPos.x - pos.x);
        int yBlock = (int)(globalPos.y - pos.y);
        int zBlock = (int)(globalPos.z - pos.z);
        //print($"{xBlock} {yBlock} {zBlock}");
        chunck.blocks[xBlock, yBlock, zBlock] = blockID;

        UpdateChunckMesh(chunck);
    }

    public void MineBlock(Vector3 chunckableGlobalBlockPos)
    {
        var chunck = GetChunk(chunckableGlobalBlockPos);
        var pos = chunck.renderer.transform.position;

        int xBlock = (int)(chunckableGlobalBlockPos.x - pos.x);
        int yBlock = (int)(chunckableGlobalBlockPos.y - pos.y);
        int zBlock = (int)(chunckableGlobalBlockPos.z - pos.z);

        byte blockID = chunck.blocks[xBlock, yBlock, zBlock];
        chunck.blocks[xBlock, yBlock, zBlock] = 0;

        var mesh = UpdateMesh(chunck);//, (int)pos.x, (int)pos.y, (int)pos.z);
        chunck.meshFilter.mesh = mesh;
        chunck.collider.sharedMesh = mesh;

        for (int p = 0; p < 6; p++)
        {
            var blockPos = new Vector3(xBlock, yBlock, zBlock);

            Vector3 checkingBlockPos = blockPos + facesOffsets[p];


            if (!IsBlockChunk((int)checkingBlockPos.x, (int)checkingBlockPos.y, (int)checkingBlockPos.z))
            {
                var otherChunck = GetChunk(checkingBlockPos + pos);

                var otherMesh = UpdateMesh(otherChunck);
                otherChunck.meshFilter.mesh = otherMesh;
                otherChunck.collider.sharedMesh = otherMesh;
            }
        }

        PickBlock(chunckableGlobalBlockPos, blockID);

    }

    public void PickBlock(Vector3 pos, byte ID)
    {
        //print("����� ������, ������� ������");
        var blockData = new BlockData { pos = pos, ID = ID };
        onBlockPick?.Invoke(blockData);
    }

    public void PlaceBlock(Vector3 pos, byte ID)
    {
        var blockData = new BlockData { pos = pos, ID = ID };
        onBlockPlace?.Invoke(blockData);
    }

    Mesh GenerateMesh(ChunckComponent chunck, int posX, int posY, int posZ)
    {
        Mesh mesh = new();
        mesh.Clear();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (chunck.blocks[x, y, z] > 0)
                    {
                        BlockUVS b = BlockUVS.GetBlock(chunck.blocks[x, y, z]);

                        //if (x == 0 && z == 0)
                        //    b = new BlockUVS(2, 15);

                        if ((z + 1 >= size && procedural.GetBlockID(x + posX, y + posY, z + 1 + posZ) == 0) || (!(z + 1 >= size) && chunck.blocks[x, y, z + 1] == 0))
                        {
                            CreateBlockSide(BlockSide.Front, x, y, z, b);
                        }
                        if ((z - 1 < 0 && procedural.GetBlockID(x + posX, y + posY, z - 1 + posZ) == 0) || (!(z - 1 < 0) && chunck.blocks[x, y, z - 1] == 0))
                        {
                            CreateBlockSide(BlockSide.Back, x, y, z, b);
                        }
                        if ((x + 1 >= size && procedural.GetBlockID(x + 1 + posX, y + posY, z + posZ) == 0) || (!(x + 1 >= size) && chunck.blocks[x + 1, y, z] == 0))
                        {
                            CreateBlockSide(BlockSide.Right, x, y, z, b);
                        }
                        if ((x - 1 < 0 && procedural.GetBlockID(x - 1 + posX, y + posY, z + posZ) == 0) || (!(x - 1 < 0) && chunck.blocks[x - 1, y, z] == 0))
                        {
                            CreateBlockSide(BlockSide.Left, x, y, z, b);
                        }
                        if ((y + 1 >= size && procedural.GetBlockID(x + posX, y + posY + 1, z + posZ) == 0) || (!(y + 1 >= size) && chunck.blocks[x, y + 1, z] == 0))
                        {
                            CreateBlockSide(BlockSide.Top, x, y, z, b);
                        }
                        if ((y - 1 < 0 && procedural.GetBlockID(x + posX, y + posY - 1, z + posZ) == 0) || (!(y - 1 < 0) && chunck.blocks[x, y - 1, z] == 0))
                        {
                            CreateBlockSide(BlockSide.Bottom, x, y, z, b);
                        }
                        //if (!(y + 1 >= size) && chunck.blocks[x, y + 1, z] == 0 || y + 1 >= size)
                        //{
                        //    CreateBlockSide(BlockSide.Top, x, y, z, b);
                        //}
                        //if (!(y - 1 < 0) && chunck.blocks[x, y - 1, z] == 0)
                        //{
                        //    CreateBlockSide(BlockSide.Bottom, x, y, z, b);
                        //}
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.OptimizeReorderVertexBuffer();
        mesh.Optimize();

        return mesh;
    }

    public Mesh UpdateMesh(ChunckComponent chunck)
    {
        var frontChunck  = GetChunk(chunck.pos + (Vector3.forward * size));
        var backChunck   = GetChunk(chunck.pos + (Vector3.back * size));
        var rightChunck  = GetChunk(chunck.pos + (Vector3.right * size));
        var leftChunck   = GetChunk(chunck.pos + (Vector3.left * size));
        var topChunck    = GetChunk(chunck.pos + (Vector3.up * size));
        var bottomChunck = GetChunk(chunck.pos + (Vector3.down * size));

        vertices.Clear();
        triangulos.Clear();
        uvs.Clear();

        Mesh mesh = chunck.meshFilter.mesh;//new();
        mesh.Clear();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    if (chunck.blocks[x, y, z] > 0)
                    {
                        BlockUVS b = BlockUVS.GetBlock(chunck.blocks[x, y, z]); //new(0, 15, 3, 15, 2, 15);

                        var frontCheck = (z + 1 >= size && frontChunck.blocks[x, y, 0] == 0);
                        var backCheck = (z - 1 < 0 && backChunck.blocks[x, y, size - 1] == 0);
                        var rightCheck = (x + 1 >= size && rightChunck.blocks[0, y, z] == 0);
                        var leftCheck = (x - 1 < 0 && leftChunck.blocks[size - 1, y, z] == 0);
                        var topCheck = (y + 1 >= size && topChunck.blocks[x, 0, z] == 0);
                        var bottomCheck = (y - 1 < 0 && bottomChunck.blocks[x, size - 1, z] == 0);

                        if ((!(z + 1 >= size) && chunck.blocks[x, y, z + 1] == 0) || frontCheck)
                        {
                            CreateBlockSide(BlockSide.Front, x, y, z, b);
                        }
                        if ((!(z - 1 < 0) && chunck.blocks[x, y, z - 1] == 0) || backCheck)
                        {
                            CreateBlockSide(BlockSide.Back, x, y, z, b);
                        }
                        if ((!(x + 1 >= size) && chunck.blocks[x + 1, y, z] == 0) || rightCheck)
                        {
                            CreateBlockSide(BlockSide.Right, x, y, z, b);
                        }
                        if ((!(x - 1 < 0) && chunck.blocks[x - 1, y, z] == 0) || leftCheck)
                        {
                            CreateBlockSide(BlockSide.Left, x, y, z, b);
                        }
                        if ((!(y + 1 >= size) && chunck.blocks[x, y + 1, z] == 0) || topCheck)
                        {
                            CreateBlockSide(BlockSide.Top, x, y, z, b);
                        }
                        if ((!(y - 1 < 0) && chunck.blocks[x, y - 1, z] == 0) || bottomCheck)
                        {
                            CreateBlockSide(BlockSide.Bottom, x, y, z, b);
                        }
                        //if (!(y + 1 >= size) && chunck.blocks[x, y + 1, z] == 0 || y + 1 >= size)
                        //{
                        //    CreateBlockSide(BlockSide.Top, x, y, z, b);
                        //}
                        //if (!(y - 1 < 0) && chunck.blocks[x, y - 1, z] == 0)
                        //{
                        //    CreateBlockSide(BlockSide.Bottom, x, y, z, b);
                        //}
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.OptimizeReorderVertexBuffer();
        mesh.Optimize();

        StartCoroutine(DelayableUpdateNavMesh(chunck));

        return mesh;
    }


    void DictionaryInits()
    {
        List<Vector3> verticesFront = new List<Vector3>
            {
                new Vector3( 0, 0, 1 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBack = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesRight = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3( 0, 0, 1 ),
                new Vector3( 0, 1, 1 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesLeft = new List<Vector3>
            {
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3(-1, 1, 0 ),
            };
        List<Vector3> verticesTop = new List<Vector3>
            {
                new Vector3( 0, 1, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBottom = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3( 0, 0, 1 ),
            };

        blockVerticesSet.Add(BlockSide.Front, null);
        blockVerticesSet.Add(BlockSide.Back, null);
        blockVerticesSet.Add(BlockSide.Right, null);
        blockVerticesSet.Add(BlockSide.Left, null);
        blockVerticesSet.Add(BlockSide.Top, null);
        blockVerticesSet.Add(BlockSide.Bottom, null);

        blockVerticesSet[BlockSide.Front] = verticesFront;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Back] = verticesBack;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Right] = verticesRight;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Left] = verticesLeft;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Top] = verticesTop;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Bottom] = verticesBottom;
    }

    void InitTriangulos()
    {
        List<int> trianglesFront = new List<int> { 3, 2, 1, 4, 3, 1 };
        List<int> trianglesBack = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesRight = new List<int> { 1, 3, 2, 4, 3, 1 };
        List<int> trianglesLeft = new List<int> { 2, 3, 1, 1, 3, 4 };
        List<int> trianglesTop = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesBottom = new List<int> { 3, 2, 1, 4, 3, 1 };

        blockTrianglesSet.Add(BlockSide.Front, trianglesFront);
        blockTrianglesSet.Add(BlockSide.Back, trianglesBack);
        blockTrianglesSet.Add(BlockSide.Right, trianglesRight);
        blockTrianglesSet.Add(BlockSide.Left, trianglesLeft);
        blockTrianglesSet.Add(BlockSide.Top, trianglesTop);
        blockTrianglesSet.Add(BlockSide.Bottom, trianglesBottom);
    }


    void CreateBlockSide(BlockSide side, int x, int y, int z, BlockUVS b)
    {
        List<Vector3> vrtx = blockVerticesSet[side];
        List<int> trngls = blockTrianglesSet[side];
        int offset = 1;

        triangulos.Add(trngls[0] - offset + vertices.Count);
        triangulos.Add(trngls[1] - offset + vertices.Count);
        triangulos.Add(trngls[2] - offset + vertices.Count);

        triangulos.Add(trngls[3] - offset + vertices.Count);
        triangulos.Add(trngls[4] - offset + vertices.Count);
        triangulos.Add(trngls[5] - offset + vertices.Count);

        vertices.Add(new Vector3(x + vrtx[0].x, y + vrtx[0].y, z + vrtx[0].z)); // 1
        vertices.Add(new Vector3(x + vrtx[1].x, y + vrtx[1].y, z + vrtx[1].z)); // 2
        vertices.Add(new Vector3(x + vrtx[2].x, y + vrtx[2].y, z + vrtx[2].z)); // 3
        vertices.Add(new Vector3(x + vrtx[3].x, y + vrtx[3].y, z + vrtx[3].z)); // 4

        AddUVS(side, b);
    }

    void AddUVS(BlockSide side, BlockUVS b)
    {
        switch (side)
        {
            case BlockSide.Front:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Back:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Right:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Left:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Top:
                uvs.Add(new Vector2(TextureOffset * b.TextureX, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, (TextureOffset * b.TextureY) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureX, (TextureOffset * b.TextureY) + TextureOffset));

                break;
            case BlockSide.Bottom:
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, (TextureOffset * b.TextureYBottom) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, (TextureOffset * b.TextureYBottom) + TextureOffset));

                break;

        }

    }

    public byte GeneratedBlockID(int x, int y, int z)
    {
        Random.InitState(888);

        // ============== ��������� ��� =============
        var k = 10000000;// ��� ������ ��� ����

        Vector3 offset = new(Random.value * k, Random.value * k, Random.value * k);

        float noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        float noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale / 2);
        float noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        goraValue += (30 - y) / 3000f;// World bump
        //goraValue /= y / 1f;// ��� ���� ������;

        byte blockID = 0;
        if (goraValue > 0.35f)
        {
            if (goraValue > 0.3517f)
            {
                blockID = 2;
            }
            else
            {
                blockID = 1;
            }
        }
        // ==========================================

        // =========== �������� �������� ============
        k = 10000;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale);
        noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale);
        noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale);

        float noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        noiseValue += (30 - y) / 30f;// World bump
        noiseValue /= y / 8f;

        //cavernas /= y / 19f;
        //cavernas /= 2;
        //Debug.Log($"{noiseValue} --- {y}");

        if (noiseValue > landThresold)
        {
            if (noiseValue > 0.5f)
            {
                blockID = 2;
            }
            else
            {
                blockID = 1;
            }
        }
        // ==========================================

        // =========== �����, ���� ���� =============
        k = 10000;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale * 2));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale * 3));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale * 2));

        float rockValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (rockValue > 0.8f)
        {
            if (rockValue > 0.801f)
                blockID = 2;
            else
                blockID = 1;
        }
        // ==========================================

        // =========== �����, ���� ���� =============
        k = 100;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 2));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 1));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 2));

        float smallRockValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (smallRockValue > smallRockThresold && noiseValue > (landThresold - 0.08f))
        {
            blockID = 2;
            if (smallRockValue < smallRockThresold + 0.01f)
                blockID = 1;
        }
        // ==========================================

        // =========== ������ ========================
        k = 33333;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        float gravelValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (gravelValue > 0.85f && (noiseValue > landThresold))
        {
            blockID = BLOCKS.GRAVEL;
        }
        // ==========================================

        // =========== ����� ========================
        k = 10;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        float coalValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (coalValue > 0.92f && (noiseValue > landThresold))
        {
            blockID = BLOCKS.ORE_COAL;
        }
        // ==========================================

        // =========== �������� ���� ========================
        k = 700;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        float oreValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (oreValue > 0.93f && (noiseValue > landThresold))
        {
            blockID = 30;
        }
        // ==========================================

        // =========== ������� ���� ========================
        k = 635;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        float saltpeterValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (saltpeterValue > 0.935f && (noiseValue > landThresold))
        {
            blockID = BLOCKS.SALTPETER;
        }
        // ==========================================

        // =========== ���� ========================
        k = 364789;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        float sulfurValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (sulfurValue > 0.93f && (noiseValue > landThresold))
        {
            blockID = BLOCKS.ORE_SULFUR;
        }
        // ==========================================


        // ���� ����
        ////////// ��� ��� ////////////////////////////////////////////
        //k = 10000000;// ��� ������ ��� ����

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        //noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale / 2);
        //noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        //float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //goraValue += (30 - y) / 3000f;// World bump
        //goraValue /= y / 80f;// ��� ���� ������;

        //blockID = 0;
        //if (goraValue > 0.08f && goraValue < 0.3f)
        //{
        //    blockID = 2;
        //}
        ///==============================================



        if (oreValue < minValue)
            minValue = oreValue;
        if (oreValue > maxValue)
            maxValue = oreValue;

        /////////////////////////////////////////////////////////////////////
        //k = 10000000;// ��� ������ ��� ����

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        //noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale * 2);
        //noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        //float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //goraValue += (30 - y) / 30000f;// World bump
        //goraValue /= y / 8f;

        ////blockID = 0;
        //if (goraValue > 0.1f && goraValue < 0.3f)
        //{
        //    blockID = 2;
        //}
        ////////////////////////////////////////////////////////////////

        return blockID;

        //Random.InitState(888);
        //int k = 1000000;
        //Vector3 offset = new Vector3(Random.value * k, Random.value * k, Random.value * k);

        //Vector3 pos = new Vector3
        //(
        //    x + offset.x,
        //    y + offset.y,
        //    z + offset.z
        //);
        //float noiseX = Mathf.Abs((float)(pos.x + offset.x) / ebota);
        //float noiseY = Mathf.Abs((float)(pos.y + offset.y) / ebota);
        //float noiseZ = Mathf.Abs((float)(pos.z + offset.z) / ebota);
        //#pragma warning disable CS0436 // ��� ����������� � ��������������� �����
        //            var res = noise.snoise(new float3(noiseX, noiseY, noiseZ));//snoise(pos);
        //#pragma warning restore CS0436 // ��� ����������� � ��������������� �����

        //if (y < 3) res = 0.5f;

        //if (res > 0.3f)
        //{
        //    return true;
        //}


    }

    bool IsBlockChunk(int x, int y, int z)
    {
        if (x < 0 || x > size - 1 || y < 0 || y > size - 1 || z < 0 || z > size - 1)
            return false;
        else
            return true;
    }
    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    public static readonly Vector3[] facesOffsets = new Vector3[6]
    {
        new Vector3( 0.0f, 0.0f,-1.0f),
        new Vector3( 0.0f, 0.0f, 1.0f),
        new Vector3( 0.0f, 1.0f, 0.0f),
        new Vector3( 0.0f,-1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3( 1.0f, 0.0f, 0.0f),
    };
}


public enum BlockSide : byte
{
    Front,
    Back,
    Right,
    Left,
    Top,
    Bottom
}

public enum BlockType : byte
{
    Grass,
    Dirt,
    Stone
}

public static class VectorExt
{
    public static Vector3 ToGlobalBlockPos(this Vector3 pos)
    {
        Vector3 formatedPos;
        formatedPos.x = Mathf.FloorToInt(pos.x);
        formatedPos.y = Mathf.FloorToInt(pos.y);
        formatedPos.z = Mathf.FloorToInt(pos.z);
        return formatedPos;
    }

    public static Vector3 ToGlobalRoundBlockPos(this Vector3 pos)
    {
        pos.x = Mathf.RoundToInt(pos.x);
        pos.y = Mathf.RoundToInt(pos.y);
        pos.z = Mathf.RoundToInt(pos.z);
        return pos;
    }
}