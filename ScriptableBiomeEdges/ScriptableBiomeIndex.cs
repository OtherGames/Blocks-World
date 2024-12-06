using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome Noise", menuName = "GENERATION/Biome Noise")]
public class ScriptableBiomeIndex : ScriptableObject
{
    [SerializeField] public float scale = 1;
    [SerializeField] int octaves = 4;
    [SerializeField] int octavesSimplex = 3;
    [SerializeField] float frequencyFactor = 2f;
    [SerializeField] float amplitudeFactor = 0.5f;
    [SerializeField] public int height = 100;
    [SerializeField] public float smoothThreshold = 0.18f;
    [SerializeField] public float heightOffset = 10;
    [SerializeField] public float localBiomeThreshold = 1f;
    [SerializeField] float simplexThreshold = 0.1f;
    [SerializeField] float simplexScale = 1f;
    [SerializeField] public float heightEbala = 10f;
    [SerializeField] float sharpnessEbala = 3f;
    [SerializeField] float zaebalo = 2f;

    [Space]

    public bool byBlockID;
    public byte blockID;

    [Space]

    public float min =  999999f;
    public float max = -999999f;

    public float noiseValue;
    public bool thresholded;

    public BiomePointData biomePointData;

    protected Biome biome;

    public virtual void Init(Biome biome, float globalBiomeThreshold, string biomeName = "")
    {
        min =  999999f;
        max = -999999f;

        this.biome = biome;

        biomePointData.name = biomeName;
        biomePointData.smoothOffset = smoothThreshold;
    }

    public virtual BiomePointData GetBiomeNoise(float x, float y, float z, float biomeScale, float globalBiomeThresold)
    {
        float noiseX = (x + biome.noiseOffset) * (biome.frequency * biomeScale);
        float noiseY = (y + biome.noiseOffset) * (biome.frequency * biomeScale);
        float noiseZ = (z + biome.noiseOffset) * (biome.frequency * biomeScale);

        float noise = GeneratePerliNoise(noiseX * scale, noiseZ * scale);
        biomePointData.biomeNoise = noise;

        var yThresolded = (1f - noise) * height;
        biomePointData.yThreshold = yThresolded;
        biomePointData.height = height;
        var noiseThreshold = biome.sizeThresold * globalBiomeThresold * localBiomeThreshold;
        biomePointData.noiseThreshold = noiseThreshold;

        if (yThresolded < -y)
        {
            thresholded = false;
            biomePointData.thresholded = false;
            biomePointData.pointType = BiomeThresholdedType.Default;
            return biomePointData;
        }

        if (noise > noiseThreshold)
        {
            thresholded = true;
            biomePointData.thresholded = true;
            biomePointData.pointType = BiomeThresholdedType.Biomos;
            return biomePointData;
        }
        else if (noise + smoothThreshold > noiseThreshold)
        {
            var simplexNoise = GenerateSimplexNoise
            (
                noiseX * simplexScale,
                noiseY * simplexScale,
                noiseZ * simplexScale
            );
            var zupa = (Mathf.Pow(noise * zaebalo, sharpnessEbala));
            if(zupa > max)
            {
                max = zupa;
            }
            if(zupa < min)
            {
                min = zupa;
            }
            simplexNoise += 1f - Mathf.Pow(noise * zaebalo, sharpnessEbala);
            simplexNoise /= ((y + heightOffset) / heightEbala);

            //Debug.Log(simplexNoise);
            if (Mathf.Abs(simplexNoise) < simplexThreshold)
            {
                thresholded = true;
                biomePointData.thresholded = true;
                biomePointData.pointType = BiomeThresholdedType.Border;
                return biomePointData;
            }
            //if (heightOffset < y)
            //{
            //    return EbalaBiomola.Emptos;
            //}
        }

        thresholded = false;
        biomePointData.thresholded = false;
        biomePointData.pointType = BiomeThresholdedType.Default;

        return biomePointData;
    }

    public virtual float GenerateSimplexNoise(float x, float y, float z)
    {
        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octavesSimplex; i++)
        {
            noise += SimplexNoise.Noise.Generate
            (
                x * frequency,
                y * frequency,
                z * frequency
            ) * amplitude;
            maxAmplitude += amplitude;

            frequency *= frequencyFactor;
            amplitude *= amplitudeFactor;
        }

        noise /= maxAmplitude; // Нормализация
        return noise;
    }

    float perlinNoise, perlinFrequency, perlinAmplitude, perlinMaxAmplitude;
    public virtual float GeneratePerliNoise(float x, float z)
    {
        perlinNoise = 0f;
        perlinFrequency = 1f;
        perlinAmplitude = 1f;
        perlinMaxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            perlinNoise += Mathf.PerlinNoise(x * perlinFrequency, z * perlinFrequency) * perlinAmplitude;
            perlinMaxAmplitude += perlinAmplitude;

            perlinFrequency *= frequencyFactor;
            perlinAmplitude *= amplitudeFactor;
        }

        perlinNoise /= perlinMaxAmplitude; // Нормализация
        //noise = Mathf.Pow(noise, sharpness); // Применяем резкость
        return perlinNoise;
    }
}

public enum BiomeThresholdedType : byte
{
    Default,
    Border,
    Biomos
}

[System.Serializable]
public struct BiomePointData
{
    public string name;
    public float biomeNoise;
    public float noiseThreshold;
    public float smoothOffset;
    public byte blockID;
    public BiomeThresholdedType pointType;
    public bool byBlockID;
    public bool thresholded;

    // Для 2D шумов выбора биома
    public float yThreshold;
    public float height;
}