using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public enum BuildingHeightSource { Unknown = 0, Reported = 1, FloorEstimate = 2, DefaultEstimate = 3 }

    public sealed class CatalogBuilding
    {
        public CatalogBuilding(string sourceId, int partIndex, BuildingFootprint footprint, int sourceVersion,
            BuildingHeightSource heightSource, string sourcesJson)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("Source ID is required.", nameof(sourceId));
            if (partIndex < 0) throw new ArgumentOutOfRangeException(nameof(partIndex));
            if (sourceVersion < 1) throw new ArgumentOutOfRangeException(nameof(sourceVersion));
            if (heightSource < BuildingHeightSource.Reported || heightSource > BuildingHeightSource.DefaultEstimate)
                throw new ArgumentOutOfRangeException(nameof(heightSource));
            Footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
            SourcesJson = sourcesJson ?? throw new ArgumentNullException(nameof(sourcesJson));
            SourceId = sourceId; PartIndex = partIndex; SourceVersion = sourceVersion; HeightSource = heightSource;
        }
        public string SourceId { get; }
        public int PartIndex { get; }
        public BuildingFootprint Footprint { get; }
        public int SourceVersion { get; }
        public BuildingHeightSource HeightSource { get; }
        public string SourcesJson { get; }
    }

    public sealed class BuildingCatalog
    {
        public BuildingCatalog(string tileId, LocalGeoFrame frame, IReadOnlyList<CatalogBuilding> buildings,
            string sourceRelease, DateTimeOffset importedAt, string license, string attribution, string provider,
            string sourceSha256 = null, string attributionUrl = null)
        {
            if (string.IsNullOrWhiteSpace(tileId) || string.IsNullOrWhiteSpace(sourceRelease) ||
                string.IsNullOrWhiteSpace(license) || string.IsNullOrWhiteSpace(attribution) || string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Tile identity, release, license and attribution are required.");
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            if (sourceSha256 != null)
            {
                if (sourceSha256.Length != 64) throw new ArgumentException("Invalid source SHA-256.", nameof(sourceSha256));
                foreach (char digit in sourceSha256)
                    if (!((digit >= '0' && digit <= '9') || (digit >= 'a' && digit <= 'f')))
                        throw new ArgumentException("Invalid source SHA-256.", nameof(sourceSha256));
            }
            if (buildings == null) throw new ArgumentNullException(nameof(buildings));
            if (buildings.Count > 2000) throw new ArgumentException("Tile building limit exceeded.", nameof(buildings));
            var copied = new CatalogBuilding[buildings.Count];
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copied.Length; i++)
            {
                var building = buildings[i] ?? throw new ArgumentException("Null building.", nameof(buildings));
                if (!keys.Add(building.Footprint.StableId + "/part/" + building.PartIndex))
                    throw new ArgumentException("Duplicate building part identity.", nameof(buildings));
                foreach (var point in building.Footprint.Outline)
                    if (Math.Abs(point.EastM) > 5000 || Math.Abs(point.NorthM) > 5000)
                        throw new ArgumentException("Tile exceeds its 5 km local frame.", nameof(buildings));
                copied[i] = building;
            }
            Array.Sort(copied, (a,b) =>
            {
                int id = StringComparer.Ordinal.Compare(a.Footprint.StableId, b.Footprint.StableId);
                return id == 0 ? a.PartIndex.CompareTo(b.PartIndex) : id;
            });
            Buildings = Array.AsReadOnly(copied); TileId = tileId; SourceRelease = sourceRelease;
            ImportedAt = importedAt; License = license; Attribution = attribution; Provider = provider;
            SourceSha256 = sourceSha256; AttributionUrl = attributionUrl;
        }
        public string TileId { get; }
        public LocalGeoFrame Frame { get; }
        public IReadOnlyList<CatalogBuilding> Buildings { get; }
        public string SourceRelease { get; }
        public DateTimeOffset ImportedAt { get; }
        public string License { get; }
        public string Attribution { get; }
        public string Provider { get; }
        /// <summary>Provenance of the upstream extract; does not authenticate this normalized tile.</summary>
        public string SourceSha256 { get; }
        public string AttributionUrl { get; }
    }
}
