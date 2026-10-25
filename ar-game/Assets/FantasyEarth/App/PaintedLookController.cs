using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.App
{
    public sealed class PaintedLookController : MonoBehaviour
    {
        private static readonly int MistColor = Shader.PropertyToID("_FE_MistColor");
        private static readonly int MistStrength = Shader.PropertyToID("_FE_MistStrength");
        [SerializeField] private BiomeLookLibrary library;
        [SerializeField] private Volume volume;
        [SerializeField] private ParticleSystem motes;

        public void Apply(Biome biome)
        {
            if (library == null || !library.TryGet(biome, out var look)) return;
            volume.sharedProfile = look.Profile;
            Shader.SetGlobalColor(MistColor, look.MistColor);
            Shader.SetGlobalFloat(MistStrength, look.MistStrength);
            var main = motes.main;
            main.startColor = look.MoteColor;
            var emission = motes.emission;
            emission.rateOverTime = look.MotesPerSecond;
        }
    }
}
