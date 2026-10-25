using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class LocalGeoFrameTests
    {
        [Test]
        public void ProjectionMatchesIndependentWgs84ProjReference()
        {
            var frame = new LocalGeoFrame(new GeoPoint(55, 5), 200);
            var point = frame.Project(new GeoPoint(53.80939444, 2.12955), 73);
            Assert.AreEqual(-189013.869, point.EastM, .002);
            Assert.AreEqual(-128642.040, point.NorthM, .002);
            Assert.AreEqual(-4220.171, point.UpM, .002);
        }

        [TestCase(0, 179.999, 0, -179.999)]
        [TestCase(90, 0, 89.999, 45)]
        [TestCase(-90, 0, -89.999, -45)]
        [TestCase(42.357, -71.063, 42.358, -71.062)]
        public void ProjectionRoundTripsAcrossPolesAndAntimeridian(double originLat, double originLon, double lat, double lon)
        {
            var frame = new LocalGeoFrame(new GeoPoint(originLat, originLon), 15);
            var projected = frame.Project(new GeoPoint(lat, lon), 23.5);
            var restored = frame.Unproject(projected);
            Assert.AreEqual(lat, restored.Point.LatitudeDeg, 1e-8);
            Assert.AreEqual(lon, restored.Point.LongitudeDeg, 1e-8);
            Assert.AreEqual(23.5, restored.AltitudeM, .001);
        }

        [Test]
        public void CrossingAntimeridianStaysLocal()
        {
            var frame = new LocalGeoFrame(new GeoPoint(0, 179.999));
            var local = frame.Project(new GeoPoint(0, -179.999));
            Assert.That(local.EastM, Is.InRange(222.0, 223.0));
            Assert.AreEqual(0, local.NorthM, .001);
        }
    }
}
