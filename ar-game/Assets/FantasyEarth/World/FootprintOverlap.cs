using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public static class FootprintOverlap
    {
        public static double IntersectionArea(BuildingFootprint a, BuildingFootprint b) =>
            IntersectionArea(a,FootprintTriangulator.Triangulate(a),b,FootprintTriangulator.Triangulate(b));

        public static double IntersectionOverUnion(BuildingFootprint a, BuildingFootprint b)
        {
            double intersection = IntersectionArea(a,b);
            return intersection / (a.AreaM2+b.AreaM2-intersection);
        }

        internal static bool BoundsOverlap(BuildingFootprint a, BuildingFootprint b) =>
            a.MinEastM+a.WidthM > b.MinEastM && b.MinEastM+b.WidthM > a.MinEastM &&
            a.MinNorthM+a.DepthM > b.MinNorthM && b.MinNorthM+b.DepthM > a.MinNorthM;

        internal static double IntersectionArea(BuildingFootprint a, int[] trianglesA, BuildingFootprint b, int[] trianglesB)
        {
            if (!BoundsOverlap(a,b)) return 0;
            double area = 0;
            // Triangulations partition both polygons, so the sum counts each interior intersection once.
            for (int i = 0; i < trianglesA.Length; i += 3)
                for (int j = 0; j < trianglesB.Length; j += 3)
                {
                    var polygon = new List<PlanarPoint> { a.Outline[trianglesA[i]],a.Outline[trianglesA[i+1]],a.Outline[trianglesA[i+2]] };
                    for (int edge = 0; edge < 3 && polygon.Count > 0; edge++)
                        polygon = Clip(polygon,b.Outline[trianglesB[j+edge]],b.Outline[trianglesB[j+(edge+1)%3]]);
                    for (int p = 1; p < polygon.Count - 1; p++)
                        area += Math.Abs(BuildingFootprint.Cross(polygon[0],polygon[p],polygon[p+1])) / 2;
                }
            return Math.Max(0,Math.Min(Math.Min(a.AreaM2,b.AreaM2),area));
        }

        private static List<PlanarPoint> Clip(List<PlanarPoint> polygon, PlanarPoint edgeA, PlanarPoint edgeB)
        {
            var result = new List<PlanarPoint>();
            for (int i = 0; i < polygon.Count; i++)
            {
                PlanarPoint a = polygon[i], b = polygon[(i+1)%polygon.Count];
                double da = BuildingFootprint.Cross(edgeA,edgeB,a), db = BuildingFootprint.Cross(edgeA,edgeB,b);
                bool insideA = da >= -1e-8, insideB = db >= -1e-8;
                if (insideA) result.Add(a);
                if (insideA != insideB)
                {
                    double t = da/(da-db);
                    result.Add(new PlanarPoint(a.EastM+(b.EastM-a.EastM)*t,a.NorthM+(b.NorthM-a.NorthM)*t));
                }
            }
            return result;
        }
    }
}
