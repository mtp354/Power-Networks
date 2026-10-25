"""Convert Overture GeoJSON to a versioned, bounded local building tile for Unity."""

import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import tempfile

from pyproj import Transformer
from shapely.geometry import Polygon


class SkipPart(ValueError):
    pass


def _number(value):
    return type(value) in (int, float) and math.isfinite(value)


def _height(properties):
    height = properties.get("height")
    if height is not None:
        if not _number(height) or not 0 < height <= 500:
            raise SkipPart("invalid_height")
        return height, 1
    floors = properties.get("num_floors")
    if floors is not None:
        if type(floors) is not int or not 0 < floors <= 166:
            raise SkipPart("invalid_floors")
        return floors * 3.0, 2
    return 9.0, 3


def _outline(rings, transformer):
    if not isinstance(rings, list) or not rings:
        raise SkipPart("invalid_ring")
    if len(rings) != 1:
        raise SkipPart("unsupported_holes")
    if not isinstance(rings[0], list) or len(rings[0]) > 10000:
        raise SkipPart("invalid_ring")
    points = []
    for coordinate in rings[0]:
        if not isinstance(coordinate, (list, tuple)) or len(coordinate) < 2:
            raise SkipPart("invalid_coordinate")
        lon, lat = coordinate[:2]
        if not _number(lat) or not _number(lon) or not -90 <= lat <= 90 or not -180 <= lon <= 180:
            raise SkipPart("invalid_coordinate")
        east, north, _ = transformer.transform(lon, lat, 0)
        if not math.isfinite(east) or not math.isfinite(north) or abs(east) > 5000 or abs(north) > 5000:
            raise SkipPart("outside_local_frame")
        point = (round(east, 4), round(north, 4))
        if not points or point != points[-1]:
            points.append(point)
    if points and points[0] == points[-1]:
        points.pop()
    # Remove exactly redundant corners without smoothing or replacing the footprint.
    changed = True
    while changed and len(points) > 3:
        changed = False
        for i, b in enumerate(points):
            a, c = points[i - 1], points[(i + 1) % len(points)]
            if abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) < 1e-8:
                points.pop(i)
                changed = True
                break
    if not 3 <= len(points) <= 64:
        raise SkipPart("unsupported_vertex_count")
    polygon = Polygon(points)
    if not polygon.is_valid or polygon.area < .01:
        raise SkipPart("invalid_polygon")
    if not polygon.exterior.is_ccw:
        points.reverse()
    first = min(range(len(points)), key=lambda i: points[i])
    points = points[first:] + points[:first]
    return [value for point in points for value in point]


