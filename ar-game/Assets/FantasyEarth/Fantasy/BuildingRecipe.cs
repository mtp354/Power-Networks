using System;
using System.Globalization;
using FantasyEarth.World;

namespace FantasyEarth.Fantasy
{
    public enum BuildingStyle { TimberHall, StoneKeep, SpiredHouse }

    public readonly struct BuildingRecipe
    {
        private BuildingRecipe(ulong seed, Biome biome, BuildingStyle style, double roofHeightM, int floors)
        { Seed = seed; Biome = biome; Style = style; RoofHeightM = roofHeightM; Floors = floors; }
        public ulong Seed { get; }
        public Biome Biome { get; }
        public BuildingStyle Style { get; }
        public double RoofHeightM { get; }
        public int Floors { get; }

        public static BuildingRecipe Create(BuildingFootprint footprint, Biome biome, ulong worldSeed = 1)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            if (!Enum.IsDefined(typeof(Biome), biome)) throw new ArgumentOutOfRangeException(nameof(biome));
            ulong seed = StableSeed.Hash("FantasyEarth/v1/" + worldSeed.ToString("x16", CultureInfo.InvariantCulture) +
                "/building/" + footprint.StableId);
            var style = (BuildingStyle)(seed % 3);
            double slope = 0.3 + ((seed >> 12) % 1000) / 2500.0;
            double roof = style == BuildingStyle.StoneKeep ? 0.8 :
                Math.Min(footprint.HeightM * 0.65, Math.Min(footprint.WidthM, footprint.DepthM) * slope);
            return new BuildingRecipe(seed, biome, style, Math.Max(0.35, roof),
                Math.Max(1, Math.Min(8, (int)Math.Round(footprint.HeightM / 3))));
        }
    }
}
