using System;
using FantasyEarth.Diagnostics;
using FantasyEarth.Fantasy;
using FantasyEarth.Positioning;
using FantasyEarth.World;
using UnityEngine;

namespace FantasyEarth.App
{
    public sealed class WorldPresenter : MonoBehaviour
    {
        [SerializeField] private GeospatialSampler sampler;
        [SerializeField] private ClimateService climate;
        [SerializeField] private PaintedLookController look;
        [SerializeField] private SessionLogger logger;
        private readonly AlignmentGate _gate = new AlignmentGate(AlignmentThresholds.Default);
        private Biome _biome = Biome.Temperate;
        private bool _hasEntry;

        public SessionLogEntry LastEntry { get; private set; }

        private void Start() => look.Apply(Biome.Temperate);

        private void Update()
        {
            _gate.Update(sampler.Current, Time.deltaTime);
            KoppenClass koppen = sampler.Position.HasValue ? climate.ClimateAt(sampler.Position.Value).Value : KoppenClass.None;
            Biome biome = BiomeRules.FromKoppen(koppen);
            if (biome != _biome) { look.Apply(biome); _biome = biome; }
            bool stateChanged = !_hasEntry || LastEntry.Alignment != _gate.State;
            LastEntry = new SessionLogEntry(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sampler.Position,
                sampler.Current.HorizontalAccuracyM, sampler.Current.YawAccuracyDeg, sampler.EarthStateName,
                sampler.Current.IsTracking, _gate.State, _gate.RebuiltBlend, koppen, biome);
            _hasEntry = true;
            logger.Record(LastEntry, stateChanged);
        }
    }
}
