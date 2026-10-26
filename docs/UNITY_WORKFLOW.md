# Unity connection and validation

Verified from Codex on 2026-10-25 against commit `a566ec9`. The existing Unity CLI and Pipeline package work; no additional Unity MCP package is required for this workflow.

## Project and installed tools

- Project: `/home/matt/Projects/Power Networks/Power-Networks/ar-game`.
- Editor: `6000.3.27f1` (Unity 6.3 LTS), installed with Android and iOS Build Support.
- CLI: `/home/matt/.local/bin/unity`, version `1.0.0-beta.8`.
- Project package: `com.unity.pipeline` `0.9.1-exp.1`.
- Verified Pipeline endpoint: local port `7800`. The port can change on another launch; discover it with `unity status`.

## Open and connect

The Editor must be running on this project's `ar-game/` folder. Unity Hub alone does not host the project's Pipeline server.

```bash
PROJ="/home/matt/Projects/Power Networks/Power-Networks/ar-game"
unity open "$PROJ" --editor-version 6000.3.27f1 --format json
unity status --project-path "$PROJ" --format json
```

Allow the initial import and script compilation to finish. The successful status response has `count: 1`, the exact project path, version `6000.3.27f1`, and `state: ready`. A launch command's successful exit alone does not prove that the Editor is ready.

On 2026-10-25 the owner confirmed the Editor was closed. Codex opened it with the command above, waited for import, and verified a responsive Pipeline connection.

## Codex host access

This session's default shell runs in an isolated process namespace. Even after the Editor started, a sandboxed `unity status` returned `STATUS_NO_INSTANCES`, while the same check with approved host access returned the correct ready instance.

For editor-facing commands in this Codex environment, use the shell tool's `sandbox_permissions: "require_escalated"` with a specific justification. This succeeded for opening Unity, reading status/Console, configuring auto-tick, and running tests. Ordinary source-file work can stay in the workspace sandbox. Do not disable the sandbox or install a second bridge to address this discovery issue.

If a future connection fails, distinguish these cases:

1. Editor closed: open this project with the pinned Editor.
2. Editor still importing: wait for import/compilation before retrying status.
3. Editor open but invisible to the sandbox: repeat the status check with approved host access.
4. Editor reports a dialog or compile failure: record the actual error and address that specific condition.

The installed CLI does not support the newer `unity status --until-ready` or `unity commands` forms shown in some skills. Use individual status checks and `unity command --query <term>` with this installed version.

## Verified command forms

Run the following through host access when invoked from this Codex environment. Keep the project path explicit to avoid targeting another checkout.

```bash
PROJ="/home/matt/Projects/Power Networks/Power-Networks/ar-game"

# Keep the Editor responsive while unfocused, at about 60 ticks/second.
unity command editor set_auto_tick --enable true --interval_ms 16 --project-path "$PROJ" --format json

# Read status and Console without clearing existing evidence.
unity command editor status --project-path "$PROJ" --format json
unity command console get --project-path "$PROJ" --format json

# Compile after code changes; poll recompile_status if asynchronous.
unity command script recompile --project-path "$PROJ" --format json
unity command script recompile_status --project-path "$PROJ" --format json

# Run the FantasyEarth suite; obtain terminal results before reporting success.
unity command test run --mode EditMode --filter FantasyEarth --project-path "$PROJ" --format json
unity command test status --project-path "$PROJ" --format json
```

The auto-tick command is **`editor set_auto_tick`** in this package. Discover it with `unity command --query tick`; searching for `autotick` returns no commands in this version.

The test run used during takeover returned its completed results synchronously. An asynchronous run must instead be polled to completion; a successful trigger response alone is not a test verdict. Do not rerun an unchanged, passing suite just to obtain another status result.

## Verified baseline

On 2026-10-25 at 16:28:13 UTC:

- 47 FantasyEarth EditMode cases discovered: 33 biome cases and 14 World cases.
- 47 passed, 0 failed, 0 skipped, 0 inconclusive.
- Compilation reported no errors.
- Console after testing: 0 errors, 0 warnings.
- No scene, package, Player setting, or production source change was needed to establish the connection.

Unity's local NUnit report is `/home/matt/.config/unity3d/mtp354/Fantasy Earth/TestResults.xml`. This is a generated, replaceable report, not a committed artifact. Future test runs may replace it. Never print the authentication token from `Library/Pipeline/.unity-pipeline-port`; the CLI reads it automatically.

## Completed through Task 8

The 2026-10-25 Task 8 run contains **108 passed, 0 failed, 0 skipped** EditMode cases, including bundled-data, alignment-gate, and session-formatter checks. The live Console ground truth after this run reported 0 errors, 0 warnings, and no compilation failure. Pipeline retains historical missing-type errors from the intentional test-first compilation runs; these are recorded separately from the current Console state.

## Tasks 9–10 device checkpoint

Task 9's rendering assets passed 111 EditMode cases and four synthetic GPU previews. Task 10 adds the World scene, application wiring, permissions, private session logging and diagnostics. After the native startup fix, **115 EditMode cases pass**, with 0 compile errors. The final development build `build_e3218f132aec` succeeded with 0 errors and three vendor test-image warnings. The live Console after building likewise reports 0 errors and those three warnings.

