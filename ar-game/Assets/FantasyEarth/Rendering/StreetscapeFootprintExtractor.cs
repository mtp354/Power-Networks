using System;
using System.Collections.Generic;
using FantasyEarth.World;
using UnityEngine;

namespace FantasyEarth.Rendering
{
    /// <summary>
    /// Read-only matching adapter for a borrowed Streetscape building mesh. Its convex hull is for
    /// identity matching, not replacement rendering. The caller supplies mesh-local to catalog E/U/N.
    /// </summary>
    public static class StreetscapeFootprintExtractor
    {
        public static bool TryExtract(Mesh borrowedMesh, Matrix4x4 meshToCatalog, string sessionId, out BuildingFootprint footprint)
        {
            footprint = null;
            if (borrowedMesh == null || !borrowedMesh.isReadable || borrowedMesh.vertexCount < 3 || borrowedMesh.vertexCount > 100000 ||
                string.IsNullOrWhiteSpace(sessionId)) return false;
            var vertices = new List<Vector3>(borrowedMesh.vertexCount);
            borrowedMesh.GetVertices(vertices);
            var points = new List<PlanarPoint>(vertices.Count);
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var local in vertices)
            {
                Vector3 point = meshToCatalog.MultiplyPoint3x4(local);
                if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) return false;
                minY = Mathf.Min(minY,point.y); maxY = Mathf.Max(maxY,point.y);
                points.Add(new PlanarPoint(point.x,point.z));
            }
            if (maxY-minY < .1f || maxY-minY > 500) return false;
            points.Sort((a,b) => { int x=a.EastM.CompareTo(b.EastM); return x==0 ? a.NorthM.CompareTo(b.NorthM) : x; });
            var unique = new List<PlanarPoint>();
            foreach (var point in points)
                if (unique.Count==0 || Math.Abs(point.EastM-unique[unique.Count-1].EastM)>1e-6 ||
                    Math.Abs(point.NorthM-unique[unique.Count-1].NorthM)>1e-6) unique.Add(point);
            if (unique.Count<3) return false;
            var hull = new List<PlanarPoint>();
            foreach (var point in unique)
            {
                while (hull.Count>=2 && Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=1e-8) hull.RemoveAt(hull.Count-1);
                hull.Add(point);
            }
            int lower=hull.Count;
            for (int i=unique.Count-2;i>=0;i--)
            {
                var point=unique[i];
                while (hull.Count>lower && Cross(hull[hull.Count-2],hull[hull.Count-1],point)<=1e-8) hull.RemoveAt(hull.Count-1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count-1);
            if (hull.Count<3 || hull.Count>64) return false;
            try { footprint=new BuildingFootprint(sessionId,hull,maxY-minY); return true; }
            catch (ArgumentException) { return false; }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static double Cross(PlanarPoint a, PlanarPoint b, PlanarPoint c) =>
            (b.EastM-a.EastM)*(c.NorthM-a.NorthM)-(b.NorthM-a.NorthM)*(c.EastM-a.EastM);
    }
}
