# Building data and Streetscape matching

The owner authorized continued next-step work while the S25 outdoor test runs. Do not pause, inspect, install to, or otherwise disturb the phone until the owner returns. Work in the Editor and on public sample data.

- [x] Add WGS84 local east/up/north projection with independent reference-vector, round-trip, pole and antimeridian tests.
- [x] Add an immutable canonical building catalog with source release, version, height provenance and attribution metadata.
- [x] Convert Overture GeoJSON to bounded local tiles; preserve feature IDs and multipart identities, and explicitly report unsupported holes/geometry and estimated heights.
- [x] Add a validated runtime tile reader using Unity's existing JSON support, with no new Unity package dependency.
- [x] Match observed footprints to canonical building identities by polygon overlap, rejecting weak or ambiguous matches deterministically.
- [x] Add a mesh-to-footprint adapter for Streetscape matching with an explicit local-to-catalog transform; never mutate or destroy the source mesh.
- [x] Validate the full path on a small public Boston extract and render a real-footprint preview without committing raw extracts or using the owner's location.
- [x] Run meaningful Python/Unity tests and Android compilation, preserve the active phone session, document remaining live anchoring integration, and commit the work.

Completed evidence: [building data and matching report](../../handoffs/reports/building-data-matching.md).
153 Unity cases and eight Python cases pass; 23 supported public footprints rendered;
Android development build succeeded with zero errors. The phone was not accessed.

World remains independent of Unity. The on-device runtime scene and Streetscape setting stay unchanged during the outdoor test. No claim of live matching or physical AR alignment is made from synthetic or public-data validation.

Primary references: [Overture schema](https://docs.overturemaps.org/schema/reference/buildings/building/), [Python download tool](https://docs.overturemaps.org/getting-data/overturemaps-py/), [source attribution](https://docs.overturemaps.org/attribution/), [PROJ topocentric frame](https://proj.org/en/stable/operations/conversions/topocentric.html), [ARCore Streetscape Geometry](https://developers.google.com/ar/develop/unity-arf/geospatial/streetscape-geometry).
