using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {

	public string name;
	public Noise.NormalizeMode normalizeMode;

	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;
	public float amplitude;
	public float frequency;


    public int seed;
	public Vector2 offset;

	#if UNITY_EDITOR

	protected override void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}

		base.OnValidate ();
	}
	#endif

}
