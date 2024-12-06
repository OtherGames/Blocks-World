using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Simplex Octaved", menuName = "GENERATION/Simplex Octaved")]
public class SimplexOctaved : ScriptableGeneration
{
    [SerializeField] int octaves = 4;
    [SerializeField] float frequencyModifier = 2f;
    [SerializeField] float amplitudeModifier = 0.5f;

    public override byte GetBlockID(int x, int y, int z, ref GenerateBlockIdSettings settings)
    {

        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            noise += SimplexNoise.Noise.Generate
            (
                Mathf.Abs(((x + settings.seed.x) / settings.noiseScale) * frequency),
                Mathf.Abs(((y + settings.seed.y) / settings.noiseScale) * frequency) / settings.yCorrect,
                Mathf.Abs(((z + settings.seed.z) / settings.noiseScale) * frequency)
            ) * amplitude;
            maxAmplitude += amplitude;

            frequency *= frequencyModifier;
            amplitude *= amplitudeModifier;
        }

        noise /= maxAmplitude; // Нормализация

        if (noise > settings.landThresold)
        {
            return settings.mainBlockID;
        }

        return 0;
    }
}
