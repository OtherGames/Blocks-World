using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Biome
    {
        public string Name;        // �������� �����
        public float Size = 20f;   // ������� ���� (���������� ������ "�����")
        public float Probability = 1f; // ��� �����, ������ �� ����������� ��� ���������
    }

    public Biome[] Biomes;

    public int GetBiomeIndex(float x, float y, float z)
    {
        if (Biomes == null || Biomes.Length == 0)
        {
            Debug.LogError("Biomes array is empty! Please configure biomes in the inspector.");
            return -1;
        }

        float maxNoiseValue = float.MinValue;
        int selectedBiomeIndex = 0;

        // ���������� ��� ����� � ��������� ��� ��� �������
        for (int i = 0; i < Biomes.Length; i++)
        {
            float scale = Biomes[i].Size;

            // ���������� ��� ��� �������� �����
            float noiseValue = Mathf.PerlinNoise(x / scale, z / scale);

            // ��������� ����������� (Probability) ��� ��������� ����
            noiseValue *= Biomes[i].Probability;

            // �������� ���� � ������������ ��������� ����
            if (noiseValue > maxNoiseValue)
            {
                maxNoiseValue = noiseValue;
                selectedBiomeIndex = i;
            }
        }

        return selectedBiomeIndex; // ���������� ������ ����� � ������������ �����
    }
}
