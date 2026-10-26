# Brooklyn waterfront pilot database

`BrooklynWaterfront.bytes` is a derived Overture Maps database made available
under the [Open Database License (ODbL) 1.0](https://opendatacommons.org/licenses/odbl/1-0/).
The license text is included in this directory. This notice applies to the map
database, not the project's independently authored code, meshes or shaders.

© OpenStreetMap contributors, Overture Maps Foundation. Building footprint
sources include Microsoft Global ML Building Footprints (ODbL). Height data
includes USGS 3D Elevation Program lidar. Transportation includes data from TomTom.
Parks and water are derived from OpenStreetMap. Original per-feature `sources`
metadata is retained in `sourcesJson`.

- Overture release: `2026-10-21.0`.
- Coverage: longitude -74.005 to -73.983, latitude 40.694 to 40.710.
- First area: DUMBO/Brooklyn Bridge Park and nearby Brooklyn waterfront, with
  adjacent waterfront features where they fall within this bounded extract.
- Source attribution: https://docs.overturemaps.org/attribution/.
- OpenStreetMap copyright: https://www.openstreetmap.org/copyright.
- Microsoft footprints: https://github.com/microsoft/GlobalMLBuildingFootprints.
- USGS 3DEP: https://www.usgs.gov/3d-elevation-program.

Changes by Fantasy Earth: project to a WGS84 local east/north frame; retain
supported canonical building rings/heights and their estimate flags; clip map
surfaces to the pilot bounds; buffer road centerlines using explicitly estimated
class widths; simplify surface edges by up to 0.08 m; triangulate polygons while
preserving holes; quantize surface coordinates to 0.01 m. Underground surfaces and
unsupported features are reported as skips. Terrain is represented schematically
for the map and is not an elevation model or AR placement altitude.

The database retains source IDs/versions, contribution metadata, release/import
time, source-file SHA-256 hashes and skip statistics. It contains 1,268 supported
building parts and 2,706 road/green-space/water surfaces. Building omissions are
reported, including courtyard rings that the current building generator cannot
render; surface holes remain intact.

The derived database is provided directly in this directory. Reproduce it using
`Tools/maps/build_map_pack.py`, the `Tools/maps/nyc-brooklyn.json` configuration
and the pinned dependencies in `Tools/footprints/requirements.txt`. Raw downloads
stay outside source control. Visible map attribution and detailed credits are
included in the app.
