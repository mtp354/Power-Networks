# Offline NYC map packs

The first pack covers DUMBO/Brooklyn Bridge Park and adjacent waterfront within
longitude `-74.005..-73.983`, latitude `40.694..40.710`. Its opening preview is in
Pier 1. This is a bounded Brooklyn pilot, not all-NYC coverage or a tile service.

The runtime reads a bundled normalized pack without sending device location to a
new provider. Outside coverage or without a valid location, the map explicitly
shows NYC preview mode and no player marker. Map terrain is schematic; it is not
AR terrain altitude or navigation data.

## Reproduce

Use the pinned Python environment from `Tools/footprints/requirements.txt`. Run
from the repository root; all raw inputs stay in ignored `ar-game/Temp/`:

```bash
/tmp/fantasy-earth-geo-tools/bin/overturemaps download \
  --bbox=-74.005,40.694,-73.983,40.710 --release=2026-10-21.0 \
  --type=building -f geojson -o ar-game/Temp/nyc-brooklyn-building.geojson
/tmp/fantasy-earth-geo-tools/bin/overturemaps download \
  --bbox=-74.005,40.694,-73.983,40.710 --release=2026-10-21.0 \
  --type=segment -f geojson -o ar-game/Temp/nyc-brooklyn-segment.geojson
/tmp/fantasy-earth-geo-tools/bin/overturemaps download \
  --bbox=-74.005,40.694,-73.983,40.710 --release=2026-10-21.0 \
  --type=land_use -f geojson -o ar-game/Temp/nyc-brooklyn-land-use.geojson
/tmp/fantasy-earth-geo-tools/bin/overturemaps download \
  --bbox=-74.005,40.694,-73.983,40.710 --release=2026-10-21.0 \
  --type=water -f geojson -o ar-game/Temp/nyc-brooklyn-water.geojson
/tmp/fantasy-earth-geo-tools/bin/python ar-game/Tools/maps/build_map_pack.py \
  --config ar-game/Tools/maps/nyc-brooklyn.json --source-dir ar-game/Temp \
  --output ar-game/Assets/FantasyEarth/Data/Maps/BrooklynWaterfront.bytes
/tmp/fantasy-earth-geo-tools/bin/python -m unittest discover -s ar-game/Tools/maps -v
```

The converter reuses the canonical building importer. It preserves upstream
IDs, versions, sources, height-estimate flags, release/import time and input
hashes. Roads are buffered using class width estimates; underground road segments
and unsupported geometry are reported. Green land-use and water polygons are
clipped, simplified with topology preserved and triangulated using constrained
Delaunay triangles, retaining surface holes. Invalid input cannot replace an old
pack. A pack is limited to 8 million JSON characters, 2,000 building parts,
6,000 surfaces, 100,000 surface triangles and a ±5 km local frame.

`mapSchemaVersion:1` extends the existing building tile root with `mapName`,
`coverageBbox`, `boundsM`, `defaultCenter`, `surfaces`, `contributors`, `mapSources`
and map skip statistics. Surface kinds: 1=road/path, 2=green space, 3=water.
Surface vertices are flat east/north triangle triples, rounded to 0.01 m. The
building reader remains compatible with the extended root.

For another neighborhood, use a separate public configuration, raw input files
and bundled pack; preserve attribution and keep pack limits bounded. Automatic
multi-pack selection, on-demand downloads, offline storage management and wider
NYC coverage are future work. Do not silently reinterpret an out-of-coverage
location as the demo center.

## Review

With the pinned Editor running, from `ar-game`:

```bash
unity command script run --file AgentScripts/PreviewBrooklynMap.cs \
  --entry PreviewBrooklynMap.Run --project-path "$PWD" --format json
```

This writes two images in ignored `Temp/nyc-map-previews/` using a temporary
additive scene. Open the saved World scene first; an untitled test-runner scene
cannot host an additive preview. It restores render state and the active scene.
`AgentScripts/PatchMapScene.cs` patches existing World references without
regenerating existing AR objects. `CheckMapPlayMode.cs` exercises runtime controls
in Editor Play Mode and can capture its public-data Game view.

The normalized map database is available under ODbL 1.0 with its source metadata
and attribution in `Assets/FantasyEarth/Data/Maps/`. The app includes visible map
attribution and a credits panel. See the [Overture attribution](https://docs.overturemaps.org/attribution/)
and [surface triangulation documentation](https://shapely.readthedocs.io/en/stable/reference/shapely.constrained_delaunay_triangles.html).
