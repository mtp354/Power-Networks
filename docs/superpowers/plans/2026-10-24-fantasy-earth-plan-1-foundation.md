# Fantasy Earth AR, Plan 1: Foundation and painted camera view

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the repo, put a Unity 6 LTS AR project with ARCore Geospatial inside it, and ship a development build to the owner's Galaxy S25. The build opens into the painted-fantasy camera view, styled by the real climate, and logs how well Geospatial positioning locks on during a walk.

**Architecture:** Pure C# assemblies hold all decision logic, with no UnityEngine references, so it can be unit tested in EditMode:
- `World` is the canonical real-world layer: geography and climate, with provenance.
- `Fantasy` interprets the real world, for example climate becomes a biome.
- `Positioning` is the alignment gate that later decides when rebuilt mode is allowed.
- `Diagnostics` is the session log format.

Thin MonoBehaviours in `App` read ARCore and drive rendering. A URP Render Graph renderer feature does the painterly full-screen pass, and per-biome URP Volume profiles do color grading and bloom. This is Plan 1 of 3. Plan 2 is rebuilt mode, using Streetscape Geometry and stable building IDs. Plan 3 is the map view and attribution UI.

**Tech Stack:** Unity 6000.3.27f1 (Unity 6.3 LTS), URP with Render Graph, AR Foundation 6, ARCore XR Plugin, ARCore Extensions `v1.57.0-arf6`, External Dependency Manager for Unity 1.2.190, Unity Test Framework (EditMode), and Python 3 (numpy, tifffile, imagecodecs, pytest) for the climate data tool.

**Spec:** `docs/superpowers/specs/2026-10-24-ar-fantasy-world-v1-design.md`

## Execution status — 2026-10-25

Tasks **1–10 are complete**, including fresh-device alignment lock acceptance on
the camera-repaired calibrated APK. Rebuilt building placement and the map remain
later-plan work.

| Task | Status | Implementation commit / evidence |
|---|---|---|
| 1: repo restructure | Complete | `7c49f6f` |
| 2: Unity LTS project | Complete | `7b5ad7d` |
| 3: ARCore and Android | Complete | `558a549`, follow-up resolver fixes; existing smoke APK |
| 4: World primitives | Complete | `64f4b34`; original 14 cases remain passing |
| 5: biome mapping | Complete | `a566ec9`; 33 cases validated during takeover |
| 6: climate dataset/model | Complete | `e7fb32b`, cache-verification follow-up `a17bf03`; generated 6,480,000-byte LFS asset, attribution, Python and real-location tests |
| 7: alignment gate | Complete | `da14188`; 36 additional EditMode cases |
| 8: session formatter | Complete | `649bea8`; 17 additional EditMode cases |
| 9: painted rendering | Complete | `12f6204`; four biome previews and device camera checks |
| 10: World scene/device run | Complete | `2ef32e1`, calibration `53b0743`, camera repair `b1a4b24`; 82 actual Locked records, first lock 74.906 s, median 0.88 m / 1.36° |

The latest source suite contains **175 passing EditMode cases**, with zero failures/skips, including later building-data, live-placement and shared ground-prop work. Plan 1 device acceptance used the earlier camera-repaired calibrated APK. A subsequent live-placement APK is now installed for its separate outdoor check; the ground-prop slice is not installed or wired into World. See `docs/handoffs/reports/` for completion reports and `docs/UNITY_WORKFLOW.md` for the working Editor connection. Older Tasks 1–5 checkboxes below are the original historical checklist; the status table and reports establish their current state.

Accepted setup adjustments supersede the original instructions below: Android minimum API **25**; use the **EDM4U bundled with ARCore Extensions**, with no duplicate OpenUPM package; retain installed iOS Build Support for Extensions' editor code. Session JSON additionally writes null for null EarthState or non-finite blend values to preserve JSON validity. The calibrated horizontal entry limit is **2.0 m**, with the original 2° yaw and 1-second dwell retained. Actual lock evidence is in `docs/handoffs/reports/alignment-lock-field-summary.json`.

## Global Constraints

