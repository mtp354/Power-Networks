# First live building placement

The owner approved using ARCore's observed building shapes and temporary session
identities to validate live placement before local map tiles are available.

Contract: show fantasy-styled observed buildings only with confident alignment,
at their SDK poses under the XR origin. Preserve source geometry (including
concavities/courtyards), borrow SDK meshes read-only, and release owned render
meshes on removal/reset. Temporary identities are not canonical map matches.
No new location service or coordinate export is introduced.

- [x] Verify the pinned SDK's pose space and add transform tests including a translated/rotated XR origin.
- [x] Add an owned observed-building mesh view using existing biome/style recipes, procedural facade details and a fade driven by alignment blend.
- [x] Start Streetscape only after consent, permissions, native Extensions and Earth tracking are ready; safely pause/reset/remove observations.
- [x] Bound active buildings, distance, vertex budget and mesh creation rate; update poses without regenerating meshes every frame.
- [x] Patch World narrowly, preserve existing scene references/build list, and extend diagnostics with coordinate-free placement status/counts.
- [x] Validate borrowed-mesh ownership, lifecycle, source-shape preservation, fade and scene wiring; render a synthetic placement preview and build Android.
- [x] Verify Java dependencies, install the validated build without clearing data, smoke-check the phone and document the outdoor placement check.

Implemented and installed; 159 Unity cases pass. Build and initial process checks
pass, while physical placement and live source arrival remain device acceptance
work. See the [live-placement report](../../handoffs/reports/live-building-placement.md).

Device acceptance:

- [x] Owner confirms live camera video and the Buildings diagnostics line on the installed live-placement build.
- [x] Outdoors, confirm native building observations arrive and drawn count becomes positive after alignment lock. Owner observed persistent buildings; the session recorded 126 Locked samples.
- [ ] Check physical placement/stability while panning/walking, then Home/reopen recovery without stale duplicates. **Failed physical placement:** owner reports persistent geometry in the wrong real-world positions. Offset investigation/recovery acceptance remain open; map work was prioritized next.

The first live slice re-skins the actual Streetscape surface rather than guessing
a ground altitude or extruding the matching adapter's convex hull. The original
procedural canonical-footprint generator remains available for later map-ID
integration. Camera-only mode and the calibrated alignment thresholds are retained.
Rollback is the confirmed working camera APK; preserve it before installing.
