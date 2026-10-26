# Brooklyn waterfront map pilot

Implemented and validated in Unity and on Android on 2026-10-25. The owner
confirmed map visuals, drag/pinch, Credits open/close and live camera return all
work on the S25. An in-coverage location/heading walk remains pending.
This is a bounded DUMBO/Brooklyn Bridge Park pilot, opening at Pier 1.

## Delivered

- A bundled offline Overture `2026-10-21.0` map pack covers public bounds
  `[-74.005, 40.694, -73.983, 40.710]`. It contains **1,268 building parts**,
  **1,815 road/path surfaces, 878 green surfaces and 13 water surfaces**.
  Source identities, versions, height-estimate flags, contributor metadata,
  input hashes and skipped geometry counts accompany the derived data.
- The reproducible converter reuses the canonical footprint importer and clips
  surface polygons to coverage. Topology-preserving simplification and constrained
  triangulation retain surface holes. Roads use explicitly identified class width
  estimates. Unsupported building holes, elevated geometry and oversized outlines
  are counted, rather than converted to invented geometry.
- Pure World map data, viewport and eligibility rules remain separate from Unity.
  A map-specific building detail level omits facade decoration while retaining
  the existing seeded architecture. Owned building batches and three surface
  meshes share the existing painted material. Nearby shared geographic props
  appear on known green space, excluding mapped roads, water and footprints.
- A north-up tilted camera, touch pan/pinch, bottom Map/Camera button and swipe
  handle are wired into World. Keyboard Tab and mouse pan/wheel support Editor
  review. Credits use uGUI/TMP, respect the safe area and include source/license
  links. The existing three-finger diagnostics gesture remains available.
- Map rendering switches the AR camera's rendering without tearing down the
  native session. Live-building preparation pauses in map mode. Loaded geometry
  is reused when returning to the map; it is not regenerated on every toggle.
- A player marker, accuracy disc, reported heading and follow/recenter behavior
  use existing Geospatial samples without requiring alignment lock. Only valid
  in-coverage samples with horizontal accuracy at most 250 m show a marker;
  accuracy above 25 m is labelled approximate. Unavailable/outside-coverage
  position explicitly shows NYC preview and never invents a player at Pier 1.
  No additional location provider, backend or device-coordinate upload was added.
- ODbL text, attribution and a pack manifest are bundled alongside the database.
  The in-app map footer and credits retain visible attribution.

Reproduction and pack-extension instructions:
[Tools/maps/README.md](../../../ar-game/Tools/maps/README.md).
Pack SHA-256:
`97b1c83b7be3e42947fa0f292febdf263f79549c5ba090a2c0b4042fd0125524`
(3,508,595 bytes). Raw public extracts remain ignored under `ar-game/Temp/`.

## Validation

- **182/182 FantasyEarth EditMode cases passed**, zero failed/skipped. Seven new
  cases cover real pack/provenance/coverage, triangle holes, schema rejection,
  viewport/follow bounds and no invented marker, map mesh winding/detail, and
  shared prop identity across frames. The existing World test checks every
  serialized map dependency.
- **5/5 Python map-converter cases passed**, including courtyard area/holes,
  clipping, source identity and width metadata, unsupported surfaces, and atomic
  output preservation after invalid input. Existing importer tests were not
  rerun for this slice.
- Actual Editor Play Mode loaded the full pack with one EventSystem and correct
  preview labels. A script exercised pan, zoom, recenter, credits open/close and
  Map → Camera → Map, confirming the AR camera was re-enabled. Console ground
  truth afterward was zero errors/warnings. This does not prove physical camera
  video or an in-coverage device marker.
- Isolated URP previews rendered 1,268 buildings, 2,706 surfaces and 174 nearby
  props, totalling 326,619 vertices. The final Pier 1 image was visually inspected:
  [waterfront](../../../ar-game/Docs/Previews/BrooklynWaterfront.png) and
  [coverage overview](../../../ar-game/Docs/Previews/BrooklynMapOverview.png).
  [Runtime UI preview](../../../ar-game/Docs/Previews/BrooklynMapUi.png) shows the
  controls before the opening center moved from inland DUMBO to Pier 1. All images
  contain public map data, with no private position marker or camera surroundings.
- Final Android ARM64 IL2CPP development build **`build_06c082e3ff3e`** succeeded
  in 8.287 seconds with zero errors and two vendor test-image warnings. An earlier
  complete map build took 78.821 seconds; the final build reused that compilation.
  Final live Editor Console had zero errors and three vendor warnings, including
  the existing immutable XR package warning. No package cache files were edited.
- APK verification found actual definitions of both `GoogleAuthUtil` and
  `FusedLocationProviderClient`. World is the sole packaged scene (`level0`).
  Generated resolver blocks and the three known injected XR preloads were removed
  from source settings after building; persistent explicit Java dependencies remain.
- Installed with `adb install -r` on authorized Galaxy S25 `R3CY70NJ2FD`, retaining
  consent and private logs. Cold launch succeeded. Coordinate-free on-phone
  diagnostics confirmed map readiness with **1,268 buildings, 2,706 surfaces,
  254,571 vertices before nearby props**, about 1.5 seconds after opening. ADB
  supplied a pan gesture and exercised Map → Camera → Map. A single readiness
  event across both openings confirms reuse. Installed APK hash matches the build.
  See the [coordinate-free device record](nyc-map-device-summary.json).
  Current-process Unity/crash error
  count was zero during the smoke check. The owner subsequently confirmed the
  map visuals, drag/pinch, Credits open/close and restored live video all work.
  This owner confirmation is separate from the automated smoke evidence.

APK: `ar-game/Builds/fantasy-earth-nyc-map.apk`, 75,174,061 bytes, SHA-256
`e9c99a877ac6993f9b86b8199340aabf01e104dbdab4bdf1b64dbe45be14cf00`.
Ignored Pipeline evidence is in the foundation ledger under `map-final-tests`,
`map-play-*`, `map-waterfront-preview-corrected`, `map-final-android-build`,
`map-final-build-status`, `map-clean-build-inputs` and `map-final-console`.

## Limits and next steps

The user's live-building walk produced persistent buildings at incorrect physical
positions. That physical-placement failure remains open in the
[live-placement report](live-building-placement.md). The map uses a geographic
frame independent of the AR transform and does not fix or conceal that offset.

Phone map visuals, drag/pinch, credits and live camera return are accepted. Next
validate the marker/follow/heading outdoors inside this pack's public coverage. The current
location source requires existing consent, precise permission and ARCore Earth
tracking; independent map-only location is future work. Invalid location affects
the marker, not offline browsing. No live in-coverage marker test is claimed.

Terrain is a schematic flat plane, not measured AR altitude or slopes. Bridge
paths are flattened, road widths can be estimates and some buildings are omitted.
Props are procedural decorations and the runtime cache is capped at 32 local
chunks / 512 props; no eviction or full-city streaming is implemented. Height,
route safety, sustained frame time, thermals and two-device agreement have not
been field-validated. Address search, routing, more neighborhoods and multi-pack
selection/downloads are subsequent features.

The prior live-building APK (`c47547b6…`, build `build_b11d1aab1679`) and the
camera/lock-confirmed APK (`508450cb…`, build `build_99fecdc2f11b`) remain locally
available for rollback. No application data or logs were cleared.
