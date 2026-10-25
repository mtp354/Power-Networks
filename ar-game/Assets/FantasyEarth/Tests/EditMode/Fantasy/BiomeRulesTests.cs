using System;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.Fantasy
{
    public class BiomeRulesTests
    {
        // ── 1. Exhaustive mapping of every defined Köppen value ──────────────
        // Expected values are derived directly from the brief's mapping table,
        // independently of the production switch statement.

        [TestCase(KoppenClass.Af,  Biome.Tropical)]
        [TestCase(KoppenClass.Am,  Biome.Tropical)]
        [TestCase(KoppenClass.Aw,  Biome.Tropical)]
        [TestCase(KoppenClass.BWh, Biome.Desert)]
        [TestCase(KoppenClass.BWk, Biome.Desert)]
        [TestCase(KoppenClass.BSh, Biome.Desert)]
        [TestCase(KoppenClass.BSk, Biome.Desert)]
        [TestCase(KoppenClass.Csa, Biome.Temperate)]
        [TestCase(KoppenClass.Csb, Biome.Temperate)]
        [TestCase(KoppenClass.Csc, Biome.Temperate)]
        [TestCase(KoppenClass.Cwa, Biome.Temperate)]
        [TestCase(KoppenClass.Cwb, Biome.Temperate)]
        [TestCase(KoppenClass.Cwc, Biome.Temperate)]
        [TestCase(KoppenClass.Cfa, Biome.Temperate)]
        [TestCase(KoppenClass.Cfb, Biome.Temperate)]
        [TestCase(KoppenClass.Cfc, Biome.Temperate)]
        [TestCase(KoppenClass.Dsa, Biome.Temperate)]
        [TestCase(KoppenClass.Dsb, Biome.Temperate)]
        [TestCase(KoppenClass.Dsc, Biome.Snowy)]
        [TestCase(KoppenClass.Dsd, Biome.Snowy)]
        [TestCase(KoppenClass.Dwa, Biome.Temperate)]
        [TestCase(KoppenClass.Dwb, Biome.Temperate)]
        [TestCase(KoppenClass.Dwc, Biome.Snowy)]
        [TestCase(KoppenClass.Dwd, Biome.Snowy)]
        [TestCase(KoppenClass.Dfa, Biome.Temperate)]
        [TestCase(KoppenClass.Dfb, Biome.Temperate)]
        [TestCase(KoppenClass.Dfc, Biome.Snowy)]
        [TestCase(KoppenClass.Dfd, Biome.Snowy)]
        [TestCase(KoppenClass.ET,  Biome.Snowy)]
        [TestCase(KoppenClass.EF,  Biome.Snowy)]
        [TestCase(KoppenClass.None, Biome.Temperate)]
        public void FromKoppen_MapsEveryClass(KoppenClass input, Biome expected)
        {
            Assert.AreEqual(expected, BiomeRules.FromKoppen(input));
        }

        // ── 2. Regression: ocean / no-data cell returns Temperate ────────────
        [Test]
        public void FromKoppen_NoneIsTemperate()
        {
            Assert.AreEqual(Biome.Temperate, BiomeRules.FromKoppen(KoppenClass.None));
        }

        // ── 3. Undefined enum value must throw ───────────────────────────────
        [Test]
        public void FromKoppen_RejectsUndefinedClass()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => BiomeRules.FromKoppen((KoppenClass)255));
        }
    }
}

