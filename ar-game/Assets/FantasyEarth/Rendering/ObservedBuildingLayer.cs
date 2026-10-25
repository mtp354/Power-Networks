using System;
using System.Collections.Generic;
using FantasyEarth.Fantasy;
using UnityEngine;
using UnityEngine.Rendering;

namespace FantasyEarth.Rendering
{
    /// <summary>Owns bounded render copies. The caller owns SDK observations and session readiness.</summary>
    [ExecuteAlways]
    public sealed class ObservedBuildingLayer : MonoBehaviour
    {
        private sealed class View
        {
            public Mesh Borrowed, Owned;
            public MeshRenderer Renderer;
            public Transform Transform;
            public Biome Biome;
            public MaterialPropertyBlock Properties;
        }
        private static readonly int VisibilityProperty = Shader.PropertyToID("_Visibility");
        private static readonly int LiveFacadeProperty = Shader.PropertyToID("_LiveFacade");
        private static readonly int StyleProperty = Shader.PropertyToID("_FacadeStyle");
        private static readonly int BaseYProperty = Shader.PropertyToID("_FacadeBaseY");
        private static readonly int TrimProperty = Shader.PropertyToID("_FacadeTrim");
        private readonly Dictionary<string,View> _views = new Dictionary<string,View>(StringComparer.Ordinal);
        private Transform _sessionSpace;
        private Material _material;
        private int _maximumBuildings, _maximumVertices;
        private float _maximumDistance, _visibility;
        public int ActiveCount => _views.Count;
        public int VisibleCount => _visibility>0 ? _views.Count : 0;
        public int OwnedVertexCount { get; private set; }
        public bool Contains(string temporaryId) => _views.ContainsKey(temporaryId);

        public void Configure(Transform sessionSpace, Material material, int maximumBuildings=24,
            int maximumVertices=250000, float maximumDistance=150)
        {
            if (sessionSpace==null || material==null) throw new ArgumentException("Session space and shared material are required.");
            if (maximumBuildings<1 || maximumBuildings>64 || maximumVertices<3 || maximumVertices>1000000 ||
                !ObservedBuildingMesh.Finite(maximumDistance) || maximumDistance<=0 || maximumDistance>500)
                throw new ArgumentOutOfRangeException(nameof(maximumBuildings));
            Clear(); _sessionSpace=sessionSpace; _material=material;
            _maximumBuildings=maximumBuildings; _maximumVertices=maximumVertices; _maximumDistance=maximumDistance;
            _visibility=0;
        }

        public bool TryUpsert(string temporaryId, Mesh borrowed, Pose sessionPose, Biome biome, Vector3 cameraInSession)
        {
            if (_sessionSpace==null || _material==null || string.IsNullOrWhiteSpace(temporaryId) || borrowed==null ||
                !ValidPose(sessionPose) || !Finite(cameraInSession) || !borrowed.isReadable ||
                borrowed.vertexCount>ObservedBuildingMesh.MaximumSourceVertices) return false;
            Vector3 cameraLocal = Quaternion.Inverse(sessionPose.rotation)*(cameraInSession-sessionPose.position);
            if (borrowed.bounds.SqrDistance(cameraLocal)>_maximumDistance*_maximumDistance) return false;
            _views.TryGetValue(temporaryId,out var previous);
            if (previous!=null && previous.Borrowed==borrowed && previous.Biome==biome)
            {
                previous.Transform.SetLocalPositionAndRotation(sessionPose.position,sessionPose.rotation);
                return true;
            }
            int oldVertices = previous==null ? 0 : previous.Owned.vertexCount;
            if ((previous==null && _views.Count>=_maximumBuildings) ||
                OwnedVertexCount-oldVertices+borrowed.vertexCount>_maximumVertices) return false;
            Mesh owned;
            BuildingRecipe recipe;
            try { owned=ObservedBuildingMesh.Create(borrowed,temporaryId,biome,out recipe); }
            catch (ArgumentException) { return false; }
            Remove(temporaryId);
            var go = new GameObject("Observed fantasy building");
            go.transform.SetParent(_sessionSpace,false);
            go.transform.SetLocalPositionAndRotation(sessionPose.position,sessionPose.rotation);
            go.AddComponent<MeshFilter>().sharedMesh=owned;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=_material;
            // Camera background cannot receive virtual shadows. Avoid a second render of every live mesh.
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            var properties = new MaterialPropertyBlock();
            properties.SetFloat(LiveFacadeProperty,1);
            properties.SetFloat(StyleProperty,(float)recipe.Style);
            properties.SetFloat(BaseYProperty,owned.bounds.min.y);
            properties.SetColor(TrimProperty,BuildingMeshBuilder.Palette.For(biome).Trim);
            var view = new View { Borrowed=borrowed,Owned=owned,Renderer=renderer,Transform=go.transform,
                Biome=biome,Properties=properties };
            _views.Add(temporaryId,view); OwnedVertexCount+=owned.vertexCount;
            Present(view);
            return true;
        }

        public void SetPresentation(float blend, bool tracking)
        {
            _visibility=tracking && ObservedBuildingMesh.Finite(blend) ? Mathf.Clamp01(blend) : 0;
            foreach (var view in _views.Values) Present(view);
        }

        private void Present(View view)
        {
            if (view.Renderer==null) return;
            view.Properties.SetFloat(VisibilityProperty,_visibility);
            view.Renderer.SetPropertyBlock(view.Properties);
            view.Renderer.enabled=_visibility>0;
        }

        public void Remove(string temporaryId)
        {
            if (!_views.TryGetValue(temporaryId,out var view)) return;
            _views.Remove(temporaryId); OwnedVertexCount-=view.Owned.vertexCount;
            Release(view);
        }

        public void Clear()
        {
            foreach (var view in _views.Values) Release(view);
            _views.Clear(); OwnedVertexCount=0;
        }

        private static void Release(View view)
        {
            if (view.Renderer!=null) { view.Renderer.enabled=false; DestroyOwned(view.Renderer.gameObject); }
            DestroyOwned(view.Owned);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private static bool Finite(Vector3 value) => ObservedBuildingMesh.Finite(value.x) &&
            ObservedBuildingMesh.Finite(value.y) && ObservedBuildingMesh.Finite(value.z);
        private static bool ValidPose(Pose pose) => Finite(pose.position) && ObservedBuildingMesh.Finite(pose.rotation.x) &&
            ObservedBuildingMesh.Finite(pose.rotation.y) && ObservedBuildingMesh.Finite(pose.rotation.z) &&
            ObservedBuildingMesh.Finite(pose.rotation.w) && Mathf.Abs(Quaternion.Dot(pose.rotation,pose.rotation)-1)<.01f;
        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();
    }
}
