using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable Generation", menuName = "GENERATION/oct")]
public class MountainOctaves : ScriptableGeneration
{
    [SerializeField] float scale = 1;
    [SerializeField] public float height = 100;
    [SerializeField] float sharpness = 2f;  // Степень резкости
    [SerializeField] int octaves = 4;       // Количество октав

    // Границы высот для разных блоков
    public float dirtHeight = 15f;      // Максимальная высота для земли
    public float stoneHeight = 30f;     // Максимальная высота для камня
    public float snowHeight = 50f;      // Максимальная высота для снега

    float noiseX, noiseZ, baseNoise, heightValue;
    float scaleDivine; 

    public override void Init(GenerateBlockIdSettings settings)
    {
        base.Init(settings);

        scaleDivine = 1f / settings.noiseScale * scale;
    }

    public override byte GetBlockID(int x, int y, int z, ref GenerateBlockIdSettings settings)
    {
        noiseX = (x + settings.seed.x) * scaleDivine; // / (settings.noiseScale * scale);
        noiseZ = (z + settings.seed.z) * scaleDivine; // / (settings.noiseScale * scale);

        baseNoise = GenerateSharpNoise(noiseX, noiseZ);

        heightValue = baseNoise * height;

        if (heightValue > y)
        {
            return settings.mainBlockID;
        }

        return 0;
    }

    public override byte GetBlockID(int x, int y, int z, ref GenerateBlockIdSettings settings, out float outNoise)
    {
        noiseX = (x + settings.seed.x) * scaleDivine; // / (settings.noiseScale * scale);
        noiseZ = (z + settings.seed.z) * scaleDivine; // / (settings.noiseScale * scale);
        //noiseX = GetScalingX(x);
        //noiseZ = GetScalingZ(z);

        baseNoise = GenerateSharpNoise(noiseX, noiseZ);

        heightValue = baseNoise * height;

        if (heightValue > y)
        {
            outNoise = baseNoise;
            return settings.mainBlockID;
        }

        outNoise = 0;
        return 0;
    }

    protected float GetScalingX(float x)
    {
        return (x + generationSettings.seed.x) * scaleDivine;
    }

    protected float GetScalingZ(float z)
    {
        return (z + generationSettings.seed.z) * scaleDivine;
    }

    float noise = 0f;
    float frequency = 1f;
    float amplitude = 1f;
    float maxAmplitude = 0f;
    float GenerateSharpNoise(float x, float z)
    {
        noise = 0f;
        frequency = 1f;
        amplitude = 1f;
        maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            noise += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maxAmplitude += amplitude;

            frequency *= 2f;
            amplitude *= 0.5f;
        }

        //noise /= maxAmplitude; // Нормализация
        //noise = Mathf.Pow(noise, sharpness); // Применяем резкость
        noise = Normalize(noise);
        noise = Pow6(noise);
        return noise;
    }

    private float Normalize(float noise)
    {
        return noise / maxAmplitude;
    }

    private float Pow6(float noise)
    {
        return noise * noise * noise * noise * noise * noise;
    }
}
