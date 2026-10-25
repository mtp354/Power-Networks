using System.Collections.Generic;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FantasyEarth.App
{
    /// <summary>Starts the pinned SDK manager only with a native tracking session; owns no SDK mesh.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class LiveBuildingController : MonoBehaviour
    {
        [SerializeField] private GeospatialSampler sampler;
        [SerializeField] private WorldPresenter presenter;
        [SerializeField] private XROrigin origin;
        [SerializeField] private ObservedBuildingLayer layer;
        [SerializeField] private Material buildingMaterial;
        private sealed class Observation
        {
            public ARStreetscapeGeometry Geometry;
            public string TemporaryId;
            public Mesh Borrowed;
            public float NextAttempt;
        }
        private const int MaximumObservations=128;
        private readonly Dictionary<TrackableId,Observation> _observations = new Dictionary<TrackableId,Observation>();
        private ARStreetscapeGeometryManager _source;
        private Biome _biome;
        private bool _paused;
        private float _nextDiscovery;
        private Transform _sessionSpace;
        private float _nextDiagnostic;
        private string _lastDiagnostic;
        public string Status { get; private set; } = "WaitingForSession";
        public int ObservedCount => _observations.Count;
        public int PreparedCount => layer==null ? 0 : layer.ActiveCount;
        public int RenderedCount => layer==null ? 0 : layer.VisibleCount;
        public int OwnedVertexCount => layer==null ? 0 : layer.OwnedVertexCount;
        public int SkippedAttempts { get; private set; }

        private void OnEnable() => ARSession.stateChanged+=OnSessionStateChanged;

        private void Update()
        {
            if (_paused || sampler==null || !sampler.StreetscapeReady || origin==null || origin.Camera==null ||
                presenter==null || layer==null || buildingMaterial==null)
            {
                StopSource(); Status="WaitingForSession"; LogDiagnostic(); return;
            }
            // ARCore Extensions poses are in session space. TrackablesParent includes origin movement/offset.
            if (_sessionSpace!=origin.TrackablesParent && _source!=null) StopSource();
            if (_source!=null) return;
            _sessionSpace=origin.TrackablesParent;
            if (_sessionSpace==null) { Status="WaitingForOrigin"; return; }
            layer.Configure(_sessionSpace,buildingMaterial);
            _biome=presenter.LastEntry.Biome;
            var sourceObject=new GameObject("Live Streetscape source");
            sourceObject.transform.SetParent(transform,false);
            _source=sourceObject.AddComponent<ARStreetscapeGeometryManager>();
            _source.enabled=false;
            _source.StreetscapeGeometriesChanged+=OnChanged;
            _source.enabled=true;
            Status="WaitingForBuildings";
        }

        private void LateUpdate()
        {
            if (_source==null || _paused || !sampler.StreetscapeReady) return;
            var entry=presenter.LastEntry;
            if (_biome!=entry.Biome) { layer.Clear(); _biome=entry.Biome; }
            layer.SetPresentation((float)entry.RebuiltBlend,entry.IsTracking);
            Vector3 cameraSession=_sessionSpace.InverseTransformPoint(origin.Camera.transform.position);
            bool attempted=false;
            foreach (var observation in _observations.Values)
            {
                var geometry=observation.Geometry;
                if (geometry.trackingState!=TrackingState.Tracking)
                { layer.Remove(observation.TemporaryId); continue; }
                bool exists=layer.Contains(observation.TemporaryId);
                if (!exists && (attempted || Time.unscaledTime<observation.NextAttempt)) continue;
                if (!exists) attempted=true; // At most one new mesh read/copy per frame, even for rejected candidates.
                if (observation.Borrowed==null) observation.Borrowed=geometry.mesh;
                if (!layer.TryUpsert(observation.TemporaryId,observation.Borrowed,geometry.pose,_biome,cameraSession))
                {
                    layer.Remove(observation.TemporaryId);
                    observation.NextAttempt=Time.unscaledTime+3;
                    SkippedAttempts++;
                }
            }
            Status=layer.ActiveCount==0 ? (_observations.Count==0 ? "WaitingForBuildings" : "WaitingForNearbyBuildings") :
                entry.RebuiltBlend>0 ? "LiveBuildings" : "WaitingForAlignment";
            LogDiagnostic();
        }

        private void OnChanged(ARStreetscapeGeometriesChangedEventArgs changes)
        {
            // SDK event lists are reused each Update. Consume them immediately; do not retain the lists.
            foreach (var geometry in changes.Removed)
                if (_observations.TryGetValue(geometry.trackableId,out var removed))
                { layer.Remove(removed.TemporaryId); _observations.Remove(geometry.trackableId); }
            Discover(changes.Added);
            if (Time.unscaledTime>=_nextDiscovery)
            {
                Discover(changes.Updated);
                _nextDiscovery=Time.unscaledTime+.5f;
            }
        }

        private void Discover(List<ARStreetscapeGeometry> geometries)
        {
            foreach (var geometry in geometries)
            {
                if (_observations.Count>=MaximumObservations) break;
                TrackableId id=geometry.trackableId;
                if (_observations.ContainsKey(id) || geometry.streetscapeGeometryType!=StreetscapeGeometryType.Building) continue;
                _observations.Add(id,new Observation { Geometry=geometry,TemporaryId="streetscape-session:"+id });
            }
        }

        private void OnSessionStateChanged(ARSessionStateChangedEventArgs state)
        { if (state.state!=ARSessionState.SessionTracking) StopSource(); }

        private void StopSource()
        {
            if (_source!=null)
            {
                _source.enabled=false;
                _source.StreetscapeGeometriesChanged-=OnChanged;
                Destroy(_source.gameObject);
                _source=null;
            }
            // A new manager on resume also resets its private native-handle/mesh cache.
            _observations.Clear();
            if (layer!=null && layer.ActiveCount>0) layer.Clear();
            _sessionSpace=null;
        }

        private void OnApplicationPause(bool paused)
        { _paused=paused; if (paused) { StopSource(); Status="Paused"; } }

        private void LogDiagnostic()
        {
            if (!Debug.isDebugBuild || Time.unscaledTime<_nextDiagnostic) return;
            _nextDiagnostic=Time.unscaledTime+2;
            string line="[FantasyEarth.Buildings] status="+Status+" observed="+ObservedCount+" prepared="+PreparedCount+" drawn="+RenderedCount+
                " vertices="+OwnedVertexCount+" skipped="+SkippedAttempts;
            if (line==_lastDiagnostic) return;
            _lastDiagnostic=line;
            Debug.Log(line); // Counts only. Never log poses, mesh vertices, IDs or geographic coordinates.
        }
        private void OnDisable()
        { ARSession.stateChanged-=OnSessionStateChanged; StopSource(); }
    }
}
