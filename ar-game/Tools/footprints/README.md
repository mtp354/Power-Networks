# Building tiles

This offline pipeline converts a small Overture building GeoJSON extract into a
versioned metric tile read by `FantasyEarth.Data.BuildingTileReader`. It does not
download the player's location or run a network request from the Unity app.

## Reproduce the public sample

Run from the repository root. The Boston bounding box is a public demonstration,
unrelated to device test locations. Keep downloads and normalized data in ignored
`ar-game/Temp/`; neither is included in source control or the APK.

```bash
python3 -m venv /tmp/fantasy-earth-geo-tools
/tmp/fantasy-earth-geo-tools/bin/pip install -r ar-game/Tools/footprints/requirements.txt
mkdir -p ar-game/Temp
/tmp/fantasy-earth-geo-tools/bin/overturemaps download \
  --bbox=-71.064,42.356,-71.062,42.358 \
  --type=building --release=2026-10-21.0 -f geojson \
  --connect_timeout 15 --request_timeout 45 \
  -o ar-game/Temp/boston-buildings.geojson
/tmp/fantasy-earth-geo-tools/bin/python ar-game/Tools/footprints/build_building_tile.py \
  ar-game/Temp/boston-buildings.geojson ar-game/Temp/boston-building-tile.json \
  --tile-id boston-public-demo --origin-lat 42.357 --origin-lon -71.063 \
  --release 2026-10-21.0
/tmp/fantasy-earth-geo-tools/bin/python -m unittest discover -s ar-game/Tools/footprints -v
```

With the Editor open on `ar-game`, run the isolated preview from its project
directory using the existing Unity Pipeline CLI:

```bash
unity command script run --file AgentScripts/PreviewBuildingTile.cs \
  --entry PreviewBuildingTile.Build --project-path "$PWD" --format json
```

It reads the tile, checks canonical identity matches and renders generated fantasy
buildings into `Temp/building-data-preview/Boston.png`. It restores the active
scene, releases temporary meshes/materials, and saves no scene or build settings.

## Format and constraints

Schema version 1 contains `tileId`, `provider`, `sourceRelease`, `importedAtUtc`,
`sourceSha256`, `origin` (WGS84 latitude, longitude, ellipsoid altitude), `license`,
`attribution`, `attributionUrl`, `buildings`, and import statistics. The hash
identifies the upstream extract; it does not authenticate the normalized tile.
Import time records conversion time, independently of the source release.

Each building preserves its source feature ID, version, part index and source
metadata as `sourcesJson`. `stableId` is `overture:<feature ID>`; do not assume
every feature ID is a GERS ID. Multipart components share this identity and
therefore the same style seed. Part indices select components within a release;
they are not persistent identities across releases.

`outline` is a flat sequence of east/north metre pairs, in a fixed WGS84 tangent
frame. Unity's corresponding axes are x=east, y=up, z=north. The converter and C#
projection use the same ECEF/topocentric definition, with an independent PROJ
reference test. Footprint vertices use altitude zero for horizontal projection;
these tiles do not provide terrain elevation or an AR placement altitude.

Limits: 10,000 input features; 2,000 accepted building parts; each local axis
within ±5 km; 3–64 vertices per simple outer ring; positive heights up to 500 m.
The runtime reader caps JSON at 8 million characters. Winding and starting corner
are canonical. Redundant closing/collinear points are removed without smoothing.
Courtyard holes, excessive vertex counts, underground/elevated geometry and
invalid geometry/heights are reported as skips. They are not silently filled,
clamped or repaired. Duplicate source IDs fail the import. Output replacement is
atomic; invalid input leaves a previous tile intact.

`heightSource`: 1=reported height; 2=floor estimate (3 m per supplied floor);
3=default estimate (9 m). No default estimate is represented as a measurement.

The overlap matcher uses actual triangulated polygon intersections, filters by
bounds, caches catalog triangulations and groups multipart candidates by stable
ID. Defaults require IoU ≥0.35 and a lead of ≥0.10 over another identity. These
are provisional matching thresholds, not calibrated device acceptance limits.
Weak/ambiguous results expose no claimed building. Use on observations changing
at a bounded rate, not every render frame; dense-neighborhood performance remains
to be measured.

`StreetscapeFootprintExtractor` borrows a readable mesh and an explicit mesh-local
to catalog matrix. It neither modifies nor destroys the mesh. Its projected
convex hull is only an observation for identity matching; render canonical rings,
not this hull. The caller must handle session changes and removed trackables and
must derive a validated geographic/world transform before live integration.

## Data attribution

The converter preserves upstream sources and includes the Overture attribution
URL and ODbL metadata. Before distributing data or displaying it in the app, add
the required visible source attribution and review the selected release's
contributors. Current sample data is local development evidence only.

Primary references: [download tool](https://docs.overturemaps.org/getting-data/overturemaps-py/),
[building schema](https://docs.overturemaps.org/schema/reference/buildings/building/),
[source attribution](https://docs.overturemaps.org/attribution/),
[PROJ topocentric conversion](https://proj.org/en/stable/operations/conversions/topocentric.html),
[ARCore Streetscape Geometry](https://developers.google.com/ar/develop/unity-arf/geospatial/streetscape-geometry).
