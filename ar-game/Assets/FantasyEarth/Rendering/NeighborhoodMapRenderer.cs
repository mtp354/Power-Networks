using System;
using System.Collections;
using System.Collections.Generic;
using FantasyEarth.Fantasy;
using FantasyEarth.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.Rendering
{
    /// <summary>Owns an offline schematic map. Its plane heights are never AR terrain altitudes.</summary>
    public sealed class NeighborhoodMapRenderer : MonoBehaviour
    {
        public const int MapLayer=29;
        private readonly List<Mesh> _meshes=new List<Mesh>();
        private readonly List<GameObject> _objects=new List<GameObject>();
        private readonly HashSet<string> _propIds=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _propChunks=new HashSet<string>(StringComparer.Ordinal);
        private Material _material;
        private MapRegion _region;
        public int BuildingCount { get; private set; }
        public int VertexCount { get; private set; }
        public int PropCount => _propIds.Count;
        public bool Ready { get; private set; }

        public IEnumerator Build(MapRegion region,Material material)
        {
            Clear(); _region=region??throw new ArgumentNullException(nameof(region)); _material=material??throw new ArgumentNullException(nameof(material));
            var b=region.Bounds;
            var ground=new MapSurface("map-plane",0,MapSurfaceKind.Park,new[] { new PlanarPoint(b.West,b.South),new PlanarPoint(b.East,b.South),
                new PlanarPoint(b.East,b.North),new PlanarPoint(b.West,b.South),new PlanarPoint(b.East,b.North),new PlanarPoint(b.West,b.North) },0,"[]");
            AddMesh(SurfaceMesh(new[] { ground },new Color(.48f,.46f,.34f,0),0),"Map ground");
            foreach (MapSurfaceKind kind in Enum.GetValues(typeof(MapSurfaceKind)))
            {
                var surfaces=new List<MapSurface>(); foreach (var surface in region.Surfaces) if (surface.Kind==kind) surfaces.Add(surface);
                Color color=kind==MapSurfaceKind.Road ? new Color(.69f,.57f,.38f,0) : kind==MapSurfaceKind.Park ? new Color(.28f,.47f,.24f,0) : new Color(.14f,.40f,.49f,0);
                AddMesh(SurfaceMesh(surfaces,color,kind==MapSurfaceKind.Road ? .06f : kind==MapSurfaceKind.Park ? .03f : .015f),"Map "+kind);
                yield return null;
            }
            var batch=new List<Mesh>();
            try
            {
                foreach (var building in region.Buildings.Buildings)
                {
                    batch.Add(BuildingMeshBuilder.Create(building.Footprint,BuildingRecipe.Create(building.Footprint,Biome.Temperate),false));
                    BuildingCount++;
                    if (batch.Count<32) continue;
                    AddMesh(Combine(batch),"Map building chunk"); ReleaseBatch(batch); yield return null;
                }
                if (batch.Count>0) { AddMesh(Combine(batch),"Map building chunk"); ReleaseBatch(batch); }
            }
            finally { ReleaseBatch(batch); }
            Ready=true;
        }

        public bool AddPropsNear(PlanarPoint center)
        {
            if (!Ready || _propChunks.Count>=32 || PropCount>=512) return false;
            int east=(int)Math.Floor(center.EastM/128),north=(int)Math.Floor(center.NorthM/128);
            string key=east+"/"+north;
            if (!_propChunks.Add(key)) return false;
            var origin=_region.Buildings.Frame.Unproject(new LocalPoint((east+.5)*128,0,(north+.5)*128));
            var local=new LocalGeoFrame(origin.Point);
            var obstacles=new List<BuildingFootprint>();
            // Transform only nearby canonical rings into this candidate query's frame.
            foreach (var record in _region.Buildings.Buildings)
            {
                var footprint=record.Footprint;
                if (footprint.MinEastM+footprint.WidthM<east*128-100 || footprint.MinEastM>(east+1)*128+100 ||
                    footprint.MinNorthM+footprint.DepthM<north*128-100 || footprint.MinNorthM>(north+1)*128+100) continue;
                var outline=new PlanarPoint[footprint.Outline.Count];
                for (int i=0;i<outline.Length;i++)
                {
                    var p=footprint.Outline[i];
                    outline[i]=local.Project(_region.Buildings.Frame.Unproject(new LocalPoint(p.EastM,0,p.NorthM)).Point).Planar;
                }
                obstacles.Add(new BuildingFootprint(footprint.StableId,outline,footprint.HeightM));
            }
            var planned=GroundPropPlanner.Generate(local,96,_=>Biome.Temperate,p=>
            {
                var geo=local.Unproject(new LocalPoint(p.EastM,0,p.NorthM));
                return _region.EligibleMapGround(_region.Buildings.Frame.Project(geo.Point).Planar);
            },obstacles);
            var accepted=new List<GroundProp>();
            foreach (var prop in planned)
                if (PropCount<512 && _propIds.Add(prop.Id)) accepted.Add(prop.Reproject(_region.Buildings.Frame,.08));
            if (accepted.Count>0) AddMesh(GroundPropMeshBuilder.Create(accepted),"Map seeded props");
            return true;
        }

        private void AddMesh(Mesh mesh,string name)
        {
            mesh.hideFlags=HideFlags.DontSave; _meshes.Add(mesh); VertexCount+=mesh.vertexCount;
            var go=new GameObject(name); _objects.Add(go); go.layer=MapLayer; go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=_material; renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        public static Mesh SurfaceMesh(IReadOnlyList<MapSurface> surfaces,Color color,float height)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>(); var colors=new List<Color>(); var normals=new List<Vector3>();
            foreach (var surface in surfaces)
                for (int i=0;i<surface.Triangles.Count;i+=3)
                {
                    int start=vertices.Count;
                    for (int p=0;p<3;p++)
                    { var point=surface.Triangles[i+p]; vertices.Add(new Vector3((float)point.EastM,height,(float)point.NorthM)); colors.Add(color); normals.Add(Vector3.up); }
                    triangles.Add(start); triangles.Add(start+2); triangles.Add(start+1);
                }
            var mesh=new Mesh { name="Map surface",indexFormat=vertices.Count>65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetNormals(normals); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh Combine(List<Mesh> sources)
        {
            var instances=new CombineInstance[sources.Count];
            for (int i=0;i<instances.Length;i++) instances[i]=new CombineInstance { mesh=sources[i],transform=Matrix4x4.identity };
            var mesh=new Mesh { name="Map building chunk",indexFormat=IndexFormat.UInt32 };
            mesh.CombineMeshes(instances,true,true); return mesh;
        }
        private static void ReleaseBatch(List<Mesh> batch) { foreach (var mesh in batch) DestroyOwned(mesh); batch.Clear(); }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        public void Clear()
        {
            foreach (var go in _objects) if (go!=null) { go.SetActive(false); DestroyOwned(go); }
            foreach (var mesh in _meshes) if (mesh!=null) DestroyOwned(mesh);
            _objects.Clear(); _meshes.Clear(); _propIds.Clear(); _propChunks.Clear(); BuildingCount=0; VertexCount=0; Ready=false;
        }
        private void OnDestroy() => Clear();
    }
}
