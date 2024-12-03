using UnityEngine;

public class BiomomoGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Biome
    {
        public string name;
        public float frequency; // ��� (�������) �����
        [Range(0.0001f, 0.999f)]
        public float sizeThresold;
        public float noiseOffset;
    }

    public Biome[] biomes;
    public float scale = 0.1f; // ������� ���� (������� �����������)


    private void Awake()
    {
        foreach (var biome in biomes)
        {
            //biome.frequency = 1f / biome.frequency;
        }
        scale = 1f / scale;
    }

    public int GetBiomeAtPosition(float x, float y, float z)
    {
        var biomeIdx = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            var biome = biomes[i];
            float baseNoise = Mathf.PerlinNoise
            (
                (x + biome.noiseOffset) * (scale * biome.frequency),
                (z + biome.noiseOffset) * (scale * biome.frequency)
            );

            if (baseNoise < biome.sizeThresold)
            {
                biomeIdx = i;
            }
        }

        return biomeIdx;


        //// �������� ��� ��� �����
        //float baseNoise = Mathf.PerlinNoise(x * scale, z * scale);

        //// �������������� ��� ��� ������������� ������
        //float biomeNoise = Mathf.PerlinNoise((x + 1000) * scale, (z + 1000) * scale);

        //// ������������ baseNoise ��� ������������ �����
        //baseNoise = Mathf.Clamp01(Mathf.Pow(baseNoise, 3)); // ���������� ����� �����

        //// ����� ��� ������ �������� (��������, ���� ��� �������)
        //if (baseNoise < 0.1f)
        //{
        //    return 0; // ��������� ������ ����
        //}

        //// ���������� ������
        //int biomeCount = 4; // ����������� ��� ���������� ������

        //// ������������ ��� ��� ������������ �������������
        //float step = 1.0f / biomeCount;

        //// ���������� ����
        //for (int i = 0; i < biomeCount - 1; i++)
        //{
        //    if (biomeNoise <= step * (i + 1))
        //    {
        //        return i; // ���������� ������ �����
        //    }
        //}

        //// ��������� ���� �������� ��� ����������
        //return biomeCount - 1;
    }

}
