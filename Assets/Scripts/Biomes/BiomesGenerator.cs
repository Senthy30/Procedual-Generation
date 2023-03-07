using UnityEngine;

/*
public class BiomesGenerator : MonoBehaviour {

    private int seed, mapChunkSize;
    private float scale, oceanLevel;

    public enum DrawMode { Biome, Heat, Moisture, Colour };
    public DrawMode drawMode;

    public bool autoUpdate;

    public Noise.NormalizeMode normalizeMode;

    public int octaves;
    public float persistance;
    public float lacunarity;
    public float amplitude;
    public float frequency;
    public Vector2Int offset;

    public Gradient heatGradient;
    public Material material;

    public AnimationCurve moistureHeightCurve;

    public NoiseData[] noiseData;
    public Biomes[] biomes;

    public void GenerateBigMap(int chunksLoaded = 3) {
        UpdateStaticValues();

        GameObject lastParent = GameObject.Find("Parent planes");
        if(lastParent) {
            DestroyImmediate(lastParent);
        }

        GameObject parent = new GameObject("Parent planes");
        parent.transform.position = Vector3.zero;
        parent.transform.localScale = Vector3.one;

        for (int y = -chunksLoaded; y <= chunksLoaded; y++) {
            for (int x = -chunksLoaded; x <= chunksLoaded; x++) {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Vector2 offsetPlane = new Vector2(mapChunkSize * x, mapChunkSize * y);
                plane.transform.position = 5 * 2 * new Vector3(offsetPlane.x, 0, offsetPlane.y);

                Renderer textureRender = plane.GetComponent<Renderer>();
                Material material_1 = new Material(material);
                textureRender.sharedMaterial = material_1;

                Texture2D texture = new Texture2D(mapChunkSize, mapChunkSize);
                if(drawMode == DrawMode.Heat)
                    texture = CreateHeatTexture(new Vector2(-offsetPlane.x, offsetPlane.y));
                else if(drawMode == DrawMode.Moisture)
                    texture = CreateMoistureTexture(new Vector2(-offsetPlane.x, offsetPlane.y));
                else if(drawMode == DrawMode.Colour)
                    texture = CreateNoiseTexture(new Vector2(-offsetPlane.x, offsetPlane.y));
                else if(drawMode == DrawMode.Biome)
                    texture = CreateBiomesTexture(new Vector2(-offsetPlane.x, offsetPlane.y));
                textureRender.sharedMaterial.mainTexture = texture;
                textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);

                plane.transform.parent = parent.transform;
            }
        }
    }

    void UpdateStaticValues() {
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        seed = mapGenerator.seed;
        mapChunkSize = mapGenerator.getMapChunkSize();
        scale = mapGenerator.noiseScale;
        oceanLevel = mapGenerator.oceanLevel;
    }

    public void DisplayResult() {
        UpdateStaticValues();

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.Heat) {
            display.DrawTexture(CreateHeatTexture(offset));
        } else if(drawMode == DrawMode.Moisture) {
            display.DrawTexture(CreateMoistureTexture(offset));
        } else if(drawMode == DrawMode.Colour) {
            display.DrawTexture(CreateNoiseTexture(offset));
        }
    }

    float GradientNoiseTransition(int indexNoise, float noiseValue, float currentHeight, float nextHeight) {

        float minAbs = Mathf.Abs(nextHeight - noiseValue);

        if (nextHeight == 1 || minAbs >= noiseData[indexNoise].transitionValue)
            noiseValue = currentHeight;
        else {
            float OldRange = noiseData[indexNoise].transitionValue;
            float NewRange = (nextHeight - currentHeight);
            float NewValue = ((minAbs * NewRange) / OldRange) + currentHeight;

            noiseValue = currentHeight + nextHeight - NewValue;
        }

        return noiseValue;
    }

    float[,] CreateHeatNoise(Vector2 centre) {
        int indexNoise = 0;
        float[,] heatNoise = Noise.GenerateNoiseMap(
            mapChunkSize + 2, mapChunkSize + 2, seed + 125, scale,
            noiseData[indexNoise].octaves, noiseData[indexNoise].persistance, noiseData[indexNoise].lacunarity, 
            centre, normalizeMode, 
            noiseData[indexNoise].amplitude, noiseData[indexNoise].frequency
        );
        
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        float[,] heightNoise = mapGenerator.getNoiseMap(centre);

        for (int y = 0; y < mapChunkSize; y++) {
            for(int x = 0; x < mapChunkSize; x++) {
                for(int k = heatRegions.Length - 1; k >= 0; k--) {
                    if (heatNoise[x, y] >= heatRegions[k].startHeight) {

                        if (noiseData[indexNoise].transitionValue != 0) {
                            float currentHeight = heatRegions[k].startHeight;
                            float nextHeight = (k == heatRegions.Length - 1) ? 1 : heatRegions[k + 1].startHeight;
                            heatNoise[x, y] = GradientNoiseTransition(indexNoise, heatNoise[x, y], currentHeight, nextHeight);
                        } else {
                            //heatNoise[x, y] = heatRegions[k].startHeight;
                        }

                        heatNoise[x, y] = Mathf.Clamp(heatNoise[x, y] - 1f / 4 * (heightNoise[x, y] * heightNoise[x, y]) * heatNoise[x, y], 0, 0.999f);

                        break;
                    }
                }
            }
        }

        return heatNoise;
    }

    Texture2D CreateHeatTexture(Vector2 centre) {
        float[,] heatNoise = CreateHeatNoise(centre);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = heatGradient.Evaluate(heatNoise[x, y]);
            }
        }

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    float[,] CreateMoistureNoise(Vector2 centre) {
        int indexNoise = 1;
        float[,] moistureNoise = Noise.GenerateNoiseMap(
            mapChunkSize + 2, mapChunkSize + 2, seed + 255, scale,
            noiseData[indexNoise].octaves, noiseData[indexNoise].persistance, noiseData[indexNoise].lacunarity,
            centre, normalizeMode,
            noiseData[indexNoise].amplitude, noiseData[indexNoise].frequency
        );
        float[,] heatNoise = CreateHeatNoise(centre);
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        float[,] heightNoise = mapGenerator.getNoiseMap(centre);

        for (int y = 0; y < mapChunkSize; y++) {
            for(int x = 0; x < mapChunkSize; x++) {
                moistureNoise[x, y] += (2f / 3 * (heatNoise[x, y] * heatNoise[x, y]) - 0.3f) * moistureNoise[x, y];
                //if (heightNoise[x, y] <= oceanLevel) moistureNoise[x, y] += Mathf.Min(0.99f, moistureNoise[x, y] + 4f * (oceanLevel - heightNoise[x, y]));
                moistureNoise[x, y] = Mathf.Clamp(moistureNoise[x, y] + moistureHeightCurve.Evaluate(heightNoise[x, y]), 0, 0.999f);
            }
        }

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                for (int k = moistureRegions.Length - 1; k >= 0; k--) {
                    if (moistureNoise[x, y] >= moistureRegions[k].startHeight) {

                        

                        break;
                    }
                }
            }
        }

        return moistureNoise;
    }

    Texture2D CreateMoistureTexture(Vector2 centre) {
        float[,] moistureNoise = CreateMoistureNoise(centre);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = heatGradient.Evaluate(moistureNoise[x, y]);
            }
        }

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    Texture2D CreateNoiseTexture(Vector2 centre) {
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        Color[] colourMap = mapGenerator.getColourNoise(centre);

        return TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize);
    }

    bool CheckInRange(int indexBiome, float heatValue, float moistureValue, float heightValue) {
        bool returnValue = true;

        returnValue &= heatValue <= biomes[indexBiome].heat;
        returnValue &= moistureValue <= biomes[indexBiome].moisture;
        returnValue &= heightValue <= biomes[indexBiome].height;

        return returnValue;
    }

    int[,] CreateBiomesNoise(Vector2 centre) {
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();

        float[,] heatNoise = CreateHeatNoise(centre);
        float[,] moistureNoise = CreateMoistureNoise(centre);
        float[,] heightNoise = mapGenerator.getNoiseMap(centre);
        int[,] biomesNoise = new int[mapChunkSize, mapChunkSize];

        bool wasPrinted = false; ;
        for(int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                int typeBiome = biomes.Length - 1;
                for(int k = 0; k < biomes.Length - 1; k++) {
                    if(CheckInRange(k, heatNoise[x, y], moistureNoise[x, y], heightNoise[x, y])) {
                        typeBiome = k;
                        break;
                    }
                }
                if(!wasPrinted && typeBiome == biomes.Length - 1) {
                    wasPrinted = true;
                    Debug.Log(new Vector3(heatNoise[x, y], moistureNoise[x, y], heightNoise[x, y]));
                }
                biomesNoise[x, y] = typeBiome;
            }
        }

        return biomesNoise;
    }

    Texture2D CreateBiomesTexture(Vector2 centre) {
        int[,] biomesNoise = CreateBiomesNoise(centre);
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                colourMap[y * mapChunkSize + x] = biomes[biomesNoise[x, y]].color;
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
*/