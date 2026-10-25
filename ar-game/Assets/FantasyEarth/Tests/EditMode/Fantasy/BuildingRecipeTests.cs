using System.Collections.Generic;
using System.Globalization;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.Fantasy
{
    public class BuildingRecipeTests
    {
        private static BuildingFootprint Footprint(string id) => new BuildingFootprint(id, new[]
        {
            new PlanarPoint(-3, -4), new PlanarPoint(3, -4),
            new PlanarPoint(3, 4), new PlanarPoint(-3, 4)
        }, 8);

        [Test]
        public void SeedMatchesKnownUtf8FnvVector()
        {
            Assert.AreEqual(0xa430d84680aabd0bUL, StableSeed.Hash("hello"));
        }

        [Test]
        public void BuildingRecipeIsStableAcrossCultureAndCallOrder()
        {
            var before = BuildingRecipe.Create(Footprint("preview:hall"), Biome.Temperate, 42);
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                BuildingRecipe.Create(Footprint("other"), Biome.Snowy, 99);
                var after = BuildingRecipe.Create(Footprint("preview:hall"), Biome.Temperate, 42);
                Assert.AreEqual(before.Seed, after.Seed);
                Assert.AreEqual(before.Style, after.Style);
                Assert.AreEqual(before.RoofHeightM, after.RoofHeightM);
                Assert.AreEqual(before.Floors, after.Floors);
                Assert.AreNotEqual(before.Seed, BuildingRecipe.Create(Footprint("preview:hall"), Biome.Temperate, 43).Seed);
            }
            finally { CultureInfo.CurrentCulture = original; }
        }

        [Test]
        public void StableIdsProduceAllArchitecturalStyles()
        {
            var styles = new HashSet<BuildingStyle>();
            for (int i = 0; i < 60; i++) styles.Add(BuildingRecipe.Create(Footprint("preview:" + i), Biome.Temperate).Style);
            Assert.AreEqual(3, styles.Count);
        }
    }
}
