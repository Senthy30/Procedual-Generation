using System.Collections.Generic;
using UnityEngine;

public static class Biome {

    const float epsilon = 0.001f;

    public static float[,] CreateHeatNoise(int mapChunkSize, Vector2 centre, ref NoiseData noiseData, ref float[,] heightMap) {

        float[,] heatMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, centre, noiseData);

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                heatMap[x, y] = Mathf.Clamp01(heatMap[x, y]);
                //heatMap[x, y] = Mathf.Clamp(heatMap[x, y] - 1f / 4 * (heightMap[x, y] * heightMap[x, y]) * heatMap[x, y], 0, 0.999f);

                float roundTo = ((int)(heatMap[x, y] * 10)) / 10f;
                heatMap[x, y] = roundTo;
            }
        }

        return heatMap;
    }

    public static Color[] CreateHeatColor(int mapChunkSize, float[,] heatNoise, ref Gradient amplificationGradient) {
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colorMap[y * mapChunkSize + x] = amplificationGradient.Evaluate(heatNoise[x, y]);
            }
        }

        return colorMap;
    }

    public static Texture2D CreateHeatTexture(int mapChunkSize, float[,] heatNoise, ref Gradient amplificationGradient) {
        Color[] colourMap = CreateHeatColor(mapChunkSize, heatNoise, ref amplificationGradient);

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    public static float[,] CreateMoistureNoise(
        int mapChunkSize, Vector2 centre,
        ref NoiseData noiseData, ref float[,] heightMap, ref float[,] heatMap, ref AnimationCurve _moistureHeightCurve
    ) {

        AnimationCurve moistureHeightCurve = new AnimationCurve(_moistureHeightCurve.keys);
        float[,] moistureNoise = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, centre, noiseData);

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                moistureNoise[x, y] += (2f / 3 * (heatMap[x, y] * heatMap[x, y]) - 0.3f) * moistureNoise[x, y];
                moistureNoise[x, y] = Mathf.Clamp01(moistureNoise[x, y] + moistureHeightCurve.Evaluate(heightMap[x, y]));
            }
        }

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                float roundTo = ((int)(moistureNoise[x, y] * 10)) / 10f;
                moistureNoise[x, y] = roundTo;
            }
        }

        return moistureNoise;
    }

    public static Color[] CreateMoistureColor(int mapChunkSize, float[,] moistureNoise, ref Gradient amplificationGradient) {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = amplificationGradient.Evaluate(moistureNoise[x, y]);
            }
        }

        return colourMap;
    }

    public static Texture2D CreateMoistureTexture(int mapChunkSize, float[,] moistureNoise, ref Gradient amplificationGradient) {
        Color[] colourMap = CreateMoistureColor(mapChunkSize, moistureNoise, ref amplificationGradient);

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    static bool CheckInRange(int k, ref BiomeData biomeData, float heatValue, float moistureValue, float heightValue) {
        bool returnValue = true;

        returnValue &= heatValue <= biomeData.biomes[k].heat + epsilon;
        returnValue &= moistureValue <= biomeData.biomes[k].moisture + epsilon;
        returnValue &= heightValue <= biomeData.biomes[k].height + epsilon;

        return returnValue;
    }

    public static int[,] CreateBiomesNoise(
        int mapChunkSize, ref float[,] heightMap, ref float[,] heatMap, ref float[,] moistureMap, ref BiomeData biomeData
    ) {

        int[,] biomesNoise = new int[mapChunkSize, mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                int lengthBiomes = biomeData.biomes.Length - 1;
                int typeBiome = lengthBiomes;

                for (int k = 0; k <= lengthBiomes; k++) {
                    if (CheckInRange(k, ref biomeData, heatMap[x, y], moistureMap[x, y], heightMap[x, y])) {
                        typeBiome = k;
                        break;
                    }
                }

                biomesNoise[x, y] = typeBiome;
            }
        }

        return biomesNoise;
    }

    public static Color[] CreateBiomesColor(int mapChunkSize, int[,] biomesMap, ref BiomeData biomeData) {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = biomeData.biomes[biomesMap[x, y]].color;
            }
        }

        return colourMap;
    }

    public static Texture2D CreateBiomesTexture(int mapChunkSize, int[,] biomesMap, ref BiomeData biomeData) {
        Color[] colourMap = CreateBiomesColor(mapChunkSize, biomesMap, ref biomeData);

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

}