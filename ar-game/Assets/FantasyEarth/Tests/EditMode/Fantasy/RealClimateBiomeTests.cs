using System.IO;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Fantasy
{
    public class RealClimateBiomeTests
    {
        private ClimateModel _model;

        [OneTimeSetUp]
        public void LoadBundledGrid()
        {
            byte[] cells = File.ReadAllBytes(Path.Combine(Application.dataPath,
                "FantasyEarth/Data/koppen_1991_2020_0p1.bytes"));
            _model = new ClimateModel(new KoppenGrid(cells, KoppenGrid.GlobalWidth, KoppenGrid.GlobalHeight));
        }

        [TestCase(33.45, -112.07, Biome.Desert)]
        [TestCase(1.35, 103.82, Biome.Tropical)]
        [TestCase(51.50, -0.12, Biome.Temperate)]
        [TestCase(71.29, -156.79, Biome.Snowy)]
        public void ClimateAt_RealCityMatchesBiome(double latitude, double longitude, Biome expected)
        {
            Assert.AreEqual(expected, BiomeRules.FromKoppen(_model.ClimateAt(new GeoPoint(latitude, longitude)).Value));
        }

        [Test]
        public void ClimateAt_MidAtlanticIsNoneAndTemperate()
        {
            var observation = _model.ClimateAt(new GeoPoint(30, -40));
            Assert.AreEqual(KoppenClass.None, observation.Value);
            Assert.AreEqual(Biome.Temperate, BiomeRules.FromKoppen(observation.Value));
        }
    }
}
