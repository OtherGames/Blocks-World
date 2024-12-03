using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable Generation", menuName = "GENERATION/oct")]
public class MountainOctaves : ScriptableGeneration
{
    [SerializeField] float scale = 1;
    [SerializeField] public float height = 100;
    [SerializeField] float sharpness = 2f;  // ������� ��������
    [SerializeField] int octaves = 4;       // ���������� �����

    // ������� ����� ��� ������ ������
    public float dirtHeight = 15f;      // ������������ ������ ��� �����
    public float stoneHeight = 30f;     // ������������ ������ ��� �����
    public float snowHeight = 50f;      // ������������ ������ ��� �����

    public override byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings)
    {
        var noiseX = (x + settings.seed.x) / (settings.noiseScale * scale);
        var noiseZ = (z + settings.seed.z) / (settings.noiseScale * scale);

        float baseNoise = GenerateSharpNoise(noiseX, noiseZ);

        var heightValue = baseNoise * height;

        if (heightValue > y)
        {
            return settings.mainBlockID;
        }

        return 0;
    }

    public override byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings, out float outNoise)
    {
        var noiseX = (x + settings.seed.x) / (settings.noiseScale * scale);
        var noiseZ = (z + settings.seed.z) / (settings.noiseScale * scale);

        float baseNoise = GenerateSharpNoise(noiseX, noiseZ);

        var heightValue = baseNoise * height;

        if (heightValue > y)
        {
            outNoise = baseNoise;
            return settings.mainBlockID;
        }

        outNoise = 0;
        return 0;
    }

    float GenerateSharpNoise(float x, float z)
    {
        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            noise += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maxAmplitude += amplitude;

            frequency *= 2f;
            amplitude *= 0.5f;
        }

        noise /= maxAmplitude; // ������������
        noise = Mathf.Pow(noise, sharpness); // ��������� ��������
        return noise;
    }
}
