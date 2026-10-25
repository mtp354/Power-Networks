# Task 7: alignment gate with hysteresis

Read [the shared instructions](README.md), then implement only this brief. This is a bounded pure-C# task, but its timing and bad-data behavior need careful review.

## Read first

- `ar-game/Assets/FantasyEarth/World/FantasyEarth.World.asmdef` as a pure-assembly template.
- `ar-game/Assets/FantasyEarth/Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef`.
- One existing World test for NUnit/namespace conventions.

This task has no dependency on Tasks 5–6. Do not wait for the climate dataset and do not create biome or diagnostics stubs.

## Files allowed

Create under `ar-game/Assets/FantasyEarth/`:

- `Positioning/FantasyEarth.Positioning.asmdef`
- `Positioning/PositioningSample.cs`
- `Positioning/AlignmentThresholds.cs`
- `Positioning/AlignmentGate.cs` (also contains `AlignmentState`)
- `Tests/EditMode/Positioning/AlignmentGateTests.cs`
- Associated file/folder `.meta` files.

Modify only `Tests/EditMode/FantasyEarth.Tests.EditMode.asmdef` to add `FantasyEarth.Positioning`, preserving all other settings/references. The new production asmdef is named `FantasyEarth.Positioning`, has no assembly references, and sets `noEngineReferences: true`.

## Exact API

Namespace: `FantasyEarth.Positioning`.

```csharp
public readonly struct PositioningSample
{
    public PositioningSample(bool isTracking, double horizontalAccuracyM, double yawAccuracyDeg);
    public bool IsTracking { get; }
    public double HorizontalAccuracyM { get; }
    public double YawAccuracyDeg { get; }
    public static PositioningSample FromEarth(bool earthEnabled, bool earthTracking,
        double horizontalAccuracyM, double yawAccuracyDeg);
}

public sealed class AlignmentThresholds
{
    public double EnterHorizontalM = 1.5;
    public double EnterYawDeg = 2.0;
    public float EnterDwellS = 1.0f;
    public double ExitHorizontalM = 3.0;
    public double ExitYawDeg = 4.0;
    public float ExitDwellS = 0.5f;
    public float BlendDurationS = 0.75f;
    public static AlignmentThresholds Default => new AlignmentThresholds();
}

public enum AlignmentState { Unlocked, Locked }

public sealed class AlignmentGate
{
    public AlignmentGate(AlignmentThresholds thresholds);
    public AlignmentState State { get; }
    public float RebuiltBlend { get; }
    public void Update(PositioningSample sample, float deltaTimeS);
}
```

`PositioningSample` stores its arguments, including invalid accuracy values: the gate handles invalid samples. `FromEarth` preserves both accuracies and sets tracking to `earthEnabled && earthTracking`. It must not reference ARCore types. `AlignmentThresholds.Default` returns a fresh instance on each access.

## Ordered update rules

Start `Unlocked` with blend 0 and zero timers. In each `Update`:

1. Treat negative, NaN, or infinite `deltaTimeS` as 0. Do not accumulate time or change the blend from that invalid duration; still evaluate the sample, including immediate tracking loss.
2. A **good** sample is tracking, has two finite non-negative accuracies, and has horizontal accuracy `<= EnterHorizontalM` and yaw accuracy `<= EnterYawDeg`.
3. A **bad** sample is not tracking, has either accuracy non-finite or negative, or has horizontal accuracy `> ExitHorizontalM` or yaw accuracy `> ExitYawDeg`.
4. While Unlocked, accumulate good time continuously. Every non-good sample resets the enter timer. Lock when the timer reaches `EnterDwellS - 1e-4`.
5. While Locked, accumulate bad time continuously. Every non-bad sample resets the exit timer. Unlock at `ExitDwellS - 1e-4`. **Loss of tracking unlocks immediately**, even with `deltaTimeS == 0`.
6. After updating state, move the blend by `deltaTimeS / BlendDurationS` toward 1 if Locked, otherwise toward 0. Clamp the result to [0, 1]. Do not snap the blend on tracking loss: state changes immediately and blending follows this rule.

Use the relevant timer for the current state and reset timers on transitions so stale dwell time cannot leak into the next cycle. Values between entry and exit thresholds preserve state and reset the applicable dwell timer. Keep the exact defaults; device tuning belongs to a later plan. Avoid adding behavior for custom threshold configurations that changes the specified valid/default behavior.

Use `System` math and NaN/infinity checks supported by this project's C# runtime. No Unity `Mathf`, `Time`, MonoBehaviours, ARCore, or new math package.

## Required tests

Use namespace `FantasyEarth.Tests.Positioning`, fixture `AlignmentGateTests`, a fresh gate/default thresholds per test, and 0.1-second updates unless stated otherwise. A good sample is `(true, 1.0, 1.0)`.

| Test | Required observation |
|---|---|
| `StartsUnlockedWithZeroBlend` | Initial state Unlocked, blend 0 |
| `LocksOnlyAfterFullEnterDwell` | 9 good steps stay Unlocked; the 10th locks |
| `NonGoodSampleResetsEnterDwell` | 5 good steps, one `(true, 2.0, 1.0)`, then 9 good remain Unlocked; one more locks |
| `UnlocksAfterExitDwell` | From Locked, 4 steps at `(true, 3.5, 1.0)` remain Locked; the 5th unlocks |
| `InBetweenValuesKeepCurrentState` | 20 steps at `(true, 2.0, 3.0)` preserve either initial state |
| `TrackingLossUnlocksImmediately` | One untracked sample unlocks a Locked gate |
| `NaNOrNegativeAccuracyNeverLocks` | Repeated invalid samples never lock; cover NaN, +/-infinity, and negative values in each accuracy field |
| `FlappingTrackingNeverLocks` | 100 alternating good/untracked steps never lock |
| `BlendRampsOverBlendDuration` | Check the detailed sequence below |
| `NegativeDeltaTimeIsIgnored` | 20 good updates at delta -1 leave state Unlocked and blend 0 |
| `FromEarth_RequiresEnabledAndTracking` | All four enabled/tracking combinations; accuracies are preserved |

Blend sequence:

1. On the locking update, blend is `0.1 / 0.75`, within `1e-4`.
2. Seven more good updates reach exactly 1.
3. Four updates at `(true, 3.5, 1.0)` leave it at 1.
4. The fifth such update unlocks and leaves `1 - 0.1 / 0.75`, within `1e-4`.
5. Seven more bad updates reach exactly 0.

Also cover these contract boundaries, using parameterized cases where useful:

- Entry equality is good (`1.5 m`, `2 degrees`); exit equality (`3 m`, `4 degrees`) is not bad.
- A non-bad sample resets a partially accumulated exit dwell.
- NaN and +/-infinite delta time never advance dwell/blend.
- Invalid accuracy while already Locked unlocks after the exit dwell, rather than remaining locked forever.
- Tracking loss with zero delta time still unlocks immediately.

Do not weaken assertions to tolerate off-by-one state transitions. Test timing through public behavior, not private timers.

Follow the shared compile/test/report procedure. Suggested commit title, if committing: `Add alignment gate with hysteresis and blend`.