- Repo root: `/home/matt/Projects/Power Networks/Power-Networks`. The path contains a space, so always quote it. Below, `$REPO` means this path and `$PROJ` means `"$REPO/ar-game"`.
- Unity editor: `6000.3.27f1`. Fallback `6000.0.85f1` is allowed **only** if Task 3's Protobuf check fails on 6000.3.27f1.
- ARCore Extensions: `https://github.com/google-ar/arcore-unity-extensions.git#v1.57.0-arf6`.
- Android only. minSdk 24, IL2CPP, ARM64 only, ARCore set to **Required**.
- Application ID: `com.mtp354.fantasyearth`. Product name: `Fantasy Earth` (working title). Company: `mtp354`.
- ARCore auth: **Keyless**. No API keys or other secrets in the repo; `ProjectSettings/ARCoreExtensionsProjectSettings.json` must contain `"AndroidCloudServicesApiKey":""`.
- Assemblies `FantasyEarth.World`, `FantasyEarth.Fantasy`, `FantasyEarth.Positioning`, and `FantasyEarth.Diagnostics` set `"noEngineReferences": true`. `FantasyEarth.World` references no other FantasyEarth assembly: the real-world layer never depends on the fantasy layer.
- Release builds show no HUD. The diagnostics overlay and session logs exist only when `Debug.isDebugBuild` is true.
- Precise location never leaves the phone except through ARCore's own service. Session logs live only in the app's private storage.
- Every number written to text uses `CultureInfo.InvariantCulture`.
- No paid services or assets in this plan.
- Commits end with the trailer `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
- Run EditMode tests: `unity command test run --mode EditMode --filter FantasyEarth --project-path "$PROJ"`, then poll `unity command test status --project-path "$PROJ"` until it reports completion. Expected: 0 failed.

## Review Focus

1. **Device locale uses comma decimals** (for example `de-DE`): session log lines must still be valid JSON with `.` decimals. Test in Task 8.
2. **Geospatial reports NaN, infinite, or negative accuracies, or tracking flaps on and off every frame:** the gate must never lock on bad data and never flicker. Tests in Task 7.
3. **Positions on the map's edges** (latitude ±90, longitude ±180, crossing the antimeridian): the climate lookup returns a valid cell and never throws an index exception. Tests in Task 4.
4. **Location permission denied, or ARCore auth fails** (EarthState is an error): the app stays in painted mode with the Temperate default and doesn't crash, and the overlay shows the error. Tested by `PositioningSample.FromEarth` in Task 7, plus a device check in Task 10.
5. **Ocean or no-data climate cells** (a pier, the coast, a ferry): the climate lookup gives Köppen `None`, which maps to Temperate, and nothing throws. Tests in Tasks 5 and 6.

---

### Task 1: Move Power Networks into `power-networks/`

`data/raw/` (2.5 GB) is untracked, so `git mv` would leave it behind. Use plain `mv`, then `git add -A`.

**Files:**
- Move: `src/`, `data/`, `ReadMe.txt`, `requirements.txt`, `.gitignore` into `power-networks/`
- Move: `docs/ROADMAP.md` and `docs/DATASETS_SAVED_FOR_LATER.md` into `power-networks/docs/`
- Keep in place: `docs/superpowers/`
- Create: `.gitignore` (repo root), `README.md` (repo root)

- [ ] **Step 1: Move the files and recreate the virtualenv**

```bash
cd "$REPO" && mkdir -p power-networks/docs
mv src data ReadMe.txt requirements.txt .gitignore power-networks/
mv docs/ROADMAP.md docs/DATASETS_SAVED_FOR_LATER.md power-networks/docs/
rm -rf .venv && python3 -m venv power-networks/.venv && power-networks/.venv/bin/pip install -q -r power-networks/requirements.txt
```

- [ ] **Step 2: Create the root `.gitignore` and `README.md`.**
  - `.gitignore` lines: `.superpowers/`, `.venv/`, `__pycache__/`, `*.pyc`, `.DS_Store`.
  - `README.md` is three short bullets: `ar-game/` is the Fantasy Earth Unity AR project; `power-networks/` is the original Power Networks data project; `docs/superpowers/` holds design specs and plans.

- [ ] **Step 3: Verify that Power Networks still resolves its data folders**

Run: `cd "$REPO/power-networks" && .venv/bin/python -c "from src.schema import DATA_DIR, RAW_DIR, PROCESSED_DIR; print(DATA_DIR.is_dir(), RAW_DIR.is_dir(), PROCESSED_DIR.is_dir())"`
Expected: `True True True`

- [ ] **Step 4: Verify that no ignored data was staged**

Run: `cd "$REPO" && git add -A && git status --short | grep -cE 'data/raw|\.venv|__pycache__'`
Expected: `0`.

- [ ] **Step 5: Commit** with message `Move Power Networks into power-networks/`.

---

### Task 2: Fresh Unity 6.3 LTS AR project at `ar-game/`

The 6000.6 project at `~/Projects/Power Networks/AR-Game` has no custom content. Recreating it from the template on LTS is safer than downgrading it, because 6000.6's core package pins (URP 17.6, AR Foundation 6.6) don't resolve on 6.3.

**Files:**
- Create: `ar-game/` (Unity project), `ar-game/.gitignore`, `ar-game/.gitattributes`
- Delete: `~/Projects/Power Networks/AR-Game` (after rescuing its `.superpowers/` to `$REPO/.superpowers/`)

- [ ] **Step 1: Close the old editor and install the LTS editor with Android support**

```bash
unity status --format json        # note the pid of any instance for AR-Game; `kill <pid>`
unityhub --headless install --version 6000.3.27f1 -m android --cm   # long; run with a long wait
unityhub --headless editors --installed | grep 6000.3.27f1
```
Expected: the last command prints the 6000.3.27f1 install path, and `~/Unity/Hub/Editor/6000.3.27f1/Editor/Data/PlaybackEngines/AndroidPlayer` exists.

- [ ] **Step 2: Create the project, install Pipeline, and remove the old project**

```bash
unity editors default 6000.3.27f1
unity projects create ar-game --path "$REPO" --editor-version 6000.3.27f1 --template com.unity.template.ar-mobile --no-cloud --non-interactive
unity pipeline install --project-path "$PROJ"
mv "/home/matt/Projects/Power Networks/AR-Game/.superpowers" "$REPO/.superpowers" && rm -rf "/home/matt/Projects/Power Networks/AR-Game"
```

- [ ] **Step 3: Write `ar-game/.gitignore`.** Ignore `/[Ll]ibrary/`, `/[Tt]emp/`, `/[Oo]bj/`, `/[Bb]uild/`, `/[Bb]uilds/`, `/[Ll]ogs/`, `/[Uu]ser[Ss]ettings/`, `/[Mm]emoryCaptures/`, `*.csproj`, `*.sln`, `*.apk`, `*.aab`, `.vscode/`, `/Assets/StreamingAssets/aa/`, `.superpowers/`, `Tools/**/.cache/`, and `field-logs/`.

- [ ] **Step 4: Write `ar-game/.gitattributes`.** Track `*.png *.jpg *.jpeg *.psd *.tga *.tif *.tiff *.exr *.hdr *.fbx *.obj *.wav *.mp3 *.ogg *.bytes *.ttf *.otf` with `filter=lfs diff=lfs merge=lfs -text`.

- [ ] **Step 5: Open the project and verify a clean compile**

Run: `cd "$PROJ" && unity open .`, then poll `unity status --project-path "$PROJ" --format json` until `count` is 1 and `state` is `ready`. Then run `unity command script recompile_status --project-path "$PROJ" --format json`.
Expected: `"compilationFailed": false`, `"errors": []`.

- [ ] **Step 6: Verify that only source files are staged**

Run: `cd "$REPO" && git add ar-game && git status --short | grep -cE 'Library/|Temp/|Logs/|UserSettings/|\.csproj'`
Expected: `0`

- [ ] **Step 7: Commit** with message `Add Unity 6.3 LTS AR project at ar-game/`.

---

### Task 3: ARCore Extensions, keyless auth, and Android player settings

**Files:**
- Modify: `ar-game/Packages/manifest.json` (scoped registry plus two dependencies)
- Modify: `ar-game/ProjectSettings/ProjectSettings.asset`, `ProjectSettings/ARCoreExtensionsProjectSettings.json`, XR management settings (via the editor)
- Create: `ar-game/Assets/FantasyEarth/App/ARCoreExtensionsConfig.asset`

**Interfaces:**
- Produces: the `ARCoreExtensionsConfig.asset` used by the scene in Task 10. Assembly names for asmdef references: `Google.XR.ARCoreExtensions`, `Unity.XR.ARFoundation`, `Unity.XR.CoreUtils`.

- [ ] **Step 1: Add the packages.** Add a scoped registry `{"name":"package.openupm.com","url":"https://package.openupm.com","scopes":["com.google.external-dependency-manager"]}`. Add dependencies `"com.google.external-dependency-manager": "1.2.190"` and `"com.google.ar.core.arfoundation.extensions": "https://github.com/google-ar/arcore-unity-extensions.git#v1.57.0-arf6"`. EDM4U is required: ARCore Extensions injects keyless auth's `play-services-auth-base` dependency through it.

- [ ] **Step 2: Check for the Protobuf conflict that broke 6000.6**

Run: `unity command script recompile --project-path "$PROJ"`, wait for `script recompile_status` to report `completed`, then run `grep -c "IBufferMessage\|TypeLoadException" "$PROJ/Logs/Editor.log"` and `unity status --project-path "$PROJ" --format json`.
Expected: `compilationFailed: false`, the grep count is `0`, and the Pipeline instance is `ready`.
If this fails: stop and report. The only allowed fallback is to repeat Task 2 on `6000.0.85f1`, amend the Task 2 commit, and repeat this task.

- [ ] **Step 3: Configure Android.** Switch the build target to Android (`unity command build.target switch --target Android --confirm`, then poll `build.target switch_status`). Set Player settings:
  - Company `mtp354`, Product `Fantasy Earth`, Application ID `com.mtp354.fantasyearth`.
  - Minimum API Level 24, Scripting Backend IL2CPP, Target Architectures ARM64 only.
  - Under Publishing Settings, enable both Custom Main Gradle Template and Custom Launcher Gradle Template.
  - In XR Plug-in Management (Android), enable ARCore and set the ARCore Requirement to **Required**.
  - In Project Settings → XR Plug-in Management → ARCore Extensions, set Android Authentication Strategy to **Keyless** and turn on the Geospatial optional feature.

- [ ] **Step 4: Create the session config.** Create `Assets/FantasyEarth/App/ARCoreExtensionsConfig.asset` (an `ARCoreExtensionsConfig`) with `GeospatialMode = Enabled` and `StreetscapeGeometryMode = Disabled`. Plan 2 enables Streetscape Geometry.

- [ ] **Step 5: Verify that no secret is stored**

Run: `grep -o '"AndroidCloudServicesApiKey":"[^"]*"' "$PROJ/ProjectSettings/ARCoreExtensionsProjectSettings.json"`
Expected: `"AndroidCloudServicesApiKey":""`

- [ ] **Step 6: Verify that an Android APK builds** (using the template's sample scene for now)

Run: `unity command build run --target Android --outputPath Builds/smoke.apk --confirm --project-path "$PROJ"`, then poll `unity command build status --project-path "$PROJ"`.
Expected: `status: completed` with result `Succeeded`, and `Builds/smoke.apk` exists. `unzip -l "$PROJ/Builds/smoke.apk" | grep -c play-services-auth-base` must be more than 0. If it's 0, check that EDM4U's resolver added the dependency to `Assets/Plugins/Android/mainTemplate.gradle`, and rerun.

- [ ] **Step 7: Commit** with message `Add ARCore Extensions with keyless Geospatial and Android settings`.

---

### Task 4: World layer core (geo points, provenance, Köppen grid)

**Files:**
- Create: `ar-game/Assets/FantasyEarth/World/FantasyEarth.World.asmdef` (`noEngineReferences: true`)
- Create: `ar-game/Assets/FantasyEarth/World/GeoPoint.cs`, `Observation.cs`, `KoppenClass.cs`, `KoppenGrid.cs`
- Create: `ar-game/Assets/FantasyEarth/Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef` (Editor platform only; references `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, and each FantasyEarth assembly as later tasks create it; `overrideReferences: true` with `nunit.framework.dll`; `defineConstraints: ["UNITY_INCLUDE_TESTS"]`)
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/World/GeoPointTests.cs`, `KoppenGridTests.cs`

**Interfaces (namespace `FantasyEarth.World`):**
- Produces:
  - `public readonly struct GeoPoint { public GeoPoint(double latitudeDeg, double longitudeDeg); public double LatitudeDeg { get; } public double LongitudeDeg { get; } }`. Throws `ArgumentOutOfRangeException` if latitude is outside [-90, 90] or either value is NaN or infinite. Longitude is normalized into [-180, 180).
  - `public enum ObservationSource { KoppenGeiger1991To2020, ArcoreGeospatial }`
  - `public readonly struct Observation<T> { public Observation(T value, ObservationSource source, DateTimeOffset observedAt, double confidence); public T Value { get; } public ObservationSource Source { get; } public DateTimeOffset ObservedAt { get; } public double Confidence { get; } }`. Throws `ArgumentOutOfRangeException` if confidence is outside [0, 1].
  - `public enum KoppenClass : byte { None = 0, Af = 1, Am, Aw, BWh, BWk, BSh, BSk, Csa, Csb, Csc, Cwa, Cwb, Cwc, Cfa, Cfb, Cfc, Dsa, Dsb, Dsc, Dsd, Dwa, Dwb, Dwc, Dwd, Dfa, Dfb, Dfc, Dfd, ET, EF }` (EF = 30)
  - `public sealed class KoppenGrid { public const int GlobalWidth = 3600; public const int GlobalHeight = 1800; public KoppenGrid(byte[] cells, int width, int height); public KoppenClass ClassAt(GeoPoint p); }`. Row 0 is the cell band touching 90°N, and column 0 starts at 180°W. Cell size is `180/height` by `360/width` degrees. The constructor throws `ArgumentException` if `cells.Length != width*height` or any byte is greater than 30.

- [ ] **Step 1: Write the failing tests.** Use a 36×18 test grid (10° cells) where `cells[row*36+col] = (row*36+col) % 31`.
  - `GeoPoint_RejectsLatitudeOutOfRange` (91 and -91 throw)
  - `GeoPoint_RejectsNaN`
  - `GeoPoint_NormalizesLongitude`: 180 → -180, 190 → -170, -540 → -180
  - `Observation_RejectsConfidenceAbove1`
  - `ClassAt_TopLeftCell`: (89.9, -179.9) → `cells[0]`
  - `ClassAt_NorthPoleUsesFirstRow`: (90, 0) → row 0, col 18
  - `ClassAt_SouthPoleUsesLastRow`: (-90, 0) → row 17, col 18, with no exception
  - `ClassAt_Antimeridian`: (0, 180) and (0, -180) → the same cell, col 0
  - `ClassAt_LastColumn`: (0, 179.99) → col 35
  - `Constructor_RejectsWrongLength`
  - `Constructor_RejectsByteAbove30`

- [ ] **Step 2: Run the EditMode tests.** Expected: compile failure, because the types don't exist yet.
- [ ] **Step 3: Implement the four files.** In `ClassAt`, clamp the row to `[0, height-1]` so latitude -90 maps to the last row; `GeoPoint` has already normalized the longitude.
- [ ] **Step 4: Run the EditMode tests.** Expected: all World tests pass.
- [ ] **Step 5: Commit** with message `Add World layer: GeoPoint, Observation, KoppenGrid`.

---

### Task 5: Fantasy layer, climate to biome

**Files:**
- Create: `ar-game/Assets/FantasyEarth/Fantasy/FantasyEarth.Fantasy.asmdef` (`noEngineReferences: true`, references `FantasyEarth.World`)
- Create: `ar-game/Assets/FantasyEarth/Fantasy/Biome.cs`, `BiomeRules.cs`
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/Fantasy/BiomeRulesTests.cs`

