using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public readonly struct PlanarPoint
    {
        public PlanarPoint(double eastM, double northM)
        {
            if (!Finite(eastM) || !Finite(northM)) throw new ArgumentOutOfRangeException(nameof(eastM));
            EastM = eastM; NorthM = northM;
        }
        public double EastM { get; }
        public double NorthM { get; }
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>A simple outer ring in a local east/north frame, in metres. No courtyard holes.</summary>
    public sealed class BuildingFootprint
    {
        public BuildingFootprint(string stableId, IReadOnlyList<PlanarPoint> outline, double heightM)
        {
            if (string.IsNullOrWhiteSpace(stableId)) throw new ArgumentException("A stable building ID is required.", nameof(stableId));
            if (!PlanarPoint.Finite(heightM) || heightM <= 0 || heightM > 500) throw new ArgumentOutOfRangeException(nameof(heightM));
            if (outline == null) throw new ArgumentNullException(nameof(outline));
            int count = outline.Count;
            if (count > 1 && Same(outline[0], outline[count - 1])) count--;
            if (count < 3 || count > 64) throw new ArgumentException("A footprint needs 3–64 vertices.", nameof(outline));
            var points = new PlanarPoint[count];
            double minX = double.MaxValue, minZ = double.MaxValue, maxX = double.MinValue, maxZ = double.MinValue;
            for (int i = 0; i < count; i++)
            {
                points[i] = outline[i];
                if (Math.Abs(points[i].EastM) > 100000 || Math.Abs(points[i].NorthM) > 100000)
                    throw new ArgumentException("Recenter the footprint into a local metric frame.", nameof(outline));
                minX = Math.Min(minX, points[i].EastM); maxX = Math.Max(maxX, points[i].EastM);
                minZ = Math.Min(minZ, points[i].NorthM); maxZ = Math.Max(maxZ, points[i].NorthM);
                if (Same(points[i], outline[(i + 1) % count])) throw new ArgumentException("Zero-length footprint edge.", nameof(outline));
            }
            for (int i = 0; i < count; i++)
                for (int j = i + 1; j < count; j++)
                {
                    if (j == i + 1 || (i == 0 && j == count - 1)) continue;
                    if (Intersects(points[i], points[(i + 1) % count], points[j], points[(j + 1) % count]))
                        throw new ArgumentException("Footprint edges intersect.", nameof(outline));
                }
            // Translate before summing to avoid cancellation when a local origin is far away.
            double twiceArea = 0;
            for (int i = 1; i < count - 1; i++) twiceArea += Cross(points[0], points[i], points[i + 1]);
            if (Math.Abs(twiceArea) < 0.02) throw new ArgumentException("Footprint area is too small.", nameof(outline));
            if (twiceArea < 0) Array.Reverse(points);
            int first = 0;
            for (int i = 1; i < count; i++)
                if (points[i].EastM < points[first].EastM ||
                    (points[i].EastM == points[first].EastM && points[i].NorthM < points[first].NorthM)) first = i;
            if (first != 0)
            {
                var ordered = new PlanarPoint[count];
                for (int i = 0; i < count; i++) ordered[i] = points[(first + i) % count];
                points = ordered;
            }
            StableId = stableId; HeightM = heightM; AreaM2 = Math.Abs(twiceArea) / 2;
            MinEastM = minX; MinNorthM = minZ; WidthM = maxX - minX; DepthM = maxZ - minZ;
            Outline = Array.AsReadOnly(points);
        }

        public string StableId { get; }
        public IReadOnlyList<PlanarPoint> Outline { get; }
        public double HeightM { get; }
        public double AreaM2 { get; }
        public double MinEastM { get; }
        public double MinNorthM { get; }
        public double WidthM { get; }
        public double DepthM { get; }

        internal static double Cross(PlanarPoint a, PlanarPoint b, PlanarPoint c) =>
            (b.EastM - a.EastM) * (c.NorthM - a.NorthM) - (b.NorthM - a.NorthM) * (c.EastM - a.EastM);
        private static bool Same(PlanarPoint a, PlanarPoint b) =>
            Math.Abs(a.EastM - b.EastM) < 1e-7 && Math.Abs(a.NorthM - b.NorthM) < 1e-7;
        private static bool Intersects(PlanarPoint a, PlanarPoint b, PlanarPoint c, PlanarPoint d)
        {
            if (Math.Max(a.EastM, b.EastM) < Math.Min(c.EastM, d.EastM) - 1e-8 ||
                Math.Max(c.EastM, d.EastM) < Math.Min(a.EastM, b.EastM) - 1e-8 ||
                Math.Max(a.NorthM, b.NorthM) < Math.Min(c.NorthM, d.NorthM) - 1e-8 ||
                Math.Max(c.NorthM, d.NorthM) < Math.Min(a.NorthM, b.NorthM) - 1e-8) return false;
            return Cross(a, b, c) * Cross(a, b, d) <= 1e-12 && Cross(c, d, a) * Cross(c, d, b) <= 1e-12;
        }
    }
}
