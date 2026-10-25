# Seeded ground props while the phone is in the field

The owner authorized other feature work during the live-building walk. Do not
pause, inspect, install to, or otherwise use the phone until the owner returns.

This slice prepares shared deterministic ground props for camera and map. It
does not guess ground height or add props to the running live scene. Unknown
ground is ineligible; placement requires an explicit surface sample, avoids
canonical building footprints with clearance, and uses geographic cell identities
independent of a caller's local frame or query order.

- [x] Add a bounded, versioned geographic cell grid that handles poles and longitude wrap.
- [x] Generate seeded vegetation, boulder and rune-stone recipes with frame-independent identity/position and biome-specific appearance.
- [x] Require eligible ground height, reject unknown/non-finite ground, and keep props clear of building footprints.
- [x] Build low-poly biome-aware prop meshes in shared chunks with explicit ownership and bounded counts.
- [x] Add reusable presentation: map props stay visible; camera props use alignment blend and disappear on tracking loss.
- [x] Test repeatability, overlapping queries, frame changes, poles/dateline, clearance, mesh bounds and lifecycle; render a synthetic four-biome preview.
- [x] Build Android without installing, document integration limits and commit the work while preserving the owner's phone test.

Evidence: 175 Unity cases pass; five synthetic GPU renders include 36 props and
12,048 prop vertices in four chunks; Android build succeeds with zero errors and
the required Java dependencies present. See the
[ground-prop report](../../handoffs/reports/ground-props.md).

The first candidate grid is an algorithmic geographic partition, not observed
vegetation. The supplied shared world/ground mask determines eligibility. Surface
and canonical map data wiring, real terrain anchoring and physical device prop
alignment remain separate acceptance work.
