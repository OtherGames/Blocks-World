using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable Generation", menuName = "GENERATION/per")]
public class PerlinNoiseGeneration : ScriptableGeneration
{
    [SerializeField] float scale = 1;
    [SerializeField] float thresold = 0.5f;
    [SerializeField] float height = 100;
    [SerializeField] private AnimationCurve heightCurve;


    public override byte GetBlockID(int x, int y, int z, ref GenerateBlockIdSettings settings)
    {
        float baseNoise = Mathf.PerlinNoise
        (
            (x + settings.seed.x) / (settings.noiseScale * scale),
            (z + settings.seed.z) / (settings.noiseScale * scale)
        );

        baseNoise = heightCurve.Evaluate(baseNoise);

        var heightValue = baseNoise * height;
        //heightValue *= heightValue;

        if (heightValue > y)
        {
            return settings.mainBlockID;
        }

        return 0;
    }
}
