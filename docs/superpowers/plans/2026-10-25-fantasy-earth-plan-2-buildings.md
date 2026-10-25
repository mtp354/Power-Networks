# Plan 2 first slice: reusable fantasy buildings

The owner requested a small camera repair, then progress on other tasks with alignment fine-tuning deferred. The missing Android location/auth dependency repair is installed and the owner confirmed camera video is back.

This slice implements reusable geometry and art that can feed the camera and map later. It uses synthetic, explicitly named preview footprints; no live location or map download is needed.

- [x] Add validated immutable local building footprints and simple-polygon triangulation in World.
- [x] Add stable UTF-8 hashing and deterministic biome-aware building recipes in Fantasy.
- [x] Generate footprint-shaped walls, roofs, windows, timber details, battlements and banners using shared vertex-colored meshes.
- [x] Add a lightweight building shader, a reusable renderer component and an offline BuildingPreview scene. Keep World as the sole Android build scene.
- [x] Test geometry, deterministic recipes and mesh ownership; compile and render previews in Unity.
- [x] Record the completed slice and remaining integration, and commit it.

Footprints support simple outer rings, including concave outlines. Holes, Overture ingestion, matching Streetscape meshes to stable building IDs, anchoring rebuilt visuals, seeded ground props and the map view remain integration work after this slice. No claim of physical AR alignment is made from the preview.
