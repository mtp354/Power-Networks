using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public static class FootprintTriangulator
    {
        public static int[] Triangulate(BuildingFootprint footprint)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            var points = footprint.Outline;
            var ring = new List<int>();
            for (int i = 0; i < points.Count; i++) ring.Add(i);
            // Remove redundant collinear vertices before clipping ears.
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < ring.Count && ring.Count > 3; i++)
                    if (Math.Abs(BuildingFootprint.Cross(points[ring[(i + ring.Count - 1) % ring.Count]],
                        points[ring[i]], points[ring[(i + 1) % ring.Count]])) < 1e-8)
                    { ring.RemoveAt(i); removed = true; break; }
            } while (removed);

            var triangles = new List<int>();
            while (ring.Count > 3)
            {
                bool clipped = false;
                for (int i = 0; i < ring.Count; i++)
                {
                    int a = ring[(i + ring.Count - 1) % ring.Count], b = ring[i], c = ring[(i + 1) % ring.Count];
                    if (BuildingFootprint.Cross(points[a], points[b], points[c]) <= 1e-8) continue;
                    bool occupied = false;
                    foreach (int other in ring)
                    {
                        if (other == a || other == b || other == c) continue;
                        if (BuildingFootprint.Cross(points[a], points[b], points[other]) >= -1e-8 &&
                            BuildingFootprint.Cross(points[b], points[c], points[other]) >= -1e-8 &&
                            BuildingFootprint.Cross(points[c], points[a], points[other]) >= -1e-8)
                        { occupied = true; break; }
                    }
                    if (occupied) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    ring.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) throw new ArgumentException("Footprint could not be triangulated.", nameof(footprint));
            }
            triangles.AddRange(ring);
            return triangles.ToArray();
        }
    }
}
