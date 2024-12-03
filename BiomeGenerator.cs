using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Biome
    {
        public string Name;        // Название биома
        public float Size = 20f;   // Масштаб шума (определяет размер "пятен")
        public float Probability = 1f; // Вес биома, влияет на вероятность его появления
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

        // Перебираем все биомы и вычисляем шум для каждого
        for (int i = 0; i < Biomes.Length; i++)
        {
            float scale = Biomes[i].Size;

            // Генерируем шум для текущего биома
            float noiseValue = Mathf.PerlinNoise(x / scale, z / scale);

            // Учитываем вероятность (Probability) как множитель шума
            noiseValue *= Biomes[i].Probability;

            // Выбираем биом с максимальным значением шума
            if (noiseValue > maxNoiseValue)
            {
                maxNoiseValue = noiseValue;
                selectedBiomeIndex = i;
            }
        }

        return selectedBiomeIndex; // Возвращаем индекс биома с максимальным шумом
    }
}
