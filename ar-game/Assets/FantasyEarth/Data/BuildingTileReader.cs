using System;
using System.Globalization;
using System.IO;
using FantasyEarth.World;
using UnityEngine;
using UnityEngine.Scripting;

namespace FantasyEarth.Data
{
    /// <summary>Reads the normalized tile format, not arbitrary GeoJSON. No network or asset side effects.</summary>
    public static class BuildingTileReader
    {
        [Serializable, Preserve] private sealed class Tile
        {
            public int schemaVersion;
            public string tileId, provider, sourceRelease, importedAtUtc, license, attribution;
            public string sourceSha256, attributionUrl;
            public double[] origin;
            public Building[] buildings;
        }
        [Serializable, Preserve] private sealed class Building
        {
            public string stableId, sourceId, sourcesJson;
            public int partIndex, sourceVersion, heightSource;
            public double heightM;
            public double[] outline;
        }

        public static BuildingCatalog Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 8000000)
                throw new InvalidDataException("Missing or oversized building tile.");
            try
            {
                var tile = JsonUtility.FromJson<Tile>(json);
                if (tile == null || tile.schemaVersion != 1 || tile.origin == null || tile.origin.Length != 3 ||
                    tile.buildings == null || tile.buildings.Length > 2000)
                    throw new InvalidDataException("Unsupported or incomplete building tile.");
                if (!DateTimeOffset.TryParse(tile.importedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                    throw new InvalidDataException("Missing or invalid tile import time.");
                var frame = new LocalGeoFrame(new GeoPoint(tile.origin[0], tile.origin[1]), tile.origin[2]);
                var buildings = new CatalogBuilding[tile.buildings.Length];
                for (int i = 0; i < buildings.Length; i++)
                {
                    Building item = tile.buildings[i];
                    if (item == null || item.outline == null || item.outline.Length < 6 ||
                        item.outline.Length > 130 || item.outline.Length % 2 != 0)
                        throw new InvalidDataException("Invalid normalized building outline.");
                    var points = new PlanarPoint[item.outline.Length / 2];
                    for (int p = 0; p < points.Length; p++) points[p] = new PlanarPoint(item.outline[p*2],item.outline[p*2+1]);
                    var footprint = new BuildingFootprint(item.stableId, points, item.heightM);
                    buildings[i] = new CatalogBuilding(item.sourceId,item.partIndex,footprint,item.sourceVersion,
                        (BuildingHeightSource)item.heightSource,item.sourcesJson);
                }
                return new BuildingCatalog(tile.tileId,frame,buildings,tile.sourceRelease,time,tile.license,tile.attribution,tile.provider,
                    tile.sourceSha256,tile.attributionUrl);
            }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid building tile data.",error); }
        }
    }
}
