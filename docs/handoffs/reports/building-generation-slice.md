# Plan 2 first slice — reusable fantasy buildings

Implemented 2026-10-25 after the owner requested progress on other tasks and deferred alignment fine-tuning.

## Delivered

- `World/BuildingFootprint` holds a stable building ID, height and an immutable local east/north outer ring. It validates finite coordinates, size, winding, duplicate edges and self-intersections. Winding, closing vertices and starting corners normalize to one canonical ring.
- `World/FootprintTriangulator` triangulates simple concave footprints without filling missing corners. It removes redundant collinear vertices.
- `Fantasy/StableSeed` uses specified FNV-1a over UTF-8. `BuildingRecipe` combines a versioned world-seed/building-ID key with dimensions and biome. Results do not depend on call order, process string hashing, culture or Unity's random state.
- `Rendering/BuildingMeshBuilder` makes footprint-shaped walls and ridge-clipped roofs, with timber framing, battlements, small roof spires, windows, doors and banners. One vertex-colored mesh and one shared building material serve each building; individual windows are not GameObjects.
- `Rendering/ProceduralBuildingView` regenerates from serialized footprint data in both Edit Mode and runtime. It releases its transient meshes on rebuild, disable and destruction. The saved-scene reload check caught a deferred-initialization problem; a failing enable regression was added, and immediate OnEnable reconstruction fixed it.
- `Scenes/BuildingPreview.unity` is an isolated offline scene with 12 synthetic buildings: three architectural styles in each of four biome palettes. It is not in the Android build list; World remains the sole application scene.

## Review it

Open `ar-game/Assets/FantasyEarth/Scenes/BuildingPreview.unity` in Unity. Use Scene view to orbit the four groups, or the preview camera to inspect the overview. The generated meshes rebuild automatically when the scene opens.

Saved Unity GPU renders:

- [Four-biome overview](../../../ar-game/Docs/Previews/BuildingPreview.png)
- [Temperate close-up](../../../ar-game/Docs/Previews/TemperateBuildings.png)

To regenerate the scene, close BuildingPreview first, then run:

```bash
unity command script run --file AgentScripts/BuildBuildingPreview.cs --entry BuildBuildingPreview.Build --project-path "$PROJ" --format json
```

The builder preserves existing material GUIDs and the Android build list, creates the preview additively and restores the previously active scene. Four individual biome images and the overview are written to ignored `ar-game/Temp/building-previews/`.

## Validation

- Test-first compilation failed on the missing footprint/recipe types, then on the missing mesh builder/view.
- **134/134 FantasyEarth EditMode cases pass**, including 13 building cases: concave area coverage, immutable/canonical rings, invalid geometry, a known FNV vector, culture/call-order determinism, architectural variety, finite/repeatable bounded meshes, biome palettes and mesh ownership/enable lifecycle.
- Five actual URP GPU renders completed. Temperate, Snowy and Overview were visually inspected. No shader or Render Graph errors were observed.
- Reopening the saved scene rebuilt all 12 building meshes and 19,008 vertices automatically, with one shared building material.
- Before the Android build, live Console ground truth reported 0 errors and 0 warnings.

## Android build check

Final development build `build_b99a5b0e4b01` succeeded in 23.744 seconds with 0 errors. Its report contains two vendor test-image warnings; the live Console has 0 errors and three known vendor warnings, including the immutable SDK test-image warning emitted after the build report. No application or shader error was observed.

The APK is 75,033,002 bytes, SHA-256 `76c7085f6bbb82968234541b0c05b38fc2e05b24c9d70bf44c82a81207ea1fd0`. It contains only `level0` for World. The required `FusedLocationProviderClient` and `GoogleAuthUtil` class definitions were verified again after this incremental build, confirming the dependency repair survives rebuilding. Generated XR preload settings were removed from the source diff afterward.

The phone retains the repaired camera APK that the owner confirmed working. Building visuals are reviewed in the offline Editor scene; this build check does not establish device rendering or anchored appearance of the new building shader.

## Camera repair and deferral

The earlier black camera regression was a missing Google location/auth dependency in the incremental APK. Explicit persistent Gradle dependencies and an APK class-definition verifier fix that build path. The repaired APK is installed and the owner confirmed camera video is back. See [camera-dependency-fix.md](camera-dependency-fix.md).

Alignment fine-tuning remains deferred. The 2 m entry limit is still provisional and no actual new-build lock is claimed.

## Next integration

This completes reusable geometry and an art prototype, not live rebuilt mode. The preview footprints are synthetic; no real location or map IDs were committed. Footprints currently support simple outer rings of 3–64 vertices and no courtyard holes. Mesh generation is synchronous at creation and has not been profiled for a large neighbourhood.

The next work is loading real building footprints and provenance, matching ARCore Streetscape meshes to stable building IDs, connecting the shared generator to both views, adding seeded ground props, and implementing the map view. Anchored appearance and no-wobble acceptance require later device testing. Building visuals are not yet displayed over the phone camera.
