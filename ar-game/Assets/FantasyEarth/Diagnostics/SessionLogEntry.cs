using FantasyEarth.Fantasy;
using FantasyEarth.Positioning;
using FantasyEarth.World;

namespace FantasyEarth.Diagnostics
{
    public readonly struct SessionLogEntry
    {
        public SessionLogEntry(long unixTimeMs, GeoPoint? position, double horizontalAccuracyM,
            double yawAccuracyDeg, string earthState, bool isTracking, AlignmentState alignment,
            float rebuiltBlend, KoppenClass koppen, Biome biome)
        {
            UnixTimeMs = unixTimeMs;
            Position = position;
            HorizontalAccuracyM = horizontalAccuracyM;
            YawAccuracyDeg = yawAccuracyDeg;
            EarthState = earthState;
            IsTracking = isTracking;
            Alignment = alignment;
            RebuiltBlend = rebuiltBlend;
            Koppen = koppen;
            Biome = biome;
        }

        public long UnixTimeMs { get; }
        public GeoPoint? Position { get; }
        public double HorizontalAccuracyM { get; }
        public double YawAccuracyDeg { get; }
        public string EarthState { get; }
        public bool IsTracking { get; }
        public AlignmentState Alignment { get; }
        public float RebuiltBlend { get; }
        public KoppenClass Koppen { get; }
        public Biome Biome { get; }
    }
}
