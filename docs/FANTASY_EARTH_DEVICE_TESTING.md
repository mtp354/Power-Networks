# Fantasy Earth development device run

Use Unity `6000.3.27f1` with Android Build Support, SDK/NDK and OpenJDK. This project uses the Unity CLI/Pipeline connection described in [UNITY_WORKFLOW.md](UNITY_WORKFLOW.md).

## Google Cloud and signing

Enable the ARCore API in the owner's Google Cloud project. Create an Android OAuth client with:

- Package: `com.mtp354.fantasyearth`
- Development certificate SHA-1: `8E:1E:0D:3C:96:4E:94:1A:9F:FA:35:99:42:0B:B3:51:68:0A:70:59`

The owner confirmed this setup on 2026-10-25. The installed development APK's signing certificate matches that fingerprint. A different computer or keystore requires registering its actual signing fingerprint. Follow Google's [keyless authorization instructions](https://developers.google.com/ar/develop/authorization?platform=unity-arf). Android keyless authorization does not require importing the downloaded OAuth JSON into Unity.

## Build and install

Open `ar-game` in the pinned Editor and wait for compilation. In this Codex session, Editor and ADB commands require approved host access because the default sandbox cannot discover them.

```bash
PROJ="/home/matt/Projects/Power Networks/Power-Networks/ar-game"
unity command test run --mode EditMode --filter FantasyEarth --project-path "$PROJ" --format json
unity command build run --target Android --outputPath Builds/fantasy-earth-dev.apk --options '["Development"]' --confirm --project-path "$PROJ" --format json
unity command build status --project-path "$PROJ" --format json
```

Poll `build status` until `completed` and `Succeeded`. This installed CLI requires a JSON array for `--options`. Verify the required Geospatial Java class definitions in every APK before installation, including incremental builds:

```bash
python3 "$PROJ/Tools/diagnostics/verify_geospatial_apk.py" "$PROJ/Builds/fantasy-earth-dev.apk"
```

Then:

```bash
adb devices -l
adb -s DEVICE_SERIAL install -r "$PROJ/Builds/fantasy-earth-dev.apk"
adb -s DEVICE_SERIAL shell am start -W -n com.mtp354.fantasyearth/com.unity3d.player.UnityPlayerGameActivity
```

Only `Assets/FantasyEarth/Scenes/World.unity` is in the build list. `Builds/` is ignored. The Android resolver may insert temporary Gradle dependency blocks and XR packages may insert preloaded assets during a build. Keep the explicit location/auth dependencies outside resolver-managed blocks in `mainTemplate.gradle`: cached resolver passes can otherwise omit them from incremental APKs. Inspect generated changes before committing.

## Permissions and camera fallback

Allow camera access. The app presents Google's data-use notice before enabling Geospatial. “Camera only” keeps Geospatial disabled for that session; “Use location” records consent and asks Android for precise location. Denied or approximate-only location keeps the painted Temperate camera view available. ARCore Extensions starts only after consent, fine-location permission and camera AR tracking are ready. This also avoids its native telemetry coroutine running before a first-run camera prompt is answered.

For a deliberate camera-delay regression check on an installed development app, pause it first, revoke only its camera permission, relaunch, and leave the prompt open for at least ten seconds. The process must remain alive. Then allow camera and verify live video. Do not clear application data or session logs for this test.

```bash
adb -s DEVICE_SERIAL shell input keyevent KEYCODE_HOME
adb -s DEVICE_SERIAL shell pm revoke com.mtp354.fantasyearth android.permission.CAMERA
adb -s DEVICE_SERIAL shell am start -W -n com.mtp354.fantasyearth/com.unity3d.player.UnityPlayerGameActivity
adb -s DEVICE_SERIAL shell pidof com.mtp354.fantasyearth
```

To test denied-location fallback after consent has been recorded, use Android Settings → Apps → Fantasy Earth → Permissions → Location → Don't allow, then reopen the app and deny its request. The camera must keep updating with no crash. Restore “Allow only while using the app” and enable precise location for the outdoor test.

## Outdoor acceptance

A three-finger tap toggles the development overlay. It shows Earth state, tracking, horizontal/yaw accuracy, alignment, blend, climate class and biome. It does not display coordinates. In a non-development build the overlay and session logger are disabled.

With precise location allowed, go outdoors on a Street View-covered street for about five minutes, walking and panning. Look for `Earth: Enabled`, improving accuracies and `Alignment: Locked` at least once. The current gate defaults require horizontal accuracy ≤2.0 m and yaw accuracy ≤2° for 1 second. The 2.0 m horizontal entry limit was selected from the first S25 field trace; its fresh-device lock check is tracked in the [calibration report](handoffs/reports/alignment-calibration-report.md). Plan 1 logs alignment; it always renders the painted camera view. Rebuilt buildings and the map are later plans.

This acceptance check passed on 2026-10-25: the owner saw lock, and the repaired
calibrated APK recorded 82 Locked samples, first lock at 74.906 seconds and locked
medians 0.88 m / 1.36°. See the [aggregate evidence](handoffs/reports/alignment-lock-field-summary.json).
No repeat walk is needed to close Plan 1. Later anchored-building acceptance will
need its own device validation.

## First live-building build

The current live-placement prototype enables Streetscape only through the guarded
consent/permission/tracking path. It preserves and styles ARCore's observed
surfaces, using temporary session identities approved for this first test. World
remains the sole build scene. The installed APK is
`Builds/fantasy-earth-live-buildings.apk`; its build/hash/rollback information is in
the [live-placement report](handoffs/reports/live-building-placement.md).

The three-finger panel now shows Buildings status plus observed/prepared/drawn
counts. Prepared copies can exist before lock; drawn is zero at zero blend.
After lock, look for `LiveBuildings` and a positive drawn count. Pan across real
building corners/roofs and walk a few metres to check placement and stability.
Then test Home/reopen and recovery after lock. If nothing appears, report status
and the three counts: zero observations and rejected render candidates mean
different things. No coordinates or private screenshots are needed.

This device check remains distinct from Plan 1's completed lock test. The owner
subsequently reported persistent buildings in the wrong real-world positions;
physical placement failed and remains open. Map development proceeded separately.

## Brooklyn waterfront map pilot

The current installed build is `Builds/fantasy-earth-nyc-map.apk`; its exact
artifact, coverage and validation are in the
[map report](handoffs/reports/nyc-brooklyn-map.md). Tap **Map** near the bottom, or
swipe upward from the bottom handle area. Drag to pan and pinch to zoom. **Preview**
returns to Pier 1 when there is no valid local player. **Credits** opens the
scrollable data/license panel. Tap **Camera** to return to live video; a downward
swipe starting in the bottom handle area also returns. Three-finger diagnostics
remains available.

Offline browsing works without alignment lock. The location marker currently
uses existing ARCore Earth tracking, consent and precise permission. It appears
only inside this bounded Brooklyn pack with valid reported accuracy; unavailable
or outside-coverage location is explicitly labelled NYC preview with no invented
marker. **Follow me** recenters a valid marker after panning. Heading appears
only with a usable reported orientation. Test real follow/heading inside pack
coverage; changing preview center does not simulate a physical location.

The phone smoke test loaded all 1,268 buildings and 2,706 surfaces and returned
Map → Camera → Map without rebuilding. Owner visual acceptance of pinch, credits
and restored live video remains pending. Terrain and bridge heights are
schematic; this map is not navigation guidance.

`ErrorNotAuthorized` requires checking ARCore API enablement and the Android OAuth package/certificate pair. `Enabled` with no lock can reflect positioning quality or VPS coverage; record the real measurements before tuning thresholds.

## Inspect measurements without exporting coordinates

Logs are written under Unity's Android `Application.persistentDataPath`:

`/sdcard/Android/data/com.mtp354.fantasyearth/files/sessions/<UTC yyyyMMdd-HHmmss>.jsonl`

They are app-specific files containing precise coordinates. Leave raw logs on the phone. First press Home to pause the app and allow its writer to flush, then run:

```bash
python3 ar-game/Tools/diagnostics/summarize_session.py --serial DEVICE_SERIAL
```

The tool runs `sed` on Android to remove `lat` and `lon` before any records reach the host, validates the sanitized JSON, and prints record count, Earth states, locked record count, time to first lock and locked accuracy medians. A partially written final record means the app was read before it finished pausing; wait for pause, then retry. `--file` accepts only an already sanitized local file.

Do not commit APKs, raw session logs, field screenshots with private surroundings, or Google credential downloads. The root ignore rules cover the downloaded `client_secret_*.json` files.
