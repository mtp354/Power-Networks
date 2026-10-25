# Task 5: Köppen class to fantasy biome

Read [the shared instructions](README.md), then implement only this brief. This is the smallest complete remaining task.

## Read first

- `ar-game/Assets/FantasyEarth/World/KoppenClass.cs`
- `ar-game/Assets/FantasyEarth/World/FantasyEarth.World.asmdef`
- `ar-game/Assets/FantasyEarth/Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef`
- One existing World test for NUnit/namespace conventions.

Prerequisite: the existing World assembly. No climate dataset or positioning implementation is needed.

## Files allowed

Create under `ar-game/Assets/FantasyEarth/`:

- `Fantasy/FantasyEarth.Fantasy.asmdef`
- `Fantasy/Biome.cs`
- `Fantasy/BiomeRules.cs`
- `Tests/EditMode/Fantasy/BiomeRulesTests.cs`
- Associated file/folder `.meta` files.

Modify only `Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef` to add `FantasyEarth.Fantasy`, preserving all other references and settings. The new production asmdef is named `FantasyEarth.Fantasy`, references only `FantasyEarth.World`, and sets `noEngineReferences: true`. Follow the existing World asmdef's other conventions.

## Exact API

Namespace: `FantasyEarth.Fantasy`.

```csharp
public enum Biome { Temperate, Desert, Snowy, Tropical }

public static class BiomeRules
{
    public static Biome FromKoppen(FantasyEarth.World.KoppenClass k);
}
```

Keep the enum order shown. Use an explicit switch mapping. Unknown enum values throw `ArgumentOutOfRangeException`; do not silently assign them Temperate.

| Köppen classes | Result |
|---|---|
| `Af`, `Am`, `Aw` | `Tropical` |
| `BWh`, `BWk`, `BSh`, `BSk` | `Desert` |
| `Csa`, `Csb`, `Csc`, `Cwa`, `Cwb`, `Cwc`, `Cfa`, `Cfb`, `Cfc` | `Temperate` |
| `Dsa`, `Dsb`, `Dwa`, `Dwb`, `Dfa`, `Dfb` | `Temperate` |
| `Dsc`, `Dsd`, `Dwc`, `Dwd`, `Dfc`, `Dfd`, `ET`, `EF` | `Snowy` |
| `None` | `Temperate` |

## Required tests and acceptance

Use namespace `FantasyEarth.Tests.Fantasy` and fixture `BiomeRulesTests`:

1. `FromKoppen_MapsEveryClass`: one explicit NUnit `TestCase` for each of the **31** defined Köppen values and its expected biome. Expected values must be written independently from the production implementation.
2. `FromKoppen_NoneIsTemperate`: a separate named regression test for ocean/no-data fallback.
3. `FromKoppen_RejectsUndefinedClass`: `(KoppenClass)255` throws `ArgumentOutOfRangeException`.

The fixture therefore has 33 cases before any additional cases. All existing World cases must continue passing. Do not create a real-data test or add a data download.

Follow the shared compile/test/report procedure. Suggested commit title, if committing: `Add Fantasy layer: Köppen to biome rules`.