**Interfaces (namespace `FantasyEarth.Fantasy`):**
- Consumes: `KoppenClass` (Task 4)
- Produces: `public enum Biome { Temperate, Desert, Snowy, Tropical }` and `public static class BiomeRules { public static Biome FromKoppen(KoppenClass k); }`

| Köppen | Biome |
|---|---|
| `Af, Am, Aw` | Tropical |
| `BWh, BWk, BSh, BSk` | Desert |
| `Csa` through `Cfc` | Temperate |
| `Dsa, Dsb, Dwa, Dwb, Dfa, Dfb` | Temperate |
| `Dsc, Dsd, Dwc, Dwd, Dfc, Dfd, ET, EF` | Snowy |
| `None` | Temperate |

- [ ] **Step 1: Write the failing tests.**
  - `FromKoppen_MapsEveryClass`, with one `[TestCase]` per enum value (31 in total) asserting the table above.
  - `FromKoppen_NoneIsTemperate`, as a separate named test.
- [ ] **Step 2: Run them.** Expected: compile failure.
- [ ] **Step 3: Implement `BiomeRules.FromKoppen`** with a switch. Undefined enum values throw `ArgumentOutOfRangeException`.
- [ ] **Step 4: Run them.** Expected: pass.
- [ ] **Step 5: Commit** with message `Add Fantasy layer: Köppen to biome rules`.

