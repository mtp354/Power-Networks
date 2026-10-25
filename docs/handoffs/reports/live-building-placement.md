# First live building placement

The owner authorized a first live build using ARCore-observed building shapes
with temporary session identities, before local canonical map tiles are available.

## Behavior

- `GeospatialSampler` retains consent, permission and AR-session startup guards.
  Its runtime configuration begins with both Geospatial and Streetscape disabled;
  they become enabled only through the existing authorized session path.
- `LiveBuildingController` creates the native Streetscape manager only after
  Earth is tracking and Extensions is active. It consumes added/updated/removed
  observations immediately because the SDK reuses its event lists.
- SDK building poses are applied as local poses under `XROrigin.TrackablesParent`.
  Origin translation, rotation and offset are therefore inherited once. The
  controller follows updated trackable poses rather than guessing altitude from
  a map footprint or the camera's height. No separate persistent/geographic anchor
  is created in this first slice.
- `ObservedBuildingMesh` makes an owned copy, retaining every source vertex and
  triangle, including concave cutouts/courtyards represented in the source. It
  recalculates normals and applies biome/style colors only to the copy. Existing
  building recipes/palettes supply style; shader details add windows, frames,
  timber divisions or stone joints. This first slice preserves ARCore roof shapes;
  it does not add the canonical generator's spires/battlements to observed meshes.
- `ObservedBuildingLayer` uses one shared material plus property blocks. Shader
  dither visibility follows `AlignmentGate.RebuiltBlend`. Zero blend disables its
  renderers; tracking loss hides/removes them immediately. Accuracy-driven unlock
  with tracking retained follows the existing blend-out.
- Removal, disable, application pause, session loss and origin replacement clear
  owned render copies. A fresh SDK manager on resume discards its stale native
  handle cache. Borrowed SDK meshes are never modified or destroyed by app code;
  their resource lifetime remains with the SDK/Unity.
- Limits: 128 retained observations, 24 prepared render copies, 40,000 vertices
  per source, 250,000 owned vertices total, 150 m from camera to building bounds,
  at most one new mesh read/copy attempt each frame. Rejected observations retry
  after three seconds. Existing pose updates reuse meshes. These are bounds,
  not measured device performance budgets.
- The development overlay reports status and observed/prepared/drawn counts,
  owned vertices and skipped attempts. Development logs use the
  `[FantasyEarth.Buildings]` prefix and counts/status only, without coordinates,
  poses, trackable IDs or mesh data.

## Scene and data scope

`PatchLiveBuildings.cs` patched existing World objects without regenerating the
scene: two App components, wired references, the shared building material, and
camera far clip increased from 100 m to 250 m. The existing configuration asset
now requests Streetscape; the runtime clone still disables it during startup and
camera-only/permission fallback. World remains the only enabled build scene.
`BuildWorldScene.cs` retains this wiring for future regeneration.

No map download, new location provider, credential change or precise-coordinate
export was added. Temporary `streetscape-session:` identities can change after
restart/session reset; they are not stable map IDs or claimed canonical matches.
Canonical catalog matching and more architectural geometry remain later work.

## Validation

- Missing-type compilation failure was observed before adding the render layer.
  The first scene check failed while Streetscape/wiring was still absent.
- **159/159 FantasyEarth EditMode cases pass**, zero failed/skipped: the previous
  153 plus five render-layer cases and an unready-controller guard case. Cases
  verify exact source geometry preservation, borrowed ownership, transformed
  origin placement, mesh reuse, biome replacement, fade/visibility, teardown,
  rejection limits and safe initialization without native state. Scene checks
  verify the source manager is absent until runtime readiness.
- A lifecycle check initially exposed that a plain component does not receive
  EditMode disable callbacks. The explicit render owner now uses ExecuteAlways
  (with no update/native behavior), ensuring its temporary preview resources are
  also released in Editor workflows.
