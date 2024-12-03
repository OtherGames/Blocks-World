using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BLOCKS;

public class ProceduralGeneration : MonoBehaviour
{

    public float yCorrect = 1f;
    [Tooltip("Чем больше тем мир более \"растянутый\"")]
    public int noiseScale = 89;
    public float landThresold = 0.11f;
    [SerializeField] float rockThresold = 0.5f;
    [Tooltip("Уровень высоты на которой будет генериться ландшафт")]
    [SerializeField] protected float landHeight = 30f;
    [Tooltip("Диапазон высоты")]
    [SerializeField] protected float landBump = 30f;
    [SerializeField] protected float landHeightSlice = 8f;
    [Header("Настройки биомов")]
    [SerializeField] protected float biomeScale = 1f;
    [SerializeField] protected float biomeThresoldFactor = 0.001f;
    [SerializeField] public Biome[] biomes;
    [SerializeField] GenerateBlockIdSettings[] generationLayers;

    public BiomeNoiseResult[] biomsNoiseResults;
    public BiomePointData[] biomsPointsData;

    BiomomoGenerator biomeGenerator;


    private void Awake()
    {
        biomeScale = 1f / biomeScale;

        biomsNoiseResults = new BiomeNoiseResult[biomes.Length];
        biomsPointsData = new BiomePointData[biomes.Length];
        for (int i = 0; i < biomsNoiseResults.Length; i++)
        {
            biomsPointsData[i].name = biomes[i].name;
            biomsNoiseResults[i].names = biomes[i].name;
            biomsNoiseResults[i].idx = i;
        }

        foreach (var item in biomes)
        {
            if (item.scriptableBiomeNoise)
            {
                item.scriptableBiomeNoise.Init(item.name);
            }
        }
    }

    public float averageNoise = 0;

