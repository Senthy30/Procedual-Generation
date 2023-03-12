using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum SizeMode { Small, Big};
	public SizeMode sizeMode;

	public enum DrawMode {NoiseMap, Mesh, Heat, Moisture, Biome};
	public DrawMode drawMode;

	public float oceanLevel;

	public TerrainData terrainData;
	public NoiseData[] noiseData;
	public BiomeData biomeData;

    public Material terrainMaterial, editorMapMaterial;

	[Range(0,MeshGenerator.numSupportedChunkSizes-1)]
	public int chunkSizeIndex;
	[Range(0,MeshGenerator.numSupportedFlatshadedChunkSizes-1)]
	public int flatshadedChunkSizeIndex;

	[Range(0,MeshGenerator.numSupportedLODs-1)]
	public int editorPreviewLOD;
	public bool autoUpdate;

	public Gradient amplificationGradient;
    public AnimationCurve moistureHeightCurve;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


	void Awake() {
        biomeData.biomes[0].height = oceanLevel;
        biomeData.biomes[1].height = oceanLevel;
        biomeData.biomes[2].height = oceanLevel;
    }

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor ();
		}
	}

	public int mapChunkSize {
		get {
			if (terrainData.useFlatShading) {
				return MeshGenerator.supportedFlatshadedChunkSizes [flatshadedChunkSizeIndex] -1;
			} else {
				return MeshGenerator.supportedChunkSizes [chunkSizeIndex] -1;
			}
		}
	}

	public void DrawMapInEditor() {
		biomeData.biomes[0].height = oceanLevel;
        biomeData.biomes[1].height = oceanLevel;
        biomeData.biomes[2].height = oceanLevel;

        if (sizeMode == SizeMode.Big) {
			GenerateBigMap();

			return;
		}

        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.Mesh) {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));

			return;
		} 

		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD,terrainData.useFlatShading));
		} else if (drawMode == DrawMode.Heat) {
            display.DrawTexture(Biome.CreateHeatTexture(mapChunkSize, mapData.heatMap, ref amplificationGradient));
        } else if (drawMode == DrawMode.Moisture) {
            display.DrawTexture(Biome.CreateMoistureTexture(mapChunkSize, mapData.moistureMap, ref amplificationGradient));
        } else if (drawMode == DrawMode.Biome) {
            display.DrawTexture(Biome.CreateBiomesTexture(mapChunkSize, mapData.biomesMap, ref biomeData));
        }
    }

	void GenerateBigMap() {
        GameObject lastParent = GameObject.Find("Parent Chunks");
        if (lastParent != null) {
            DestroyImmediate(lastParent);
        }

        GameObject parent = new GameObject("Parent Chunks");
        int sizeMapEditor = 3;
		int sizeChunk = 20;
        for (int y = -sizeMapEditor; y <= sizeMapEditor; y++) {
            for (int x = -sizeMapEditor; x <= sizeMapEditor; x++) {
                MapData mapData = GenerateMapData(new Vector2(-x, y) * mapChunkSize);

                GameObject chunkPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                chunkPlane.transform.position = new Vector3(10 * x * sizeChunk, 0, 10 * y * sizeChunk);
                chunkPlane.transform.localScale = new Vector3(sizeChunk, 1, sizeChunk);

                Texture2D texture = new Texture2D(mapChunkSize, mapChunkSize);
                if (drawMode == DrawMode.NoiseMap) {
                    texture = TextureGenerator.TextureFromHeightMap(mapData.heightMap);
                } else if (drawMode == DrawMode.Heat) {
                    texture = Biome.CreateHeatTexture(mapChunkSize, mapData.heatMap, ref amplificationGradient);
                } else if (drawMode == DrawMode.Moisture) {
                    texture = Biome.CreateMoistureTexture(mapChunkSize, mapData.moistureMap, ref amplificationGradient);
                } else if (drawMode == DrawMode.Biome) {
                    texture = Biome.CreateBiomesTexture(mapChunkSize, mapData.biomesMap, ref biomeData);
                }

                Renderer textureRender = chunkPlane.GetComponent<Renderer>();
				textureRender.material = new Material(editorMapMaterial);
                textureRender.sharedMaterial.mainTexture = texture;

                chunkPlane.transform.parent = parent.transform;
            }
        }
    }

	public void RequestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (centre, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = GenerateMapData (centre);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod,terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	MapData GenerateMapData(Vector2 centre) {
        int heightNoiseDataIndex = 0;
        int heatNoiseDataIndex = 1;
		int moistureNoiseDataIndex = 2;

		int baseSeed = noiseData[heightNoiseDataIndex].seed;
		noiseData[heatNoiseDataIndex].seed = baseSeed + 127;
		noiseData[moistureNoiseDataIndex].seed = baseSeed + 255;

		float[,] heightMap = CreateNoiseMap(centre, heightNoiseDataIndex);

        float[,] heatMap = Biome.CreateHeatNoise(mapChunkSize + 2, centre, ref noiseData[heatNoiseDataIndex], ref heightMap);

        float[,] moistureMap = Biome.CreateMoistureNoise(mapChunkSize + 2, centre, ref noiseData[moistureNoiseDataIndex], ref heightMap, ref heatMap, ref moistureHeightCurve);

        int[,] biomesMap = Biome.CreateBiomesNoise(mapChunkSize + 2, ref heightMap, ref heatMap, ref moistureMap, ref biomeData);

        return new MapData (heightMap, heatMap, moistureMap, biomesMap);
	}

	int[] dx = new int[4] {-1, 0, 1, 0};
    int[] dy = new int[4] {0, 1, 0, -1};

    float[,] CreateNoiseMap(Vector2 centre, int heightNoiseDataIndex) {
        float[,] baseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, centre, noiseData[heightNoiseDataIndex]);

		int flatNoiseDataIndex = 3;
		noiseData[flatNoiseDataIndex].seed = noiseData[heightNoiseDataIndex].seed + 63;

		float[,] flatMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, centre, noiseData[flatNoiseDataIndex]);

		int lenghtRows = flatMap.GetLength(0);
		int lenghtCols = flatMap.GetLength(1);

		int maxINTDistance = 2000000;
        Tuple<int, float>[,] mapDistances = new Tuple<int, float>[lenghtCols, lenghtRows];

        for (int x = 0; x < lenghtRows; x++) {
            for (int y = 0; y < lenghtCols; y++)
                mapDistances[x, y] = new Tuple<int, float>(maxINTDistance, 0);
        }

        for (int x = 0; x < lenghtRows; x++) {
            for (int y = 0; y < lenghtCols; y++) {
				float oldValue = flatMap[x, y];
                flatMap[x, y] = Mathf.Lerp(oceanLevel, oceanLevel + 0.3f, baseMap[x, y]);

                if (baseMap[x, y] <= oceanLevel || oldValue <= noiseData[flatNoiseDataIndex].minLevel) {					
					mapDistances[x, y] = new Tuple<int, float>(0, baseMap[x, y]);
					flatMap[x, y] = 0;
                } else {
                    baseMap[x, y] = flatMap[x, y];
                }
            }
        }

        Queue<Vector2Int> Q = new Queue<Vector2Int>();
        bool[,] inQ = new bool[lenghtRows, lenghtCols];

        for (int x = 0; x < lenghtRows; x++) {
			for (int y = 0; y < lenghtCols; y++) {

				if (flatMap[x, y] != 0)
					continue;

                for (int d = 0; d < 4; d++) {
                    Vector2Int nextPos = new Vector2Int(x + dx[d], y + dy[d]);

                    if (nextPos.x < 0 || nextPos.x >= lenghtCols || nextPos.y < 0 || nextPos.y >= lenghtRows)
                        continue;

					if (flatMap[nextPos.x, nextPos.y] != 0) {
						Q.Enqueue(new Vector2Int(x, y));
						inQ[x, y] = true;
						break;
					}
                }
            }
		}

		int maxDistance = 20;
		int maxIteration = lenghtCols * lenghtRows * 5;
		int currentIteration = 0;

		int[,] freq = new int[lenghtRows, lenghtCols];

        while (Q.Count > 0) {
            Vector2Int currentPos = Q.Dequeue();
			inQ[currentPos.x, currentPos.y] = false;
			freq[currentPos.x, currentPos.y]++;

			currentIteration++;
			if(currentIteration >= maxIteration) {
				break;
			}

            if (mapDistances[currentPos.x, currentPos.y].Item1 + 1 > maxDistance)
				continue;

            for (int d = 0; d < 4; d++) {
				Vector2Int nextPos = new Vector2Int(currentPos.x + dx[d], currentPos.y + dy[d]);

				if (nextPos.x < 0 || nextPos.x >= lenghtCols || nextPos.y < 0 || nextPos.y >= lenghtRows)
					continue;

				if (mapDistances[nextPos.x, nextPos.y].Item1 < mapDistances[currentPos.x, currentPos.y].Item1 + 1)
					continue;

				mapDistances[nextPos.x, nextPos.y] = new Tuple<int, float>(mapDistances[currentPos.x, currentPos.y].Item1 + 1, mapDistances[currentPos.x, currentPos.y].Item2);

				if (!inQ[nextPos.x, nextPos.y]) {
					Q.Enqueue(nextPos);
					inQ[nextPos.x, nextPos.y] = true;
				}
			}
		}   

		for (int y = 0; y < lenghtRows; y++) {
			for(int x = 0; x < lenghtCols; x++) {
				if (mapDistances[x, y].Item1 > 0 && mapDistances[x, y].Item1 != maxINTDistance) {
					float newValue = mapDistances[x, y].Item2 + (flatMap[x, y] - mapDistances[x, y].Item2) * Mathf.Pow((float)mapDistances[x, y].Item1 / maxDistance, 1);
					baseMap[x, y] = newValue;
				}
			}
		}

        Debug.Log(currentIteration);
        Debug.Log(maxIteration);

        Q.Clear();

        return baseMap;
    }

	float ConvertToRange(float oldValue, float oldMin, float oldMax, float newMin, float newMax) {
		float oldRange = (oldMax - oldMin);
		float newRange = (newMax - newMin);

		return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
    }

	void OnValidate() {

		if (terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}
		for(int k = 0; k < noiseData.Length; k++) {
            if (noiseData != null) {
				noiseData[k].OnValuesUpdated -= OnValuesUpdated;
				noiseData[k].OnValuesUpdated += OnValuesUpdated;
            }
        }

	}

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}

}
	

public struct MapData {
	public readonly float[,] heightMap;
    public readonly float[,] heatMap;
    public readonly float[,] moistureMap;
    public readonly int[,] biomesMap;

    public MapData (float[,] heightMap, float[,] heatMap, float[,] moistureMap, int[,] biomesMap) {
		this.heightMap = heightMap;
		this.heatMap = heatMap;
		this.moistureMap = moistureMap;
		this.biomesMap = biomesMap;
	}
}
