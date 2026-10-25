using System;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class ClimateModelTests
    {
        private static ClimateModel MakeModel()
        {
            return new ClimateModel(new KoppenGrid(new byte[] { (byte)KoppenClass.Cfb, 0 }, 2, 1));
        }

        [Test]
        public void ClimateAt_ReportsProvenance()
        {
            var observation = MakeModel().ClimateAt(new GeoPoint(0, -90));
            Assert.AreEqual(KoppenClass.Cfb, observation.Value);
            Assert.AreEqual(ObservationSource.KoppenGeiger1991To2020, observation.Source);
            Assert.AreEqual(new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero), observation.ObservedAt);
            Assert.AreEqual(1.0, observation.Confidence);
        }

        [Test]
        public void ClimateAt_OceanIsNoneWithZeroConfidence()
        {
            var observation = MakeModel().ClimateAt(new GeoPoint(0, 90));
            Assert.AreEqual(KoppenClass.None, observation.Value);
            Assert.AreEqual(0.0, observation.Confidence);
            Assert.AreEqual(ObservationSource.KoppenGeiger1991To2020, observation.Source);
            Assert.AreEqual(new DateTimeOffset(2020, 12, 31, 0, 0, 0, TimeSpan.Zero), observation.ObservedAt);
        }

        [Test]
        public void Constructor_RejectsNullGrid()
        {
            Assert.Throws<ArgumentNullException>(() => new ClimateModel(null));
        }
    }
}