---

### Task 6: Bundled climate data and `ClimateModel`

**Files:**
- Create: `ar-game/Tools/climate/requirements.txt` (`numpy`, `tifffile`, `imagecodecs`, `pytest`)
- Create: `ar-game/Tools/climate/build_koppen_bytes.py`
- Test: `ar-game/Tools/climate/test_build_koppen_bytes.py`
- Create: `ar-game/Assets/FantasyEarth/Data/koppen_1991_2020_0p1.bytes` (generated, Git LFS)
- Create: `ar-game/Assets/FantasyEarth/Data/ATTRIBUTION.md`
- Create: `ar-game/Assets/FantasyEarth/World/ClimateModel.cs`
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/World/ClimateModelTests.cs`, `ar-game/Assets/FantasyEarth/Tests/EditMode/Fantasy/RealClimateBiomeTests.cs`

**Interfaces:**
- Consumes: `KoppenGrid`, `Observation<T>`, `ObservationSource` (Task 4), `BiomeRules` (Task 5)
- Produces:
  - Python: `def to_grid_bytes(array: numpy.ndarray, expected_shape: tuple[int, int] | None = None) -> bytes` and `def main(cache_dir: pathlib.Path, out_path: pathlib.Path) -> None`.
  - C#: `public sealed class ClimateModel { public ClimateModel(KoppenGrid grid); public Observation<KoppenClass> ClimateAt(GeoPoint p); }`. It returns source `KoppenGeiger1991To2020` and `ObservedAt` `2020-12-31T00:00:00Z`. Confidence is `1.0` for a real class and `0.0` for `None`.

Source data: Beck et al. (2023), "High-resolution (1 km) Köppen-Geiger maps for 1901–2099 based on constrained CMIP6 projections", figshare article 21789074, licence CC BY 4.0. Download `koppen_geiger_tif.zip` from `https://ndownloader.figshare.com/files/61012822` (about 130 MB) and use the `1991_2020/koppen_geiger_0p1.tif` member.

