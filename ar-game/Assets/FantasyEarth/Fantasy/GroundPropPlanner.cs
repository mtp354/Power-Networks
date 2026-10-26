using System;
using System.Collections.Generic;
using System.Globalization;
using FantasyEarth.World;

namespace FantasyEarth.Fantasy
{
    public enum GroundPropKind { Vegetation, Boulder, RuneStone }

    public readonly struct GroundProp
    {
        internal GroundProp(string id,ulong seed,GeoPoint geographic,LocalPoint position,GroundPropKind kind,
            Biome biome,double scale,double yaw)
        {
            Id=id; Seed=seed; GeographicPosition=geographic; Position=position; Kind=kind; Biome=biome; Scale=scale; YawDeg=yaw;
        }
        public string Id { get; }
        public ulong Seed { get; }
        public GeoPoint GeographicPosition { get; }
        public LocalPoint Position { get; }
        public GroundPropKind Kind { get; }
        public Biome Biome { get; }
        public double Scale { get; }
        public double YawDeg { get; }
        public double RadiusM => (Kind==GroundPropKind.Vegetation ? 2.4 : Kind==GroundPropKind.Boulder ? 1.2 : .8)*Scale;
        public GroundProp Reproject(LocalGeoFrame frame,double groundUpM)
        {
            if (frame==null) throw new ArgumentNullException(nameof(frame));
            LocalPoint point=frame.Project(GeographicPosition,frame.OriginAltitudeM);
            return new GroundProp(Id,Seed,GeographicPosition,new LocalPoint(point.EastM,groundUpM,point.NorthM),Kind,Biome,Scale,YawDeg);
        }
    }

    /// <summary>Shared camera/map candidate rules. Eligible ground height is supplied, never invented.</summary>
    public static class GroundPropPlanner
    {
        public static IReadOnlyList<GroundProp> Generate(LocalGeoFrame frame,double radiusM,Func<GeoPoint,Biome> biomeAt,
            Func<PlanarPoint,double?> eligibleGroundUpM,IReadOnlyList<BuildingFootprint> buildings,ulong worldSeed=1)
        {
            if (frame==null || biomeAt==null || eligibleGroundUpM==null || buildings==null) throw new ArgumentNullException();
            if (!Finite(radiusM) || radiusM<=0 || radiusM>128 || buildings.Count>2000) throw new ArgumentOutOfRangeException(nameof(radiusM));
            foreach (var building in buildings) if (building==null) throw new ArgumentException("Null building obstacle.",nameof(buildings));
            var props=new List<GroundProp>();
            foreach (var cell in GeoCell.Near(frame.Origin,radiusM))
            {
                string id="FantasyEarth/props/v1/"+worldSeed.ToString("x16",CultureInfo.InvariantCulture)+"/"+cell.Key;
                ulong seed=StableSeed.Hash(id);
                if (seed%100>=55) continue;
                ulong jitter=StableSeed.Hash(id+"/position");
                GeoPoint geographic=cell.Point(.35+.3*Unit(jitter),.35+.3*Unit(jitter>>24));
                LocalPoint point=frame.Project(geographic,frame.OriginAltitudeM);
                if (point.EastM*point.EastM+point.NorthM*point.NorthM>radiusM*radiusM) continue;
                Biome biome=biomeAt(geographic);
                if (!Enum.IsDefined(typeof(Biome),biome)) throw new ArgumentOutOfRangeException(nameof(biomeAt));
                ulong style=StableSeed.Hash(id+"/style");
                int choice=(int)(style%100);
                var kind=choice<55 ? GroundPropKind.Vegetation : choice<85 ? GroundPropKind.Boulder : GroundPropKind.RuneStone;
                var recipe=new GroundProp(id,seed,geographic,point,kind,biome,.85+.45*Unit(style>>16),360*Unit(style>>40));
                double clearance=recipe.RadiusM+.75;
                bool blocked=false;
                foreach (var building in buildings)
                {
                    if (point.EastM<building.MinEastM-clearance || point.EastM>building.MinEastM+building.WidthM+clearance ||
                        point.NorthM<building.MinNorthM-clearance || point.NorthM>building.MinNorthM+building.DepthM+clearance) continue;
                    if (FootprintClearance.DistanceM(building,point.Planar)<=clearance) { blocked=true; break; }
                }
                if (blocked) continue;
                double? ground=eligibleGroundUpM(point.Planar);
                if (!ground.HasValue || !Finite(ground.Value) || Math.Abs(ground.Value)>10000) continue;
                props.Add(new GroundProp(id,seed,geographic,new LocalPoint(point.EastM,ground.Value,point.NorthM),kind,biome,recipe.Scale,recipe.YawDeg));
                if (props.Count>1024) throw new ArgumentException("Ground prop query exceeds its result budget.");
            }
            props.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
            return props.AsReadOnly();
        }

        private static double Unit(ulong value) => (value&0xffffff)/16777216.0;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
