using System;

namespace FantasyEarth.World
{
    /// <summary>A WGS84 latitude/longitude in degrees. Longitude is normalized into [-180, 180).</summary>
    public readonly struct GeoPoint
    {
        public GeoPoint(double latitudeDeg, double longitudeDeg)
        {
            if (double.IsNaN(latitudeDeg) || double.IsInfinity(latitudeDeg) || latitudeDeg < -90.0 || latitudeDeg > 90.0)
            {
                throw new ArgumentOutOfRangeException(nameof(latitudeDeg), latitudeDeg, "Latitude must be a finite value in [-90, 90].");
            }

            if (double.IsNaN(longitudeDeg) || double.IsInfinity(longitudeDeg))
            {
                throw new ArgumentOutOfRangeException(nameof(longitudeDeg), longitudeDeg, "Longitude must be finite.");
            }

            LatitudeDeg = latitudeDeg;
            LongitudeDeg = NormalizeLongitude(longitudeDeg);
        }

        public double LatitudeDeg { get; }

        public double LongitudeDeg { get; }

        private static double NormalizeLongitude(double longitudeDeg)
        {
            double shifted = (longitudeDeg + 180.0) % 360.0;
            if (shifted < 0.0)
            {
                shifted += 360.0;
            }

            double result = shifted - 180.0;
            // Floating-point rounding can land exactly on +180; fold it back to the half-open range.
            return result >= 180.0 ? -180.0 : result;
        }
    }
}