- [x] **Step 1: Write the failing pytest tests**
  - `test_to_grid_bytes_row_major`: a 2×3 uint8 array gives the row-major bytes.
  - `test_rejects_wrong_shape`: shape (1800, 3601) with `expected_shape=(1800, 3600)` raises `ValueError`.
  - `test_rejects_values_above_30`

- [x] **Step 2: Run them.** `cd "$PROJ/Tools/climate" && python3 -m venv .cache/venv && .cache/venv/bin/pip install -q -r requirements.txt && .cache/venv/bin/pytest -q`. Expected: fail, because the functions don't exist yet.

- [x] **Step 3: Implement the script.**
  - `main` downloads the zip into `cache_dir` if it's missing and reads the member with `tifffile`.
  - Assert shape `(1800, 3600)`, dtype `uint8`, max ≤ 30, and that the GeoTIFF tie point is (-180, 90) with pixel scale 0.1. If the raster is stored south-up, flip it.
  - Write the `to_grid_bytes` output to `out_path`.
  - Defaults: `out_path` is `../../Assets/FantasyEarth/Data/koppen_1991_2020_0p1.bytes` and `cache_dir` is `.cache/`, both relative to the script.

- [x] **Step 4: Run pytest, then generate the asset.** Expected: pytest passes. `.cache/venv/bin/python build_koppen_bytes.py` writes a file of exactly `6480000` bytes.

- [x] **Step 5: Write `ATTRIBUTION.md`.** It gives the dataset citation and the CC BY 4.0 link, and notes that the in-app credits screen comes in Plan 3.

- [x] **Step 6: Write the failing C# tests.**
  - `ClimateModelTests` (using a synthetic grid):
    - `ClimateAt_ReportsProvenance`: source, timestamp, and confidence 1.0.
    - `ClimateAt_OceanIsNoneWithZeroConfidence`
  - `RealClimateBiomeTests` loads `Path.Combine(Application.dataPath, "FantasyEarth/Data/koppen_1991_2020_0p1.bytes")` with `File.ReadAllBytes`. Assert the biome from `BiomeRules.FromKoppen(model.ClimateAt(p).Value)` for:
    - Phoenix (33.45, -112.07) → Desert
    - Singapore (1.35, 103.82) → Tropical
    - London (51.50, -0.12) → Temperate
    - Utqiaġvik (71.29, -156.79) → Snowy
    - Mid-Atlantic (30.0, -40.0) → Köppen `None`, biome Temperate

- [x] **Step 7: Implement `ClimateModel`, then run all EditMode tests.** Expected: all pass.

- [x] **Step 8: Commit.** Check `git lfs ls-files | grep -c koppen` returns `1`, then commit with message `Add bundled Köppen-Geiger climate grid and ClimateModel`.

---

### Task 7: Alignment gate (Positioning)

**Files:**
- Create: `ar-game/Assets/FantasyEarth/Positioning/FantasyEarth.Positioning.asmdef` (`noEngineReferences: true`)
- Create: `ar-game/Assets/FantasyEarth/Positioning/PositioningSample.cs`, `AlignmentThresholds.cs`, `AlignmentGate.cs`
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/Positioning/AlignmentGateTests.cs`

**Interfaces (namespace `FantasyEarth.Positioning`):**
- Produces:
  - `public readonly struct PositioningSample { public PositioningSample(bool isTracking, double horizontalAccuracyM, double yawAccuracyDeg); public bool IsTracking { get; } public double HorizontalAccuracyM { get; } public double YawAccuracyDeg { get; } public static PositioningSample FromEarth(bool earthEnabled, bool earthTracking, double horizontalAccuracyM, double yawAccuracyDeg); }`. `FromEarth` sets `IsTracking = earthEnabled && earthTracking`.
  - `public sealed class AlignmentThresholds { public double EnterHorizontalM = 1.5; public double EnterYawDeg = 2.0; public float EnterDwellS = 1.0f; public double ExitHorizontalM = 3.0; public double ExitYawDeg = 4.0; public float ExitDwellS = 0.5f; public float BlendDurationS = 0.75f; public static AlignmentThresholds Default => new AlignmentThresholds(); }`. These are starting values that Plan 2 tunes on the S25.
  - `public enum AlignmentState { Unlocked, Locked }`
  - `public sealed class AlignmentGate { public AlignmentGate(AlignmentThresholds thresholds); public AlignmentState State { get; } public float RebuiltBlend { get; } public void Update(PositioningSample sample, float deltaTimeS); }`

Rules, applied in this order within each `Update`:
1. A negative or non-finite `deltaTimeS` counts as 0.
2. A sample is **good** when it's tracking, both accuracies are finite and ≥ 0, horizontal ≤ EnterHorizontalM, and yaw ≤ EnterYawDeg.
3. A sample is **bad** when it's not tracking, an accuracy is non-finite or negative, horizontal > ExitHorizontalM, or yaw > ExitYawDeg.
4. **Unlocked → Locked** once good samples have accumulated `≥ EnterDwellS - 1e-4` seconds continuously. Any non-good sample resets the timer to 0.
5. **Locked → Unlocked** once bad samples have accumulated `≥ ExitDwellS - 1e-4` seconds continuously. Any non-bad sample resets the timer. **Losing tracking unlocks immediately.**
6. After the state update, `RebuiltBlend` moves by `deltaTimeS / BlendDurationS`: toward 1 when Locked, toward 0 when Unlocked, clamped to [0, 1].

- [x] **Step 1: Write the failing tests** (`AlignmentThresholds.Default`; each step is 0.1 s; "good" means `(true, 1.0, 1.0)`):
  - `StartsUnlockedWithZeroBlend`
  - `LocksOnlyAfterFullEnterDwell`: 9 good steps leave it Unlocked; the 10th locks it.
  - `NonGoodSampleResetsEnterDwell`: 5 good, 1 sample at `(true, 2.0, 1.0)`, then 9 good leave it Unlocked; 1 more locks it.
  - `UnlocksAfterExitDwell`: once Locked, 4 steps at `(true, 3.5, 1.0)` leave it Locked; the 5th unlocks it.
  - `InBetweenValuesKeepCurrentState`: Locked plus 20 steps at `(true, 2.0, 3.0)` stays Locked; Unlocked plus the same stays Unlocked.
  - `TrackingLossUnlocksImmediately`: Locked plus one `(false, 1.0, 1.0)` → Unlocked.
  - `NaNOrNegativeAccuracyNeverLocks`: 50 steps each of NaN horizontal, +∞ yaw, and -1 m horizontal leave it Unlocked.
  - `FlappingTrackingNeverLocks`: 100 steps alternating good and `(false, 1.0, 1.0)` leave it Unlocked.
  - `BlendRampsOverBlendDuration`:
    - Right after the locking update, the blend is `0.1/0.75 ± 1e-4`.
    - After 7 more good steps it's exactly `1.0`.
    - 5 steps at 3.5 m then unlock it; the blend is still `1.0` after the first 4 steps and `1 - 0.1/0.75 ± 1e-4` after the 5th.
    - After 7 more such steps it's exactly `0.0`.
  - `NegativeDeltaTimeIsIgnored`: 20 good samples with `deltaTimeS = -1` leave it Unlocked with blend 0.
  - `FromEarth_RequiresEnabledAndTracking`: all four combinations.

- [x] **Step 2: Run them.** Expected: compile failure.
- [x] **Step 3: Implement the three files.**
- [x] **Step 4: Run them.** Expected: all pass.
- [x] **Step 5: Commit** with message `Add alignment gate with hysteresis and blend`.

---

### Task 8: Session log format (Diagnostics)

**Files:**
- Create: `ar-game/Assets/FantasyEarth/Diagnostics/FantasyEarth.Diagnostics.asmdef` (`noEngineReferences: true`; references World, Fantasy, Positioning)
- Create: `ar-game/Assets/FantasyEarth/Diagnostics/SessionLogEntry.cs`, `SessionLogFormatter.cs`
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/Diagnostics/SessionLogFormatterTests.cs`

