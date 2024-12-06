using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BLOCKS;


public class TestProceduralGeneration : ProceduralGeneration
{
    [Space(38)]

    [SerializeField] GenerateBlockIdSettings mainGeneration;

    [Space(38)]

    [SerializeField] float scaleY = 1f;
    [SerializeField] float landScale = 100;
    [SerializeField] float thresold = 0.8f;
    [SerializeField] float slice = 80;
    [SerializeField] int randomFactor = 1000;
    [SerializeField] bool useHeight;
    [SerializeField] bool useSlice;
    [SerializeField] bool useValuePower;


    public override byte GetBlockID(int x, int y, int z)
    {
      
        return GetBlockID(x, y, z, ref mainGeneration);
    }

    public byte GetBlockooooID(int x, int y, int z)
    {
        Random.InitState(888);


        // ============== Генерация Гор =============
        var k = randomFactor;//10000000;// чем больше тем реже

        Vector3 offset = new(Random.value * k, Random.value * k, Random.value * k);

        float noiseX = Mathf.Abs((float)(x + offset.x) / noiseScale / 2);
        float noiseY = Mathf.Abs((float)(y + offset.y) / noiseScale / 2);
        float noiseZ = Mathf.Abs((float)(z + offset.z) / noiseScale / 2);

        //float goraValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        ////goraValue += (30 - y) / 3000f;// World bump
        ////goraValue /= y / 1f;// для воды заебок;

        byte blockID = 0;
      
        // =========== Основной ландшафт ============
        k = randomFactor;

        offset = new Vector3(Random.value * k, Random.value * k, Random.value * k);

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
            blockID = DIRT;
        }

        return blockID;

    }
}