def build_tile(document, *, tile_id, origin_lat, origin_lon, release, imported_at):
    if not isinstance(document, dict) or document.get("type") != "FeatureCollection" or not isinstance(document.get("features"), list):
        raise ValueError("Expected a GeoJSON FeatureCollection")
    if len(document["features"]) > 10000:
        raise ValueError("Source feature limit exceeded; download a smaller bounding box")
    if not _number(origin_lat) or not _number(origin_lon) or not -90 <= origin_lat <= 90 or not -180 <= origin_lon <= 180:
        raise ValueError("Invalid tile origin")
    if not tile_id or not release:
        raise ValueError("Tile ID and source release are required")
    transformer = Transformer.from_pipeline(
        "+proj=pipeline +step +proj=cart +ellps=WGS84 +step +proj=topocentric +ellps=WGS84 "
        f"+lat_0={origin_lat!r} +lon_0={origin_lon!r} +h_0=0")
    buildings, skipped, identities = [], Counter(), set()
    for feature in document["features"]:
        if not isinstance(feature, dict):
            skipped["invalid_feature"] += 1
            continue
        properties = feature.get("properties") or {}
        geometry = feature.get("geometry") or {}
        if not isinstance(properties, dict) or not isinstance(geometry, dict):
            skipped["invalid_feature"] += 1
            continue
        identifier = feature.get("id") or properties.get("id")
        if not isinstance(identifier, str) or not identifier.strip():
            skipped["missing_identity"] += 1
            continue
        if identifier in identities:
            raise ValueError("Duplicate source building identity")
        identities.add(identifier)
        if geometry.get("type") == "Polygon":
            polygons = [geometry.get("coordinates")]
        elif geometry.get("type") == "MultiPolygon":
            polygons = geometry.get("coordinates")
            if not isinstance(polygons, list) or not polygons:
                skipped["invalid_multipart"] += 1
                continue
        else:
            skipped["unsupported_geometry"] += 1
            continue
        for part_index, rings in enumerate(polygons):
            try:
                if properties.get("is_underground") is True:
                    raise SkipPart("underground")
                for field in ("min_height", "min_floor"):
                    value = properties.get(field)
                    if value is not None and (not _number(value) or value != 0):
                        raise SkipPart("elevated_geometry")
                version = properties.get("version")
                if type(version) is not int or version < 1:
                    raise SkipPart("invalid_source_version")
                height, height_source = _height(properties)
                outline = _outline(rings, transformer)
                buildings.append({
                    "stableId": "overture:" + identifier, "sourceId": identifier, "partIndex": part_index,
                    "sourceVersion": version, "heightM": round(height, 3), "heightSource": height_source,
                    "outline": outline,
                    "sourcesJson": json.dumps(properties.get("sources") or [], sort_keys=True, separators=(",", ":"), allow_nan=False),
                })
            except SkipPart as error:
                skipped[str(error)] += 1
    if len(buildings) > 2000:
        raise ValueError("Tile building limit exceeded; download a smaller bounding box")
    buildings.sort(key=lambda item: (item["stableId"], item["partIndex"]))
    return {
        "schemaVersion": 1, "tileId": tile_id, "origin": [origin_lat, origin_lon, 0.0],
        "provider": "Overture Maps Foundation", "sourceRelease": release, "importedAtUtc": imported_at,
        "license": "ODbL-1.0",
        "attribution": "© OpenStreetMap contributors, Overture Maps Foundation; additional contributors in source metadata.",
        "attributionUrl": "https://docs.overturemaps.org/attribution/",
        "buildings": buildings, "inputFeatures": len(document["features"]), "skippedParts": sum(skipped.values()),
        "skipReasons": [{"reason": reason, "count": skipped[reason]} for reason in sorted(skipped)],
    }


def write_tile(source, output, *, tile_id, origin_lat, origin_lon, release):
    source, output = Path(source), Path(output)
    if source.resolve() == output.resolve():
        raise ValueError("Source and tile output must be different files")
    raw = source.read_bytes()
    tile = build_tile(json.loads(raw), tile_id=tile_id, origin_lat=origin_lat, origin_lon=origin_lon,
                      release=release, imported_at=datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z"))
    tile["sourceSha256"] = hashlib.sha256(raw).hexdigest()
    encoded = json.dumps(tile, ensure_ascii=False, allow_nan=False, separators=(",", ":")) + "\n"
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=output.parent, delete=False) as file:
            temporary = Path(file.name)
            file.write(encoded)
        os.replace(temporary, output)
    finally:
        if temporary and temporary.exists():
            temporary.unlink()
    return tile


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--tile-id", required=True)
    parser.add_argument("--origin-lat", type=float, required=True)
    parser.add_argument("--origin-lon", type=float, required=True)
    parser.add_argument("--release", required=True)
    args = parser.parse_args()
    tile = write_tile(args.source, args.output, tile_id=args.tile_id, origin_lat=args.origin_lat,
                      origin_lon=args.origin_lon, release=args.release)
    print(json.dumps({"inputFeatures": tile["inputFeatures"], "buildings": len(tile["buildings"]),
                      "skippedParts": tile["skippedParts"], "skipReasons": tile["skipReasons"],
                      "sourceSha256": tile["sourceSha256"]}, indent=2))