The APK is installed on the authorized Galaxy S25. The owner confirmed live camera video; logs establish both the denied-location fallback and `Earth: Enabled` after authorization. The updated APK also survives leaving the first-run camera permission prompt open. The owner completed the outdoor walk and confirmed the live camera and three-finger overlay. The updated process survived, and 1,797 valid session records include 1,099 Earth-tracking records. No lock occurred: the best paired accuracy was 1.87 m / 1.41°, outside the 1.5 m horizontal entry limit. Task 10's lock acceptance check remains open; see the field report before changing thresholds. See [device testing instructions](FANTASY_EARTH_DEVICE_TESTING.md) and the [Task 10 report](handoffs/reports/task-10-report.md) for current evidence and remaining acceptance.

## Alignment calibration follow-up

The owner authorized field-based calibration after the first walk. The horizontal entry limit is now **2.0 m**, retaining the 2° yaw limit and 1-second dwell. **121/121 EditMode cases pass.** Development build `build_f418eec5f7d0` succeeded, installed on the S25 and launched without observed application errors. The original field run remains a zero-lock result; the replay predicts a sustained lock at 2.0 m but requires fresh device confirmation. See the [calibration report](handoffs/reports/alignment-calibration-report.md).

## Camera repair and building-generation slice

The black camera/session regression was traced to missing Java location/auth dependencies in the incremental APK. Explicit persistent Gradle dependencies and `Tools/diagnostics/verify_geospatial_apk.py` now guard that path; the owner confirmed live camera video is restored. Alignment fine-tuning is deferred by request. The next completed slice adds reusable deterministic building geometry and an offline `BuildingPreview.unity` scene. All 134 EditMode cases pass, and the saved preview automatically reconstructs 12 meshes. See the [building report](handoffs/reports/building-generation-slice.md).

## Building data and matching follow-up

While the owner runs a fresh outdoor test, the next slice adds WGS84 local frames,
Overture normalization, runtime tile reading, canonical building catalogs,
polygon-overlap identity matching and a borrowed-mesh adapter. **153/153 Unity
EditMode cases and 8/8 Python cases pass**; a public Boston sample produced 23
rendered building parts and 23 correct identity round trips. Android build
`build_6fa6bf0ce4d7` succeeded with zero errors and three known vendor warnings;
the required location/auth Java definitions remain present. No APK was installed
or ADB command sent during this work. Live tile acquisition, geographic placement,
Streetscape lifecycle wiring and anchored rendering remain. See the
[data/matching report](handoffs/reports/building-data-matching.md).

## Plan 1 device acceptance completed

The owner returned from a fresh walk and confirmed alignment lock. The existing
phone-side coordinate redaction tool validated 240 JSON records, including 82
Locked samples. First lock was at 74.906 seconds; locked medians were 0.88 m / 1.36°.
Observed locked intervals total 41.958 seconds, excluding a 799.970-second logging
gap. The installed APK hash still matches repaired calibrated build
`build_99fecdc2f11b`; the building-data APK remains uninstalled. Task 10 Step 5 and
calibration acceptance are complete. See the
[measured aggregate](handoffs/reports/alignment-lock-field-summary.json).

## First live Streetscape placement

The owner approved temporary session identities for initial live placement.
World now has a guarded Streetscape controller and an owned observed-building
render layer; source geometry is preserved and fantasy facade styling fades with
alignment blend. **159/159 EditMode cases pass**, and three synthetic GPU renders
were inspected. Build `build_b11d1aab1679` succeeded with zero errors and two vendor
test-image warnings; the dependency verifier passed. The APK is installed on the
S25, preserving consent/data and the camera-confirmed APK for rollback. Initial
startup has no current-process Unity/crash errors; live geometry/physical placement
still requires outdoor validation. See the
[live-placement report](handoffs/reports/live-building-placement.md).

## Shared ground-prop preparation

While the owner tests live buildings, shared geographic prop candidates and
eligible-ground/building-clearance rules were added without changing World or
the phone. A reusable chunk renderer supplies four biome looks and separate map
and camera visibility policies. **175/175 EditMode cases pass**; five synthetic
GPU previews include 36 props in four shared-material chunks. Terrain adapters and
live anchoring remain. See the [ground-prop report](handoffs/reports/ground-props.md).

## Brooklyn waterfront map

The first offline NYC pack now renders in World, with uGUI/TMP controls and a
separate layer/camera. **182/182 EditMode cases and 5/5 Python map-converter cases
pass**. Actual Editor Play Mode exercised pan/zoom/recenter, credits and camera
switching; GPU previews show real waterfront geometry and shared props. Android
build `build_06c082e3ff3e` succeeded, passed Java dependency verification and is
installed on the S25. Coordinate-free device diagnostics confirm full map loading
and cached map/camera switching. The owner confirmed map visuals, drag/pinch,
Credits open/close and live camera return; in-coverage marker acceptance remains. Physical live-building placement failed the owner's walk and is still
unresolved. See the [map report](handoffs/reports/nyc-brooklyn-map.md).

The installed Pipeline play-mode commands are `editor.playmode start` and
`editor.playmode stop`, with no parameters. Finish asynchronous tests/builds
before entering Play Mode or running additive preview scripts. Test Runner can
temporarily restore an untitled scene; reopen saved World first. Map preview and
runtime check entry points are documented in
[Tools/maps/README.md](../ar-game/Tools/maps/README.md).
