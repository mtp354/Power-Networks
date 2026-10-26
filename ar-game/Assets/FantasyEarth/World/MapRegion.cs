using System;
using System.Collections.Generic;

namespace FantasyEarth.World
{
    public readonly struct MetricBounds
    {
        public MetricBounds(double west,double south,double east,double north)
        {
            if (!PlanarPoint.Finite(west) || !PlanarPoint.Finite(south) || !PlanarPoint.Finite(east) ||
                !PlanarPoint.Finite(north) || west>=east || south>=north) throw new ArgumentException("Invalid metric bounds.");
            West=west; South=south; East=east; North=north;
        }
        public double West { get; }
        public double South { get; }
        public double East { get; }
        public double North { get; }
        public bool Contains(PlanarPoint p) => p.EastM>=West && p.EastM<=East && p.NorthM>=South && p.NorthM<=North;
        public PlanarPoint Clamp(PlanarPoint p) => new PlanarPoint(Math.Max(West,Math.Min(East,p.EastM)),Math.Max(South,Math.Min(North,p.NorthM)));
    }

    public enum MapSurfaceKind { Road=1, Park=2, Water=3 }

    public sealed class MapSurface
    {
        public MapSurface(string sourceId,int partIndex,MapSurfaceKind kind,IReadOnlyList<PlanarPoint> triangles,int sourceVersion,string sourcesJson)
        {
            if (kind<MapSurfaceKind.Road || kind>MapSurfaceKind.Water) throw new ArgumentOutOfRangeException(nameof(kind));
            if (string.IsNullOrWhiteSpace(sourceId) || partIndex<0 || sourceVersion<0 || sourcesJson==null) throw new ArgumentException("Invalid surface provenance.");
            if (triangles==null || triangles.Count<3 || triangles.Count%3!=0 || triangles.Count>300000) throw new ArgumentException("Invalid surface triangles.");
            var copied=new PlanarPoint[triangles.Count];
            double west=double.MaxValue,south=double.MaxValue,east=double.MinValue,north=double.MinValue;
            for (int i=0;i<copied.Length;i++)
            {
                var p=triangles[i];
                if (Math.Abs(p.EastM)>5000 || Math.Abs(p.NorthM)>5000) throw new ArgumentException("Surface outside local frame.");
                copied[i]=p; west=Math.Min(west,p.EastM); south=Math.Min(south,p.NorthM);
                east=Math.Max(east,p.EastM); north=Math.Max(north,p.NorthM);
            }
            for (int i=0;i<copied.Length;i+=3)
                if (BuildingFootprint.Cross(copied[i],copied[i+1],copied[i+2])<1e-8) throw new ArgumentException("Invalid surface winding or area.");
            Bounds=new MetricBounds(west,south,east,north); Triangles=Array.AsReadOnly(copied);
            SourceId=sourceId; PartIndex=partIndex; Kind=kind; SourceVersion=sourceVersion; SourcesJson=sourcesJson;
        }
        public string SourceId { get; }
        public int PartIndex { get; }
        public MapSurfaceKind Kind { get; }
        public int SourceVersion { get; }
        public string SourcesJson { get; }
        public MetricBounds Bounds { get; }
        public IReadOnlyList<PlanarPoint> Triangles { get; }
        public bool Contains(PlanarPoint p)
        {
            if (!Bounds.Contains(p)) return false;
            for (int i=0;i<Triangles.Count;i+=3)
                if (BuildingFootprint.Cross(Triangles[i],Triangles[i+1],p)>=-1e-8 &&
                    BuildingFootprint.Cross(Triangles[i+1],Triangles[i+2],p)>=-1e-8 &&
                    BuildingFootprint.Cross(Triangles[i+2],Triangles[i],p)>=-1e-8) return true;
            return false;
        }
    }

    public sealed class MapRegion
    {
        public MapRegion(string name,BuildingCatalog buildings,IReadOnlyList<MapSurface> surfaces,MetricBounds bounds,
            double west,double south,double east,double north,GeoPoint defaultCenter,IReadOnlyList<string> contributors)
        {
            if (string.IsNullOrWhiteSpace(name) || buildings==null || surfaces==null || contributors==null) throw new ArgumentException("Incomplete map region.");
            if (!(west>=-180 && west<east && east<=180 && south>=-90 && south<north && north<=90)) throw new ArgumentException("Invalid geographic coverage.");
            if (surfaces.Count>6000) throw new ArgumentException("Map surface limit exceeded.");
            var copy=new MapSurface[surfaces.Count]; var ids=new HashSet<string>(StringComparer.Ordinal); int vertices=0;
            for (int i=0;i<copy.Length;i++)
            {
                var s=surfaces[i] ?? throw new ArgumentException("Null map surface.");
                if (!ids.Add(((int)s.Kind)+"/"+s.SourceId+"/"+s.PartIndex)) throw new ArgumentException("Duplicate map surface identity.");
                vertices+=s.Triangles.Count; copy[i]=s;
            }
            if (vertices>300000) throw new ArgumentException("Map triangle budget exceeded.");
            Name=name; Buildings=buildings; Surfaces=Array.AsReadOnly(copy); Bounds=bounds;
            West=west; South=south; East=east; North=north; DefaultCenter=defaultCenter;
            if (!Covers(defaultCenter)) throw new ArgumentException("Default center is outside coverage.");
            var sources=new string[contributors.Count]; for (int i=0;i<sources.Length;i++) sources[i]=contributors[i];
            Contributors=Array.AsReadOnly(sources);
        }
        public string Name { get; }
        public BuildingCatalog Buildings { get; }
        public IReadOnlyList<MapSurface> Surfaces { get; }
        public IReadOnlyList<string> Contributors { get; }
        public MetricBounds Bounds { get; }
        public GeoPoint DefaultCenter { get; }
        public double West { get; }
        public double South { get; }
        public double East { get; }
        public double North { get; }
        public bool Covers(GeoPoint point) => point.LongitudeDeg>=West && point.LongitudeDeg<=East && point.LatitudeDeg>=South && point.LatitudeDeg<=North;
        public double? EligibleMapGround(PlanarPoint point)
        {
            bool green=false;
            foreach (var surface in Surfaces)
                if (surface.Contains(point))
                { if (surface.Kind!=MapSurfaceKind.Park) return null; green=true; }
            return green ? .08 : (double?)null; // Schematic map plane, never a physical AR altitude.
        }
    }
}
