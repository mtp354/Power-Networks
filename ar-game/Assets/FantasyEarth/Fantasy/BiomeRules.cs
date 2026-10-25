using System;
using FantasyEarth.World;

namespace FantasyEarth.Fantasy
{
    /// <summary>Maps Köppen-Geiger climate classes to fantasy biomes.</summary>
    public static class BiomeRules
    {
        /// <summary>
        /// Returns the <see cref="Biome"/> for a given <see cref="KoppenClass"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="k"/> is not a defined <see cref="KoppenClass"/> value.
        /// </exception>
        public static Biome FromKoppen(KoppenClass k)
        {
            switch (k)
            {
                // Tropical
                case KoppenClass.Af:
                case KoppenClass.Am:
                case KoppenClass.Aw:
                    return Biome.Tropical;

                // Desert
                case KoppenClass.BWh:
                case KoppenClass.BWk:
                case KoppenClass.BSh:
                case KoppenClass.BSk:
                    return Biome.Desert;

                // Temperate – C group
                case KoppenClass.Csa:
                case KoppenClass.Csb:
                case KoppenClass.Csc:
                case KoppenClass.Cwa:
                case KoppenClass.Cwb:
                case KoppenClass.Cwc:
                case KoppenClass.Cfa:
                case KoppenClass.Cfb:
                case KoppenClass.Cfc:
                    return Biome.Temperate;

                // Temperate – D group (mild sub-types)
                case KoppenClass.Dsa:
                case KoppenClass.Dsb:
                case KoppenClass.Dwa:
                case KoppenClass.Dwb:
                case KoppenClass.Dfa:
                case KoppenClass.Dfb:
                    return Biome.Temperate;

                // Snowy – D group (cold sub-types) and E group
                case KoppenClass.Dsc:
                case KoppenClass.Dsd:
                case KoppenClass.Dwc:
                case KoppenClass.Dwd:
                case KoppenClass.Dfc:
                case KoppenClass.Dfd:
                case KoppenClass.ET:
                case KoppenClass.EF:
                    return Biome.Snowy;

                // Ocean / no-data fallback
                case KoppenClass.None:
                    return Biome.Temperate;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(k),
                        k,
                        "KoppenClass value has no defined biome mapping.");
            }
        }
    }
}

