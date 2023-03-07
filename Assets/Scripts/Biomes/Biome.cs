using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Noise;
using UnityEngine.UIElements;

public static class Biome {

    const float epsilon = 0.001f;

    public static float[,] CreateHeatNoise(
        int mapChunkSize, int seed, float scale, Noise.NormalizeMode normalizeMode, Vector2 centre, 
        ref NoiseData noiseData, ref float[,] heightMap
    ) {

        float[,] heatMap = Noise.GenerateNoiseMap(
            mapChunkSize + 2, mapChunkSize + 2, seed + 125, scale,
            noiseData.octaves, noiseData.persistance, noiseData.lacunarity,
            centre, normalizeMode,
            noiseData.amplitude, noiseData.frequency
        );

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

    public static Texture2D CreateHeatTexture(int mapChunkSize, float[,] heatNoise, ref Gradient amplificationGradient) {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = amplificationGradient.Evaluate(heatNoise[x, y]);
            }
        }

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    public static float[,] CreateMoistureNoise(
        int mapChunkSize, int seed, float scale, Noise.NormalizeMode normalizeMode, Vector2 centre,
        ref NoiseData noiseData, ref float[,] heightMap, ref float[,] heatMap, ref AnimationCurve _moistureHeightCurve
    ) {

        AnimationCurve moistureHeightCurve = new AnimationCurve(_moistureHeightCurve.keys);
        float[,] moistureNoise = Noise.GenerateNoiseMap(
            mapChunkSize + 2, mapChunkSize + 2, seed + 255, scale,
            noiseData.octaves, noiseData.persistance, noiseData.lacunarity,
            centre, normalizeMode,
            noiseData.amplitude, noiseData.frequency
        );

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

    public static Texture2D CreateMoistureTexture(int mapChunkSize, float[,] moistureNoise, ref Gradient amplificationGradient) {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = amplificationGradient.Evaluate(moistureNoise[x, y]);
            }
        }

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    static bool CheckInRange(float heatValue, float moistureValue, float heightValue, ref Biomes biomes) {
        bool returnValue = true;

        returnValue &= heatValue <= biomes.heat + epsilon;
        returnValue &= moistureValue <= biomes.moisture + epsilon;
        returnValue &= heightValue <= biomes.height + epsilon;

        return returnValue;
    }

    public static int[,] CreateBiomesNoise(
        int mapChunkSize, Vector2 centre, ref float[,] heightMap, ref float[,] heatMap, ref float[,] moistureMap, ref Biomes[] biomes
    ) {

        int[,] biomesNoise = new int[mapChunkSize, mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                int typeBiome = biomes.Length - 1;

                for (int k = 0; k < biomes.Length - 1; k++) {
                    if (CheckInRange(heatMap[x, y], moistureMap[x, y], heightMap[x, y], ref biomes[k])) {
                        typeBiome = k;
                        break;
                    }
                }

                biomesNoise[x, y] = typeBiome;
            }
        }

        return biomesNoise;
    }

    public static Texture2D CreateBiomesTexture(int mapChunkSize, int[,] biomesMap, ref Biomes[] biomes) {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = biomes[biomesMap[x, y]].color;
            }
        }

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

}

[System.Serializable]
public struct NoiseData {
    public string name;
    public int octaves;
    public float persistance, lacunarity, amplitude, frequency;
    [Range(0, 1)]
    public float transitionValue;
}

[System.Serializable]
public struct Biomes {
    public string name;
    public Color color;
    public float heat;
    public float moisture;
    public float height;
}