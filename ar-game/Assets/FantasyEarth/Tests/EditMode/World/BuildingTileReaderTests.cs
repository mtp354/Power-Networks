using System.IO;
using FantasyEarth.Data;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class BuildingTileReaderTests
    {
        private const string Tile = @"{""schemaVersion"":1,""tileId"":""synthetic"",""origin"":[0,0,0],
            ""provider"":""Synthetic test"",""sourceRelease"":""fixture"",""importedAtUtc"":""2026-10-25T00:00:00Z"",
            ""license"":""fixture"",""attribution"":""Synthetic geometry"",""attributionUrl"":""https://example.invalid/fixture"",
            ""sourceSha256"":""0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"",
            ""buildings"":[{""stableId"":""overture:fixture-a"",""sourceId"":""fixture-a"",""partIndex"":0,
            ""sourceVersion"":2,""heightM"":9,""heightSource"":3,""outline"":[0,0,8,0,8,6,0,6],""sourcesJson"":""[]""}]}";

        [Test]
        public void ReaderPreservesCatalogIdentityGeometryAndProvenance()
        {
            var catalog = BuildingTileReader.Read(Tile);
            Assert.AreEqual("synthetic", catalog.TileId);
            Assert.AreEqual("Synthetic test", catalog.Provider);
            Assert.AreEqual("fixture", catalog.SourceRelease);
            Assert.AreEqual("Synthetic geometry", catalog.Attribution);
            Assert.AreEqual("https://example.invalid/fixture", catalog.AttributionUrl);
            Assert.AreEqual("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",catalog.SourceSha256);
            Assert.AreEqual(1, catalog.Buildings.Count);
            var building = catalog.Buildings[0];
            Assert.AreEqual("overture:fixture-a", building.Footprint.StableId);
            Assert.AreEqual("fixture-a", building.SourceId);
            Assert.AreEqual(48, building.Footprint.AreaM2);
            Assert.AreEqual(BuildingHeightSource.DefaultEstimate, building.HeightSource);
            Assert.AreEqual(2, building.SourceVersion);
        }

        [TestCase("\"schemaVersion\":1", "\"schemaVersion\":99")]
        [TestCase("\"heightM\":9", "\"heightM\":0")]
        [TestCase("\"heightSource\":3", "\"heightSource\":0")]
        [TestCase("\"origin\":[0,0,0]", "\"origin\":[0,0]")]
        [TestCase("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", "not-a-checksum")]
        public void ReaderRejectsUnsupportedOrIncompleteTiles(string original, string replacement)
        {
            Assert.Throws<InvalidDataException>(() => BuildingTileReader.Read(Tile.Replace(original,replacement)));
        }
    }
}
