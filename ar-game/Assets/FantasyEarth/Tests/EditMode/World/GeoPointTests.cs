using System;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class GeoPointTests
    {
        [TestCase(91.0)]
        [TestCase(-91.0)]
        public void GeoPoint_RejectsLatitudeOutOfRange(double latitude)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, 0.0));
        }

        [Test]
        public void GeoPoint_RejectsNaN()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(double.NaN, 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0.0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0.0, double.PositiveInfinity));
        }

        [TestCase(180.0, -180.0)]
        [TestCase(190.0, -170.0)]
        [TestCase(-540.0, -180.0)]
        public void GeoPoint_NormalizesLongitude(double input, double expected)
        {
            var p = new GeoPoint(0.0, input);
            Assert.AreEqual(expected, p.LongitudeDeg, 1e-9);
        }

        [Test]
        public void Observation_RejectsConfidenceAbove1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Observation<int>(1, ObservationSource.ArcoreGeospatial, DateTimeOffset.UnixEpoch, 1.01));
        }
    }
}
