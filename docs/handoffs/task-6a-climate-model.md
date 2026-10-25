# Task 6a: ClimateModel with synthetic data

Read [the shared instructions](README.md), then implement only this brief. This extracts the simplest C# part of original Task 6; completing it does not complete Task 6.

## Read first

- `ar-game/Assets/FantasyEarth/World/GeoPoint.cs`
- `ar-game/Assets/FantasyEarth/World/KoppenClass.cs`
- `ar-game/Assets/FantasyEarth/World/KoppenGrid.cs`
- `ar-game/Assets/FantasyEarth/World/Observation.cs`
- `ar-game/Assets/FantasyEarth/Tests/EditMode/World/KoppenGridTests.cs`

Prerequisite: the existing World assembly. This task is independent of Tasks 5 and 7. Use only a tiny synthetic grid; no real climate file is needed.

## Files allowed

Create under `ar-game/Assets/FantasyEarth/`:

- `World/ClimateModel.cs`
- `Tests/EditMode/World/ClimateModelTests.cs`
- Their `.meta` files.

No asmdef changes are necessary: World and its test reference already exist. Do not introduce a Fantasy reference into World.

## Exact API and behavior

Namespace: `FantasyEarth.World`.

```csharp
public sealed class ClimateModel
{
    public ClimateModel(KoppenGrid grid);
    public Observation<KoppenClass> ClimateAt(GeoPoint p);
}
```

- Store the supplied grid once. Use its existing `ClassAt(p)` method; do not duplicate coordinate/index calculations.
- Return the class from that grid as `Observation<KoppenClass>.Value`.
- Set `Source` to `ObservationSource.KoppenGeiger1991To2020`.
- Set `ObservedAt` to **2020-12-31T00:00:00Z**, regardless of when the function runs. This is the original plan's fixed dataset provenance timestamp.
- Set `Confidence` to **0.0** for `KoppenClass.None` and **1.0** for any real class.
- This assembly remains pure C#: no `Application.dataPath`, Unity APIs, file I/O, network calls, biome conversion, or clock dependency.

## Required tests and acceptance

Use namespace `FantasyEarth.Tests.World` and fixture `ClimateModelTests`. A two-cell grid is sufficient:

```csharp
new KoppenGrid(new byte[] { (byte)KoppenClass.Cfb, 0 }, 2, 1)
```

- `new GeoPoint(0, -90)` selects `Cfb`.
- `new GeoPoint(0, 90)` selects `None`.

Required tests:

1. `ClimateAt_ReportsProvenance`: select the Cfb cell and assert the exact class, source, UTC timestamp, and confidence 1.0.
2. `ClimateAt_OceanIsNoneWithZeroConfidence`: select the None cell and assert class None, confidence 0.0, and the same source/timestamp.

All existing World cases must continue passing. Do not add `RealClimateBiomeTests` while its real dataset is absent.

Follow the shared compile/test/report procedure. Suggested commit title, if committing: `Add ClimateModel with provenance and synthetic-grid tests`.

## Leave explicitly unfinished

Original Task 6 still needs `Tools/climate/` and its Python tests, the downloaded/converted 6,480,000-byte `.bytes` asset in Git LFS, `Data/ATTRIBUTION.md`, and real-location biome tests. Mention those remaining pieces in your report; do not mark the full task complete.