    public virtual byte GetBlockID(int x, int y, int z)
    {
        Random.InitState(888);

        float mainNoise = 0;
        byte blockID = 0;

        //var biomeIdx = biomeGenerator.GetBiomeAtPosition(x, y, z);
        var biomeIdx = GetBiomeIndex(x, y, z, out var biome, ref biomsNoiseResults);

        if (biome.scriptableBiomeNoise && biome.scriptableBiomeNoise.byBlockID)
        {
            var id = biome.scriptableBiomeNoise.blockID;
            if (id > 0)
            {
                if (id == 91)
                    return 0;

                return id;
            }
        }

        if(biomeIdx == -1)
        {
            return 0;// generationLayers[1].mainBlockID;
        }

        bool hasActiveBiomes = false;
        for (int i = 0; i < biomsPointsData.Length; i++)
        {
            if (biomsPointsData[i].thresholded)
            {
                hasActiveBiomes = true;
                break;
            }
        }

        if (biomeIdx == 0 || hasActiveBiomes)
        {
            // =========== Основной ландшафт ============
            var k = 1000;

            var offset = new Vector3(Random.value * k, Random.value * k, Random.value * k);

            float noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale);
            float noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale);
            float noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale);

            float noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY / yCorrect, noiseZ);

            noiseValue += (landHeight - y) / landBump;// World bump
            noiseValue /= y / landHeightSlice;

            if (noiseValue > landThresold)
            {
                mainNoise = noiseValue;
                if (noiseValue > landThresold + 0.15f)
                {
                    blockID = STONE;
                }
                else
                {
                    blockID = DIRT;
                }
            }


            // ==========================================

            // =========== Шаманим ============
            k = 10000;

            offset = new(Random.value * k, Random.value * k, Random.value * k);

            var scale = noiseScale - 30;
            noiseX = Mathf.Abs((float)(x + offset.x) / scale);
            noiseY = Mathf.Abs((float)(y + offset.y) / scale);
            noiseZ = Mathf.Abs((float)(z + offset.z) / scale);

            float noiseAlterValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

            noiseAlterValue += (30 - y) / 30f;// World bump
                                              //noiseValue /= y / 8f;


            if (blockID == 0 && noiseAlterValue > rockThresold)
            {
                mainNoise = noiseAlterValue;
                blockID = STONE;
            }


            //// =========== Горы Хуёры ===================
            if (GetBlockID(x, y, z, rockSettings) > 0)
            {
                if (GetBlockID(x, y, z, excludeRockSettings, out var outNoise) == 0)
                {
                    mainNoise = outNoise;
                    blockID = 3;// STONE_8_3;
                }
            }
            //// ==========================================
        }
        else
        {
            var biomSettings = generationLayers[biomeIdx];

#if UNITY_EDITOR
            if (biomSettings.useDebug)
            {
                if (biomSettings.useLandHeight)
                {
                    if(y < biomSettings.landBump)
                    {
                        blockID = biomSettings.mainBlockID;
                        return blockID;
                    }
                }
                else
                {
                    if (y < biomSettings.debugHeight)
                    {
                        blockID = biomSettings.mainBlockID;
                        return blockID;
                    }
                }
            }
#endif

            blockID = GetBlockID(x, y, z, biomSettings);
        }

        averageNoise = 0;
        for (int i = 0; i < biomsPointsData.Length; i++)
        {
            var biomePoint = biomsPointsData[i];
            if (biomePoint.thresholded)
            {
                if (!biomes[i].scriptableBiomeNoise.byBlockID)
                {
                    biomsNoiseResults[i].blockID = GetBlockID(x, y, z, generationLayers[i], out var outnoise);
                    biomsNoiseResults[i].noiseValue = outnoise;
                }

                if (i == 3)
                {
                    averageNoise += biomsNoiseResults[i].noiseValue;
                }
            }
        }

        if (biomsPointsData[3].thresholded)
        {
            var zaeb = generationLayers[3].scriptableGeneration as MountainOctaves;

            var yThreshold = zaeb.height * averageNoise;

            var biomPoint = biomsPointsData[3];
            //var biomeThreshold = biomes[3].sizeThresold * biomeThresoldFactor;
            //var smooth = biomes[3].scriptableBiomeNoise.smoothThreshold;
            //print($"{(biomPoint.biomeNoise - biomPoint.smoothOffset) - biomPoint.noiseThreshold}");

            // Внутрення область биома
            if (biomPoint.biomeNoise > biomPoint.noiseThreshold + biomPoint.smoothOffset)
            {
                //print("t,t,t,t,");
                if (yThreshold > y)
                {
                    return 11;
                }
            }
            else // Внешняя область биома
            {
                mainNoise = Mathf.Clamp01(mainNoise);
                var diff = biomPoint.biomeNoise - biomPoint.noiseThreshold;

                float mainLer = 0;
                if(mainNoise > 0)
                {
                    mainLer = y;
                }
                var lerped = Mathf.Lerp(mainLer, yThreshold, diff / biomPoint.smoothOffset);

                if (lerped > y)
                {
                    return 15;// generationLayers[3].mainBlockID;
                }
            }

            //var yThreshold = zaeb.height * lerped;

            
        }


        return blockID;
    }

    public virtual byte GetBlockID_Old(int x, int y, int z)
    {
        Random.InitState(888);


        // ============== Генерация Гор =============
        var k = 1000;//10000000;// чем больше тем реже

        Vector3 offset = new(Random.value * k, Random.value * k, Random.value * k);

        float noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        float noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale / 2);
        float noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        //float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        ////goraValue += (30 - y) / 3000f;// World bump
        ////goraValue /= y / 1f;// для воды заебок;

        byte blockID = 0;
        //if (goraValue > 0.35f)
        //{
        //    if (goraValue > 0.3517f)
        //    {
        //        blockID = 2;
        //    }
        //    else
        //    {
        //        blockID = 1;
        //    }
        //}
        // ==========================================

        // =========== Основной ландшафт ============
        k = 1000;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale);
        noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale);
        noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale);

        float noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY / yCorrect, noiseZ);

        noiseValue += (landHeight - y) / landBump;// World bump
        noiseValue /= y / landHeightSlice;
        //noiseValue += (30 - y) / 30f;// World bump
        //noiseValue /= y / 8f;

        //cavernas /= y / 19f;
        //cavernas /= 2;
        //Debug.Log($"{noiseValue} --- {y}");

        if (noiseValue > landThresold)
        {
            if (noiseValue > landThresold + 0.1f)
            {
                blockID = STONE;
            }
            else
            {
                blockID = DIRT;
            }
        }


        // ==========================================

        // =========== Шаманим ============
        k = 10000;

        offset = new(Random.value * k, Random.value * k, Random.value * k);

        var scale = noiseScale - 30;
        noiseX = Mathf.Abs((float)(x + offset.x) / scale);
        noiseY = Mathf.Abs((float)(y + offset.y) / scale);
        noiseZ = Mathf.Abs((float)(z + offset.z) / scale);

        float noiseAlterValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        noiseAlterValue += (30 - y) / 30f;// World bump
        //noiseValue /= y / 8f;

        //cavernas /= y / 19f;
        //cavernas /= 2;
        //Debug.Log($"{noiseValue} --- {y}");

        if (blockID == 0 && noiseAlterValue > rockThresold)
        {
            blockID = STONE;
        }


        //// =========== Горы Хуёры ===================
        if (GetBlockID(x, y, z, rockSettings) > 0)
        {
            if (GetBlockID(x, y, z, excludeRockSettings) == 0)
            {
                blockID = STONE_8_3;
            }
        }
        //// ==========================================



        // ==========================================

        //// =========== Скалы, типа пики =============
        //k = 10000;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale * 2));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale * 3));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale * 2));

        //float rockValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (rockValue > 0.8f)
        //{
        //    if (rockValue > 0.801f)
        //        blockID = 2;
        //    else
        //        blockID = 1;
        //}
        //// ==========================================

        //// =========== Скалы, типа пики =============
        //k = 100;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 2));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 1));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 2));

        //float smallRockValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (smallRockValue > smallRockThresold && noiseValue > (landThresold - 0.08f))
        //{
        //    blockID = 2;
        //    if (smallRockValue < smallRockThresold + 0.01f)
        //        blockID = 1;
        //}
        //// ==========================================

        //// =========== Гравий ========================
        //k = 33333;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        //float gravelValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (gravelValue > 0.85f && (noiseValue > landThresold))
        //{
        //    blockID = BLOCKS.GRAVEL;
        //}
        //// ==========================================

        //// =========== Уголь ========================
        //k = 10;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        //float coalValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (coalValue > 0.92f && (noiseValue > landThresold))
        //{
        //    blockID = BLOCKS.ORE_COAL;
        //}
        //// ==========================================

        //// =========== Жэлэзная руда ========================
        //k = 700;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        //float oreValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (oreValue > 0.93f && (noiseValue > landThresold))
        //{
        //    blockID = 30;
        //}
        //// ==========================================

        //// =========== Селитра руда ========================
        //k = 635;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        //float saltpeterValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (saltpeterValue > 0.935f && (noiseValue > landThresold))
        //{
        //    blockID = BLOCKS.SALTPETER;
        //}
        //// ==========================================

        //// =========== Сера ========================
        //k = 364789;

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / (noiseScale / 9));
        //noiseY = Mathf.Abs((float)(y + offset.y) / (noiseScale / 9));
        //noiseZ = Mathf.Abs((float)(z + offset.z) / (noiseScale / 9));

        //float sulfurValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //if (sulfurValue > 0.93f && (noiseValue > landThresold))
        //{
        //    blockID = BLOCKS.ORE_SULFUR;
        //}
        //// ==========================================


        // Типа горы
        ////////// Для рек ////////////////////////////////////////////
        //k = 10000000;// чем больше тем реже

        //offset = new(Random.value * k, Random.value * k, Random.value * k);

        //noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        //noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale / 2);
        //noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        //float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        //goraValue += (30 - y) / 3000f;// World bump
        //goraValue /= y / 80f;// для воды заебок;

        //blockID = 0;
        //if (goraValue > 0.08f && goraValue < 0.3f)
        //{
        //    blockID = 2;
        //}
        ///==============================================



        //if (oreValue < minValue)
        //    minValue = oreValue;
        //if (oreValue > maxValue)
        //    maxValue = oreValue;

        /////////////////////////////////////////////////////////////////////
        //k = 10000000;// чем больше тем реже

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
        //#pragma warning disable CS0436 // Тип конфликтует с импортированным типом
        //            var res = noise.snoise(new float3(noiseX, noiseY, noiseZ));//snoise(pos);
        //#pragma warning restore CS0436 // Тип конфликтует с импортированным типом

        //if (y < 3) res = 0.5f;

        //if (res > 0.3f)
        //{
        //    return true;
        //}
    }


    Vector3 settingableOffset;
    public byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings)
    {
        byte blockID = 0;

        if (settings.scriptableGeneration)
        {
            return settings.scriptableGeneration.GetBlockID(x, y, z, settings);
        }

        settingableOffset = settings.seed;

        var noiseX = Mathf.Abs((float)(x + settingableOffset.x) / settings.noiseScale);
        var noiseY = Mathf.Abs((float)(y + settingableOffset.y) / settings.noiseScale);
        var noiseZ = Mathf.Abs((float)(z + settingableOffset.z) / settings.noiseScale);
        noiseY /= settings.yCorrect;

        var noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (settings.useLandHeight)
        {
            noiseValue += (settings.landHeight - y) / settings.landBump;// World bump
        }

        if (settings.useValuePower)
        {
            noiseValue *= noiseValue;
        }

        if (settings.useHeightSlice)
        {
            noiseValue /= y / settings.landHeightSlice;
        }

        //if (settings.useValuePower)
        //{
        //    noiseValue *= y * y * y * y;
        //}

        //noiseValue += (30 - y) / 30f;// World bump
        //noiseValue /= y / 8f;

        //cavernas /= y / 19f;
        //cavernas /= 2;


        if (noiseValue > settings.landThresold)
        {
            blockID = settings.mainBlockID;
        }

#if UNITY_EDITOR
        if (!settings.notUseInclude)
        {
#endif
            for (int i = 0; i < settings.includeGenerators.Length; i++)
            {
                var include = settings.includeGenerators[i];

                if (noiseValue + include.thresold > settings.landThresold)
                {
                    var includeID = GetBlockID(x, y, z, include.settings);
                    if (includeID > 0)
                    {
                        blockID = includeID;
                    }
                }
            }
#if UNITY_EDITOR
        }
#endif

        return blockID;
    }

    public byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings, out float outNoise)
    {
        byte blockID = 0;

        if (settings.scriptableGeneration)
        {
            return settings.scriptableGeneration.GetBlockID(x, y, z, settings, out outNoise);
        }

        settingableOffset = settings.seed;

        var noiseX = Mathf.Abs((float)(x + settingableOffset.x) / settings.noiseScale);
        var noiseY = Mathf.Abs((float)(y + settingableOffset.y) / settings.noiseScale);
        var noiseZ = Mathf.Abs((float)(z + settingableOffset.z) / settings.noiseScale);
        noiseY /= settings.yCorrect;

        var noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);
        
        if (settings.useLandHeight)
        {
            noiseValue += (settings.landHeight - y) / settings.landBump;// World bump
        }

        if (settings.useValuePower)
        {
            noiseValue *= noiseValue;
        }

        if (settings.useHeightSlice)
        {
            noiseValue /= y / settings.landHeightSlice;
        }

        if (noiseValue > settings.landThresold)
        {
            blockID = settings.mainBlockID;
        }

        outNoise = noiseValue;

        return blockID;
    }

    float biomNoiseX, biomNoiseY, biomNoiseZ;
    /// <summary>
    /// Метод определяет биом
    /// </summary>
    public int GetBiomeIndex(float x, float y, float z, out Biome outBiome, ref BiomeNoiseResult[] biomsResults)
    {
        var biomeIdx = 0;
        outBiome = default;
        
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // пропускается первая генерация. так как ты хотел проверить
        // прикольную генерацию первого элемента массива генераций, она
        // пока не ипользуется, но ты хотел не терять её
        for (int i = 1; i < biomes.Length; i++)
        {
            var biome = biomes[i];

            float noise;

            if (biome.scriptableBiomeNoise)
            {
                var biomePointData = biome.scriptableBiomeNoise.GetBiomeNoise
                (
                  x, y, z,
                  biome,
                  biomeScale,
                  biomeThresoldFactor
                );

                biomsPointsData[i] = biomePointData;

                biomsResults[i].active = biome.scriptableBiomeNoise.thresholded;

                if (biome.scriptableBiomeNoise.byBlockID)
                {
                    biomsResults[i].noiseValue = biome.scriptableBiomeNoise.noiseValue;
                    biomsResults[i].blockID = biome.scriptableBiomeNoise.blockID;
                }

                if(biomePointData.pointType == BiomeThresholdedType.Biomos)
                {
                    outBiome = biome;
                    biomeIdx = i;
                }
                if(biomePointData.pointType == BiomeThresholdedType.Border)
                {
                    outBiome = biome;
                    biomeIdx = - 1;
                }
            }
            else
            {
                //float baseNoise = Mathf.PerlinNoise
                //(
                //    (x + biome.noiseOffset) * (biomeScale * biome.frequency),
                //    (z + biome.noiseOffset) * (biomeScale * biome.frequency)
                //);

                biomNoiseX = Mathf.Abs((x + biome.noiseOffset) * (biomeScale * biome.frequency));
                biomNoiseY = Mathf.Abs((y + biome.noiseOffset) * (biomeScale * biome.frequency));
                biomNoiseZ = Mathf.Abs((z + biome.noiseOffset) * (biomeScale * biome.frequency));

                noise = GetOctavedSimplexNoise(biomNoiseX, biomNoiseY, biomNoiseZ, 3);

                if (noise > biome.sizeThresold * biomeThresoldFactor)
                {
                    biomeIdx = i;
                }
            }
        }

        return biomeIdx;
    }

    float GetOctavedSimplexNoise(float x, float y, float z, int octaves)
    {
        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            noise += SimplexNoise.Noise.Generate
            (
                x * frequency,
                y * frequency,
                z * frequency
            ) * amplitude;
            maxAmplitude += amplitude;

            frequency *= 2.18f;
            amplitude *= 1.5f;
        }

        noise /= maxAmplitude; // Нормализация
        return noise;
    }

    GenerateBlockIdSettings rockSettings = new GenerateBlockIdSettings()
    {
        mainBlockID = DIRT,
        noiseScale = 180,
        yCorrect = 1,
        landThresold = 1.8f,
        landHeight = 300,
        landBump = 800,
        landHeightSlice = 188,
        seed = Vector3.one * 888,

        useLandHeight = false,
        useHeightSlice = true,
        useValuePower = true,

        includeGenerators = new IncludeSettings[0],
    };

    [Space]
    [SerializeField]
    GenerateBlockIdSettings excludeRockSettings = new GenerateBlockIdSettings()
    {
        mainBlockID = DIRT,
        noiseScale = 58,
        yCorrect = 0.7f,
        landThresold = 0.8f,
        landHeight = 0,
        landBump = 0,
        landHeightSlice = 0,
        seed = Vector3.one * 888,

        useLandHeight = false,
        useHeightSlice = false,
        useValuePower = false,
    };
}