**Interfaces (namespace `FantasyEarth.Diagnostics`):**
- Consumes: `GeoPoint`, `KoppenClass`, `Biome`, `AlignmentState`
- Produces:
  - `public readonly struct SessionLogEntry { public SessionLogEntry(long unixTimeMs, GeoPoint? position, double horizontalAccuracyM, double yawAccuracyDeg, string earthState, bool isTracking, AlignmentState alignment, float rebuiltBlend, KoppenClass koppen, Biome biome); }`, with a get-only property for each parameter: `UnixTimeMs`, `Position`, `HorizontalAccuracyM`, `YawAccuracyDeg`, `EarthState`, `IsTracking`, `Alignment`, `RebuiltBlend`, `Koppen`, `Biome`.
  - `public static class SessionLogFormatter { public static string ToJsonLine(SessionLogEntry e); }`. One JSON object, with no trailing newline. Keys appear in this exact order: `t, lat, lon, hAcc, yawAcc, earth, tracking, alignment, blend, koppen, biome`. Latitude and longitude use `F7`; accuracies and blend use `F2`; all formatting is invariant culture. A null position gives `"lat":null,"lon":null`. Non-finite accuracies give `null`. `earth` is a JSON-escaped string, with quotes and backslashes escaped.

- [x] **Step 1: Write the failing tests.**
  - `FormatsExactLine`: the entry `(1792890000000, new GeoPoint(51.5000001, -0.1234567), 1.234, 0.5, "Enabled", true, AlignmentState.Locked, 0.5f, KoppenClass.Cfb, Biome.Temperate)` gives exactly `{"t":1792890000000,"lat":51.5000001,"lon":-0.1234567,"hAcc":1.23,"yawAcc":0.50,"earth":"Enabled","tracking":true,"alignment":"Locked","blend":0.50,"koppen":"Cfb","biome":"Temperate"}`.
  - `NullPositionWritesNulls`
  - `NonFiniteAccuracyWritesNull`
  - `EscapesEarthState`: the input `a"b\c` produces `"earth":"a\"b\\c"`.
  - `UsesDotDecimalsUnderGermanCulture`: set `CultureInfo.CurrentCulture = new CultureInfo("de-DE")` inside a try/finally that restores it, then assert the same exact line as `FormatsExactLine`.

- [x] **Step 2: Run them.** Expected: compile failure.
- [x] **Step 3: Implement the two files** with a `StringBuilder` and no JSON library.
- [x] **Step 4: Run them.** Expected: all pass.
- [x] **Step 5: Commit** with message `Add session log JSON line formatter`.

---

### Task 9: Painted look (painterly pass and per-biome looks)

