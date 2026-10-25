using System.Globalization;
using FantasyEarth.Diagnostics;
using FantasyEarth.Fantasy;
using FantasyEarth.Positioning;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.Diagnostics
{
    public class SessionLogFormatterTests
    {
        private const string Expected = "{\"t\":1792890000000,\"lat\":51.5000001,\"lon\":-0.1234567,\"hAcc\":1.23,\"yawAcc\":0.50,\"earth\":\"Enabled\",\"tracking\":true,\"alignment\":\"Locked\",\"blend\":0.50,\"koppen\":\"Cfb\",\"biome\":\"Temperate\"}";

        private static SessionLogEntry Entry(double horizontal = 1.234, double yaw = 0.5,
            string earth = "Enabled", bool tracking = true, float blend = 0.5f)
        {
            return new SessionLogEntry(1792890000000, new GeoPoint(51.5000001, -0.1234567),
                horizontal, yaw, earth, tracking, AlignmentState.Locked, blend, KoppenClass.Cfb, Biome.Temperate);
        }

        [Test]
        public void FormatsExactLine()
        {
            Assert.AreEqual(Expected, SessionLogFormatter.ToJsonLine(Entry()));
        }

        [Test]
        public void NullPositionWritesNulls()
        {
            var entry = new SessionLogEntry(1792890000000, null, 1.234, 0.5, "Enabled", true,
                AlignmentState.Locked, 0.5f, KoppenClass.Cfb, Biome.Temperate);
            Assert.AreEqual(Expected.Replace("\"lat\":51.5000001,\"lon\":-0.1234567", "\"lat\":null,\"lon\":null"),
                SessionLogFormatter.ToJsonLine(entry));
        }

        [TestCase(double.NaN, 0.5, true, false)]
        [TestCase(double.PositiveInfinity, 0.5, true, false)]
        [TestCase(double.NegativeInfinity, 0.5, true, false)]
        [TestCase(1.234, double.NaN, false, true)]
        [TestCase(1.234, double.PositiveInfinity, false, true)]
        [TestCase(1.234, double.NegativeInfinity, false, true)]
        [TestCase(double.NaN, double.PositiveInfinity, true, true)]
        public void NonFiniteAccuracyWritesNull(double horizontal, double yaw, bool nullHorizontal, bool nullYaw)
        {
            string expected = Expected;
            if (nullHorizontal) expected = expected.Replace("\"hAcc\":1.23", "\"hAcc\":null");
            if (nullYaw) expected = expected.Replace("\"yawAcc\":0.50", "\"yawAcc\":null");
            Assert.AreEqual(expected, SessionLogFormatter.ToJsonLine(Entry(horizontal, yaw)));
        }

        [Test]
        public void EscapesEarthState()
        {
            string line = SessionLogFormatter.ToJsonLine(Entry(earth: "a\"b\\c"));
            Assert.AreEqual(Expected.Replace("\"Enabled\"", "\"a\\\"b\\\\c\""), line);
        }

        [Test]
        public void EscapesControlCharactersWithoutRawLineBreaks()
        {
            string line = SessionLogFormatter.ToJsonLine(Entry(earth: "\b\f\n\r\t\u0001"));
            Assert.AreEqual(Expected.Replace("\"Enabled\"", "\"\\b\\f\\n\\r\\t\\u0001\""), line);
            foreach (char c in line) Assert.GreaterOrEqual((int)c, 32);
        }

        [Test]
        public void UsesDotDecimalsUnderGermanCulture()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                Assert.AreEqual(Expected, SessionLogFormatter.ToJsonLine(Entry()));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void FalseTrackingIsLowercaseJsonBoolean()
        {
            Assert.AreEqual(Expected.Replace("\"tracking\":true", "\"tracking\":false"),
                SessionLogFormatter.ToJsonLine(Entry(tracking: false)));
        }

        [Test]
        public void NullEarthStateWritesJsonNull()
        {
            Assert.AreEqual(Expected.Replace("\"earth\":\"Enabled\"", "\"earth\":null"),
                SessionLogFormatter.ToJsonLine(Entry(earth: null)));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteBlendWritesJsonNull(float blend)
        {
            Assert.AreEqual(Expected.Replace("\"blend\":0.50", "\"blend\":null"),
                SessionLogFormatter.ToJsonLine(Entry(blend: blend)));
        }
    }
}
