using System;
using FantasyEarth.Data;
using FantasyEarth.World;
using NUnit.Framework;

namespace FantasyEarth.Tests.World
{
    public class MapRegionTests
    {
        [Test]
        public void TriangleSurfacesPreserveHolesAndRejectUnknownKinds()
        {
            var surface=new MapSurface("synthetic",0,MapSurfaceKind.Park,new[] { new PlanarPoint(0,0),new PlanarPoint(10,0),new PlanarPoint(0,10) },1,"[]");
            Assert.IsTrue(surface.Contains(new PlanarPoint(2,2)));
            Assert.IsFalse(surface.Contains(new PlanarPoint(8,8)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new MapSurface("bad",0,(MapSurfaceKind)99,surface.Triangles,1,"[]"));
        }

        [Test]
        public void BrooklynPackHasRealSurfacesProvenanceAndHonestCoverage()
        {
            var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/FantasyEarth/Data/Maps/BrooklynWaterfront.bytes");
            Assert.IsNotNull(asset);
            MapRegion region=MapPackReader.Read(asset.text);
            Assert.AreEqual("Brooklyn waterfront",region.Name);
            Assert.Greater(region.Buildings.Buildings.Count,1000);
            Assert.Greater(region.Surfaces.Count,1000);
            Assert.IsTrue(region.Covers(new GeoPoint(40.7024,-73.9900)));
            Assert.IsFalse(region.Covers(new GeoPoint(40.78,-73.97)));
            Assert.IsNotEmpty(region.Buildings.Attribution);
        }

        [Test]
        public void ViewportDoesNotInventAPlayerAtThePreviewLocation()
        {
            var viewport=new MapViewport(new MetricBounds(-500,-500,500,500),new PlanarPoint(100,100));
            Assert.IsFalse(viewport.HasPlayer);
            viewport.UpdatePlayer(null,10);
            Assert.IsFalse(viewport.HasPlayer);
            viewport.UpdatePlayer(new PlanarPoint(700,0),5);
            Assert.IsFalse(viewport.HasPlayer);
            viewport.UpdatePlayer(new PlanarPoint(20,30),8);
            Assert.IsTrue(viewport.HasPlayer);
            Assert.AreEqual(20,viewport.Center.EastM);
            viewport.Pan(10,15);
            viewport.UpdatePlayer(new PlanarPoint(25,35),8);
            Assert.AreEqual(30,viewport.Center.EastM);
            viewport.Recenter();
            Assert.AreEqual(25,viewport.Center.EastM);
            viewport.Zoom(.0001);
            Assert.AreEqual(50,viewport.ZoomM);
            viewport.Zoom(100000);
            Assert.AreEqual(650,viewport.ZoomM);
            viewport.Pan(100000,-100000);
            Assert.AreEqual(500,viewport.Center.EastM);
            Assert.AreEqual(-500,viewport.Center.NorthM);
        }

        [Test]
        public void ReaderRejectsUnsupportedMapSchemaBeforeDisplayingIt()
        {
            var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/FantasyEarth/Data/Maps/BrooklynWaterfront.bytes");
            Assert.Throws<System.IO.InvalidDataException>(()=>MapPackReader.Read(asset.text.Replace("\"mapSchemaVersion\":1","\"mapSchemaVersion\":99")));
        }
    }
}
