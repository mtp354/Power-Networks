# S25 alignment calibration — 2026-10-25

Status: calibration, automated validation, APK build and installation complete; fresh device lock acceptance pending.

## Decision

The owner authorized this next step after reviewing the first field run. Change only the default horizontal entry limit from **1.5 m to 2.0 m**. Retain the **2° yaw limit**, **1-second entry dwell**, **3 m / 4° exit limits**, **0.5-second exit dwell**, immediate tracking-loss unlock and **0.75-second blend**.

The source trace converged near 1.9 m / 1.5° late in the run. This is a provisional positioning calibration for the painted-camera prototype. Rebuilt geometry alignment has not yet been implemented or visually validated.

## Replay evidence

`AgentScripts/ReplayAlignment.cs` runs the actual `AlignmentGate` in the connected Editor using 1,797 coordinate-free measurements from the first S25 session. Its input contains relative seconds, tracking, horizontal accuracy and yaw accuracy only. Each preceding sample is held until the next record, with 0.1-second updates; gaps over 1.25 seconds reset tracking. The approximately 2 Hz log cannot reconstruct intermediate frames, so these are predictions.

| Horizontal entry limit | Predicted lock transitions | Predicted locked records |
| --- | --- | --- |
| 1.50 m | 0 | 0 |
| 1.75 m | 0 | 0 |
| **2.00 m** | **1** | **184** |
| 2.25 m | 1 | 185 |

For 2.0 m, the predicted first lock occurs about 811 seconds after session start, including startup and earlier poor positioning. Predicted locked-sample medians are 1.90 m / 1.50°. These do not replace measured on-device lock statistics. See [the aggregate replay result](alignment-calibration-replay.json). The original field run and its zero actual locks remain recorded in [the Task 10 report](task-10-report.md).

## Tests

The new measured-accuracy cases (1.87 m / 1.41° and 1.92 m / 1.59°), the 2 m / 2° boundary and inclusive-entry test fail against the old default. With the calibrated default, **121/121 FantasyEarth EditMode cases pass**, including rejection above either entry limit, full dwell, exit hysteresis, invalid values and tracking loss. Compilation has no errors.

## APK and runtime smoke check

- Development build `build_f418eec5f7d0` succeeded in 26.550 seconds with 0 errors. The same three vendor test-image warnings remain; the live Console has 0 errors and those three warnings.
- APK: `ar-game/Builds/fantasy-earth-dev.apk`, 50,222,057 bytes.
- SHA-256: `eab9e5a1359395b862f056ae88aa6c448b6d647a73f3e1f1a79d164496336793`.
- Installed successfully over the previous development build on the authorized S25. The new process launched with no Unity/AndroidRuntime errors observed. Existing private session files, including the first field run, remain present.
- The previous APK is retained at ignored `ar-game/Builds/fantasy-earth-before-calibration.apk` for rollback. Downgrading the calibration requires installing that APK; no application-data reset is needed.
- No production scene, package, permission or log-format change was made. The generated XR build preloads were removed from the source settings after building.

## Reproduce the replay

Prepare ignored `ar-game/Temp/alignment-calibration-input.json` with the schema below. Use coordinate-redacted logs, relative timestamps and `-1` for unavailable accuracy. Do not include coordinates, absolute timestamps or unrelated session fields. Do not commit the input trace.

```json
{"samples":[{"seconds":0,"tracking":false,"horizontalM":-1,"yawDeg":-1},{"seconds":0.5,"tracking":true,"horizontalM":1.9,"yawDeg":1.5}]}
```

```bash
unity command script run --file AgentScripts/ReplayAlignment.cs --entry ReplayAlignment.Run --project-path "$PROJ" --format json
```

The script writes only its aggregate predictions to ignored `Temp/alignment-calibration-results.json`. It uses Pipeline's existing editor JSON dependency; the player package dependencies are unchanged.

## Remaining acceptance

The updated APK is installed and smoke-tested. Verify an actual lock with precise location enabled, preferably where the first walk reached about 2 m horizontal / 2° yaw. Record locked horizontal/yaw medians and time to first lock from that new session. Keep Task 10 Step 5 and the calibration device check open until an actual new-build lock is observed.
