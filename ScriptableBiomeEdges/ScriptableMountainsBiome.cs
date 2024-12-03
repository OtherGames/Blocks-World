using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome Noise", menuName = "GENERATION/Biome/Mountain")]
public class ScriptableMountainsBiome : ScriptableBiomeIndex
{
    public override BiomePointData GetBiomeNoise(float x, float y, float z, Biome biome, float biomeScale, float globalBiomeThresold)
    {
        float noiseX = (x + biome.noiseOffset) * (biome.frequency * biomeScale);
        //float noiseY = (y + biome.noiseOffset) * (biome.frequency * biomeScale);
        float noiseZ = (z + biome.noiseOffset) * (biome.frequency * biomeScale);

        float noise = GeneratePerliNoise(noiseX * scale, noiseZ * scale);
        biomePointData.biomeNoise = noise;
        var noiseThreshold = biome.sizeThresold * globalBiomeThresold * localBiomeThreshold;
        biomePointData.noiseThreshold = noiseThreshold;

        if (noise > noiseThreshold)
        {
            thresholded = true;
            biomePointData.thresholded = true;
            biomePointData.pointType = BiomeThresholdedType.Biomos;
            return biomePointData;
        }

        thresholded = false;
        biomePointData.thresholded = false;
        biomePointData.pointType = BiomeThresholdedType.Default;
        return biomePointData;
    }
}
