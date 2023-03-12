using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class BiomeData : UpdatableData {

    public Biome[] biomes;

    [System.Serializable]
    public struct Biome {
        public string name;
        public Color color;
        public float heat;
        public float moisture;
        public float height;
    }

}
