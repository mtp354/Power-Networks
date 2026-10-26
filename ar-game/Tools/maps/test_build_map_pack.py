import json
from pathlib import Path
import tempfile
import unittest

from shapely.geometry import Point, Polygon
from build_map_pack import triangle_surface, build_pack, write_pack


def collection(features=()):
    return {"type": "FeatureCollection", "features": list(features)}


def feature(identifier, geometry, **properties):
    return {"type": "Feature", "id": identifier, "geometry": geometry,
            "properties": {"version": 1, "sources": [{"dataset": "Synthetic fixture"}], **properties}}


CONFIG = {"tileId": "synthetic-map", "name": "Synthetic waterfront", "origin": [0, 0],
          "bbox": [-.001, -.001, .001, .001], "defaultCenter": [0, 0], "release": "fixture"}


class MapPackTests(unittest.TestCase):
    def test_surface_triangulation_preserves_courtyard_holes_and_area(self):
        polygon = Polygon([(0,0),(10,0),(10,10),(0,10)], [[(3,3),(7,3),(7,7),(3,7)]])
        vertices = triangle_surface(polygon)
        triangles = [Polygon([(vertices[i],vertices[i+1]),(vertices[i+2],vertices[i+3]),
                              (vertices[i+4],vertices[i+5])]) for i in range(0,len(vertices),6)]
        self.assertAlmostEqual(sum(t.area for t in triangles),84)
        self.assertFalse(any(t.contains(Point(5,5)) for t in triangles))

    def test_real_feature_identity_provenance_and_width_estimates_are_preserved(self):
        road = feature("road-a", {"type": "LineString", "coordinates": [[-.0005,0],[.0005,0]]},
                       subtype="road", **{"class": "residential"})
        pack = build_pack(CONFIG, collection(), collection([road]), collection(), collection(), "2026-10-25T00:00:00Z")
        self.assertEqual(pack["mapSchemaVersion"],1)
        self.assertEqual(pack["mapName"],"Synthetic waterfront")
        self.assertEqual(pack["surfaces"][0]["sourceId"],"road-a")
        self.assertEqual(pack["surfaces"][0]["kind"],1)
        self.assertEqual(pack["surfaces"][0]["widthSource"],"ClassEstimate")
        self.assertIn("Synthetic fixture",pack["surfaces"][0]["sourcesJson"])

    def test_underground_and_non_area_water_are_reported_without_inventing_surfaces(self):
        road = feature("tunnel", {"type":"LineString","coordinates":[[-.0005,0],[.0005,0]]},
                       subtype="road", level_rules=[{"value":-1}])
        water = feature("label", {"type":"Point","coordinates":[0,0]},subtype="physical")
        pack=build_pack(CONFIG,collection(),collection([road]),collection(),collection([water]),"2026-10-25T00:00:00Z")
        self.assertEqual(pack["surfaces"],[])
        self.assertGreaterEqual(sum(s["count"] for s in pack["mapSkipReasons"]),2)

    def test_surfaces_are_clipped_to_the_pack_bounds(self):
        water=feature("water",{"type":"Polygon","coordinates":[[[-1,-1],[1,-1],[1,1],[-1,1],[-1,-1]]]},subtype="water")
        pack=build_pack(CONFIG,collection(),collection(),collection(),collection([water]),"2026-10-25T00:00:00Z")
        self.assertEqual(len(pack["surfaces"]),1)
        self.assertLess(max(abs(v) for v in pack["surfaces"][0]["vertices"]),113)

    def test_invalid_input_leaves_existing_pack_untouched(self):
        with tempfile.TemporaryDirectory() as directory:
            source=Path(directory)
            for name in ("building","segment","land-use","water"):
                (source/("nyc-brooklyn-"+name+".geojson")).write_text(json.dumps(collection()))
            output=source/"pack.bytes"
            write_pack(CONFIG,source,output)
            before=output.read_bytes()
            (source/"nyc-brooklyn-water.geojson").write_text("broken")
            with self.assertRaises(ValueError): write_pack(CONFIG,source,output)
            self.assertEqual(output.read_bytes(),before)


if __name__ == "__main__": unittest.main()
