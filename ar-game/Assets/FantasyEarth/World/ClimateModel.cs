using System;

namespace FantasyEarth.World
{
    public sealed class ClimateModel
    {
        private static readonly DateTimeOffset DatasetTime = new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero);
        private readonly KoppenGrid _grid;

        public ClimateModel(KoppenGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public Observation<KoppenClass> ClimateAt(GeoPoint p)
        {
            KoppenClass value = _grid.ClassAt(p);
            return new Observation<KoppenClass>(value, ObservationSource.KoppenGeiger1991To2020,
                DatasetTime, value == KoppenClass.None ? 0.0 : 1.0);
        }
    }
}
