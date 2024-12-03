using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastNoiseWrapper
{

    public NoiseSettings settings;
    private readonly FastNoiseLite[] octaveNoises;

    public FastNoiseWrapper(NoiseSettings settings)
    {
        this.settings = settings;
        octaveNoises = new FastNoiseLite[settings.Octaves.Length];

        for (int i = 0; i < settings.Octaves.Length; i++)
        {
            octaveNoises[i] = new FastNoiseLite();
            octaveNoises[i].SetSeed(settings.Seed);
            octaveNoises[i].SetNoiseType(settings.Octaves[i].NoiseType);
            octaveNoises[i].SetFrequency(settings.Octaves[i].Frequency);
            octaveNoises[i].SetFractalType(settings.Octaves[i].FractalType);
            octaveNoises[i].SetFractalOctaves(settings.Octaves[i].FractalOctaves);
            octaveNoises[i].SetFractalGain(settings.Octaves[i].Gain);
        }
    }

    public float GenerateNoise(float x, float z)
    {
        float result = settings.TerrainHeight;

        for (int i = 0; i < settings.Octaves.Length; i++)
        {
            float noise = octaveNoises[i].GetNoise(x, z);
            result += noise * settings.Octaves[i].Amplitude * 0.5f;
        }

        return result;
    }

    public static float RemapTo01Value(float noise) => (noise + 1) * 0.5f;


}


[CreateAssetMenu(fileName = "Noise Settings", menuName = "FAST NOISE/Noise Settings")]
public class NoiseSettings : ScriptableObject
{
    public int TerrainHeight;
    public int Seed;
    public float NoiseScale = 1;
    public NoiseOctaveSettings[] Octaves;
}

[System.Serializable]
public class NoiseOctaveSettings
{
    public FastNoiseLite.NoiseType NoiseType;
    public float Frequency = 0.2f;
    public float Amplitude = 1;

    public FastNoiseLite.FractalType FractalType;
    public int FractalOctaves = 3;
    public float Gain = 0.05f;
}

public class EbalaSmoothBiomo
{
    void WhatFuck()
    {

    }


    //float getShoto()
    //{
    //    float distanceDifference = biomeSelectionHelpers[0].Distance - biomeSelectionHelpers[1].Distance; // 0 - самый близкий, 1 - подальше


    //    float normalizedDifference = (distanceDifference + blendThreshold) / (2 * blendThreshold); //blendThreshold - это константа, равная 20, как и сказал. distanceDifference формула выше

    //    float weight_0 = 1.0f - normalizedDifference;
    //    float weight_1 = normalizedDifference;
    //    return Mathf.RoundToInt(terrainHeightNoise_1 * weight_0 + terrainHeightNoise_2 * weight_1);

    //}

}