**Files:**
- Create: `ar-game/Assets/FantasyEarth/Rendering/FantasyEarth.Rendering.asmdef` (references `FantasyEarth.Fantasy`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`)
- Create: `ar-game/Assets/FantasyEarth/Rendering/Painterly.shader`, `PainterlyRendererFeature.cs`, `BiomeLook.cs`, `BiomeLookLibrary.cs`
- Create assets in `ar-game/Assets/FantasyEarth/Rendering/`:
  - `Painterly.mat`
  - `BiomeLooks/{Temperate,Desert,Snowy,Tropical}.asset` (`BiomeLook`)
  - `BiomeLooks/{Temperate,Desert,Snowy,Tropical}_Volume.asset` (`VolumeProfile`)
  - `BiomeLooks/BiomeLookLibrary.asset`
- Modify: the Universal Renderer Data asset that the template's mobile URP asset uses (find it under `ar-game/Assets/Settings/`) to add `PainterlyRendererFeature`.
- Modify: the test asmdef, to add references `FantasyEarth.Rendering`, `Unity.RenderPipelines.Core.Runtime`, and `Unity.RenderPipelines.Universal.Runtime`.
- Test: `ar-game/Assets/FantasyEarth/Tests/EditMode/Rendering/BiomeLookLibraryTests.cs`

**Interfaces (namespace `FantasyEarth.Rendering`):**
- Consumes: `Biome` (Task 5)
- Produces:
  - `public sealed class BiomeLook : ScriptableObject { public VolumeProfile Profile; public Color MistColor; [Range(0,1)] public float MistStrength; public Color MoteColor; public float MotesPerSecond; }`
  - `public sealed class BiomeLookLibrary : ScriptableObject { [Serializable] public struct Entry { public Biome Biome; public BiomeLook Look; } public Entry[] Entries; public bool TryGet(Biome biome, out BiomeLook look); }`
  - `public sealed class PainterlyRendererFeature : ScriptableRendererFeature`, with serialized `Material material` and `RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing`. Its pass implements `RecordRenderGraph` and blits the active color through the material using the `AddBlitPass` render graph extension with `RenderGraphUtils.BlitMaterialParameters`. It skips scene-view and preview cameras.
  - Shader `FantasyEarth/Painterly`: a 4-sector Kuwahara filter with `_Radius` (int, default 4). It applies a screen-space mist tint read from the global properties **`_FE_MistColor`** (Color) and **`_FE_MistStrength`** (float), strongest in a horizontal band around `uv.y ≈ 0.45`.

Art direction (exact values are the implementer's call):
- Each Volume profile has Color Adjustments, Bloom with a low threshold (about 0.8) so real lamps and lit windows glow, and a soft Vignette.
- Temperate: warm golden. Desert: hot orange, light mist. Snowy: cool blue-white, heavier mist. Tropical: saturated green-teal, humid mist.
- The bar is "a storybook world, not a phone filter".

- [x] **Step 1: Write the failing tests.**
  - `LibraryHasLookForEveryBiome`: load `Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset` with `AssetDatabase.LoadAssetAtPath`. For each `Biome` value, `TryGet` returns true, `Profile` isn't null, and `MistStrength` is in [0, 1].
  - `RendererHasPainterlyFeatureWithMaterial`: load the renderer data asset (write its exact path into the test). Assert it contains an active `PainterlyRendererFeature` whose material uses shader `FantasyEarth/Painterly`.

- [x] **Step 2: Run them.** Expected: fail.
- [x] **Step 3: Implement the shader, feature, and ScriptableObjects; create the assets; add the feature to the renderer.** Check the feature with the user-level skill `validate-urp-render-graph-renderer-feature`.
- [x] **Step 4: Run all EditMode tests and check the console.** Expected: all pass. `unity command console get --project-path "$PROJ"` shows no shader compile errors.
- [x] **Step 5: Commit** with message `Add painterly renderer feature and per-biome looks`.

---

### Task 10: World scene, Google Cloud keyless setup, and the first device run

**Files:**
- Create: `ar-game/Assets/FantasyEarth/App/FantasyEarth.App.asmdef` (references all FantasyEarth assemblies, `Google.XR.ARCoreExtensions`, `Unity.XR.ARFoundation`, `Unity.XR.CoreUtils`, `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime`)
- Create: `ar-game/Assets/FantasyEarth/App/GeospatialSampler.cs`, `ClimateService.cs`, `PaintedLookController.cs`, `SessionLogger.cs`, `DiagnosticsOverlay.cs`, `WorldPresenter.cs`
- Create: `ar-game/Assets/FantasyEarth/Scenes/World.unity`, the only scene in the build list
- Test: on-device checks (below). All decision logic is already covered by Tasks 4–8.

**Interfaces (namespace `FantasyEarth.App`, all MonoBehaviours):**
- Consumes: everything above. From ARCore Extensions: `AREarthManager.EarthState` (compare to `EarthState.Enabled`), `AREarthManager.EarthTrackingState` (compare to `TrackingState.Tracking`), and `AREarthManager.CameraGeospatialPose` (`Latitude`, `Longitude`, `HorizontalAccuracy`, `OrientationYawAccuracy`).
- Produces:
  - `GeospatialSampler`: `[SerializeField] AREarthManager earthManager`; `public PositioningSample Current { get; }`; `public GeoPoint? Position { get; }` (non-null whenever tracking, at any accuracy); `public string EarthStateName { get; }`. It's refreshed in `Update` via `PositioningSample.FromEarth`. It never throws when the session isn't ready.
  - `ClimateService`: `[SerializeField] TextAsset koppenGrid` (the `.bytes` asset); `public Observation<KoppenClass> ClimateAt(GeoPoint p)`. It builds `new ClimateModel(new KoppenGrid(koppenGrid.bytes, KoppenGrid.GlobalWidth, KoppenGrid.GlobalHeight))` once in `Awake`.
  - `PaintedLookController`: `[SerializeField] BiomeLookLibrary library; [SerializeField] Volume volume; [SerializeField] ParticleSystem motes`; `public void Apply(Biome biome)`. It sets `volume.sharedProfile`, the globals `_FE_MistColor` and `_FE_MistStrength` (`Shader.SetGlobalColor` / `SetGlobalFloat`), the mote start color, and the emission rate.
  - `SessionLogger`: active only if `Debug.isDebugBuild`. `public void Record(SessionLogEntry entry, bool force)` writes `SessionLogFormatter.ToJsonLine` lines to `Application.persistentDataPath/sessions/<UTC yyyyMMdd-HHmmss>.jsonl`. It writes at most 2 lines per second unless `force` is set, and flushes on pause and quit.
  - `DiagnosticsOverlay`: active only if `Debug.isDebugBuild`. A three-finger tap toggles an IMGUI panel showing earth state, tracking, both accuracies, alignment, blend, Köppen class, and biome. It reads `WorldPresenter.LastEntry`.
  - `WorldPresenter`:
    - It owns an `AlignmentGate(AlignmentThresholds.Default)` and exposes `public SessionLogEntry LastEntry { get; }`.
    - Each frame it calls `gate.Update(sampler.Current, Time.deltaTime)`.
    - When `sampler.Position` is non-null it computes the biome via `ClimateService` and `BiomeRules`. It calls `look.Apply` only when the biome changes, and applies `Biome.Temperate` in `Start`.
    - It builds `LastEntry` and calls `logger.Record(LastEntry, force: stateChanged)`.
    - In Plan 1 nothing renders differently when Locked: the gate is measured and logged only.

Scene contents:
- `AR Session`.
- `XR Origin (Mobile AR)`, with `ARCameraManager`, `ARCameraBackground`, and `AREarthManager`.
- An `ARCoreExtensions` GameObject wired to the session, origin, camera manager, and `Assets/FantasyEarth/App/ARCoreExtensionsConfig.asset`.
- A global `Volume`.
- A `Motes` ParticleSystem parented to the camera, with world-space simulation.
- An `App` GameObject holding the six components.
- Post-processing enabled on the AR camera. No other UI.

- [x] **Step 1: Implement the six components and build the scene.** Remove the template scene from the build list and add `Assets/FantasyEarth/Scenes/World.unity`. Run all EditMode tests and `script recompile_status`. Expected: 0 failures and 0 compile errors.

- [x] **Step 2: Owner sets up Google Cloud for keyless auth** (in the browser; the implementer gives these instructions and waits).
  1. Create a Google Cloud project and enable the **ARCore API**.
  2. Configure the OAuth consent screen (External, testing mode, owner's account as a test user).
  3. Create an OAuth client ID of type **Android** with package `com.mtp354.fantasyearth` and the debug SHA-1 from:
     `~/Unity/Hub/Editor/6000.3.27f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android | grep SHA1`.
     If the keystore doesn't exist yet, run the Step 4 build first; Unity creates it.

- [x] **Step 3: Owner connects the S25** with Developer options and USB debugging on. Run `adb devices`. Expected: one device listed as `device`, not `unauthorized`.

- [x] **Step 4: Build and install a development build**

Run: `unity command build run --target Android --outputPath Builds/fantasy-earth-dev.apk --options '["Development"]' --confirm --project-path "$PROJ"`, poll `build status` until it's completed and succeeded, then run `adb install -r "$PROJ/Builds/fantasy-earth-dev.apk"` and `adb shell monkey -p com.mtp354.fantasyearth -c android.intent.category.LAUNCHER 1`.
Expected: the app opens straight into the camera with the painted look. After a three-finger tap, the overlay shows `earth: Enabled`.

- [x] **Step 5: Device checks (owner and implementer)**

  - **Accepted follow-up:** the current entry default is 2.0 m / 2° for 1 second; original Task 7 numbers are historical starting values. On the repaired calibrated APK the owner observed lock and the paused log confirms 82 Locked records, first lock at 74.906 seconds and locked medians 0.88 m / 1.36°. Observed locked intervals total 41.958 seconds, excluding a 799.970-second unsampled gap. See `docs/handoffs/reports/alignment-lock-field-summary.json`.

  - **First walk (historical):** live camera, diagnostics, Earth tracking and startup-fix checks passed. That session had 1,797 valid records, 1,099 tracking records and 0 Locked records; best paired accuracy was 1.87 m / 1.41°. Its original failed lock result remains preserved in `docs/handoffs/reports/task-10-report.md`; the fresh follow-up above completes acceptance.
  - Deny location permission on first launch: the app still shows the painted Temperate look, the overlay shows a non-Enabled earth state or no tracking, and it doesn't crash (`adb logcat -d | grep -c "FATAL EXCEPTION"` → 0).
  - Grant permission and go outdoors on a Street View-covered street for about 5 minutes, walking and panning: the overlay shows accuracies falling and `alignment: Locked` at least once, with no crash.
  - Pause the app with Home, wait for its writer to flush, then analyze with `python3 ar-game/Tools/diagnostics/summarize_session.py --serial DEVICE_SERIAL`. The tool removes coordinates on Android before reading the records; raw logs stay on the phone. See `docs/FANTASY_EARTH_DEVICE_TESTING.md`.
  - Expected: every sanitized record parses as JSON, `lockedRecords` is at least 1, and the tool reports locked accuracy medians and time to first lock.
  - Record in the commit message the median `hAcc` and `yawAcc` once locked, and the time to the first lock. Plan 2 tunes the thresholds from these.

- [x] **Step 6: Commit** with message `Add World scene: painted camera view, Geospatial sampling, diagnostics`. Don't commit `Builds/` or `field-logs/`: they contain precise location and are gitignored.

---

## Out of scope for this plan (covered by later plans)

- **Plan 2, rebuilt mode:** Streetscape Geometry enabled, building meshes matched to Overture footprints for stable IDs, fantasy building generation, seeded props in the camera view only when Locked, and the painted ↔ rebuilt blend driven by `AlignmentGate.RebuiltBlend` with thresholds tuned from the Task 10 logs.
- **Plan 3, map view:** the open map data pipeline, the tilted 3D fantasy neighbourhood, the avatar, the swipe between views, the seeded props shared with the camera view, and the in-app attribution screen (OSM/Overture and Köppen-Geiger CC BY 4.0).
