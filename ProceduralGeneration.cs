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
    float smallRockThresold = 0.8f;
    [Tooltip("Уровень высоты на которой будет генериться ландшафт")]
    [SerializeField] protected float landHeight = 30f;
    [Tooltip("Диапазон высоты")]
    [SerializeField] protected float landBump = 30f;
    [SerializeField] protected float landHeightSlice = 8f;

    float minValue = float.MaxValue;
    float maxValue = float.MinValue;

    public virtual byte GetBlockID(int x, int y, int z)
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

        settingableOffset.x = settings.randomFactor;
        settingableOffset.y = settings.randomFactor;
        settingableOffset.z = settings.randomFactor;
        
        var noiseX = Mathf.Abs((float)(x + settingableOffset.x) / settings.noiseScale);
        var noiseY = Mathf.Abs((float)(y + settingableOffset.y) / settings.noiseScale);
        var noiseZ = Mathf.Abs((float)(z + settingableOffset.z) / settings.noiseScale);

        var noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY / settings.yCorrect, noiseZ);

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

        //noiseValue += (30 - y) / 30f;// World bump
        //noiseValue /= y / 8f;

        //cavernas /= y / 19f;
        //cavernas /= 2;


        if (noiseValue > settings.landThresold)
        {
            blockID = DIRT;
        }

        return blockID;
    }

    GenerateBlockIdSettings rockSettings = new GenerateBlockIdSettings()
    {
        noiseScale = 180,
        yCorrect = 1,
        landThresold = 1.8f,
        landHeight = 300,
        landBump = 800,
        landHeightSlice = 188,
        randomFactor = 888,

        useLandHeight = false,
        useHeightSlice = true,
        useValuePower = true,
    };

    GenerateBlockIdSettings excludeRockSettings = new GenerateBlockIdSettings()
    {
        noiseScale = 80,
        yCorrect = 0.8f,
        landThresold = 0.8f,
        landHeight = 0,
        landBump = 0,
        landHeightSlice = 0,
        randomFactor = 888,

        useLandHeight = false,
        useHeightSlice = false,
        useValuePower = false,
    };
}

public struct GenerateBlockIdSettings
{
    public float noiseScale;
    public float yCorrect;
    public float landThresold;
    public float landHeight;
    public float landBump;
    public float landHeightSlice;
    public float randomFactor;

    public bool useLandHeight;
    public bool useHeightSlice;
    public bool useValuePower;
}
