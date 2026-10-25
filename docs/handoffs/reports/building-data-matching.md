# Building data and identity matching

Implemented 2026-10-25 while the owner ran another outdoor alignment test. The
phone session was left running throughout this work.

## Delivered

- `World/LocalGeoFrame` converts WGS84 latitude/longitude/ellipsoid altitude to a
  fixed local east/up/north frame and back. Double precision preserves local
  detail; tests include an independent PROJ reference, both poles and the
  antimeridian.
- `World/BuildingCatalog` holds immutable, sorted canonical building parts,
  provider/release/import time, source IDs/versions, original source metadata,
  height provenance, attribution and the upstream extract SHA-256. The hash is
  provenance, not normalized-tile authentication.
- `Tools/footprints/build_building_tile.py` converts Overture GeoJSON into schema
  version 1 local tiles. It preserves multipart parent identity, records skipped
  geometry and estimated heights, and atomically replaces output only after a
  complete successful conversion. Dependencies are isolated in a temporary
  Python environment; no Unity package was added.
- `Data/BuildingTileReader` validates this format with Unity's existing JSON
  support. The new Data assembly depends on World; World remains independent of
  Unity and other project assemblies.
- `World/FootprintOverlap` computes actual concave polygon overlap.
  `BuildingFootprintMatcher` groups multipart observations by stable ID, ranks
  exact IoU and rejects weak/ambiguous identities. Defaults (0.35 IoU, 0.10 lead)
  are provisional. Catalog triangulations are cached and bounds prune candidates.
- `Rendering/StreetscapeFootprintExtractor` reads a borrowed mesh through an
  explicit mesh-local-to-catalog matrix. It rejects empty/unreadable/invalid
  meshes without changing or destroying them. Its convex hull is only for
  matching; canonical footprints remain the rendering source.
- `AgentScripts/PreviewBuildingTile.cs` reads a local sample, checks identities,
  generates buildings using the existing shared generator and renders them in a
  temporary additive scene. It restores the prior scene and render state,
  releases owned objects and saves no assets/build settings.

## Public data evidence

The bounded public Boston sample is unrelated to the owner's location. Release
`2026-10-21.0`, bounding box `-71.064,42.356,-71.062,42.358`, origin
`42.357,-71.063`, extract SHA-256
`56fe3d64b9d7511dc3ce33f9321b94a5d624cd39e55d1c9a10b36495f02ffb1b`.

- 24 source features; 23 supported building parts; one explicitly skipped for
  exceeding 64 vertices.
- 20 reported heights, one floor estimate and two default estimates.
- All 23 canonical footprints matched their own identities, with no ambiguous
  result. This is a data/algorithm round trip, not an observation from ARCore.
- 23 meshes, 275,556 vertices, one shared building material; 10 timber halls,
  seven keeps and six spired houses. An actual URP render was visually inspected.
  Large facades will need art/performance tuning before neighborhood deployment.
- Source data, normalized tiles and the rendered image remain ignored under
  `ar-game/Temp/`. No public extract or device coordinates are committed or
  bundled in the APK. The image is at
  `ar-game/Temp/building-data-preview/Boston.png`.

See [the reproduction instructions](../../../ar-game/Tools/footprints/README.md)
for pinned tools, commands, schema, attribution and limits. The catalog preserves
upstream source metadata; visible in-app attribution is still needed before
distributing data.

## Validation

- Test-first missing-type failures were observed for the coordinate frame,
  importer and mesh adapter. Intermediate scoped suites passed before the full
  run. A full run caught an overlong synthetic checksum fixture, which was
  corrected without weakening checksum validation.
- **153/153 FantasyEarth EditMode cases pass**, zero failed/skipped. This includes
  the prior 134 cases and 19 new coordinate, reader, overlap/matching and borrowed
  mesh cases.
- **8/8 Python importer tests pass**, including source identity, multipart style
  identity, reported/estimated heights, unsupported holes, duplicate IDs,
  malformed neighbors and preserving an old output after an invalid source.
- Editor recompilation completed with no errors; pre-build live Console ground
  truth reported zero errors and zero warnings. Historical cached test-first
  errors are not current compile errors.
- All new Unity asset/code files have generated metadata; no duplicate GUIDs
  were found. Android's scene list and runtime scene were not edited.

Validation records live in the ignored foundation ledger (`data-final-tests`,
`data-public-preview-reviewed`, `data-prebuild-console`, `data-android-build`).

Android development build `build_6fa6bf0ce4d7` succeeded in 74.606 seconds with
zero errors and three known vendor test-image/package warnings. The APK is
75,083,430 bytes, SHA-256
`1b95188bea075b3e62dffac6413c5f1e19a8b04c37151d424b82c2b387cc5664`.
It contains only `level0` for World. The APK verifier confirmed both required
Google `FusedLocationProviderClient` and `GoogleAuthUtil` class definitions.
Generated resolver blocks and the three known build-injected XR preloads were
removed from source inputs afterward; persistent camera dependencies remain.
The APK was **not installed** and no ADB command was sent to the phone during this
work. This build proves compilation/package compatibility, not device validation
of the new building-data code or visuals.
Post-build live Console ground truth reported zero errors and four vendor warning
entries (the package-alteration warning was emitted twice); no application warning
was observed.

## Remaining live integration

This slice supplies the import/read/match/render boundaries. It does not enable
Streetscape Geometry in the app or place these meshes over the camera.

1. Load/cache bounded tiles for the neighborhood, with visible attribution and a
   clear offline/missing-data state. The current download/conversion step is an
   offline development tool, not a phone-side global tile service.
2. Derive and validate the catalog-to-AR-world transform from geographic poses
   and the XR origin. Footprints provide horizontal geometry, not terrain or
   building-base altitude. Do not substitute a guessed axis/altitude conversion.
3. Enable Streetscape only after session dependencies are ready; observe
   added/updated/removed trackables and session resets. The SDK owns its meshes;
   preserve that ownership. Match on a bounded observation cadence and keep
   unmatched/ambiguous observations unclaimed.
4. Feed canonical identity/footprints into the existing building renderer and
   alignment presentation gate; handle tile/session removal and origin changes.
   Then validate physical placement, camera stability and no-wobble behavior on
   the device. Plan 1 lock acceptance subsequently passed in the owner's walk;
   see [the measured aggregate](alignment-lock-field-summary.json). Physical
   building-placement acceptance remains separate.
5. Add deterministic ground props, the map/avatar view and the view-switch
   gesture. Dense neighborhood generation and device rendering remain unprofiled.

Subsequent progress: the owner approved a first live-placement build using
temporary session identities. The guarded Streetscape lifecycle and observed-mesh
styling layer are implemented and installed; canonical map placement/matching
remains separate. See [the live-placement report](live-building-placement.md).
