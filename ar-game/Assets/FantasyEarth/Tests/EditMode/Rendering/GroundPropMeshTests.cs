using System;
using System.Linq;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using NUnit.Framework;
using UnityEngine;

namespace FantasyEarth.Tests.Rendering
{
    public class GroundPropMeshTests
    {
        [Test]
        public void BiomeChunksContainFiniteRepeatableBoundedMeshesForAllKinds()
        {
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                var props=GroundPropPlanner.Generate(new LocalGeoFrame(new GeoPoint(0,0)),48,_=>biome,_=>(double?)0,Array.Empty<BuildingFootprint>());
                Assert.AreEqual(3,props.Select(p=>p.Kind).Distinct().Count());
                Mesh a=GroundPropMeshBuilder.Create(props),b=GroundPropMeshBuilder.Create(props);
                try
                {
                    Assert.That(a.vertexCount,Is.InRange(100,100000));
                    CollectionAssert.AreEqual(a.vertices,b.vertices); CollectionAssert.AreEqual(a.triangles,b.triangles);
                    CollectionAssert.AreEqual(a.colors,b.colors);
                    Assert.AreEqual(a.vertexCount,a.normals.Length);
                    foreach (var v in a.vertices)
                    {
                        Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
                        Assert.IsFalse(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
                        Assert.That(v.y,Is.InRange(-.01f,9f));
                        Assert.Less(Mathf.Sqrt(v.x*v.x+v.z*v.z),52);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
            }
        }

        [Test]
        public void CameraAndMapShareOneMeshWithDifferentVisibilityRulesAndCleanOwnership()
        {
            var props=GroundPropPlanner.Generate(new LocalGeoFrame(new GeoPoint(0,0)),32,_=>Biome.Temperate,_=>(double?)0,Array.Empty<BuildingFootprint>());
            var host=new GameObject("Ground prop chunk test");
            var material=new Material(Shader.Find("FantasyEarth/Buildings"));
            try
            {
                var view=host.AddComponent<GroundPropChunkView>();
                view.Configure(props,material);
                Mesh initial=host.GetComponent<MeshFilter>().sharedMesh;
                var renderer=host.GetComponent<MeshRenderer>();
                Assert.IsFalse(renderer.enabled);
                view.SetPresentation(GroundPropView.Camera,1,false);
                Assert.IsFalse(renderer.enabled);
                view.SetPresentation(GroundPropView.Map,0,false);
                Assert.IsTrue(renderer.enabled);
                view.SetPresentation(GroundPropView.Camera,.5f,true);
                var properties=new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
                Assert.AreEqual(.5f,properties.GetFloat("_Visibility"));
                Assert.AreSame(initial,host.GetComponent<MeshFilter>().sharedMesh);
                host.SetActive(false);
                Assert.IsTrue(initial==null);
                host.SetActive(true);
                Assert.IsNotNull(host.GetComponent<MeshFilter>().sharedMesh);
                Assert.IsTrue(host.GetComponent<MeshRenderer>().enabled);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
