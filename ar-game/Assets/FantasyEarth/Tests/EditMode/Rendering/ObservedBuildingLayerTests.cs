using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Rendering
{
    public class ObservedBuildingLayerTests
    {
        private GameObject _root, _host;
        private Material _material;
        private Mesh _borrowed;
        private ObservedBuildingLayer _layer;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Synthetic session space");
            _host = new GameObject("Observed layer test");
            _material = new Material(Shader.Find("FantasyEarth/Buildings"));
            var footprint = new BuildingFootprint("fixture",new[] { new PlanarPoint(0,0),new PlanarPoint(6,0),
                new PlanarPoint(6,2),new PlanarPoint(2,2),new PlanarPoint(2,6),new PlanarPoint(0,6) },8);
            _borrowed = BuildingMeshBuilder.Create(footprint,BuildingRecipe.Create(footprint,Biome.Temperate));
            _layer = _host.AddComponent<ObservedBuildingLayer>();
            _layer.Configure(_root.transform,_material,2,100000,100);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host); Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_borrowed); Object.DestroyImmediate(_material);
        }

        [Test]
        public void RenderCopyPreservesAllSourceTrianglesAndNeverMutatesBorrowedMesh()
        {
            Vector3[] vertices = _borrowed.vertices, normals = _borrowed.normals;
            int[] triangles = _borrowed.triangles;
            Color[] colors = _borrowed.colors;
            Assert.IsTrue(_layer.TryUpsert("session:first",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero));
            var copy = _root.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            Assert.AreNotSame(_borrowed,copy);
            CollectionAssert.AreEqual(vertices,copy.vertices);
            CollectionAssert.AreEqual(triangles,copy.triangles);
            CollectionAssert.AreEqual(vertices,_borrowed.vertices);
            CollectionAssert.AreEqual(normals,_borrowed.normals);
            CollectionAssert.AreEqual(colors,_borrowed.colors);
            _layer.Clear();
            Assert.IsTrue(copy==null);
            Assert.IsTrue(_borrowed!=null);
            Assert.AreEqual(0,_layer.OwnedVertexCount);
        }

        [Test]
        public void SessionPoseIsAppliedUnderTranslatedRotatedOriginAndPoseUpdatesReuseMesh()
        {
            _root.transform.SetPositionAndRotation(new Vector3(50,5,-30),Quaternion.Euler(0,90,0));
            var pose = new Pose(new Vector3(3,1,4),Quaternion.Euler(0,25,0));
            Assert.IsTrue(_layer.TryUpsert("session:first",_borrowed,pose,Biome.Temperate,Vector3.zero));
            var filter = _root.GetComponentInChildren<MeshFilter>(true);
            Mesh before = filter.sharedMesh;
            Vector3 v = before.vertices[0];
            Vector3 expected = _root.transform.TransformPoint(pose.position+pose.rotation*v);
            Assert.Less(Vector3.Distance(expected,filter.transform.TransformPoint(v)),1e-4);
            pose.position += Vector3.right*2;
            Assert.IsTrue(_layer.TryUpsert("session:first",_borrowed,pose,Biome.Temperate,Vector3.zero));
            Assert.AreSame(before,filter.sharedMesh);
            Assert.AreEqual(pose.position,filter.transform.localPosition);
        }

        [Test]
        public void TrackingLossImmediatelyHidesTheLayerAndBlendUsesSharedMaterialProperties()
        {
            _layer.TryUpsert("session:first",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero);
            var renderer = _root.GetComponentInChildren<MeshRenderer>(true);
            Assert.IsFalse(renderer.enabled);
            Assert.AreEqual(0,_layer.VisibleCount);
            _layer.SetPresentation(.5f,true);
            Assert.IsTrue(renderer.enabled);
            Assert.AreEqual(1,_layer.VisibleCount);
            Assert.AreSame(_material,renderer.sharedMaterial);
            var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            Assert.AreEqual(.5f,block.GetFloat("_Visibility"));
            _layer.SetPresentation(1,false);
            Assert.IsFalse(renderer.enabled);
            Assert.AreEqual(0,_layer.VisibleCount);
            _host.SetActive(false);
            Assert.AreEqual(0,_layer.ActiveCount);
            Assert.IsTrue(_borrowed!=null);
        }

        [Test]
        public void RemovedTracksAndBoundsDoNotLeaveUnboundedMeshes()
        {
            Assert.IsTrue(_layer.TryUpsert("first",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero));
            Assert.IsTrue(_layer.TryUpsert("second",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero));
            Assert.IsFalse(_layer.TryUpsert("third",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero));
            Assert.IsFalse(_layer.TryUpsert("far",_borrowed,new Pose(Vector3.right*300,Quaternion.identity),Biome.Temperate,Vector3.zero));
            _layer.Remove("first");
            Assert.AreEqual(1,_layer.ActiveCount);
            _layer.Clear();
            _layer.Configure(_root.transform,_material,2,3,100);
            Assert.IsFalse(_layer.TryUpsert("over-budget",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero));
            Assert.AreEqual(0,_layer.ActiveCount);
        }

        [Test]
        public void RecoloringForBiomeReleasesOldCopyAndRejectsInvalidPose()
        {
            _layer.TryUpsert("first",_borrowed,Pose.identity,Biome.Temperate,Vector3.zero);
            Mesh old = _root.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            Assert.IsTrue(_layer.TryUpsert("first",_borrowed,Pose.identity,Biome.Snowy,Vector3.zero));
            Assert.IsTrue(old==null);
            Assert.AreEqual(1,_layer.ActiveCount);
            Assert.IsFalse(_layer.TryUpsert("invalid",_borrowed,new Pose(new Vector3(float.NaN,0,0),Quaternion.identity),Biome.Snowy,Vector3.zero));
        }
    }
}
