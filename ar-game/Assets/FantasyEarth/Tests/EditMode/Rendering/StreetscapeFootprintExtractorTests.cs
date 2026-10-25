using System;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Rendering
{
    public class StreetscapeFootprintExtractorTests
    {
        [Test]
        public void BorrowedMeshTransformsIntoTheCatalogFrameAndMatchesItsIdentity()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(0,0,0),new Vector3(10,0,0),new Vector3(10,0,8),new Vector3(0,0,8),
                new Vector3(0,6,0),new Vector3(10,6,0),new Vector3(10,6,8),new Vector3(0,6,8) };
            var before = mesh.vertices;
            try
            {
                var transform = Matrix4x4.TRS(new Vector3(20,3,40),Quaternion.Euler(0,90,0),Vector3.one);
                Assert.IsTrue(StreetscapeFootprintExtractor.TryExtract(mesh,transform,"session:mesh",out var footprint));
                Assert.AreEqual(80,footprint.AreaM2,1e-4);
                Assert.AreEqual(6,footprint.HeightM,1e-4);
                var canonical = new BuildingFootprint("overture:fixture",new[] { new PlanarPoint(20,30),new PlanarPoint(28,30),
                    new PlanarPoint(28,40),new PlanarPoint(20,40) },8);
                var record = new CatalogBuilding("fixture",0,canonical,1,BuildingHeightSource.Reported,"[]");
                var catalog = new BuildingCatalog("synthetic",new LocalGeoFrame(new GeoPoint(0,0)),new[] { record },"fixture",
                    DateTimeOffset.UnixEpoch,"fixture","Synthetic geometry","test");
                var match = new BuildingFootprintMatcher(catalog).Match(footprint);
                Assert.AreEqual(BuildingMatchStatus.Matched,match.Status);
                Assert.AreEqual("overture:fixture",match.Building.Footprint.StableId);
                Assert.AreEqual(1,match.IntersectionOverUnion,1e-5);
                Assert.IsNotNull(mesh);
                CollectionAssert.AreEqual(before,mesh.vertices);
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void EmptyMeshDoesNotProduceAFalseBuilding()
        {
            var mesh = new Mesh();
            try { Assert.IsFalse(StreetscapeFootprintExtractor.TryExtract(mesh,Matrix4x4.identity,"empty",out _)); }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void UnreadableMeshIsRejectedWithoutAttemptingToReadOrChangeIt()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero,Vector3.right,Vector3.forward,Vector3.up };
            mesh.UploadMeshData(true);
            try { Assert.IsFalse(StreetscapeFootprintExtractor.TryExtract(mesh,Matrix4x4.identity,"unreadable",out _)); }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
    }
}