[System.Serializable]
public struct GenerateBlockIdSettings
{
    public string name;
    public byte mainBlockID;
    [Tooltip("Чем больше тем шум более \"растянутый\"")]
    public float noiseScale;
    public float yCorrect;
    public float landThresold;
    [Tooltip("Уровень высоты на которой будет генериться ландшафт")]
    public float landHeight;
    [Tooltip("Диапазон высоты, если сделать больше чем \"Land Height\", то будут дыры внизу с вывернутой генерацией. Так же уменьшает высоту генерации")]
    public float landBump;
    [Tooltip("Увеличивает высоту генерации и добавляет глубину впадинам")]
    public float landHeightSlice;

    public Vector3 seed;//(198.05, 136.82, 752.05)

    public bool useLandHeight;
    [Tooltip("Отключает генерацию подвешенных в воздухе платформ")]
    public bool useHeightSlice;
    [Tooltip("Просто перемножает \"Noise Value\" на саму себя")]
    public bool useValuePower;
    public bool notUseInclude;

    public ScriptableGeneration scriptableGeneration;
    public IncludeSettings[] includeGenerators;
    public GenerateBlockIdSettings[] excludeGenerators;

#if UNITY_EDITOR
    public bool useDebug;
    public float debugHeight;
#endif
}

[System.Serializable]
public struct IncludeSettings
{
    public float thresold;
    public GenerateBlockIdSettings settings;
}

[System.Serializable]
public struct Biome
{
    public string name;
    [Tooltip("Чем больше, тем шум чаще, то есть масштаб уменьшается")]
    public float frequency; // Вес (частота) биома
    [Range(0.0001f, 0.999f)] [Tooltip("Чем меньше, тем крупнее куски")]
    public float sizeThresold;
    public float noiseOffset;
    public ScriptableBiomeIndex scriptableBiomeNoise;
}

[System.Serializable]
public struct BiomeNoiseResult
{
    public string names;
    public int idx;
    public float noiseValue;
    public byte blockID;
    public bool active;
}
