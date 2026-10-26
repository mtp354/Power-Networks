"""Build an offline Overture neighborhood pack with clipped, hole-preserving surfaces."""
import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import sys
import tempfile

from pyproj import Transformer
from shapely import constrained_delaunay_triangles, transform
from shapely.geometry import box, shape

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/"footprints"))
from build_building_tile import build_tile


GREEN = {"park", "grass", "garden", "forest", "wood", "woodland", "recreation_ground", "meadow"}
WIDTHS = {"motorway":14, "trunk":12, "primary":10, "secondary":8, "tertiary":7,
          "residential":6, "living_street":5, "service":3, "footway":2.2,
          "pedestrian":3, "cycleway":2.5, "steps":1.5, "path":2}


def triangle_surface(polygon):
    triangles = []
    for triangle in constrained_delaunay_triangles(polygon).geoms:
        points = [(round(x,2),round(y,2)) for x,y in list(triangle.exterior.coords)[:3]]
        area = (points[1][0]-points[0][0])*(points[2][1]-points[0][1])-(points[1][1]-points[0][1])*(points[2][0]-points[0][0])
        if abs(area)<.0001:
            continue
        if area<0: points.reverse()
        first=min(range(3),key=lambda i:points[i]); points=points[first:]+points[:first]
        triangles.append(tuple(v for point in points for v in point))
    triangles.sort()
    return [v for triangle in triangles for v in triangle]


