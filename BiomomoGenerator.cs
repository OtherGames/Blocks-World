using UnityEngine;

public class BiomomoGenerator : MonoBehaviour
{
    [System.Serializable]
    public class Biome
    {
        public string name;
        public float frequency; // Вес (частота) биома
        [Range(0.0001f, 0.999f)]
        public float sizeThresold;
        public float noiseOffset;
    }

    public Biome[] biomes;
    public float scale = 0.1f; // Масштаб шума (уровень детализации)


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


        //// Основной шум для пятен
        //float baseNoise = Mathf.PerlinNoise(x * scale, z * scale);

        //// Дополнительный шум для распределения биомов
        //float biomeNoise = Mathf.PerlinNoise((x + 1000) * scale, (z + 1000) * scale);

        //// Нормализация baseNoise для формирования пятен
        //baseNoise = Mathf.Clamp01(Mathf.Pow(baseNoise, 3)); // Регулирует форму пятен

        //// Маска для пустых участков (например, вода или пустошь)
        //if (baseNoise < 0.1f)
        //{
        //    return 0; // Указываем пустой биом
        //}

        //// Количество биомов
        //int biomeCount = 4; // Настраиваем под количество биомов

        //// Рассчитываем шаг для равномерного распределения
        //float step = 1.0f / biomeCount;

        //// Определяем биом
        //for (int i = 0; i < biomeCount - 1; i++)
        //{
        //    if (biomeNoise <= step * (i + 1))
        //    {
        //        return i; // Возвращаем индекс биома
        //    }
        //}

        //// Последний биом получает все оставшееся
        //return biomeCount - 1;
    }

}
