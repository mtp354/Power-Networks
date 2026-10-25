using System;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using UnityEngine;

namespace FantasyEarth.Rendering
{
    /// <summary>Styles an owned copy of an observed surface; never extrudes its convex hull.</summary>
    public static class ObservedBuildingMesh
    {
        public const int MaximumSourceVertices = 40000;

        public static Mesh Create(Mesh borrowed, string temporaryId, Biome biome, out BuildingRecipe recipe)
        {
            if (borrowed == null || !borrowed.isReadable || borrowed.vertexCount < 3 ||
                borrowed.vertexCount > MaximumSourceVertices) throw new ArgumentException("Unsupported observed mesh.");
            for (int i=0;i<borrowed.subMeshCount;i++)
                if (borrowed.GetTopology(i)!=MeshTopology.Triangles) throw new ArgumentException("Expected triangle surfaces.");
            if (borrowed.subMeshCount==0) throw new ArgumentException("Missing observed surfaces.");
            Vector3[] vertices = borrowed.vertices;
            var bounds = new Bounds(vertices[0],Vector3.zero);
            foreach (var vertex in vertices)
            {
                if (!Finite(vertex.x) || !Finite(vertex.y) || !Finite(vertex.z) || vertex.sqrMagnitude>100000000)
                    throw new ArgumentException("Invalid observed vertex.");
                bounds.Encapsulate(vertex);
            }
            if (bounds.size.x<.1f || bounds.size.z<.1f || bounds.size.y<.1f || bounds.size.y>500)
                throw new ArgumentException("Unsupported building dimensions.");
            // Bounds supply recipe dimensions only. Original vertices and every source triangle are retained.
            var dimensions = new BuildingFootprint(temporaryId,new[] { new PlanarPoint(0,0),
                new PlanarPoint(bounds.size.x,0),new PlanarPoint(bounds.size.x,bounds.size.z),
                new PlanarPoint(0,bounds.size.z) },bounds.size.y);
            recipe = BuildingRecipe.Create(dimensions,biome);
            var palette = BuildingMeshBuilder.Palette.For(biome);
            Color wall = recipe.Style==BuildingStyle.StoneKeep ? Color.Lerp(palette.Wall,new Color(.47f,.50f,.48f,0),.4f) : palette.Wall;
            Mesh owned = UnityEngine.Object.Instantiate(borrowed);
            try
            {
                owned.name = "Observed fantasy building"; owned.hideFlags = HideFlags.DontSave;
                owned.RecalculateNormals(); owned.RecalculateBounds();
                Vector3[] normals = owned.normals;
                var colors = new Color[vertices.Length];
                for (int i=0;i<colors.Length;i++)
                {
                    float height = Mathf.InverseLerp(bounds.min.y,bounds.max.y,vertices[i].y);
                    colors[i] = normals[i].y>.6f ? palette.Roof : wall*Mathf.Lerp(.78f,1,height);
                    colors[i].a = 0;
                }
                owned.colors = colors;
                return owned;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(owned); else UnityEngine.Object.DestroyImmediate(owned);
                throw;
            }
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
