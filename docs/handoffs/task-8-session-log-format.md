# Task 8: deterministic session log formatting

Read [the shared instructions](README.md), then implement only this brief.

## Prerequisites and files to read

These types must already exist:

- `World/GeoPoint.cs`, `World/KoppenClass.cs`.
- `Fantasy/Biome.cs` from Task 5.
- `Positioning/AlignmentGate.cs` containing `AlignmentState` from Task 7.
- The shared `Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef`.

Paths above are relative to `ar-game/Assets/FantasyEarth/`. If Task 5 or 7 is missing, report the missing prerequisite; do not invent a duplicate enum, stub assembly, or alternative API. Task 6/the climate dataset is not a prerequisite.

## Files allowed

Create under `ar-game/Assets/FantasyEarth/`:

- `Diagnostics/FantasyEarth.Diagnostics.asmdef`
- `Diagnostics/SessionLogEntry.cs`
- `Diagnostics/SessionLogFormatter.cs`
- `Tests/EditMode/Diagnostics/SessionLogFormatterTests.cs`
- Associated file/folder `.meta` files.

Modify only `Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef` to add `FantasyEarth.Diagnostics`, preserving all other settings/references. The new production asmdef is named `FantasyEarth.Diagnostics`, references `FantasyEarth.World`, `FantasyEarth.Fantasy`, and `FantasyEarth.Positioning`, and sets `noEngineReferences: true`.

## Exact API

Namespace: `FantasyEarth.Diagnostics`. Import the existing types from World, Fantasy, and Positioning.

```csharp
public readonly struct SessionLogEntry
{
    public SessionLogEntry(long unixTimeMs, GeoPoint? position,
        double horizontalAccuracyM, double yawAccuracyDeg,
        string earthState, bool isTracking, AlignmentState alignment,
        float rebuiltBlend, KoppenClass koppen, Biome biome);

    public long UnixTimeMs { get; }
    public GeoPoint? Position { get; }
    public double HorizontalAccuracyM { get; }
    public double YawAccuracyDeg { get; }
    public string EarthState { get; }
    public bool IsTracking { get; }
    public AlignmentState Alignment { get; }
    public float RebuiltBlend { get; }
    public KoppenClass Koppen { get; }
    public Biome Biome { get; }
}

public static class SessionLogFormatter
{
    public static string ToJsonLine(SessionLogEntry e);
}
```

The struct stores each constructor argument in its corresponding get-only property. It is a data record; do not add positioning decisions or access the device clock.

## Output contract

- Return one compact JSON object, with **no trailing newline**.
- Keys are in exactly this order: `t`, `lat`, `lon`, `hAcc`, `yawAcc`, `earth`, `tracking`, `alignment`, `blend`, `koppen`, `biome`.
- Every numeric conversion uses `CultureInfo.InvariantCulture`.
- `t` is an integer. Latitude and longitude use `F7`. Accuracies and blend use `F2`.
- A missing position writes `"lat":null,"lon":null`.
- Non-finite horizontal/yaw accuracies (NaN or either infinity) write unquoted JSON `null` for the affected field. Finite values retain the specified formatting.
- `tracking` is lowercase unquoted `true` or `false`.
- `alignment`, `koppen`, and `biome` are quoted enum names.
- `earth` is a JSON-escaped string. Escape quotes, backslashes, and control characters correctly, including newlines, tabs, carriage returns, and characters below U+0020. No raw control character may break the JSON line.
- Implement using `StringBuilder` and the standard library. Do not introduce a JSON package, reflection serializer, Unity reference, file writer, or logging MonoBehaviour.

For this input:

```csharp
new SessionLogEntry(
    1792890000000,
    new GeoPoint(51.5000001, -0.1234567),
    1.234, 0.5, "Enabled", true,
    AlignmentState.Locked, 0.5f, KoppenClass.Cfb, Biome.Temperate)
```

the exact output is:

```json
{"t":1792890000000,"lat":51.5000001,"lon":-0.1234567,"hAcc":1.23,"yawAcc":0.50,"earth":"Enabled","tracking":true,"alignment":"Locked","blend":0.50,"koppen":"Cfb","biome":"Temperate"}
```

## Required tests and acceptance

Use namespace `FantasyEarth.Tests.Diagnostics` and fixture `SessionLogFormatterTests`:

1. `FormatsExactLine`: exact equality with the example, including ordering, decimal precision, and no added whitespace/newline.
2. `NullPositionWritesNulls`: both coordinates are JSON null.
3. `NonFiniteAccuracyWritesNull`: cover NaN and positive/negative infinity for each accuracy independently; other fields remain unchanged.
4. `EscapesEarthState`: input C# string `"a\"b\\c"` produces JSON fragment `"earth":"a\"b\\c"` (the fragment here is literal JSON text, not another C# string literal).
5. `UsesDotDecimalsUnderGermanCulture`: set `CultureInfo.CurrentCulture` to `new CultureInfo("de-DE")`, assert the exact example output, and restore the original culture in `finally` even if an assertion fails.
6. A control-character regression case: earth state containing newline/tab/carriage return and U+0001 produces valid escaped JSON text and no embedded raw line break. Compare the expected escaped text without adding a serialization dependency.

Use synthetic positions only. These are formatter unit tests; do not write actual session logs to disk or add UI.

Follow the shared compile/test/report procedure. Suggested commit title, if committing: `Add session log JSON line formatter`.
