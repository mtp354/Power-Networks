# Task 10 — World scene and first Android run

Status: complete. The first walk failed the original lock threshold; a later walk on the calibrated, camera-repaired APK recorded 82 Locked samples and the owner confirmed seeing lock. Task 10 Step 5 now passes; historical runs remain documented below.

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

The application now delays enabling Extensions until the camera AR session is tracking and Geospatial consent/permission are present. This preserves camera-only AR without modifying vendor packages. The scene regression test first failed against the enabled-at-start scene, then passed after the targeted scene correction. A rebuilt APK survives the delayed camera prompt. The updated APK also survived the owner's positive-consent outdoor run in the same process, with Earth tracking and no new crash. The lock criterion was not met because no sampled accuracy pair satisfied both entry limits.

## Outdoor run result — 2026-10-25

The owner completed the walk and confirmed the live camera kept updating and the three-finger diagnostics panel worked. The updated app remained in the same process through the walk and return. No new native crash or application Unity/AndroidRuntime error was observed; the crash buffer retains only the earlier pre-fix failure.

After pausing and flushing, the coordinate-redacted snapshot of session `20261025-182606.jsonl` contains **1,797 valid records**, including **1,099 Earth-tracking records**. The session spans 1,101.927 seconds and includes startup and the indoor return, so whole-session accuracy statistics must not be interpreted as outdoor-only measurements.

| Measurement | Result |
| --- | --- |
| Locked records | 0 |
| Sampled pairs meeting both entry limits | 0 |
| Best paired accuracy, normalized to the current entry limits | 1.87 m horizontal / 1.41° yaw |
| Tracking-sample horizontal accuracy, minimum / median | 0.68 m / 7.21 m |
| Tracking-sample yaw accuracy, minimum / median | 1.38° / 37.69° |
| Locked horizontal/yaw medians | Unavailable — no lock |
| Time to first lock | Unavailable — no lock |

Accuracy converged late in the session: the minute-13 medians were 1.88 m / 1.44°, and minute-14 medians were 1.90 m / 1.53°. The current gate requires ≤1.5 m horizontal **and** ≤2° yaw continuously for **1 second**. The individual minimum horizontal and yaw values occurred at different times. None of the logged pairs qualified together; no lock was observed between records either, since alignment transitions force an immediate log entry.

This evidence supports a successful camera/Geospatial integration and a failed strict lock acceptance check. It does not establish a gate logic defect. The aggregate evidence is saved in [task-10-field-summary.json](task-10-field-summary.json); precise coordinates remain on the phone.

## First-run acceptance decision (historical)

At this checkpoint Task 10 Step 5 remained open until an actual `Locked` sample was recorded. The late-run measurements suggested evaluating a 2.0 m horizontal entry limit while retaining the 2° yaw requirement. Calibration and fresh-device acceptance subsequently completed as described below.

See [the device guide](../../FANTASY_EARTH_DEVICE_TESTING.md) for reproducible commands and coordinate-safe analysis. APKs, raw session logs and credential downloads remain uncommitted.

## Authorized calibration follow-up

After this field result, the owner authorized the next calibration step. The default horizontal entry limit is now 2.0 m, selected by replay of the actual gate against coordinate-free accuracy data; yaw, dwell and exit behavior remain as above. Before that build, 121 Unity EditMode cases passed. This preserves the original run as evidence: its 0 actual locks have not been changed into replay predictions. See [the calibration report](alignment-calibration-report.md) for the repaired APK and completed fresh-device check.

## Final outdoor acceptance — 2026-10-25

The owner reported successful alignment lock on the repaired calibrated APK.
Phone-side coordinate redaction and the paused, flushed session log independently
confirm **82 Locked records out of 240**, first lock at **74.906 seconds**, and
locked medians of **0.88 m horizontal / 1.36° yaw**. Observed locked intervals total
41.958 seconds, excluding the 799.970-second sampling gap. The installed APK hash
matches camera repair build `build_99fecdc2f11b`; the subsequent building-data APK
was not installed. The running process was still present at collection, but no
continuous-process or continuous-lock claim is made across the unsampled interval.

Together with prior camera, permission-fallback, diagnostics and startup checks,
this completes Plan 1's required device acceptance. It does not validate physical
placement of rebuilt buildings. See [the measured aggregate](alignment-lock-field-summary.json)
and [calibration report](alignment-calibration-report.md) for exact evidence.
