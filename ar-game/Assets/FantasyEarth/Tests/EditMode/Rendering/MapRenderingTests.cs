using System;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Rendering
{
    public class MapRenderingTests
    {
        [Test]
        public void MapBuildingDetailKeepsFootprintAndRoofWithLessGeometry()
        {
            var footprint=new BuildingFootprint("map-fixture",new[] { new PlanarPoint(0,0),new PlanarPoint(8,0),
                new PlanarPoint(8,6),new PlanarPoint(0,6) },24);
            var recipe=BuildingRecipe.Create(footprint,Biome.Temperate);
            Mesh full=BuildingMeshBuilder.Create(footprint,recipe),map=BuildingMeshBuilder.Create(footprint,recipe,false);
            try
            {
                Assert.Less(map.vertexCount,full.vertexCount/4);
                Assert.AreEqual(0,map.bounds.min.x,1e-4); Assert.AreEqual(8,map.bounds.max.x,1e-4);
                Assert.GreaterOrEqual(map.bounds.max.y,24);
            }
            finally { UnityEngine.Object.DestroyImmediate(full); UnityEngine.Object.DestroyImmediate(map); }
        }

        [Test]
        public void SurfaceMeshFacesUpWithoutFillingMissingTriangleArea()
        {
            var surface=new MapSurface("synthetic",0,MapSurfaceKind.Park,new[] { new PlanarPoint(0,0),new PlanarPoint(10,0),new PlanarPoint(0,10) },1,"[]");
            Mesh mesh=NeighborhoodMapRenderer.SurfaceMesh(new[] { surface },Color.green,.03f);
            try
            {
                Assert.AreEqual(3,mesh.vertexCount); Assert.AreEqual(3,mesh.triangles.Length);
                var p=mesh.vertices; var t=mesh.triangles;
                Assert.Greater(Vector3.Cross(p[t[1]]-p[t[0]],p[t[2]]-p[t[0]]).y,0);
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void ReprojectedPropRetainsIdentityAndGeographicPosition()
        {
            var original=new LocalGeoFrame(new GeoPoint(0,0));
            var props=GroundPropPlanner.Generate(original,32,_=>Biome.Temperate,_=>(double?)0,Array.Empty<BuildingFootprint>());
            var frame=new LocalGeoFrame(original.Unproject(new LocalPoint(20,0,10)).Point);
            var moved=props[0].Reproject(frame,.08);
            Assert.AreEqual(props[0].Id,moved.Id); Assert.AreEqual(props[0].Seed,moved.Seed);
            Assert.AreEqual(props[0].GeographicPosition.LatitudeDeg,moved.GeographicPosition.LatitudeDeg);
            Assert.AreEqual(.08,moved.Position.UpM);
            Assert.AreEqual(frame.Project(moved.GeographicPosition).EastM,moved.Position.EastM,1e-6);
        }
    }
}
