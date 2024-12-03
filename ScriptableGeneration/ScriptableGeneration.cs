using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ScriptableGeneration : ScriptableObject
{
    Vector3 settingableOffset;
    public virtual byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings)
    {
        byte blockID = 0;

        settingableOffset = settings.seed;

        var noiseX = Mathf.Abs((float)(x + settingableOffset.x) / settings.noiseScale);
        var noiseY = Mathf.Abs((float)(y + settingableOffset.y) / settings.noiseScale);
        var noiseZ = Mathf.Abs((float)(z + settingableOffset.z) / settings.noiseScale);
        noiseY /= settings.yCorrect;

        var noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (settings.useLandHeight)
        {
            noiseValue += (settings.landHeight - y) / settings.landBump;// World bump
        }

        if (settings.useValuePower)
        {
            //noiseValue *= y * y * y;
        }

        if (settings.useHeightSlice)
        {
            noiseValue /= y / settings.landHeightSlice;
        }

        if (noiseValue > settings.landThresold)
        {
            blockID = settings.mainBlockID;
        }

#if UNITY_EDITOR
        if (!settings.notUseInclude)
        {
#endif
            for (int i = 0; i < settings.includeGenerators.Length; i++)
            {
                var include = settings.includeGenerators[i];

                if (noiseValue + include.thresold > settings.landThresold)
                {
                    var includeID = GetBlockID(x, y, z, include.settings);
                    if (includeID > 0)
                    {
                        blockID = includeID;
                    }
                }
            }
#if UNITY_EDITOR
        }
#endif

        return blockID;
    }

    public virtual byte GetBlockID(int x, int y, int z, GenerateBlockIdSettings settings, out float outNoise)
    {
        byte blockID = 0;


        settingableOffset = settings.seed;

        var noiseX = Mathf.Abs((float)(x + settingableOffset.x) / settings.noiseScale);
        var noiseY = Mathf.Abs((float)(y + settingableOffset.y) / settings.noiseScale);
        var noiseZ = Mathf.Abs((float)(z + settingableOffset.z) / settings.noiseScale);
        noiseY /= settings.yCorrect;

        var noiseValue = SimplexNoise.Noise.Generate(noiseX, noiseY, noiseZ);

        if (settings.useLandHeight)
        {
            noiseValue += (settings.landHeight - y) / settings.landBump;// World bump
        }

        if (settings.useValuePower)
        {
            //noiseValue *= y * y * y;
        }

        if (settings.useHeightSlice)
        {
            noiseValue /= y / settings.landHeightSlice;
        }

        if (noiseValue > settings.landThresold)
        {
            blockID = settings.mainBlockID;
        }

#if UNITY_EDITOR
        if (!settings.notUseInclude)
        {
#endif
            for (int i = 0; i < settings.includeGenerators.Length; i++)
            {
                var include = settings.includeGenerators[i];

                if (noiseValue + include.thresold > settings.landThresold)
                {
                    var includeID = GetBlockID(x, y, z, include.settings);
                    if (includeID > 0)
                    {
                        blockID = includeID;
                    }
                }
            }
#if UNITY_EDITOR
        }
#endif
        outNoise = noiseValue;

        return blockID;
    }
}
