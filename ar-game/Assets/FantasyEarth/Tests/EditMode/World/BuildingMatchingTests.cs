using System;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class BuildingMatchingTests
    {
        public static BuildingFootprint Rectangle(string id, double x, double z, double width = 10, double depth = 10) =>
            new BuildingFootprint(id, new[] { new PlanarPoint(x,z), new PlanarPoint(x+width,z),
                new PlanarPoint(x+width,z+depth), new PlanarPoint(x,z+depth) }, 8);

        private static CatalogBuilding Record(BuildingFootprint footprint, int part = 0) =>
            new CatalogBuilding(footprint.StableId, part, footprint, 1, BuildingHeightSource.Reported, "[]");
        private static BuildingCatalog Catalog(params CatalogBuilding[] entries) =>
            new BuildingCatalog("synthetic", new LocalGeoFrame(new GeoPoint(0,0)), entries, "fixture",
                DateTimeOffset.UnixEpoch, "synthetic", "Synthetic test data", "test");

        [Test]
        public void PolygonOverlapUsesActualConcaveArea()
        {
            var shape = new BuildingFootprint("l", BuildingFootprintTests.LShape(), 8);
            Assert.AreEqual(20, FootprintOverlap.IntersectionArea(shape, Rectangle("box",0,0,6,6)), 1e-8);
            Assert.AreEqual(20.0/36, FootprintOverlap.IntersectionOverUnion(shape, Rectangle("box",0,0,6,6)), 1e-8);
            Assert.AreEqual(1.0/3, FootprintOverlap.IntersectionOverUnion(Rectangle("a",0,0), Rectangle("b",5,0)), 1e-8);
            Assert.AreEqual(0, FootprintOverlap.IntersectionArea(Rectangle("a",0,0), Rectangle("b",10,0)), 1e-8);
        }

        [Test]
        public void MatcherFindsTheBestIdentityRegardlessOfCatalogOrder()
        {
            var a = Record(Rectangle("left",0,0));
            var b = Record(Rectangle("right",15,0));
            var observed = Rectangle("session:mesh",15.4,.2);
            foreach (var catalog in new[] { Catalog(a,b), Catalog(b,a) })
            {
                var result = new BuildingFootprintMatcher(catalog).Match(observed);
                Assert.AreEqual(BuildingMatchStatus.Matched, result.Status);
                Assert.AreEqual("right", result.Building.Footprint.StableId);
                Assert.Greater(result.IntersectionOverUnion, .8);
            }
        }

        [Test]
        public void WeakAndAmbiguousOverlapsDoNotClaimAnIdentity()
        {
            var matcher = new BuildingFootprintMatcher(Catalog(Record(Rectangle("a",0,0)), Record(Rectangle("b",1,0))));
            var ambiguous = matcher.Match(Rectangle("observation",.5,0));
            Assert.AreEqual(BuildingMatchStatus.Ambiguous, ambiguous.Status);
            Assert.IsNull(ambiguous.Building);
            var weak = new BuildingFootprintMatcher(Catalog(Record(Rectangle("a",0,0)))).Match(Rectangle("obs",9,0));
            Assert.AreEqual(BuildingMatchStatus.LowOverlap, weak.Status);
            Assert.IsNull(weak.Building);
            Assert.AreEqual(BuildingMatchStatus.NoOverlap, matcher.Match(Rectangle("far",100,100)).Status);
        }

        [Test]
        public void PartsOfTheSameBuildingDoNotCreateIdentityAmbiguity()
        {
            var matcher = new BuildingFootprintMatcher(Catalog(Record(Rectangle("same",0,0),0), Record(Rectangle("same",.1,0),1)));
            var match = matcher.Match(Rectangle("obs",0,0));
            Assert.AreEqual(BuildingMatchStatus.Matched, match.Status);
            Assert.AreEqual("same", match.Building.Footprint.StableId);
        }
    }
}
