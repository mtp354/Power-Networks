using System;
using System.Collections.Generic;
using FantasyEarth.Fantasy;
using UnityEngine;

namespace FantasyEarth.Rendering
{
    public enum GroundPropView { Camera, Map }

    /// <summary>Runtime input is provided explicitly. No geographic position is serialized into the scene.</summary>
    [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class GroundPropChunkView : MonoBehaviour
    {
        private static readonly int VisibilityProperty=Shader.PropertyToID("_Visibility");
        private static readonly int LiveFacadeProperty=Shader.PropertyToID("_LiveFacade");
        private GroundProp[] _props;
        private Material _material;
        private Mesh _mesh;
        private GroundPropView _view;
        private float _blend;
        private bool _tracking;
        private MaterialPropertyBlock _properties;

        public void Configure(IReadOnlyList<GroundProp> props,Material sharedMaterial)
        {
            if (props==null || sharedMaterial==null) throw new ArgumentNullException();
            if (props.Count>GroundPropMeshBuilder.MaximumPropsPerChunk) throw new ArgumentException("Split props into bounded chunks.",nameof(props));
            var copied=new GroundProp[props.Count];
            for (int i=0;i<copied.Length;i++) copied[i]=props[i];
            Mesh next=copied.Length==0 ? null : GroundPropMeshBuilder.Create(copied);
            Release(); _props=copied; _material=sharedMaterial; _mesh=next;
            Apply();
        }

        public void SetPresentation(GroundPropView view,float rebuiltBlend,bool tracking)
        {
            if (view!=GroundPropView.Camera && view!=GroundPropView.Map) throw new ArgumentOutOfRangeException(nameof(view));
            _view=view; _blend=ObservedBuildingMesh.Finite(rebuiltBlend) ? Mathf.Clamp01(rebuiltBlend) : 0; _tracking=tracking;
            Apply();
        }

        private void Apply()
        {
            var filter=GetComponent<MeshFilter>(); var renderer=GetComponent<MeshRenderer>();
            filter.sharedMesh=_mesh; renderer.sharedMaterial=_material;
            float visibility=_view==GroundPropView.Map ? 1 : _tracking ? _blend : 0;
            if (_properties==null) _properties=new MaterialPropertyBlock();
            _properties.SetFloat(VisibilityProperty,visibility);
            _properties.SetFloat(LiveFacadeProperty,0);
            renderer.SetPropertyBlock(_properties);
            renderer.enabled=_mesh!=null && visibility>0;
        }

        private void OnEnable()
        {
            if (_mesh==null && _props!=null && _props.Length>0 && _material!=null) _mesh=GroundPropMeshBuilder.Create(_props);
            Apply();
        }

        private void Release()
        {
            var renderer=GetComponent<MeshRenderer>(); if (renderer!=null) renderer.enabled=false;
            var filter=GetComponent<MeshFilter>(); if (filter!=null && filter.sharedMesh==_mesh) filter.sharedMesh=null;
            if (_mesh!=null)
            { if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh); }
            _mesh=null;
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
