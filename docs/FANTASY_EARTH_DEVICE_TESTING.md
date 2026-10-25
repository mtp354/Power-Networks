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

Poll `build status` until `completed` and `Succeeded`. This installed CLI requires a JSON array for `--options`. Then:

```bash
adb devices -l
adb -s DEVICE_SERIAL install -r "$PROJ/Builds/fantasy-earth-dev.apk"
adb -s DEVICE_SERIAL shell am start -W -n com.mtp354.fantasyearth/com.unity3d.player.UnityPlayerGameActivity
```

Only `Assets/FantasyEarth/Scenes/World.unity` is in the build list. `Builds/` is ignored. The Android resolver may insert temporary Gradle dependency blocks and XR packages may insert preloaded assets during a build; inspect the diff before committing those generated changes.

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

With precise location allowed, go outdoors on a Street View-covered street for about five minutes, walking and panning. Look for `Earth: Enabled`, improving accuracies and `Alignment: Locked` at least once. The current gate defaults require horizontal accuracy ≤1.5 m and yaw accuracy ≤2° for 2 seconds. Plan 1 logs alignment; it always renders the painted camera view. Rebuilt buildings and the map are later plans.

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
