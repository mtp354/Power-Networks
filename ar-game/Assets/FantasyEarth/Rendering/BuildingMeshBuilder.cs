using System;
using System.Collections.Generic;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.Rendering
{
    /// <summary>One owned, vertex-colored mesh per building; no per-window GameObjects or materials.</summary>
    public static class BuildingMeshBuilder
    {
        private readonly struct Palette
        {
            public readonly Color Wall, Roof, Trim, Window, Banner;
            public Palette(Color wall, Color roof, Color trim, Color banner)
            {
                Wall = wall; Roof = roof; Trim = trim; Banner = banner;
                Window = new Color(1f, 0.73f, 0.26f, 0.65f);
            }
            public static Palette For(Biome biome)
            {
                switch (biome)
                {
                    case Biome.Desert: return new Palette(C(.85f, .61f, .34f), C(.62f, .24f, .13f), C(.34f, .20f, .12f), C(.78f, .24f, .15f));
                    case Biome.Snowy: return new Palette(C(.61f, .71f, .78f), C(.89f, .95f, .98f), C(.23f, .30f, .40f), C(.28f, .40f, .78f));
                    case Biome.Tropical: return new Palette(C(.62f, .79f, .66f), C(.14f, .40f, .32f), C(.28f, .18f, .12f), C(.91f, .36f, .24f));
                    default: return new Palette(C(.84f, .73f, .52f), C(.38f, .20f, .27f), C(.26f, .17f, .12f), C(.12f, .49f, .46f));
                }
            }
        }

        public static Mesh Create(BuildingFootprint footprint, BuildingRecipe recipe)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            var surface = new Surface();
            Palette palette = Palette.For(recipe.Biome);
            Color wall = recipe.Style == BuildingStyle.StoneKeep ? Color.Lerp(palette.Wall, C(.47f, .50f, .48f), .4f) : palette.Wall;
            float height = (float)footprint.HeightM;
            bool keep = recipe.Style == BuildingStyle.StoneKeep;
            bool ridgeAlongNorth = footprint.DepthM >= footprint.WidthM;
            float minimum = (float)(ridgeAlongNorth ? footprint.MinEastM : footprint.MinNorthM);
            float span = (float)(ridgeAlongNorth ? footprint.WidthM : footprint.DepthM);
            float middle = minimum + span * .5f;
            Func<Vector2, float> roofY = point => height + (keep ? 0 : (float)recipe.RoofHeightM *
                Mathf.Max(0, 1 - Mathf.Abs(((ridgeAlongNorth ? point.x : point.y) - middle) / (span * .5f))));

            int[] triangles = FootprintTriangulator.Triangulate(footprint);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var polygon = new List<Vector2> { Point(footprint, triangles[i]), Point(footprint, triangles[i + 1]), Point(footprint, triangles[i + 2]) };
                if (keep) RoofFace(surface, polygon, roofY, palette.Roof);
                else
                {
                    // Split each triangulated footprint face at the ridge. Convex clipping preserves concave outlines.
                    RoofFace(surface, Clip(polygon, ridgeAlongNorth, middle, true), roofY, palette.Roof);
                    RoofFace(surface, Clip(polygon, ridgeAlongNorth, middle, false), roofY, palette.Roof);
                }
            }

            for (int edge = 0; edge < footprint.Outline.Count; edge++)
            {
                Vector2 a = Point(footprint, edge), b = Point(footprint, (edge + 1) % footprint.Outline.Count);
                Vector3 along = new Vector3(b.x - a.x, 0, b.y - a.y).normalized;
                Vector3 outward = new Vector3(along.z, 0, -along.x);
                float length = Vector2.Distance(a, b);
                surface.Quad(V(a, 0), V(a, height), V(b, height), V(b, 0), wall * .67f, wall);
                if (!keep)
                {
                    float ca = ridgeAlongNorth ? a.x : a.y, cb = ridgeAlongNorth ? b.x : b.y;
                    if ((ca - middle) * (cb - middle) < 0)
                    {
                        Vector2 cut = Vector2.Lerp(a, b, (middle - ca) / (cb - ca));
                        Gable(surface, a, cut, height, roofY, wall); Gable(surface, cut, b, height, roofY, wall);
                    }
                    else Gable(surface, a, b, height, roofY, wall);
                }

                int windows = Mathf.Clamp(Mathf.FloorToInt(length / 2.1f), 1, 12);
                float floorHeight = height / recipe.Floors;
                for (int floor = 0; floor < recipe.Floors; floor++)
                {
                    float y = (floor + .52f) * floorHeight;
                    for (int window = 0; window < windows; window++)
                    {
                        Vector3 centre = V(Vector2.Lerp(a, b, (window + .5f) / windows), y) + outward * .045f;
                        float width = Mathf.Min(keep ? .55f : .85f, length / windows * .6f);
                        float tall = Mathf.Min(1.15f, floorHeight * .55f);
                        Panel(surface, centre, along, width + .18f, Mathf.Min(tall + .2f, floorHeight * .9f), palette.Trim);
                        Panel(surface, centre + outward * .012f, along, width, tall, palette.Window);
                        surface.Beam(centre - along * width * .5f + outward * .03f,
                            centre + along * width * .5f + outward * .03f, Mathf.Min(.07f, floorHeight * .12f), .07f, palette.Trim);
                    }
                    if (recipe.Style == BuildingStyle.TimberHall || floor == 0)
                        surface.Beam(V(a, (floor + 1) * floorHeight), V(b, (floor + 1) * floorHeight), Mathf.Min(.16f, floorHeight * .2f), .2f, palette.Trim);
                }
                if (recipe.Style == BuildingStyle.TimberHall)
                {
                    int posts = Mathf.Clamp(Mathf.CeilToInt(length / 2.5f), 1, 16);
                    for (int p = 0; p < posts; p++)
                        surface.Box(V(Vector2.Lerp(a, b, (float)p / posts), height * .5f) + outward * .03f,
                            Vector3.right, new Vector3(.19f, height, .19f), palette.Trim);
                    // Diagonal half-timbering remains on each facade's plane.
                    if (length > 2)
                        surface.Beam(V(a, height * .35f) + outward * .065f,
                            V(Vector2.Lerp(a, b, .5f), height * .68f) + outward * .065f, .1f, .1f, palette.Trim);
                }
                if (keep)
                {
                    surface.Beam(V(a, height + .2f), V(b, height + .2f), .4f, .4f, wall);
                    int count = Mathf.Clamp(Mathf.CeilToInt(length / 1.15f), 2, 24);
                    for (int m = 0; m < count; m++)
                        surface.Box(V(Vector2.Lerp(a, b, (m + .5f) / count), height + .52f),
                            along, new Vector3(length / count * .58f, .85f, .42f), wall);
                }
                if (edge == 0 && length > 2.5f)
                {
                    float doorHeight = Mathf.Min(2.1f, height * .7f);
                    Vector3 door = V(Vector2.Lerp(a, b, .5f), doorHeight * .5f) + outward * .15f;
                    Panel(surface, door, along, 1.18f, doorHeight, palette.Trim);
                    Panel(surface, door + outward * .012f, along, .78f, doorHeight * .88f, palette.Banner * .55f);
                    Vector3 centre = V(Vector2.Lerp(a, b, .5f), height * .66f) + outward * .13f;
                    Panel(surface, centre, along, .66f, Mathf.Min(2.1f, height * .38f), palette.Banner);
                    Panel(surface, centre + outward * .015f, along, .13f, Mathf.Min(1.6f, height * .28f), C(.98f, .78f, .31f));
                }
            }

            if (recipe.Style == BuildingStyle.SpiredHouse)
            {
                Vector2 centre = (Point(footprint, triangles[0]) + Point(footprint, triangles[1]) + Point(footprint, triangles[2])) / 3;
                float radius = .5f;
                for (int i = 0; i < footprint.Outline.Count; i++)
                {
                    Vector2 a = Point(footprint, i), b = Point(footprint, (i + 1) % footprint.Outline.Count);
                    float t = Mathf.Clamp01(Vector2.Dot(centre - a, b - a) / (b - a).sqrMagnitude);
                    radius = Mathf.Min(radius, Vector2.Distance(centre, Vector2.Lerp(a, b, t)) * .6f);
                }
                Vector3 bottom = V(centre, roofY(centre));
                surface.Box(bottom + Vector3.up * .45f, Vector3.right, new Vector3(radius * 1.3f, .9f, radius * 1.3f), wall);
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4, next = (i + 1) * Mathf.PI / 4;
                    Vector3 a = bottom + new Vector3(Mathf.Cos(angle) * radius, .9f, Mathf.Sin(angle) * radius);
                    Vector3 b = bottom + new Vector3(Mathf.Cos(next) * radius, .9f, Mathf.Sin(next) * radius);
                    surface.Triangle(a, bottom + Vector3.up * 2.25f, b, palette.Roof);
                }
            }
            return surface.ToMesh(footprint.StableId);
        }

        private static Color C(float r, float g, float b) => new Color(r, g, b, 0);
        private static Vector2 Point(BuildingFootprint footprint, int index) => new Vector2((float)footprint.Outline[index].EastM, (float)footprint.Outline[index].NorthM);
        private static Vector3 V(Vector2 point, float y) => new Vector3(point.x, y, point.y);
        private static void Gable(Surface surface, Vector2 a, Vector2 b, float height, Func<Vector2, float> roofY, Color color) =>
            surface.Quad(V(a, height), V(a, roofY(a)), V(b, roofY(b)), V(b, height), color, color);
        private static void Panel(Surface surface, Vector3 centre, Vector3 along, float width, float height, Color color) =>
            surface.Quad(centre - along * width / 2 - Vector3.up * height / 2,
                centre - along * width / 2 + Vector3.up * height / 2,
                centre + along * width / 2 + Vector3.up * height / 2,
                centre + along * width / 2 - Vector3.up * height / 2, color, color);
        private static void RoofFace(Surface surface, List<Vector2> polygon, Func<Vector2, float> height, Color color)
        {
            for (int i = 1; i < polygon.Count - 1; i++)
                surface.Triangle(V(polygon[0], height(polygon[0])), V(polygon[i + 1], height(polygon[i + 1])), V(polygon[i], height(polygon[i])), color);
        }
        private static List<Vector2> Clip(List<Vector2> polygon, bool xAxis, float middle, bool lower)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Count];
                float ca = (xAxis ? a.x : a.y) - middle, cb = (xAxis ? b.x : b.y) - middle;
                bool insideA = lower ? ca <= 0 : ca >= 0, insideB = lower ? cb <= 0 : cb >= 0;
                if (insideA) result.Add(a);
                if (insideA != insideB) result.Add(Vector2.Lerp(a, b, ca / (ca - cb)));
            }
            return result;
        }

        private sealed class Surface
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Color> _colors = new List<Color>();
            private readonly List<int> _triangles = new List<int>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color) => Triangle(a, b, c, color, color, color);
            private void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            {
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-10f) return;
                int start = _vertices.Count;
                _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
                _colors.Add(ca); _colors.Add(cb); _colors.Add(cc);
                _triangles.Add(start); _triangles.Add(start + 1); _triangles.Add(start + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color lower, Color upper)
            { Triangle(a, b, c, lower, upper, upper); Triangle(a, c, d, lower, upper, lower); }
            public void Beam(Vector3 a, Vector3 b, float height, float depth, Color color)
            {
                Vector3 direction = (b - a).normalized;
                Vector3 forward = Vector3.Cross(direction, Vector3.up).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
                BoxAxes((a + b) / 2, direction, Vector3.Cross(forward, direction).normalized, forward,
                    new Vector3(Vector3.Distance(a, b), height, depth), color);
            }
            public void Box(Vector3 centre, Vector3 right, Vector3 size, Color color) =>
                BoxAxes(centre, right, Vector3.up, Vector3.Cross(right, Vector3.up), size, color);
            private void BoxAxes(Vector3 centre, Vector3 right, Vector3 up, Vector3 forward, Vector3 size, Color color)
            {
                Vector3 x = right * size.x / 2, y = up * size.y / 2, z = forward * size.z / 2;
                Quad(centre-x-y-z, centre-x+y-z, centre+x+y-z, centre+x-y-z, color, color);
                Quad(centre+x-y+z, centre+x+y+z, centre-x+y+z, centre-x-y+z, color, color);
                Quad(centre-x-y+z, centre-x+y+z, centre-x+y-z, centre-x-y-z, color, color);
                Quad(centre+x-y-z, centre+x+y-z, centre+x+y+z, centre+x-y+z, color, color);
                Quad(centre-x+y-z, centre-x+y+z, centre+x+y+z, centre+x+y-z, color, color);
                Quad(centre-x-y+z, centre-x-y-z, centre+x-y-z, centre+x-y+z, color, color);
            }
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = "Fantasy building " + name,
                    indexFormat = _vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_vertices); mesh.SetColors(_colors); mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
