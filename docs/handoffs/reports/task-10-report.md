# Task 10 — World scene and first Android run

Status: implementation, build and installation complete; outdoor lock acceptance pending.

## Implementation

- World is the sole build scene, with AR Session, XR Origin, Input System tracked camera, AR background, Earth manager, Extensions, biome Volume and world-space motes.
- Six App components connect Geospatial sampling, climate lookup, biome presentation, alignment measurements, development diagnostics and JSONL recording.
- Google's data-use notice precedes Geospatial enablement. Camera-only and denied precise location retain the Temperate painted view. The runtime configuration is cloned, preserving the source asset.
- Extensions starts disabled and is enabled only after consent, fine-location permission and AR camera tracking. Native Earth pose reads require enabled/tracking Earth state.
- Development-only logs flush on pause/quit. The diagnostics tool strips coordinates on Android before parsing records or reporting aggregate measurements on the host.

## Validation on 2026-10-25

- 115/115 FantasyEarth EditMode tests pass after the startup fix; compilation succeeds.
- 3/3 Python diagnostics tests pass.
- Android ARM64 IL2CPP development build `build_e3218f132aec`: Succeeded, 0 errors, 27.501 seconds. APK: 50,227,232 bytes.
- APK SHA-256: `6e750a84f3b1becb9e1cbf6ffa2af7bc73526343bcda1d6e4a9c7248229570ab`.
- Three build/Console warnings concern immutable ARCore/ARKit SDK test-image libraries and their low-scoring test images; World has no image-tracking manager. No application compile errors or shader errors.
- APK signing SHA-1 matches the registered development fingerprint. Owner confirmed ARCore API/Android OAuth setup. APK contains the keyless auth dependency.
- Installed on Galaxy S25 SM-S931U1, Android API 37, with Google Play Services for AR.
- Owner confirmed the live painted camera works. A paused pre-fix session has 674 valid JSON records and includes `GeospatialNotEnabledByUser`, `LocationPermissionNotGranted`, and `Enabled`; no lock was recorded in that indoor session.
- Updated build remains alive while the camera permission dialog is left open well beyond the original crash interval. No new native crash or Unity/AndroidRuntime error was recorded during this check.
- Fresh read-only review of Tasks 9–10 found no Important or Blocking code defects. Generated XR preloads and resolver blocks were removed from the source diff after building; the original source preloads were retained.

## Startup defect and correction

The first build crashed seven seconds into its first launch while camera permission was pending. Matching IL2CPP debug symbols place the native call in Extensions 1.57's `ReportGeospatialCreatorPlatformWithDelay`, through `EarthApi.ReportGeospatialCreatorPlatform`. The SDK waits at most 60 frames for a native session, then invokes the native function even when no session is available. The same build ran after the camera permission had been granted.

The application now delays enabling Extensions until the camera AR session is tracking and Geospatial consent/permission are present. This preserves camera-only AR without modifying vendor packages. The scene regression test first failed against the enabled-at-start scene, then passed after the targeted scene correction. A rebuilt APK survives the delayed camera prompt. Positive-consent device regression and outdoor lock are still being checked.

## Remaining acceptance

The owner must finish the updated build's outdoor five-minute walk/pan on a Street View-covered street, with precise location enabled. Record at least one `Locked` sample, median locked horizontal/yaw accuracy and time to first lock. No such measurements are claimed yet. Keep Task 10 Step 5 open until that evidence is available.

See [the device guide](../../FANTASY_EARTH_DEVICE_TESTING.md) for reproducible commands and coordinate-safe analysis. APKs, raw session logs and credential downloads remain uncommitted.
