using System;

namespace FantasyEarth.World
{
    public enum ObservationSource
    {
        KoppenGeiger1991To2020,
        ArcoreGeospatial,
    }

    /// <summary>A real-world value tagged with where it came from, when, and how confident we are in it.</summary>
    public readonly struct Observation<T>
    {
        public Observation(T value, ObservationSource source, DateTimeOffset observedAt, double confidence)
        {
            if (double.IsNaN(confidence) || confidence < 0.0 || confidence > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence must be in [0, 1].");
            }

            Value = value;
            Source = source;
            ObservedAt = observedAt;
            Confidence = confidence;
        }

        public T Value { get; }

        public ObservationSource Source { get; }

        public DateTimeOffset ObservedAt { get; }

        public double Confidence { get; }
    }
}
