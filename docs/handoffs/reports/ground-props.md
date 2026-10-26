# Shared seeded ground props

Subsequent integration: the [Brooklyn map pilot](nyc-brooklyn-map.md) now wires
these geographic candidates into World map mode with actual green-space, road,
water and building eligibility. That build is installed on the S25. Terrain is
schematic in the map; live AR altitude/anchoring remains unimplemented. The
uninstalled build and unwired status below describe this earlier standalone slice.

Implemented while the owner ran the first live-building outdoor check. No ADB
command was sent, no APK was installed, and the running phone session was not
inspected or paused during this work.

## Delivered

- `World/GeoCell` defines a versioned geographic grid with fixed latitude bands
  and power-of-two longitude columns that thin toward the poles. Cell identity
  handles ±180° aliases and both poles. Queries are bounded to 128 m and 4,096
  cells; exact inclusion is checked in the supplied WGS84 metric frame.
- `Fantasy/GroundPropPlanner` uses specified UTF-8 stable hashing with
  `FantasyEarth/props/v1/<world-seed>/<cell-key>`. Occupancy, within-cell position,
  kind, scale and yaw are independent of query order, culture, local frame and
  camera/map presentation. Changing biome alters the look without moving the
  candidate. These are procedural decorations, not observed vegetation.
- Each immutable `GroundProp` retains geographic position, local east/up/north,
  versioned identity, seed, kind, biome, scale and yaw. World and Fantasy remain
  pure C# assemblies with no Unity references.
- Eligibility requires an explicit `eligibleGroundUpM` callback. Null, non-finite
  or out-of-range heights produce no prop. The caller must establish that a
  surface is eligible, excluding water, roads and unsupported/unknown terrain.
  The generator supplies no guessed altitude or permissive fallback.
- `World/FootprintClearance` checks actual concave building rings. Placement
  excludes a prop's conservative radius plus 0.75 m from any supplied footprint;
  inexpensive bounds checks prune distant obstacles first.
- `Rendering/GroundPropMeshBuilder` creates low-poly vegetation, boulders and
  glowing rune stones, batching at most 512 props into one vertex-colored mesh.
  Vegetation becomes broadleaf trees, cacti, snow-covered conifers or palms by
  biome. Existing shared material/shader support is reused, without adding a
  package, material instance per prop or child object per prop.
- `GroundPropChunkView` owns its transient mesh, restores it after disable/enable,
  and releases it on disable/destruction or replacement. Inputs are runtime-only;
  no geographic positions are serialized into a scene. Map mode stays visible
  without camera tracking; camera mode uses rebuilt blend and hides immediately
  without tracking. Switching presentation does not rebuild geometry.

## Review the art and placement

- [Four-biome overview](../../../ar-game/Docs/Previews/GroundProps.png)
- [Temperate garden](../../../ar-game/Docs/Previews/GroundPropsTemperate.png)
- [Snowy garden](../../../ar-game/Docs/Previews/GroundPropsSnowy.png)

`AgentScripts/PreviewGroundProps.cs` creates an isolated temporary scene with four
synthetic gardens at a synthetic geographic origin. All four use the same cell
identities and eligibility mask, with a path, water strip and building exclusion.
It generated **36 props and 12,048 prop vertices across four chunk meshes**, using
one shared prop/building material. Five actual URP images were rendered; Temperate,
Snowy and Tropical were visually inspected. The other garden materials/objects
are temporary and are released after rendering. No preview scene is saved and
the active scene/build list is restored.

Reproduce from the Unity project with the running pinned Editor:

```bash
unity command script run --file AgentScripts/PreviewGroundProps.cs \
  --entry PreviewGroundProps.Run --project-path "$PWD" --format json
```

Images are written under ignored `Temp/ground-prop-previews/`. The tracked images
are synthetic; no device surroundings, private coordinates or downloaded map
extracts were used.

## Validation

- Test-first missing-type failures preceded the cell/planner and rendering code.
- **175/175 FantasyEarth EditMode cases pass**, zero failed/skipped: the prior 159
  plus 16 new cases. Coverage includes fixed version-1 cell reference keys,
  longitude aliases, both poles, metric-circle coverage across difficult frames,
  culture/call-order repeatability, overlapping local frames, world seeds, biome
  appearance without position changes, unknown/invalid ground, concave obstacle
  clearance, repeatable finite bounded meshes, shared camera/map visibility and
  mesh ownership/re-enable behavior.
- A mesh test caught boulder/moss bases extending below the supplied surface.
  Their local geometry was raised to meet the surface; the bound test was retained.
- Editor compilation completed without errors. After the tests and GPU preview,
  live Console ground truth reported zero errors and zero warnings. Historical
  missing-type errors remain in Pipeline's event cache.
- No change to World scene, AR configuration, alignment thresholds, runtime
  controller, input, packages or camera settings was needed for this slice.

Validation records are in the ignored foundation ledger (`props-full-tests`,
`props-preview`, `props-preview-console`, `props-android-build`).

Android development build `build_9cb7fc98817a` succeeded in 78.097 seconds, with
zero errors and three known vendor test-image/package warnings. The live Console
after building has zero errors and four vendor warning entries (the package
warning was emitted twice). APK: `Builds/fantasy-earth-ground-props-check.apk`,
75,137,039 bytes, SHA-256
`3b291ad38c4cc12af879e628b68addaac7378e0bd641eab6d23ffdc1d302f325`.
It contains only World (`level0`). The APK verifier confirmed both required
Google location/auth class definitions again. This cached resolver pass did not
add generated Gradle blocks; persistent explicit dependencies remained present.
The three known build-injected XR preloads were removed through PlayerSettings
afterward. No scene, build-list, Gradle or Player-settings diff remains.

This build was **not installed**. It confirms compilation/package compatibility;
the new prop runtime path remains unwired and is not claimed as device-validated.

## Integration contract and remaining work

Call `GroundPropPlanner.Generate(frame, radiusM, biomeAt, eligibleGroundUpM,
canonicalBuildings, worldSeed)` only when shared world/ground data is ready.
Generate/cache on chunk or data changes, not every frame. The biome lookup must
use each candidate's geographic point, so callers near biome boundaries agree.
Unknown/incomplete building or ground data should keep the affected surface
ineligible. Query results cap at 1,024 props; split into chunks of at most 512 for
rendering instead of silently dropping results. Empty input renders nothing.

The view consumes coordinates in a fixed local east/up/north frame. Map and camera
must use the same geographic candidates and canonical eligibility data. Camera
integration still requires a validated geographic-to-AR-world transform and a
known eligible terrain height; do not parent these coordinates directly under a
session-space origin or derive terrain altitude from camera height. Pass the
existing gate's blend/tracking to camera presentation; map presentation is
independent of camera alignment.

This slice is **not wired into World or installed on the phone**. Terrain/map data
adapters, runtime chunk loading/removal, live prop anchoring, real slope/surface
eligibility and physical placement/performance tests remain. No two-device,
terrain-alignment or frame-time/thermal claim is made from deterministic Editor
tests and synthetic previews. Stable candidate generation is ready for those
adapters; the owner's running live-building test remains the current device build.