def build_pack(config, buildings, roads, land_use, water, imported_at):
    west,south,east,north=config["bbox"]
    if not (-180<=west<east<=180 and -90<=south<north<=90): raise ValueError("Invalid map bounds")
    lat,lon=config["origin"]
    project=Transformer.from_pipeline("+proj=pipeline +step +proj=cart +ellps=WGS84 +step +proj=topocentric +ellps=WGS84 "
                                     f"+lat_0={lat} +lon_0={lon} +h_0=0")
    def xy(x,y,z=None):
        zeros=[0]*len(x) if hasattr(x,"__len__") else 0
        a,b,_=project.transform(x,y,zeros)
        return a,b
    clip_geo=box(west,south,east,north)
    corners=[project.transform(x,y,0)[:2] for x,y in [(west,south),(east,south),(east,north),(west,north)]]
    bounds=[min(p[0] for p in corners),min(p[1] for p in corners),max(p[0] for p in corners),max(p[1] for p in corners)]
    if max(abs(v) for v in bounds)>5000: raise ValueError("Map pack exceeds 5 km local frame")
    pack=build_tile(buildings,tile_id=config["tileId"],origin_lat=lat,origin_lon=lon,release=config["release"],imported_at=imported_at)
    skipped=Counter(); surfaces=[]; contributors=set()
    for document,kind,layer in [(roads,1,"segment"),(land_use,2,"land_use"),(water,3,"water")]:
        if document.get("type")!="FeatureCollection" or not isinstance(document.get("features"),list): raise ValueError("Expected source FeatureCollection")
        if len(document["features"])>10000: raise ValueError("Map source exceeds feature budget")
        seen=set()
        for feature in document["features"]:
            props=feature.get("properties") or {}; identifier=feature.get("id") or props.get("id")
            if not isinstance(identifier,str) or not identifier: skipped[layer+":missing_id"]+=1; continue
            if identifier in seen: raise ValueError("Duplicate map source ID")
            seen.add(identifier)
            if kind==1 and props.get("subtype")!="road": skipped["non_road"]+=1; continue
            if kind==2 and props.get("class") not in GREEN: skipped["non_green_land_use"]+=1; continue
            if kind==3 and props.get("subtype")=="physical": skipped["non_area_water"]+=1; continue
            if any(r.get("value",0)<0 for r in (props.get("level_rules") or [])) or props.get("level",0) is not None and props.get("level",0)<0:
                skipped["underground_surface"]+=1; continue
            geometry=shape(feature.get("geometry"))
            if not geometry.is_valid: skipped["invalid_surface"]+=1; continue
            if kind==1 and geometry.geom_type not in ("LineString","MultiLineString") or kind!=1 and geometry.geom_type not in ("Polygon","MultiPolygon"):
                skipped["unsupported_surface_geometry"]+=1; continue
            clipped=geometry.intersection(clip_geo)
            if clipped.is_empty: skipped["outside_bounds"]+=1; continue
            metric=transform(clipped,xy,interleaved=False)
            width=WIDTHS.get(props.get("class"),3) if kind==1 else 0
            if kind==1: metric=metric.buffer(width/2,cap_style="flat",join_style="mitre").intersection(box(*bounds))
            metric=metric.simplify(.08,preserve_topology=True)
            parts=list(metric.geoms) if metric.geom_type in ("MultiPolygon","GeometryCollection") else [metric]
            for part,polygon in enumerate(parts):
                if polygon.geom_type!="Polygon" or polygon.area<.02: continue
                vertices=triangle_surface(polygon)
                if not vertices: continue
                surfaces.append({"sourceId":identifier,"partIndex":part,"kind":kind,"sourceVersion":props.get("version",0),
                                 "vertices":vertices,"sourcesJson":json.dumps(props.get("sources") or [],sort_keys=True,separators=(",",":")),
                                 "widthM":width,"widthSource":"ClassEstimate" if kind==1 else "NotApplicable"})
                for source in props.get("sources") or []:
                    if source.get("dataset"): contributors.add(source["dataset"])
    for building in pack["buildings"]:
        for source in json.loads(building["sourcesJson"]):
            if source.get("dataset"): contributors.add(source["dataset"])
    surfaces.sort(key=lambda item:(item["kind"],item["sourceId"],item["partIndex"]))
    triangles=sum(len(s["vertices"])//6 for s in surfaces)
    if len(surfaces)>6000 or triangles>100000: raise ValueError("Map surface budget exceeded")
    pack.update({"mapSchemaVersion":1,"mapName":config["name"],"licenseUrl":"https://opendatacommons.org/licenses/odbl/1-0/",
                 "coverageBbox":config["bbox"],"boundsM":[round(v,2) for v in bounds],
                 "defaultCenter":config["defaultCenter"],"surfaces":surfaces,"contributors":sorted(contributors),
                 "mapSkipReasons":[{"reason":k,"count":skipped[k]} for k in sorted(skipped)]})
    return pack


def write_pack(config,source_dir,output):
    source_dir,output=Path(source_dir),Path(output)
    names=["building","segment","land-use","water"]
    raw=[(source_dir/("nyc-brooklyn-"+name+".geojson")).read_bytes() for name in names]
    pack=build_pack(config,*[json.loads(value) for value in raw],datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00","Z"))
    pack["sourceSha256"]=hashlib.sha256(raw[0]).hexdigest()
    pack["mapSources"]=[{"layer":name,"sha256":hashlib.sha256(value).hexdigest()} for name,value in zip(names,raw)]
    encoded=json.dumps(pack,ensure_ascii=False,allow_nan=False,separators=(",",":"))+"\n"
    if len(encoded)>8000000: raise ValueError("Map JSON exceeds runtime byte budget")
    output.parent.mkdir(parents=True,exist_ok=True)
    temporary=None
    try:
        with tempfile.NamedTemporaryFile(mode="w",encoding="utf-8",dir=output.parent,delete=False) as stream:
            temporary=Path(stream.name); stream.write(encoded)
        os.replace(temporary,output)
    finally:
        if temporary and temporary.exists(): temporary.unlink()
    return pack


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config",type=Path,required=True); parser.add_argument("--source-dir",type=Path,required=True); parser.add_argument("--output",type=Path,required=True)
    args=parser.parse_args(); pack=write_pack(json.loads(args.config.read_text()),args.source_dir,args.output)
    print(json.dumps({"buildings":len(pack["buildings"]),"buildingSkips":pack["skipReasons"],"surfaces":dict(Counter(s["kind"] for s in pack["surfaces"])),
                      "triangles":sum(len(s["vertices"])//6 for s in pack["surfaces"]),"mapSkips":pack["mapSkipReasons"],"contributors":pack["contributors"]},indent=2))
