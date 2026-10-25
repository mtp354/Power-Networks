using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Rendering
{
    public class BuildingMeshTests
    {
        private static BuildingFootprint Footprint(string id) => new BuildingFootprint(id, new[]
        {
            new PlanarPoint(0, 0), new PlanarPoint(6, 0), new PlanarPoint(6, 2),
            new PlanarPoint(2, 2), new PlanarPoint(2, 6), new PlanarPoint(0, 6)
        }, 8);

        [TestCase(Biome.Temperate)]
        [TestCase(Biome.Desert)]
        [TestCase(Biome.Snowy)]
        [TestCase(Biome.Tropical)]
        public void BuildingMeshesAreFiniteBoundedAndRepeatable(Biome biome)
        {
            var footprint = Footprint("preview:mesh");
            var recipe = BuildingRecipe.Create(footprint, biome);
            Mesh first = BuildingMeshBuilder.Create(footprint, recipe);
            Mesh second = BuildingMeshBuilder.Create(footprint, recipe);
            try
            {
                Assert.Greater(first.vertexCount, 100);
                Assert.AreEqual(first.vertexCount, first.colors.Length);
                Assert.AreEqual(first.vertexCount, first.normals.Length);
                CollectionAssert.AreEqual(first.vertices, second.vertices);
                CollectionAssert.AreEqual(first.colors, second.colors);
                CollectionAssert.AreEqual(first.triangles, second.triangles);
                foreach (var point in first.vertices)
                {
                    Assert.IsFalse(float.IsNaN(point.x) || float.IsInfinity(point.x) ||
                        float.IsNaN(point.y) || float.IsInfinity(point.y) ||
                        float.IsNaN(point.z) || float.IsInfinity(point.z));
                    Assert.That(point.x, Is.InRange(-0.35f, 6.35f));
                    Assert.That(point.z, Is.InRange(-0.35f, 6.35f));
                    Assert.That(point.y, Is.InRange(0f, 8f + (float)recipe.RoofHeightM + 3f));
                }
                foreach (int index in first.triangles) Assert.That(index, Is.InRange(0, first.vertexCount - 1));
                foreach (var normal in first.normals) Assert.That(normal.sqrMagnitude, Is.InRange(0.9f, 1.1f));
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }

        [Test]
        public void BiomeChangesPaletteWithoutChangingBuildingGeometry()
        {
            var footprint = Footprint("preview:palette");
            Mesh warm = BuildingMeshBuilder.Create(footprint, BuildingRecipe.Create(footprint, Biome.Desert));
            Mesh cold = BuildingMeshBuilder.Create(footprint, BuildingRecipe.Create(footprint, Biome.Snowy));
            try
            {
                CollectionAssert.AreEqual(warm.vertices, cold.vertices);
                CollectionAssert.AreNotEqual(warm.colors, cold.colors);
            }
            finally { Object.DestroyImmediate(warm); Object.DestroyImmediate(cold); }
        }

        [Test]
        public void RendererRebuildAndDisableReleaseOwnedMeshes()
        {
            var go = new GameObject("Building view test");
            var material = new Material(Shader.Find("FantasyEarth/Buildings"));
            try
            {
                var view = go.AddComponent<ProceduralBuildingView>();
                view.Configure(Footprint("preview:view"), Biome.Temperate, material);
                Mesh first = go.GetComponent<MeshFilter>().sharedMesh;
                view.Configure(Footprint("preview:view"), Biome.Snowy, material);
                Mesh second = go.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsTrue(first == null, "Rebuild must release its previous transient mesh.");
                Assert.IsNotNull(second);
                view.enabled = false;
                Assert.IsTrue(second == null, "Disable must release the generated mesh.");
                view.enabled = true;
                Assert.IsNotNull(go.GetComponent<MeshFilter>().sharedMesh, "Enable must rebuild from saved configuration immediately.");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(material); }
        }
    }
}
