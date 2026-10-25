import copy
import json
from pathlib import Path
import tempfile
import unittest

from build_building_tile import build_tile, write_tile


def feature(identifier="synthetic-a", *, height=None, floors=None, holes=False):
    rings = [[[-71.063, 42.357], [-71.0629, 42.357], [-71.0629, 42.3571],
              [-71.063, 42.3571], [-71.063, 42.357]]]
    if holes:
        rings.append([[-71.06298, 42.35702], [-71.06298, 42.35704],
                      [-71.06296, 42.35704], [-71.06298, 42.35702]])
    return {"type": "Feature", "id": identifier,
            "geometry": {"type": "Polygon", "coordinates": rings},
            "properties": {"version": 2, "height": height, "num_floors": floors,
                           "sources": [{"dataset": "Synthetic test", "record_id": "fixture-1"}]}}


def convert(features):
    return build_tile({"type": "FeatureCollection", "features": features},
                      tile_id="synthetic-tile", origin_lat=42.357, origin_lon=-71.063,
                      release="synthetic-release", imported_at="2026-10-25T00:00:00Z")


class BuildingTileTests(unittest.TestCase):
    def test_preserves_identity_source_and_reported_height(self):
        tile = convert([feature(height=12.25)])
        building = tile["buildings"][0]
        self.assertEqual(building["sourceId"], "synthetic-a")
        self.assertEqual(building["stableId"], "overture:synthetic-a")
        self.assertEqual(building["sourceVersion"], 2)
        self.assertEqual(building["heightM"], 12.25)
        self.assertEqual(building["heightSource"], 1)
        self.assertIn("Synthetic test", building["sourcesJson"])
        self.assertAlmostEqual(building["outline"][0], 0, places=2)
        self.assertEqual(tile["license"], "ODbL-1.0")

    def test_estimated_height_is_explicit(self):
        tile = convert([feature("floors", floors=4), feature("fallback")])
        by_id = {b["sourceId"]: b for b in tile["buildings"]}
        self.assertEqual((by_id["floors"]["heightM"], by_id["floors"]["heightSource"]), (12, 2))
        self.assertEqual((by_id["fallback"]["heightM"], by_id["fallback"]["heightSource"]), (9, 3))

    def test_multipart_components_share_seed_identity(self):
        item = feature(height=8)
        second = copy.deepcopy(item["geometry"]["coordinates"])
        for point in second[0]:
            point[0] += .0002
        item["geometry"] = {"type": "MultiPolygon", "coordinates": [item["geometry"]["coordinates"], second]}
        buildings = convert([item])["buildings"]
        self.assertEqual([b["partIndex"] for b in buildings], [0, 1])
        self.assertEqual(buildings[0]["stableId"], buildings[1]["stableId"])

    def test_holes_and_invalid_heights_are_reported_instead_of_filled_or_clamped(self):
        tile = convert([feature("courtyard", holes=True), feature("negative", height=-1)])
        self.assertEqual(tile["buildings"], [])
        self.assertEqual(tile["skippedParts"], 2)
        self.assertEqual(tile["skipReasons"], [{"reason": "invalid_height", "count": 1}, {"reason": "unsupported_holes", "count": 1}])

    def test_reordering_features_does_not_change_output_order(self):
        a, b = feature("a", height=7), feature("b", height=11)
        self.assertEqual(convert([b, a]), convert([a, b]))

    def test_duplicate_identity_is_rejected(self):
        with self.assertRaises(ValueError):
            convert([feature(), feature()])

    def test_malformed_geometry_is_reported_without_losing_valid_neighbors(self):
        ring, multi = feature("bad-ring"), feature("bad-multi")
        ring["geometry"]["coordinates"] = [None]
        multi["geometry"] = {"type": "MultiPolygon", "coordinates": None}
        tile = convert([ring, multi, feature("valid")])
        self.assertEqual([b["sourceId"] for b in tile["buildings"]], ["valid"])
        self.assertEqual(tile["skippedParts"], 2)

    def test_invalid_source_does_not_replace_a_previously_valid_output(self):
        with tempfile.TemporaryDirectory() as directory:
            source, output = Path(directory)/"source.json", Path(directory)/"tile.json"
            source.write_text(json.dumps({"type": "FeatureCollection", "features": [feature()]}))
            options = dict(tile_id="test", origin_lat=42.357, origin_lon=-71.063, release="fixture")
            tile = write_tile(source,output,**options)
            self.assertEqual(len(tile["sourceSha256"]),64)
            before = output.read_bytes()
            source.write_text("{broken json")
            with self.assertRaises(ValueError):
                write_tile(source,output,**options)
            self.assertEqual(output.read_bytes(),before)


if __name__ == "__main__":
    unittest.main()