- Three actual URP GPU renders at blend 0, 0.5 and 1 were visually inspected.
  Zero blend contains no buildings; the concave fixture retains its missing
  corner. Shared source meshes still had no normals after rendering, proving the
  normal calculation affected only render copies. The current live Console
  before building reported zero errors/warnings; historical test-first errors
  remain in Pipeline's cache.
- [Synthetic observed-building preview](../../../ar-game/Docs/Previews/ObservedBuildings.png).
  Regenerate with `AgentScripts/PreviewLiveBuildings.cs`, entry
  `PreviewLiveBuildings.Run`; three outputs remain in ignored
  `ar-game/Temp/live-building-previews/`. No device surroundings were captured.

Validation records are in the ignored foundation ledger, notably
`live-final-tests`, `live-surface-preview`, `live-preview-console` and
`live-final-android-build`.

## Android build and initial smoke check

Development build `build_b11d1aab1679` succeeded in 27.102 seconds with zero errors
and two known vendor test-image warnings. The final live Console has zero errors
and three vendor warnings (including the post-build immutable test-image warning).
APK: `ar-game/Builds/fantasy-earth-live-buildings.apk`, 75,087,524 bytes, SHA-256
`c47547b63cbbac566347276bdb86d686127a439ee4b51a27a55db15f1d73df31`.
The package contains only World (`level0`); both required Google location/auth
class definitions were verified again. Generated resolver blocks/XR preloads
were removed from source inputs afterward, retaining persistent dependencies.

Installed successfully over the existing app without clearing data and cold
launched on the authorized S25. The initial process (`18031`) remained alive past
the earlier startup-crash interval, with zero current-process Unity error lines
and zero crash-buffer lines. Its coordinate-free building diagnostic initially
reported `WaitingForSession`, with zero observed/prepared/drawn buildings. This
establishes startup safety; it does not establish live native geometry arrival
or device rendering/placement before outdoor tracking is available.

## Device placement acceptance

Outdoor appearance/placement remains to be confirmed on the new APK. The earlier
Plan 1 lock result is valid for the camera-repaired APK and is not silently
extended to this new live-geometry build.

1. Keep precise location enabled. Open Fantasy Earth and use the three-finger
   diagnostics panel. Confirm camera video continues updating.
2. Outdoors where lock previously worked, pan slowly across nearby buildings.
   Look for Earth enabled, alignment locked and `Buildings: LiveBuildings`.
3. Check observed/prepared/drawn counts. Prepared copies may exist before lock;
   drawn should be zero at zero blend and positive after lock when geometry exists.
4. Check that styled walls/windows sit on real building surfaces, and stay with
   them when panning and walking a few metres. Record offset, rotation, height or
   wobble problems separately from lack of geometry. Report counts/status rather
   than coordinates or private surroundings.
5. Press Home and reopen while outdoors. Camera and source collection should
   recover; after lock, buildings should return without duplicates/stale copies.

Zero observations can indicate unavailable Streetscape coverage or unready
tracking; skipped attempts indicate rejected/out-of-range/over-budget source
geometry. A lock alone does not establish building alignment. This is a first
placement prototype: no stable map identities, ground props, map/avatar view,
person/car depth occlusion or device frame-time/thermal acceptance is claimed.

## Rollback

Before deployment, the exact installed camera/lock-confirmed APK was copied to
ignored `ar-game/Builds/fantasy-earth-camera-confirmed.apk` and its SHA-256 verified
as `508450cbecdc4423eeaa2723a10d06a8ae44bb731885b28d21c862daee71092d`.
If needed, install that APK with `adb install -r`; no application-data reset is
required. Existing consent and private session logs are retained.

Primary reference: [Google's Streetscape guide](https://developers.google.com/ar/develop/unity-arf/geospatial/streetscape-geometry).
Pose-space and cache behavior were checked against the pinned local Extensions
1.57 source (`ARStreetscapeGeometry`, `ARStreetscapeGeometryManager`,
`Translators.ToUnityPose` and `ARGeospatialAnchor`).
