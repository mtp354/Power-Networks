using System;
using System.Collections;
using FantasyEarth.Data;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FantasyEarth.App
{
    public sealed class MapPresenter : MonoBehaviour
    {
        [SerializeField] private TextAsset mapPack;
        [SerializeField] private Camera arCamera;
        [SerializeField] private GeospatialSampler sampler;
        [SerializeField] private LiveBuildingController buildings;
        [SerializeField] private Material buildingMaterial;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private BiomeLookLibrary lookLibrary;
        [SerializeField] private TMP_FontAsset uiFont;
        private GameObject _root,_avatar,_accuracyDisc,_heading;
        private NeighborhoodMapRenderer _renderer;
        private Camera _camera;
        private MapHud _hud;
        private MapRegion _region;
        private MapViewport _viewport;
        private bool _loading,_failed;
        private bool _initialized;
        private int _arMask;
        private float _nextProps;
        private int _propCursor,_propEast=int.MinValue,_propNorth=int.MinValue;
        private static readonly Vector2Int[] PropNeighbors={new Vector2Int(0,0),new Vector2Int(-1,-1),new Vector2Int(0,-1),
            new Vector2Int(1,-1),new Vector2Int(-1,0),new Vector2Int(1,0),new Vector2Int(-1,1),new Vector2Int(0,1),new Vector2Int(1,1)};
        private Mesh _arrowMesh;
        public bool MapActive { get; private set; }
        public bool Ready => _renderer!=null && _renderer.Ready;
        public Camera MapCamera => _camera;
        public NeighborhoodMapRenderer Renderer => _renderer;
        public bool HasPlayer => _viewport!=null && _viewport.HasPlayer;
        public bool InputBlocked => _hud!=null && _hud.CreditsVisible;
        public string Status { get; private set; }="Brooklyn waterfront preview";

        private void Start()
        {
            if (arCamera==null || mapPack==null || buildingMaterial==null || markerMaterial==null || lookLibrary==null || uiFont==null) return;
            _arMask=arCamera.cullingMask;
            _initialized=true;
            arCamera.cullingMask&=~(1<<NeighborhoodMapRenderer.MapLayer);
            _hud=gameObject.AddComponent<MapHud>(); _hud.Initialize(this,uiFont);
            gameObject.AddComponent<MapControls>().Initialize(this);
        }

        public void ToggleMap()
        {
            if (_hud==null) return;
            if (_root==null) CreateRig();
            MapActive=!MapActive;
            arCamera.enabled=!MapActive; _camera.enabled=MapActive;
            if (buildings!=null) buildings.enabled=!MapActive;
            _hud.SetMapActive(MapActive);
            if (MapActive && !_loading && _region==null && !_failed) StartCoroutine(Load());
            if (Debug.isDebugBuild) Debug.Log("[FantasyEarth.Map] mode="+(MapActive ? "Map" : "Camera"));
        }

        private IEnumerator Load()
        {
            _loading=true; Status="Loading Brooklyn waterfront..."; yield return null;
            try
            {
                _region=MapPackReader.Read(mapPack.text);
                _viewport=new MapViewport(_region.Bounds,_region.Buildings.Frame.Project(_region.DefaultCenter).Planar);
            }
            catch (Exception error)
            { _failed=true; _loading=false; Status="Map unavailable"; Debug.LogError("[FantasyEarth.Map] Map data could not be loaded: "+error.GetType().Name); yield break; }
            var build=_renderer.Build(_region,buildingMaterial);
            while (build.MoveNext()) yield return build.Current;
            _loading=false;
            if (Debug.isDebugBuild) Debug.Log("[FantasyEarth.Map] ready buildings="+_renderer.BuildingCount+" surfaces="+_region.Surfaces.Count+" vertices="+_renderer.VertexCount);
        }

        private void LateUpdate()
        {
            if (!MapActive || _hud==null) return;
            if (Ready)
            {
                GeoPoint? position=sampler!=null && sampler.Current.IsTracking ? sampler.Position : null;
                bool covered=position.HasValue && _region.Covers(position.Value);
                _viewport.UpdatePlayer(covered ? _region.Buildings.Frame.Project(position.Value).Planar : (PlanarPoint?)null,
                    covered ? sampler.Current.HorizontalAccuracyM : double.NaN);
                Status=_viewport.HasPlayer ? (_viewport.AccuracyM>25 ? "Approximate location" : _viewport.Following ? "Following your location" : "Exploring Brooklyn waterfront") :
                    position.HasValue ? "NYC preview - outside pilot coverage" : "NYC preview - location unavailable";
                _camera.orthographicSize=(float)_viewport.ZoomM;
                _camera.transform.localRotation=Quaternion.Euler(55,0,0);
                _camera.transform.localPosition=new Vector3((float)_viewport.Center.EastM,0,(float)_viewport.Center.NorthM)-
                    (_camera.transform.localRotation*Vector3.forward)*2200;
                UpdateMarker();
                if (Time.unscaledTime>=_nextProps)
                {
                    _nextProps=Time.unscaledTime+.35f;
                    int east=(int)Math.Floor(_viewport.Center.EastM/128),north=(int)Math.Floor(_viewport.Center.NorthM/128);
                    if (east!=_propEast || north!=_propNorth) { _propCursor=0; _propEast=east; _propNorth=north; }
                    Vector2Int offset=PropNeighbors[_propCursor]; _propCursor=(_propCursor+1)%PropNeighbors.Length;
                    _renderer.AddPropsNear(new PlanarPoint(_viewport.Center.EastM+offset.x*128,_viewport.Center.NorthM+offset.y*128));
                }
            }
            _hud.Refresh(Status,Ready,HasPlayer);
        }

        public void Zoom(float factor) { if (Ready && factor>0 && !float.IsNaN(factor) && !float.IsInfinity(factor)) _viewport.Zoom(factor); }
        public void Recenter() { if (Ready) _viewport.Recenter(); }
        public void Escape() { if (InputBlocked) _hud.CloseCredits(); else if (MapActive) ToggleMap(); }
        public void Pan(Vector2 previous,Vector2 current)
        {
            if (!Ready) return;
            var plane=new Plane(_root.transform.up,_root.transform.position);
            Ray a=_camera.ScreenPointToRay(previous),b=_camera.ScreenPointToRay(current);
            if (plane.Raycast(a,out float da) && plane.Raycast(b,out float db))
            {
                Vector3 delta=_root.transform.InverseTransformVector(a.GetPoint(da)-b.GetPoint(db));
                _viewport.Pan(delta.x,delta.z);
            }
        }

        private void CreateRig()
        {
            _root=new GameObject("Offline NYC map"); _root.transform.SetParent(transform,false);
            _renderer=_root.AddComponent<NeighborhoodMapRenderer>();
            var cameraObject=new GameObject("Neighborhood map camera"); cameraObject.transform.SetParent(_root.transform,false);
            _camera=cameraObject.AddComponent<Camera>(); _camera.enabled=false;
            _camera.cullingMask=1<<NeighborhoodMapRenderer.MapLayer; _camera.clearFlags=CameraClearFlags.SolidColor;
            _camera.backgroundColor=new Color(.17f,.29f,.34f); _camera.orthographic=true; _camera.orthographicSize=220;
            _camera.nearClipPlane=.1f; _camera.farClipPlane=6000; _camera.useOcclusionCulling=false;
            var data=_camera.GetUniversalAdditionalCameraData(); data.renderPostProcessing=true;
            data.volumeLayerMask=1<<NeighborhoodMapRenderer.MapLayer; data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            var sun=new GameObject("Map sunlight").AddComponent<Light>(); sun.transform.SetParent(_root.transform,false);
            sun.type=LightType.Directional; sun.intensity=1.3f; sun.cullingMask=1<<NeighborhoodMapRenderer.MapLayer;
            sun.transform.localRotation=Quaternion.Euler(45,-35,0);
            var volume=new GameObject("Map biome look").AddComponent<Volume>(); volume.transform.SetParent(_root.transform,false);
            volume.gameObject.layer=NeighborhoodMapRenderer.MapLayer; volume.isGlobal=true;
            if (lookLibrary.TryGet(Biome.Temperate,out var look)) volume.sharedProfile=look.Profile;
            CreateMarker();
        }

        private void CreateMarker()
        {
            _avatar=new GameObject("Player traveler marker"); _avatar.transform.SetParent(_root.transform,false);
            Part(PrimitiveType.Capsule,"Traveler cloak",_avatar.transform,new Vector3(0,.8f,0),new Vector3(.7f,.65f,.7f),new Color(.12f,.54f,.50f));
            Part(PrimitiveType.Sphere,"Traveler head",_avatar.transform,new Vector3(0,1.6f,.05f),Vector3.one*.48f,new Color(.92f,.74f,.47f));
            _heading=new GameObject("Player facing arrow"); _heading.layer=NeighborhoodMapRenderer.MapLayer; _heading.transform.SetParent(_avatar.transform,false);
            _arrowMesh=new Mesh { name="Player facing marker" };
            _arrowMesh.vertices=new[] { new Vector3(-.5f,.15f,.8f),new Vector3(.5f,.15f,.8f),new Vector3(0,.15f,2) };
            _arrowMesh.triangles=new[] { 0,2,1 }; _arrowMesh.RecalculateNormals();
            _heading.AddComponent<MeshFilter>().sharedMesh=_arrowMesh;
            ColorRenderer(_heading.AddComponent<MeshRenderer>(),new Color(1,.77f,.25f));
            _accuracyDisc=Part(PrimitiveType.Cylinder,"Reported location accuracy",_root.transform,Vector3.zero,Vector3.one,new Color(.2f,.8f,.75f,.14f));
            _avatar.SetActive(false); _accuracyDisc.SetActive(false);
        }
        private GameObject Part(PrimitiveType type,string name,Transform parent,Vector3 position,Vector3 scale,Color color)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.layer=NeighborhoodMapRenderer.MapLayer;
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            Destroy(go.GetComponent<Collider>()); ColorRenderer(go.GetComponent<MeshRenderer>(),color); return go;
        }
        private void ColorRenderer(MeshRenderer renderer,Color color)
        {
            renderer.sharedMaterial=markerMaterial; renderer.shadowCastingMode=ShadowCastingMode.Off;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_BaseColor",color); renderer.SetPropertyBlock(properties);
        }
        private void UpdateMarker()
        {
            bool visible=_viewport.HasPlayer; _avatar.SetActive(visible); _accuracyDisc.SetActive(visible);
            if (!visible) return;
            Vector3 position=new Vector3((float)_viewport.Player.EastM,.12f,(float)_viewport.Player.NorthM);
            _avatar.transform.localPosition=position;
            _avatar.transform.localScale=Vector3.one*Mathf.Max(2,(float)_viewport.ZoomM*.014f);
            _accuracyDisc.transform.localPosition=position;
            float radius=Mathf.Clamp((float)_viewport.AccuracyM,1,100);
            _accuracyDisc.transform.localScale=new Vector3(radius*2,.01f,radius*2);
            bool heading=false;
            if (sampler.EunOrientation.HasValue && sampler.Current.YawAccuracyDeg<=45)
            {
                Vector3 forward=sampler.EunOrientation.Value*Vector3.forward;
                if (new Vector2(forward.x,forward.z).sqrMagnitude<.05f) forward=sampler.EunOrientation.Value*Vector3.up;
                if (new Vector2(forward.x,forward.z).sqrMagnitude>.05f)
                { _avatar.transform.localRotation=Quaternion.Euler(0,Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,0); heading=true; }
            }
            _heading.SetActive(heading);
        }
        private void OnDestroy()
        {
            if (_initialized && arCamera!=null) { arCamera.cullingMask=_arMask; arCamera.enabled=true; }
            if (_root!=null) Destroy(_root);
            if (_arrowMesh!=null) Destroy(_arrowMesh);
        }
    }
}
