using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using Newtonsoft;
using Newtonsoft.Json;
using UnityEngine.Events;
using System;

public class ChunckComponent
{
    public MeshRenderer renderer;
    public MeshFilter meshFilter;
    public MeshCollider collider;
    public Vector3 pos;
    public byte[,,] blocks;

    public NavMeshModifier navMeshModifier;
    public NavMeshSurface meshSurface;
    public Vector3Int key;
    public ChunckState chunckState;

    public Dictionary<Vector3, NavMeshLink> links = new Dictionary<Vector3, NavMeshLink>();
    public Dictionary<Vector3Int, List<TurnBlockData>> turnedBlocks = new Dictionary<Vector3Int, List<TurnBlockData>>();// Словарь который хранит информацию о повернутых блоках  
    public List<Vector3Int> grassBlocks = new List<Vector3Int>();

    public int size;
    public bool blocksLoaded;
    public bool hasAir;

    // --- Новое поле: локальная карта света для чанка ---
    public byte[,,] lightmap;

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

        key = new Vector3Int(posX / size, posY / size, posZ / size);

        onChunckInit?.Invoke(this);
    }

    public ChunckComponent leftNeighbour;
    public ChunckComponent rightNeighbour;
    public ChunckComponent frontNeighbour;
    public ChunckComponent backNeighbour;
    public ChunckComponent topNeighbour;
    public ChunckComponent downNeighbour;
    int countExistingNeighbours;

    public void SetupNeighbours()
    {
        RecalculateLightmapAndApply();

        var wg = WorldGenerator.Inst;
        if (wg.chuncks.TryGetValue(key + Vector3Int.left, out var leftChunk))
        {
            leftNeighbour = leftChunk;
            IncreaseExistingNeigbours();
            leftChunk.rightNeighbour = this;
            leftChunk.IncreaseExistingNeigbours();
            leftChunk.CheckCountExistingNeigbours();
        }

        if (wg.chuncks.TryGetValue(key + Vector3Int.right, out var rightChunk))
        {
            rightNeighbour = rightChunk;
            IncreaseExistingNeigbours();
            rightChunk.leftNeighbour = this;
            rightChunk.IncreaseExistingNeigbours();
            rightChunk.CheckCountExistingNeigbours();
        }

        if (wg.chuncks.TryGetValue(key + Vector3Int.forward, out var frontChunk))
        {
            frontNeighbour = frontChunk;
            IncreaseExistingNeigbours();
            frontChunk.backNeighbour = this;
            frontChunk.IncreaseExistingNeigbours();
            frontChunk.CheckCountExistingNeigbours();
        }

        if (wg.chuncks.TryGetValue(key + Vector3Int.back, out var backChunk))
        {
            backNeighbour = backChunk;
            IncreaseExistingNeigbours();
            backChunk.frontNeighbour = this;
            backChunk.IncreaseExistingNeigbours();
            backChunk.CheckCountExistingNeigbours();
        }

        if (wg.chuncks.TryGetValue(key + Vector3Int.up, out var topChunk))
        {
            topNeighbour = topChunk;
            IncreaseExistingNeigbours();
            topChunk.downNeighbour = this;
            topChunk.IncreaseExistingNeigbours();
            topChunk.CheckCountExistingNeigbours();
        }

        if (wg.chuncks.TryGetValue(key + Vector3Int.down, out var downChunk))
        {
            downNeighbour = downChunk;
            IncreaseExistingNeigbours();
            downChunk.topNeighbour = this;
            downChunk.IncreaseExistingNeigbours();
            downChunk.CheckCountExistingNeigbours();
        }

        CheckCountExistingNeigbours();
    }

    public void IncreaseExistingNeigbours()
    {
        countExistingNeighbours++;
        meshFilter.gameObject.name = meshFilter.gameObject.name.Insert(0, $"{countExistingNeighbours} NC ");
    }

    public void CheckCountExistingNeigbours()
    {
        if (countExistingNeighbours == 6)
        {
            RecalculateLightmapAndApply();
        }
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
        // Бля, тут пизда, надо будет оптимизировать
        // жуткая аллокация
        var turnsBlockData = new List<TurnBlockData>();
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


    // В ChunckComponent.cs — замени соответствующие методы на эти

    // Параметры, которыми можно поиграть
    const float AO_STRENGTH = 0.58f; // 0 - нет AO, 1 - сильный AO
    const float AMBIENT_INTENSITY = 1.0f; // фоновый свет, 1 = максимум

    public void RecalculateLightmapAndApply()
    {
        try
        {
            // Делаем flat lightmap — чтобы не было чёрных чанков
            lightmap = new byte[size, size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    for (int z = 0; z < size; z++)
                        lightmap[x, y, z] = VoxelLighting.MAX_LIGHT;

            // Применяем vertex colors — но AO будет вычисляться ВНУТРИ ApplyLightToMesh
            ApplyLightToMesh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RecalculateLightmapAndApply failed: {ex}");
        }
    }


    List<GameObject> testoos = new();
    /// <summary>
    /// Заполняет mesh.colors32 с учётом:
    ///  - равного фонового света (ambient)
    ///  - вершинного AO, рассчитываемого на основе наличия соседних блоков (без создания чанков)
    /// Важно: renderer.transform.TransformPoint используется чтобы работать корректно при любом pivot/offset'е меша.
    /// </summary>
    void ApplyLightToMesh()
    {
        foreach (var item in testoos)
        {
            GameObject.Destroy(item);
        }
        testoos.Clear();

        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        Mesh mesh = meshFilter.mesh; // instance
        var verts = mesh.vertices;
        Color32[] colors = new Color32[verts.Length];

        // Важное: chunkOrigin — мировые блок-координаты локальной точки [0,0,0] чанка
        Vector3Int chunkOrigin = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

        // Быстрые вспомогательные массивы для проверки соседей
        Vector3Int[] axisNeighbors = new[]{
            new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
            new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        for (int i = 0; i < verts.Length; i++)
        {
            // Мировая позиция вершины — корректно при любом transform/pivot
            Vector3 worldPos = renderer.transform.TransformPoint(verts[i]);

            // Индекс блока в мировых целых координатах
            int bx = Mathf.FloorToInt(worldPos.x);
            int by = Mathf.FloorToInt(worldPos.y);
            int bz = Mathf.FloorToInt(worldPos.z);

            // ----- 1) усреднённый "локальный" свет (flat ambient) -----
            // Мы используем уже заполнённую flat lightmap внутри чанка, и приближённый safe-get для внешних блоков.
            // Берём 8 образцов вокруг вершины и усредняем — даёт плавное интерполирование интенсивности.
            int sumLight = 0;
            int countLight = 0;
            for (int sx = 0; sx <= 1; sx++)
                for (int sy = 0; sy <= 1; sy++)
                    for (int sz = 0; sz <= 1; sz++)
                    {
                        Vector3Int sample = new Vector3Int(bx - sx, by - sy, bz - sz);
                        Vector3Int local = sample - chunkOrigin;
                        if (local.x >= 0 && local.x < size && local.y >= 0 && local.y < size && local.z >= 0 && local.z < size)
                        {
                            sumLight += lightmap[local.x, local.y, local.z];
                        }
                        else
                        {
                            // Для блоков за пределами чанка — не создаём чанки, даём приближённое значение:
                            sumLight += SafeGetLightApprox(sample);
                        }
                        countLight++;
                    }
            float avgLight = countLight > 0 ? (float)sumLight / countLight : VoxelLighting.MAX_LIGHT;
            float normalizedLight = Mathf.Clamp01(avgLight / (float)VoxelLighting.MAX_LIGHT) * AMBIENT_INTENSITY;

            // ----- 2) AO (независимая от lightmap часть) -----
            // Простая и стабильная схема: считаем количество занятых axis- соседей вокруг точки блока.
            // Чем больше соседей, тем темнее вершина. При этом мы не создаём чанки — проверяем HasChunck.
            //===============================================
            //int solidCount = 0;
            //for (int n = 0; n < axisNeighbors.Length; n++)
            //{
            //    Vector3Int nb = new Vector3Int(bx, by, bz) + axisNeighbors[n];
            //    if (IsGlobalBlockOpaqueSafe(nb)) solidCount++;
            //}

            //float aoFactor = 1f - ((float)solidCount / axisNeighbors.Length) * AO_STRENGTH;

            //==============================================

            //==============================================
            // sample for one vertex (вставь в ApplyLightToMesh вместо axisNeighbors logic)
            /*int solidCount = 0;
            for (int sx = 0; sx <= 1; sx++)
                for (int sy = 0; sy <= 1; sy++)
                    for (int sz = 0; sz <= 1; sz++)
                    {
                        // блоки, которые граничат с вершиной worldPos
                        Vector3Int sample = new Vector3Int(bx - sx, by - sy, bz - sz);
                        if (IsGlobalBlockOpaqueSafe(sample)) solidCount++;
                    }
            // AO factor 0..1: 1 = no occlusion, less = more occlusion
            float aoFactor = 1f - (solidCount / 8f) * AO_STRENGTH;/**/
            //==============================================

            /*//$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
            float neighborsCount = 0;
            bool hasLeft = false;
            bool hasRight = false;
            bool hasTop = false;
            bool hasDown = false;
            bool hasFront = false;
            bool hasBack = false;

            var leftOffset =     new Vector3Int(bx, by,     bz);
            var leftDownOffset = new Vector3Int(bx, by - 1, bz);
            var leftDownBack =   new Vector3Int(bx, by - 1, bz - 1);
            var leftBackOffset = new Vector3Int(bx, by,     bz - 1);
            if (IsGlobalBlockOpaqueSafe(leftOffset) || IsGlobalBlockOpaqueSafe(leftDownBack) || IsGlobalBlockOpaqueSafe(leftDownOffset) || IsGlobalBlockOpaqueSafe(leftBackOffset))
            {
                hasLeft = true;
            }

            var rightOffset =          new Vector3Int(bx + 1, by,     bz);
            var rightDownOffset =      new Vector3Int(bx + 1, by - 1, bz);
            var rightFrontOffset =     new Vector3Int(bx + 1, by,     bz - 1);
            var rightDownFrontOffset = new Vector3Int(bx + 1, by - 1, bz - 1);
            if (IsGlobalBlockOpaqueSafe(rightDownOffset) || IsGlobalBlockOpaqueSafe(rightOffset) || IsGlobalBlockOpaqueSafe(rightFrontOffset) || IsGlobalBlockOpaqueSafe(rightDownFrontOffset))
            {
                hasRight = true;
            }

            var topOffset =          new Vector3Int(bx,     by, bz);
            var topRightBackOffset = new Vector3Int(bx + 1, by, bz - 1);
            var topRightOffset =     new Vector3Int(bx + 1, by, bz);
            var topBackOffset =      new Vector3Int(bx,     by, bz - 1);
            if (IsGlobalBlockOpaqueSafe(topRightOffset) || IsGlobalBlockOpaqueSafe(topOffset) || IsGlobalBlockOpaqueSafe(topRightBackOffset) || IsGlobalBlockOpaqueSafe(topBackOffset))
            {
                hasTop = true;
            }

            var downOffset =      new Vector3Int(bx,     by - 1, bz);
            var downBackOffset =  new Vector3Int(bx,     by - 1, bz - 1);
            var downRightOffset = new Vector3Int(bx + 1, by - 1, bz);
            var downRightBack =   new Vector3Int(bx + 1, by - 1, bz - 1);
            if (IsGlobalBlockOpaqueSafe(downRightOffset) || IsGlobalBlockOpaqueSafe(downOffset) || IsGlobalBlockOpaqueSafe(downBackOffset) || IsGlobalBlockOpaqueSafe(downRightBack))
            {
                hasDown = true;
            }

            var frontOffsset =         new Vector3Int(bx,     by,     bz);
            var frontDownOffset =      new Vector3Int(bx,     by - 1, bz);
            var frontRightDownOffset = new Vector3Int(bx + 1, by - 1, bz);
            var frontRightOffset =     new Vector3Int(bx + 1, by,     bz);
            if (IsGlobalBlockOpaqueSafe(frontDownOffset) || IsGlobalBlockOpaqueSafe(frontRightDownOffset) || IsGlobalBlockOpaqueSafe(frontRightOffset) || IsGlobalBlockOpaqueSafe(frontOffsset))
            {
                hasFront = true;
            }

            var backOffset =     new Vector3Int(bx,     by,     bz - 1);
            var backDownOffset = new Vector3Int(bx,     by - 1, bz - 1);
            var backRightDown =  new Vector3Int(bx + 1, by - 1, bz - 1);
            var backRightTop =   new Vector3Int(bx + 1, by,     bz - 1);
            if (IsGlobalBlockOpaqueSafe(backOffset) || IsGlobalBlockOpaqueSafe(backDownOffset) || IsGlobalBlockOpaqueSafe(backRightDown) || IsGlobalBlockOpaqueSafe(backRightTop))
            {
                hasBack = true;
            }


            if (hasLeft)
                neighborsCount++;
            if (hasRight)
                neighborsCount++;
            if (hasFront)
                neighborsCount++;
            if (hasBack)
                neighborsCount++;
            if (hasTop)
                neighborsCount++;
            if (hasDown)
                neighborsCount++;

            neighborsCount -= 3;

            float aoFactor = 1f - (neighborsCount / 3f) * AO_STRENGTH;

            //$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$/**/

            
            // worldBx, worldBy, worldBz — целочисленная позиция блока, как у тебя bx,by,bz
            // Возьмем 8 образцов: (x in {bx-1, bx}), (y in {by-1, by}), (z in {bz-1, bz})
            bool b000 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx + 1, by - 1, bz - 1));
            bool b100 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx,     by - 1, bz - 1));
            bool b010 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx + 1, by,     bz - 1));
            bool b110 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx,     by,     bz - 1));
            bool b001 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx + 1, by - 1, bz));
            bool b101 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx,     by - 1, bz));
            bool b011 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx + 1, by,     bz));
            bool b111 = IsGlobalBlockOpaqueSafe(new Vector3Int(bx,     by,     bz));

            // стороны (смотри пояснение ниже)
            bool hasLeft  = b000 || b010 || b001 || b011; // x == bx-1
            bool hasRight = b100 || b110 || b101 || b111; // x == bx
            bool hasDown  = b000 || b100 || b001 || b101; // y == by-1
            bool hasTop   = b010 || b110 || b011 || b111; // y == by
            bool hasBack  = b000 || b010 || b100 || b110; // z == bz-1
            bool hasFront = b001 || b011 || b101 || b111; // z == bz

            int neighbors = (hasLeft ? 1 : 0) + (hasRight ? 1 : 0) + (hasFront ? 1 : 0) + (hasBack ? 1 : 0) + (hasTop ? 1 : 0) + (hasDown ? 1 : 0);

            // нормируем: neighbors в диапазоне [3..6], делаем 0..3 -> 0..1
            //float noiseFactor = SimplexNoise.Noise.Generate(Mathf.Abs(bx), Mathf.Abs(by), Mathf.Abs(bz));
            //float noiseFactor = Mathf.PerlinNoise(bx, bz);
            float neighborsCount = neighbors - 3; // 0..3
            neighborsCount = WorldGenerator.Inst.AmbientOcclusionStrengthCurve.Evaluate(neighborsCount);
            float aoFactor = 1f - (neighborsCount / 3f) * AO_STRENGTH;// * noiseFactor;
            aoFactor = Mathf.Clamp01(aoFactor);


            // ----- 3) финальный цвет вершины -----
            float finalIntensity = aoFactor;/* * normalizedLight;/**/
            byte col = (byte)(Mathf.Clamp01(finalIntensity) * 255f);
            colors[i] = new Color32(col, col, col, 255);

            //if (Mathf.Abs(neighborsCount - 3) < 0.1f)
            //{
            //    var testooo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //    testooo.transform.position = worldPos;
            //    testooo.transform.localScale = Vector3.one * 0.18f;
            //    testooo.GetComponent<MeshRenderer>().material = WorldGenerator.Inst.ebosmat;
            //    testoos.Add(testooo);
            //}
        }

        mesh.colors32 = colors;
        // Примечание: если у тебя MeshCollider использует sharedMesh, не забудь не менять sharedMesh в рантайме.
    }

    // Вспомогательная: проверяет доступность чанка и непрозрачность блока.
    // Не создаёт чанков.
    bool IsGlobalBlockOpaqueSafe(Vector3Int global)
    {        
        var wg = WorldGenerator.Inst;
        if (wg.HasChunck(global, out var key))
        {
            var c = wg.chuncks[key];
            // Предполагаем, что GetBlockID(Vector3) у чанка корректен и не создаёт чанков.
            byte id = c.GetBlockID(new Vector3(global.x, global.y, global.z));
            if (id == 0) return false;
            // Прозрачные ID (пример). Подставь свои правила.
            //if (id == 2) return false;
            return true;
        }
        
        return false;
    }

    // Существующая у тебя приближёнка для чтения эмиссии/света — оставляем, не создавая чанков.
    int SafeGetLightApprox(Vector3Int globalBlockPos)
    {
        try
        {
            var wg = WorldGenerator.Inst;
            if (wg == null) return 0;
            if (wg.HasChunck(globalBlockPos, out var key))
            {
                var chunk = wg.chuncks[key];
                byte id = chunk.GetBlockID(new Vector3(globalBlockPos.x, globalBlockPos.y, globalBlockPos.z));
                // Пример эмиссии
                if (id == 5) return 14;
                if (id == 6) return 15;
                return 0;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }


    //// Замените существующий RecalculateLightmapAndApply на этот метод
    //public void RecalculateLightmapAndApply()
    //{
    //    try
    //    {
    //        // Заполняем локальную lightmap "плоским" фоновым освещением, чтобы избежать чёрных чанков.
    //        lightmap = new byte[size, size, size];
    //        for (int x = 0; x < size; x++)
    //            for (int y = 0; y < size; y++)
    //                for (int z = 0; z < size; z++)
    //                    lightmap[x, y, z] = VoxelLighting.MAX_LIGHT; // максимум — весь мир ровно освещён

    //        // Применяем только AO-усреднение через уже существующий ApplyLightToMesh()
    //        ApplyLightToMesh();
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError($"RecalculateLightmapAndApply (flat ambient) failed: {ex}");
    //    }
    //}


    ////// --------- Новое/обновлённое ---------------
    ////public void RecalculateLightmapAndApply()
    ////{
    ////    // безопасно пересчитываем
    ////    try
    ////    {
    ////        lightmap = VoxelLighting.ComputeChunkLightmap(this);
    ////        ApplyLightToMesh();
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        Debug.LogError($"RecalculateLightmapAndApply failed: {ex}");
    ////    }
    ////}

    //void ApplyLightToMesh()
    //{
    //    if (meshFilter == null || meshFilter.sharedMesh == null) return;
    //    Mesh mesh = meshFilter.mesh; // instance
    //    var verts = mesh.vertices;
    //    Color32[] colors = new Color32[verts.Length];

    //    // chunkOrigin = мировой блок-координат начала чанка (как раньше)
    //    Vector3Int chunkOrigin = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

    //    for (int i = 0; i < verts.Length; i++)
    //    {
    //        // 1) получить мировую позицию вершины
    //        Vector3 worldPos = renderer != null ? renderer.transform.TransformPoint(verts[i]) : (pos + verts[i]);

    //        // 2) координата блока в мире (целая)
    //        int bx = Mathf.FloorToInt(worldPos.x);
    //        int by = Mathf.FloorToInt(worldPos.y);
    //        int bz = Mathf.FloorToInt(worldPos.z);

    //        // 3) усреднение света из 8 соседних блоков (corner sample)
    //        int sum = 0;
    //        int cnt = 0;
    //        for (int sx = 0; sx <= 1; sx++)
    //            for (int sy = 0; sy <= 1; sy++)
    //                for (int sz = 0; sz <= 1; sz++)
    //                {
    //                    Vector3Int sampleBlock = new Vector3Int(bx - sx, by - sy, bz - sz);
    //                    int val;
    //                    // если в локальном чанке — быстро брать из lightmap, иначе approximate safe
    //                    Vector3Int local = sampleBlock - chunkOrigin;
    //                    if (local.x >= 0 && local.x < size && local.y >= 0 && local.y < size && local.z >= 0 && local.z < size)
    //                    {
    //                        val = lightmap[local.x, local.y, local.z];
    //                    }
    //                    else
    //                    {
    //                        val = SafeGetLightApprox(sampleBlock);
    //                    }
    //                    sum += val;
    //                    cnt++;
    //                }

    //        float avgLight = cnt > 0 ? (float)sum / cnt : 0f;
    //        float normalizedLight = Mathf.Clamp01(avgLight / VoxelLighting.MAX_LIGHT);

    //        // 4) простой AO: считаем 6 соседних осевых блоков, чем больше занятых — тем сильнее темнее
    //        int solidCount = 0;
    //        Vector3Int[] axes = {
    //        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
    //        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
    //        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
    //    };
    //        for (int s = 0; s < axes.Length; s++)
    //        {
    //            Vector3Int sb = new Vector3Int(bx, by, bz) + axes[s];
    //            bool solid = IsGlobalBlockOpaqueSafe(sb);
    //            if (solid) solidCount++;
    //        }
    //        // AO factor: 0..1 where 1 = no occlusion, 0.6 = strong occlusion
    //        float ao = 1f - (solidCount / 6f) * 0.6f; // настраиваемый коэффициент 0.6 — меняй

    //        float final = normalizedLight * ao;
    //        byte col = (byte)(Mathf.Clamp01(final) * 255f);
    //        colors[i] = new Color32(col, col, col, 255);
    //    }

    //    mesh.colors32 = colors;
    //}

    //// Вспомогательная: проверяет, есть ли загруженный блок и он непрозрачен
    //bool IsGlobalBlockOpaqueSafe(Vector3Int global)
    //{
    //    try
    //    {
    //        if (WorldGenerator.Inst.HasChunck(global, out var key))
    //        {
    //            var c = WorldGenerator.Inst.chuncks[key];
    //            // GetBlockID у тебя принимает Vector3 в прошлой версии — можно использовать
    //            byte id = c.GetBlockID(new Vector3(global.x, global.y, global.z));
    //            // ту же логику IsOpaque как в VoxelLighting
    //            if (id == 0) return false;
    //            //if (id == 2) return false; // пример стекла
    //            return true;
    //        }
    //    }
    //    catch { }
    //    return false;
    //}



    //// Простая приближённая функция — если блок эмиттер, возвращаем его эмиссию, иначе 0.
    //int SafeGetLightApprox(Vector3Int globalBlockPos)
    //{
    //    try
    //    {
    //        var wg = WorldGenerator.Inst;
    //        if (wg.HasChunck(globalBlockPos, out var key))
    //        {
    //            var chunk = wg.chuncks[key];
    //            byte id = chunk.GetBlockID(globalBlockPos);
    //            if (id == 5) return 14;
    //            if (id == 6) return 15;
    //            return 0;
    //        }
    //        else
    //        {
    //            return 0;
    //        }
    //    }
    //    catch
    //    {
    //        return 0;
    //    }
    //}/**/

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

        //[JsonConstructor]
        //private JsonTurnedBlock(Vector3 localPos, TurnBlockData turnBlockData)
        //{
        //    pos = localPos;
        //    angle = turnBlockData.angle;
        //    axis = turnBlockData.axis;
        //}
    }
}
