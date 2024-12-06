using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wizard Tree Biom", menuName = "GENERATION/Biom/Wizard Tree")]
public class ScriptableWizardTree : ScriptableBiomeIndex
{

    float noiseX, noiseZ, noise, noiseThreshold;
    public override BiomePointData GetBiomeNoise(float x, float y, float z, float biomeScale, float globalBiomeThresold)
    {
        noiseX = (x + biome.noiseOffset) * (biome.frequency * biomeScale);
        noiseZ = (z + biome.noiseOffset) * (biome.frequency * biomeScale);

        noise = GeneratePerliNoise(noiseX * scale, noiseZ * scale);
        biomePointData.biomeNoise = noise;

        noiseThreshold = biome.sizeThresold * globalBiomeThresold * localBiomeThreshold;
        biomePointData.noiseThreshold = noiseThreshold;

        if (noise > noiseThreshold)
        {
            //biomePointData.thresholded = true;
            //biomePointData.pointType = BiomeThresholdedType.Biomos;
            biomePointData.thresholded = false;
            biomePointData.pointType = BiomeThresholdedType.Default;
            return biomePointData;
        }
        else if (noise + smoothThreshold > noiseThreshold)
        {
            biomePointData.thresholded = true;
            biomePointData.pointType = BiomeThresholdedType.Biomos;
            return biomePointData;
        }

        biomePointData.thresholded = false;
        biomePointData.pointType = BiomeThresholdedType.Default;
        return biomePointData;
    }
}
