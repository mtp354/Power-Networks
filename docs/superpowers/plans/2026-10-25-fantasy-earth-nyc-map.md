# NYC map: Brooklyn waterfront first

The owner requested map features focused on NYC and selected the Brooklyn
waterfront as the first area. Implement a bounded, offline public-data pilot
covering DUMBO/Brooklyn Bridge Park and nearby waterfront, with reusable map-pack
boundaries for later NYC coverage. Show coverage honestly; never place a fake
player marker at the demo center.

The owner also reported that live AR buildings appeared persistently but in the
wrong physical positions. Preserve that failed physical-placement result; map
development takes priority and must not depend on that AR transform.

- [x] Preserve coordinate-redacted field evidence and record the owner's AR offset report.
- [x] Build a reproducible Overture map-pack converter with real footprints, road paths, parks and water, source metadata, clipping and hole-preserving surface triangulation.
- [x] Add bounded immutable map-domain data and runtime readers; retain existing World/Fantasy separation.
- [x] Render a tilted 3D waterfront using bounded building detail/chunks, land/water/path surfaces and shared seeded props where map eligibility is known.
- [x] Add touch pan/zoom, camera↔map gesture/handle, and a clearly distinguished NYC preview when the device is outside coverage or location is unavailable.
- [x] Add a player marker/follow behavior only for valid in-coverage location, with orientation and accuracy state; do not export coordinates. Implemented/unit-validated; in-coverage field acceptance remains below.
- [x] Include visible source attribution and an in-app credits panel before bundling the map data.
- [x] Test geometry/data limits, map controls, location/coverage behavior and view switching; inspect GPU and Android/device results, preserve camera fallback and document limits. 182 Unity cases and five Python cases pass; Android installed and map/camera switching smoke-tested. See the [report](../../handoffs/reports/nyc-brooklyn-map.md).
- [ ] Owner confirms phone visuals, touch pinch, credits and live camera return.
- [ ] Validate marker/follow/heading with real Earth tracking inside pilot coverage.

Use existing Unity/Input System/URP packages. Map mode must not require a
Geospatial alignment lock. The AR session stays separate from map coordinates;
switch rendering without repeatedly destroying its native session. No new
location service, paid tile provider, backend or upload of device position is
needed for the offline pilot.
