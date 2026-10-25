using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.Fantasy
{
    public class GroundPropPlannerTests
    {
        private static IReadOnlyList<GroundProp> Plan(LocalGeoFrame frame,ulong seed=1,
            IReadOnlyList<BuildingFootprint> obstacles=null,Func<PlanarPoint,double?> ground=null) =>
            GroundPropPlanner.Generate(frame,64,_=>Biome.Temperate,ground??(_=>(double?)0),obstacles??Array.Empty<BuildingFootprint>(),seed);

        [Test]
        public void RecipesRepeatAcrossCallsCultureAndLocalFrames()
        {
            var frame=new LocalGeoFrame(new GeoPoint(0,0));
            var first=Plan(frame);
            Assert.That(first.Count,Is.GreaterThan(10));
            CultureInfo previous=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");
                var again=Plan(frame);
                Assert.AreEqual(first.Count,again.Count);
                for (int i=0;i<first.Count;i++) AssertSame(first[i],again[i]);
            }
            finally { CultureInfo.CurrentCulture=previous; }
            GeoPosition shifted=frame.Unproject(new LocalPoint(20,0,10));
            var second=Plan(new LocalGeoFrame(shifted.Point));
            var byId=second.ToDictionary(p=>p.Id);
            int shared=0;
            foreach (var prop in first)
                if (byId.TryGetValue(prop.Id,out var other)) { AssertSame(prop,other); shared++; }
            Assert.That(shared,Is.GreaterThan(10));
            Assert.AreNotEqual(first[0].Seed,Plan(frame,2)[0].Seed);
        }

        [Test]
        public void UnknownAndInvalidGroundNeverSpawnProps()
        {
            var frame=new LocalGeoFrame(new GeoPoint(0,0));
            Assert.AreEqual(0,Plan(frame,ground:_=>null).Count);
            Assert.AreEqual(0,Plan(frame,ground:_=>double.NaN).Count);
            foreach (var prop in Plan(frame,ground:p=>p.EastM>0 ? (double?)2 : null))
            { Assert.Greater(prop.Position.EastM,0); Assert.AreEqual(2,prop.Position.UpM); }
        }

        [Test]
        public void BuildingClearanceUsesConcaveFootprintAndPropRadius()
        {
            var shape=new BuildingFootprint("synthetic-obstacle",new[] { new PlanarPoint(-8,-8),new PlanarPoint(8,-8),
                new PlanarPoint(8,0),new PlanarPoint(0,0),new PlanarPoint(0,8),new PlanarPoint(-8,8) },8);
            Assert.AreEqual(0,FootprintClearance.DistanceM(shape,new PlanarPoint(-2,-2)));
            Assert.AreEqual(4,FootprintClearance.DistanceM(shape,new PlanarPoint(4,4)),1e-8);
            foreach (var prop in Plan(new LocalGeoFrame(new GeoPoint(0,0)),obstacles:new[] { shape }))
                Assert.Greater(FootprintClearance.DistanceM(shape,prop.Position.Planar),prop.RadiusM+.75);
        }

        [Test]
        public void BiomeChangesAppearanceWithoutMovingGeographicCandidates()
        {
            var frame=new LocalGeoFrame(new GeoPoint(0,0));
            var a=GroundPropPlanner.Generate(frame,64,_=>Biome.Temperate,_=>(double?)0,Array.Empty<BuildingFootprint>());
            var b=GroundPropPlanner.Generate(frame,64,_=>Biome.Snowy,_=>(double?)0,Array.Empty<BuildingFootprint>());
            Assert.AreEqual(a.Count,b.Count);
            for (int i=0;i<a.Count;i++) { AssertSame(a[i],b[i]); Assert.AreNotEqual(a[i].Biome,b[i].Biome); }
        }

        private static void AssertSame(GroundProp a,GroundProp b)
        {
            Assert.AreEqual(a.Id,b.Id); Assert.AreEqual(a.Seed,b.Seed); Assert.AreEqual(a.Kind,b.Kind);
            Assert.AreEqual(a.GeographicPosition.LatitudeDeg,b.GeographicPosition.LatitudeDeg);
            Assert.AreEqual(a.GeographicPosition.LongitudeDeg,b.GeographicPosition.LongitudeDeg);
            Assert.AreEqual(a.Scale,b.Scale); Assert.AreEqual(a.YawDeg,b.YawDeg);
        }
    }
}
