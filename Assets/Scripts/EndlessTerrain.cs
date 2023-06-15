using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class EndlessTerrain : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
	const float colliderGenerationDistanceThreshold = 5;

	public int colliderLODIndex;
	public LODInfo[] detailLevels;
	public static float maxViewDst;

	public Transform viewer;
	public Material mapMaterial;

    public GameObject waterPrefab;
    public Transform waterParent;

    public static GameObject _waterPrefab;
	public static Transform _waterParent;

	public TerrainData terrainData;

	public GameObject[] treeGameObject;

	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	static MapGenerator mapGenerator;
	int chunkSize;
	int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	public static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start() {
		mapGenerator = FindObjectOfType<MapGenerator> ();

		maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		chunkSize = mapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

		UpdateVisibleChunks ();

		_waterPrefab = waterPrefab;
		_waterParent = waterParent;
	}

	void Update() {
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
	}
		
	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedTerrainChunkCoords = new HashSet<Vector2> ();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
            alreadyUpdatedTerrainChunkCoords.Add (visibleTerrainChunks [i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk ();
        }

        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedTerrainChunkCoords.Contains (viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						terrainChunkDictionary.Add (viewedChunkCoord, 
							new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial, terrainData, treeGameObject)
						);
					}
                }
			}
		}
	}

	public class TerrainChunk {

		public static int chunkSize = -1;
		public static BiomeData biomeData = null;
		public static Gradient amplificationGradient = null;
		public static MapGenerator.DrawMode drawMode;

		private static GameObject[] treeGameObject;

		public bool resourcesComplete;
		public bool existWaterComplete;
		public bool existWaterReceived;
		public bool existWater;

        public Vector2 coord;

		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

		MeshRenderer meshRenderer;
		MeshFilter meshFilter;
		MeshCollider meshCollider;

		LODInfo[] detailLevels;
		LODMesh[] lodMeshes;
		int colliderLODIndex;

		MapData mapData;
		bool mapDataReceived;
		int previousLODIndex = -1;
		bool hasSetCollider;

		public GameObject waterObject;
		public TerrainData terrainData;

		void UpdateStaticValues(GameObject[] trGameObject) {
			mapGenerator = FindObjectOfType<MapGenerator>();
			drawMode = mapGenerator.drawMode;
			biomeData = mapGenerator.biomeData;
			amplificationGradient = mapGenerator.amplificationGradient;
			chunkSize = mapGenerator.mapChunkSize;
            treeGameObject = trGameObject;
        }

		public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material, TerrainData terrainData, GameObject[] trGameObject) {
			if(biomeData == null)
				UpdateStaticValues(trGameObject);
			
			this.coord = coord;
			this.detailLevels = detailLevels;
			this.colliderLODIndex = colliderLODIndex;
			this.terrainData = terrainData;

			position = coord * size;
			bounds = new Bounds(position,Vector2.one * size);
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

			meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
			meshCollider = meshObject.AddComponent<MeshCollider>();

			meshRenderer.material = material;
            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
			meshObject.transform.parent = parent;
			meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
			SetVisible(false);

			lodMeshes = new LODMesh[detailLevels.Length];
			for (int i = 0; i < detailLevels.Length; i++) {
				lodMeshes[i] = new LODMesh(detailLevels[i].lod);
				lodMeshes[i].updateCallback += UpdateTerrainChunk;
				if (i == colliderLODIndex) {
					lodMeshes[i].updateCallback += UpdateCollisionMesh;
				}
			}

			mapGenerator.RequestMapData(position,OnMapDataReceived);
        }

		void OnMapDataReceived(MapData mapData) {
			this.mapData = mapData;
			mapDataReceived = true;

			Color[] colorMap = null;
			if(drawMode == MapGenerator.DrawMode.Heat)
				colorMap = Biome.CreateHeatColor(chunkSize, mapData.heatMap, ref amplificationGradient);
			else if(drawMode == MapGenerator.DrawMode.Moisture)
                colorMap = Biome.CreateMoistureColor(chunkSize, mapData.moistureMap, ref amplificationGradient);
			else if(drawMode == MapGenerator.DrawMode.Biome)
                colorMap = Biome.CreateBiomesColor(chunkSize, mapData.biomesMap, ref biomeData);
			else if(drawMode == MapGenerator.DrawMode.NoiseMap) {
                Texture2D texture = TextureGenerator.TextureFromHeightMap(mapData.heightMap);
                meshRenderer.material.mainTexture = texture;
            }

			if (colorMap != null) {
				Texture2D texture = TextureGenerator.TextureFromColourMap(colorMap, chunkSize, chunkSize);
				meshRenderer.material.mainTexture = texture;
			}

            UpdateTerrainChunk ();
		}

		public void UpdateTerrainChunk() {
			if (mapDataReceived) {
				if (!existWaterReceived) {
					for (int x = 0; x < mapData.heightMap.GetLength(0); x++) {
						for (int y = 0; y < mapData.heightMap.GetLength(1); y++) {
							if (mapData.heightMap[x, y] < mapGenerator.oceanLevel) {
								existWater = true;
                                break;
							}
						}

						if (existWater)
							break;
					}
					existWaterReceived = true;

                    if (!existWater)
						existWaterComplete = true;
                }

				WaterChunk();
				ResourcesChunck();

               
                float viewerDstFromNearestEdge = Mathf.Sqrt (bounds.SqrDistance (viewerPosition));

				bool wasVisible = IsVisible ();
				bool visible = viewerDstFromNearestEdge <= maxViewDst;

				if (visible) {
					int lodIndex = 0;

					for (int i = 0; i < detailLevels.Length - 1; i++) {
						if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) {
							lodIndex = i + 1;
						} else {
							break;
						}
					}

					if (lodIndex != previousLODIndex) {
						LODMesh lodMesh = lodMeshes [lodIndex];
						if (lodMesh.hasMesh) {
							previousLODIndex = lodIndex;
							meshFilter.mesh = lodMesh.mesh;
						} else if (!lodMesh.hasRequestedMesh) {
							lodMesh.RequestMesh (mapData);
						}
					}


				}

				if (wasVisible != visible) {
					if (visible) {
						visibleTerrainChunks.Add (this);
					} else {
						visibleTerrainChunks.Remove (this);
					}
					SetVisible (visible);
				}
			}
		}

		public void UpdateCollisionMesh() {
			if (!hasSetCollider) {
				float sqrDstFromViewerToEdge = bounds.SqrDistance (viewerPosition);

				if (sqrDstFromViewerToEdge < detailLevels [colliderLODIndex].sqrVisibleDstThreshold) {
					if (!lodMeshes [colliderLODIndex].hasRequestedMesh) {
						lodMeshes [colliderLODIndex].RequestMesh (mapData);
					}
				}

				if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
					if (lodMeshes [colliderLODIndex].hasMesh) {
						meshCollider.sharedMesh = lodMeshes [colliderLODIndex].mesh;
						hasSetCollider = true;
					}
				}
			}
		}

        GameObject SpawnOakBush(int x, int y, TreeTypes treeTypes, TreeTypes mushrooms, ref Vector2Int posMushroom) {
            int length = treeTypes.prob.Length;
            int stIdx = treeTypes.startIdx;
            int maxValue = treeTypes.prob[length - 1];

            int type = 0;
            int randomValue = (Mathf.Abs(x) + Mathf.Abs(y)) % (maxValue + 1);

            for (int i = 0; i < length; i++)
                if (randomValue <= treeTypes.prob[i]) {
                    type = stIdx + i;
                    break;
                }

            if (type < 11) {
                randomValue = (Mathf.Abs(x) + Mathf.Abs(y)) * 1001593 % 100;

                if (randomValue > 70) {
                    int valX = (Mathf.Abs(x) + Mathf.Abs(y)) * 1001593 % 2;
                    int valY = (Mathf.Abs(2 * x) + Mathf.Abs(2 * y)) * 1001593 % 2;

                    if (valX == 0)
                        valX = -1;
                    if (valY == 0)
                        valY = -1;

                    posMushroom = new Vector2Int(valX, valY);
                }
            } else if (type == 11) {
                randomValue = (Mathf.Abs(x) + Mathf.Abs(y)) * 1001593 % 100;

                if (randomValue <= 95)
                    return null;
            }

            GameObject tree = Instantiate(treeGameObject[type], meshObject.transform);

            randomValue = (Mathf.Abs(x) + Mathf.Abs(y)) * 1001593 % 360;
            tree.transform.rotation = Quaternion.Euler(
                    tree.transform.rotation.eulerAngles.x,
                    randomValue,
                    tree.transform.rotation.eulerAngles.z
                );

            return tree;
        }

        public void ResourcesChunck() {
            if (resourcesComplete)
                return;
            resourcesComplete = true;

            TreeTypes[] treeTypes = new TreeTypes[3] {
                new TreeTypes(0, new int[7] {7, 14, 18, 20, 21, 22, 23}),
                new TreeTypes(7, new int[4] {4, 8, 14, 17}),
                new TreeTypes(11, new int[1] {10})
            };

            TreeTypes mushrooms = new TreeTypes(12, new int[1] { 10 });

            int stIdx = 5;
            int lengthR = mapData.treesMap.GetLength(0);
            int lengthC = mapData.treesMap.GetLength(1);
            for (int x = stIdx; x < lengthR; x++) {
                for (int y = stIdx; y < lengthC; y++) {
                    if (mapData.treesMap[x, y] != 1)
                        continue;

                    float height = terrainData.meshHeightCurve.Evaluate(mapData.heightMap[x, y]) * terrainData.meshHeightMultiplier;
                    GameObject obj = null;
                    Vector2Int posMushroom = new Vector2Int(0, 0);

                    if (mapData.biomesMap[x, y] == 7) {
                        obj = SpawnOakBush(x, y, treeTypes[0], mushrooms, ref posMushroom);
                    } else if (mapData.biomesMap[x, y] == 5) {
                        obj = SpawnOakBush(x, y, treeTypes[1], mushrooms, ref posMushroom);
                    } else if (mapData.biomesMap[x, y] == 8) {
                        obj = SpawnOakBush(x, y, treeTypes[2], mushrooms, ref posMushroom);
                    }

                    if (obj != null) {
                        obj.transform.localPosition = new Vector3(-lengthR / 2 + x, height, lengthC / 2 - y);

                        if (posMushroom.x != 0 || posMushroom.y != 0) {
							Debug.Log(posMushroom);
                            GameObject obj2 = Instantiate(treeGameObject[mushrooms.startIdx], meshObject.transform);
                            obj2.transform.localPosition = new Vector3(-lengthR / 2 + x + posMushroom.x, height + 0.05f, lengthC / 2 - y + posMushroom.y);
                        }
                    }
                }
            }
        }

        public void WaterChunk() {
			if (existWaterComplete || !existWaterReceived || (existWaterReceived && !existWater))
				return;

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            waterObject = Instantiate(_waterPrefab);
            waterObject.transform.localScale = 3.32f * Vector3.one * mapGenerator.terrainData.uniformScale;
            waterObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale + new Vector3(0, 24.5f, 0);

            Material material = new Material(waterObject.GetComponent<Renderer>().material);
            material.SetVector("_Offset_World", coord);
			material.SetFloat("Vector1_6269b1025b26473ca8bc61634f34b537", DayNightCycle.GetSmoothnessCurveAtCurrentTime());
            waterObject.GetComponent<Renderer>().material = material;

            waterObject.transform.parent = _waterParent.transform;
			waterObject.gameObject.SetActive(false);
			existWaterComplete = true;
        }

        public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
			if (waterObject != null) {
				waterObject.SetActive(visible);
			}
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}

	}

	class LODMesh {

		public Mesh mesh;
		public bool hasRequestedMesh;
		public bool hasMesh;
		int lod;
		public event System.Action updateCallback;

		public LODMesh(int lod) {
			this.lod = lod;
		}

		void OnMeshDataReceived(MeshData meshData) {
			mesh = meshData.CreateMesh ();
			hasMesh = true;

			updateCallback ();
		}

		public void RequestMesh(MapData mapData) {
			hasRequestedMesh = true;
			mapGenerator.RequestMeshData (mapData, lod, OnMeshDataReceived);
		}

	}

	[System.Serializable]
	public struct LODInfo {
		[Range(0,MeshGenerator.numSupportedLODs-1)]
		public int lod;
		public float visibleDstThreshold;


		public float sqrVisibleDstThreshold {
			get {
				return visibleDstThreshold * visibleDstThreshold;
			}
		}
	}

	[System.Serializable]
	public struct TreeTypes {
		public int startIdx;
		public int[] prob;

		public TreeTypes(int startIdx, int[] prob) {
			this.startIdx = startIdx;
			this.prob = prob;
		}
	}

}
