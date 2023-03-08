using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh, Heat, Moisture, Biome};
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData[] noiseData;
	public TextureData textureData;
	public BiomeData biomeData;

    public Material terrainMaterial;

	[Range(0,MeshGenerator.numSupportedChunkSizes-1)]
	public int chunkSizeIndex;
	[Range(0,MeshGenerator.numSupportedFlatshadedChunkSizes-1)]
	public int flatshadedChunkSizeIndex;

	[Range(0,MeshGenerator.numSupportedLODs-1)]
	public int editorPreviewLOD;
	public bool autoUpdate;

	public Gradient amplificationGradient;
    public AnimationCurve moistureHeightCurve;
	public Shader terrainShader;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


	void Awake() {
		textureData.ApplyToMaterial (terrainMaterial);
		textureData.UpdateMeshHeights (terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor ();
		}
	}

	void OnTextureValuesUpdated() {
		Debug.Log("aici");
        terrainMaterial = new Material(terrainShader);
        textureData.ApplyToMaterial (terrainMaterial);
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
		textureData.UpdateMeshHeights (terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
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

		float[,] heightMap = Noise.GenerateNoiseMap (mapChunkSize + 2, mapChunkSize + 2, centre, noiseData[heightNoiseDataIndex]);

        float[,] heatMap = Biome.CreateHeatNoise(mapChunkSize + 2, centre, ref noiseData[heatNoiseDataIndex], ref heightMap);

        float[,] moistureMap = Biome.CreateMoistureNoise(mapChunkSize + 2, centre, ref noiseData[moistureNoiseDataIndex], ref heightMap, ref heatMap, ref moistureHeightCurve);

        int[,] biomesMap = Biome.CreateBiomesNoise(mapChunkSize + 2, ref heightMap, ref heatMap, ref moistureMap, ref biomeData);

        return new MapData (heightMap, heatMap, moistureMap, biomesMap);
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
		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
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
