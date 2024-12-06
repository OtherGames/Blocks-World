using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome Noise", menuName = "GENERATION/Biom/Desert")]
public class ScriptableDesert : ScriptableBiomeIndex
{
    [Space]
    public float coeffHeight = 3f;
    public float yaBlizokHeight = 0f;
    [SerializeField] float biomBorderHeightOffset = 10;
    [SerializeField] float innerNoiseFrequency = 5f;
    [SerializeField] float innerNoiseHeight = 5f;

    float noiseThreshold;

    public override void Init(Biome biome, float globalBiomeThreshold, string biomeName = "")
    {
        base.Init(biome, globalBiomeThreshold, biomeName);

        biomePointData.height = height;
        biomePointData.byBlockID = true;
    }


    public override BiomePointData GetBiomeNoise(float x, float y, float z, float biomeScale, float globalBiomeThresold)
    {
        float noiseX = (x + biome.noiseOffset) * (biome.frequency * biomeScale);
        //float noiseY = (y + biome.noiseOffset) * (biome.frequency * biomeScale);
        float noiseZ = (z + biome.noiseOffset) * (biome.frequency * biomeScale);

        float noise = GeneratePerliNoise(noiseX * scale, noiseZ * scale);
        biomePointData.biomeNoise = noise;

        var yThresolded = (noise) * height;
        biomePointData.yThreshold = yThresolded;
        var noiseThreshold = biome.sizeThresold * globalBiomeThresold * localBiomeThreshold;
        biomePointData.noiseThreshold = noiseThreshold;

        var offset = height * noiseThreshold;

        if (noise > noiseThreshold)// Определяет область
        {
            noiseValue = noise;
            thresholded = true;
            biomePointData.thresholded = true;

            if (noise - smoothThreshold > noiseThreshold)
            {
                var localNoise = Mathf.PerlinNoise
                (
                    noiseX * innerNoiseFrequency,
                    noiseZ * innerNoiseFrequency
                );

                var heightByNoise = (noise) * (height / coeffHeight);
                var localOffset = (localNoise * innerNoiseHeight) - (innerNoiseHeight / 2);
                // Внутренняя генерация
                if (y < (heightByNoise + localOffset) - heightEbala)
                {
                    blockID = 90;
                    biomePointData.blockID = blockID;
                    biomePointData.pointType = BiomeThresholdedType.Biomos;

                    return biomePointData;
                }
            }
            else
            {
                // Граница биома
                if (y - biomBorderHeightOffset < yThresolded - offset)
                {
                    blockID = 90;
                    biomePointData.blockID = blockID;
                    biomePointData.pointType = BiomeThresholdedType.Biomos;

                    return biomePointData;
                }
            }

        // ===== Вычитание других биомов =====
            if (noise - smoothThreshold > noiseThreshold)
            {
                if (((1f - noise) * (height / coeffHeight)) - heightEbala < y - yaBlizokHeight)
                {
                    blockID = 91;
                    biomePointData.blockID = blockID;
                    biomePointData.pointType = BiomeThresholdedType.Biomos;
                    return biomePointData;
                }
            }
            else
            {
                if (((1f - noise) * height) - offset < y - heightOffset)
                {
                    blockID = 91;
                    biomePointData.blockID = blockID;
                    biomePointData.pointType = BiomeThresholdedType.Biomos;
                    return biomePointData;
                }

                
            }
        }
        else 
        {
           
            if (noise + smoothThreshold > noiseThreshold)
            {
                if (((1f - noise) * height) - offset < y - heightOffset)
                {
                    blockID = 91;
                    biomePointData.blockID = blockID;
                    biomePointData.pointType = BiomeThresholdedType.Biomos;
                    return biomePointData;
                }
            }
        }

        thresholded = false;
        blockID = 0;
        biomePointData.thresholded = false;
        biomePointData.blockID = blockID;
        biomePointData.pointType = BiomeThresholdedType.Default;
        return biomePointData;
    }
}
