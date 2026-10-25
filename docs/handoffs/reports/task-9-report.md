# Task 9: painterly rendering and biome looks

Status: **implemented and editor-validated**, 2026-10-25.

- Added the four-sector Kuwahara shader (radius 4), horizon-band mist globals, and subtle paper grain.
- Added a Render Graph renderer feature before post-processing. It explicitly requires an intermediate color target, skips non-game cameras/backbuffers, derives transient descriptors from the active graph texture, filters at half resolution, restores full resolution, and hands the result to URP through `cameraColor`.
- Added all four biome look assets and Volume profiles, with color adjustments, bloom threshold 0.8, vignette, and neutral tonemapping. Enabled HDR on the mobile pipeline asset. The AR background feature remains present and active.
- Added versioned builders outside Assets: `AgentScripts/BuildPaintedAssets.cs` creates/updates the asset set while retaining GUIDs, and `PreviewPaintedLook.cs` renders temporary synthetic previews without changing the production scene.

RED: the rendering tests failed to compile on missing Rendering/URP assembly references. GREEN: **111 EditMode cases passed, 0 failed/skipped**, including library completeness, profile overrides, the active renderer/material/shader, and missing-biome behavior.

The four synthetic previews rendered through the actual URP feature without live shader or Render Graph errors; PNGs are in ignored `ar-game/Temp/painterly-previews/`. Live Console ground truth after rendering: **0 errors, 0 warnings**. Synthetic geometry verifies the render path; camera-image quality and performance still require the planned S25 run.

Render Graph validation covered the skill's material binding, source/destination ownership, fresh per-recording handles, graph-derived descriptors, helper blits, and final resource handoff. There is no custom render callback or persistent render target to dispose. The half-resolution filter is an intentional mobile-cost choice; it uses a separate full-resolution output to preserve downstream camera resource dimensions.
