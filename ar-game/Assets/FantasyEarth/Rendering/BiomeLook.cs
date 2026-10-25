using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.Rendering
{
    [CreateAssetMenu(menuName = "Fantasy Earth/Biome Look")]
    public sealed class BiomeLook : ScriptableObject
    {
        public VolumeProfile Profile;
        public Color MistColor;
        [Range(0, 1)] public float MistStrength;
        public Color MoteColor;
        public float MotesPerSecond;
    }
}
