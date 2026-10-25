using System;

namespace FantasyEarth.World
{
    public readonly struct LocalPoint
    {
        public LocalPoint(double eastM, double upM, double northM)
        {
            if (!PlanarPoint.Finite(eastM) || !PlanarPoint.Finite(upM) || !PlanarPoint.Finite(northM))
                throw new ArgumentOutOfRangeException(nameof(eastM));
            EastM = eastM; UpM = upM; NorthM = northM;
        }
        public double EastM { get; }
        public double UpM { get; }
        public double NorthM { get; }
        public PlanarPoint Planar => new PlanarPoint(EastM, NorthM);
    }

    public readonly struct GeoPosition
    {
        public GeoPosition(GeoPoint point, double altitudeM) { Point = point; AltitudeM = altitudeM; }
        public GeoPoint Point { get; }
        public double AltitudeM { get; }
    }

    /// <summary>WGS84 geodetic/ECEF conversion followed by a fixed local east/up/north tangent frame.</summary>
    public sealed class LocalGeoFrame
    {
        private const double A = 6378137.0;
        private const double F = 1.0 / 298.257223563;
        private const double E2 = F * (2 - F);
        private const double B = A * (1 - F);
        private const double Radians = Math.PI / 180;
        private readonly double _sinLat, _cosLat, _sinLon, _cosLon, _x, _y, _z;

        public LocalGeoFrame(GeoPoint origin, double altitudeM = 0)
        {
            if (!PlanarPoint.Finite(altitudeM)) throw new ArgumentOutOfRangeException(nameof(altitudeM));
            Origin = origin; OriginAltitudeM = altitudeM;
            _sinLat = Math.Sin(origin.LatitudeDeg * Radians); _cosLat = Math.Cos(origin.LatitudeDeg * Radians);
            _sinLon = Math.Sin(origin.LongitudeDeg * Radians); _cosLon = Math.Cos(origin.LongitudeDeg * Radians);
            Ecef(origin, altitudeM, out _x, out _y, out _z);
        }
        public GeoPoint Origin { get; }
        public double OriginAltitudeM { get; }

        public LocalPoint Project(GeoPoint point, double altitudeM = 0)
        {
            if (!PlanarPoint.Finite(altitudeM)) throw new ArgumentOutOfRangeException(nameof(altitudeM));
            Ecef(point, altitudeM, out double x, out double y, out double z);
            double dx = x - _x, dy = y - _y, dz = z - _z;
            return new LocalPoint(-_sinLon * dx + _cosLon * dy,
                _cosLat * _cosLon * dx + _cosLat * _sinLon * dy + _sinLat * dz,
                -_sinLat * _cosLon * dx - _sinLat * _sinLon * dy + _cosLat * dz);
        }

        public GeoPosition Unproject(LocalPoint point)
        {
            double x = _x - _sinLon * point.EastM + _cosLat * _cosLon * point.UpM - _sinLat * _cosLon * point.NorthM;
            double y = _y + _cosLon * point.EastM + _cosLat * _sinLon * point.UpM - _sinLat * _sinLon * point.NorthM;
            double z = _z + _sinLat * point.UpM + _cosLat * point.NorthM;
            double p = Math.Sqrt(x * x + y * y);
            if (p < 1e-9 && Math.Abs(z) < 1e-9) throw new ArgumentException("Earth's centre has no geodetic position.", nameof(point));
            double theta = Math.Atan2(z * A, p * B), sin = Math.Sin(theta), cos = Math.Cos(theta);
            double ep2 = (A * A - B * B) / (B * B);
            double latitude = Math.Atan2(z + ep2 * B * sin * sin * sin, p - E2 * A * cos * cos * cos);
            double sinLat = Math.Sin(latitude), cosLat = Math.Cos(latitude);
            double radius = A / Math.Sqrt(1 - E2 * sinLat * sinLat);
            double altitude = Math.Abs(cosLat) > 1e-8 ? p / cosLat - radius : Math.Abs(z) - B;
            return new GeoPosition(new GeoPoint(latitude / Radians, Math.Atan2(y, x) / Radians), altitude);
        }

        private static void Ecef(GeoPoint point, double altitude, out double x, out double y, out double z)
        {
            double lat = point.LatitudeDeg * Radians, lon = point.LongitudeDeg * Radians;
            double sin = Math.Sin(lat), cos = Math.Cos(lat), radius = A / Math.Sqrt(1 - E2 * sin * sin);
            x = (radius + altitude) * cos * Math.Cos(lon);
            y = (radius + altitude) * cos * Math.Sin(lon);
            z = (radius * (1 - E2) + altitude) * sin;
        }
    }
}
