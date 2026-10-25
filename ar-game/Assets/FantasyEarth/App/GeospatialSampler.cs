using FantasyEarth.Positioning;
using FantasyEarth.World;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace FantasyEarth.App
{
    [DefaultExecutionOrder(-100)]
    public sealed class GeospatialSampler : MonoBehaviour
    {
        private const string ConsentPreference = "FantasyEarth.GeospatialConsentV1";
        [SerializeField] private AREarthManager earthManager;
        [SerializeField] private ARCoreExtensions extensions;
        private ARCoreExtensionsConfig _originalConfig;
        private ARCoreExtensionsConfig _runtimeConfig;
        private bool _consented;
        private bool _showNotice;
        private GUIStyle _noticeStyle;
        private GUIStyle _buttonStyle;

        public PositioningSample Current { get; private set; }
        public GeoPoint? Position { get; private set; }
        public string EarthStateName { get; private set; } = "SessionNotReady";
        public bool StreetscapeReady => Current.IsTracking && extensions!=null && extensions.isActiveAndEnabled &&
            _runtimeConfig!=null && _runtimeConfig.StreetscapeGeometryMode==StreetscapeGeometryMode.Enabled;

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _consented = PlayerPrefs.GetInt(ConsentPreference, 0) == 1;
            _showNotice = !_consented;
            // Extensions 1.57 starts native telemetry from OnEnable after only 60 frames.
            // A first-run camera prompt can outlast that wait, leaving no native session.
            // The scene also serializes this disabled; keep initialization safe in other scenes.
            if (extensions != null) extensions.enabled = false;
            if (extensions != null && extensions.ARCoreExtensionsConfig != null)
            {
                _originalConfig = extensions.ARCoreExtensionsConfig;
                _runtimeConfig = Instantiate(_originalConfig);
                _runtimeConfig.GeospatialMode = GeospatialMode.Disabled;
                _runtimeConfig.StreetscapeGeometryMode = StreetscapeGeometryMode.Disabled;
                extensions.ARCoreExtensionsConfig = _runtimeConfig;
            }
#endif
        }

        private void Start()
        {
            if (_consented) RequestLocationPermission();
        }

        private void Update()
        {
            Position = null;
            Current = new PositioningSample(false, double.NaN, double.NaN);
            if (Application.isEditor)
            {
                EarthStateName = "EditorSimulation";
                return;
            }
#if UNITY_ANDROID
            if (!_consented || !Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                if (_runtimeConfig != null)
                {
                    _runtimeConfig.GeospatialMode = GeospatialMode.Disabled;
                    _runtimeConfig.StreetscapeGeometryMode = StreetscapeGeometryMode.Disabled;
                }
                EarthStateName = _consented ? "LocationPermissionNotGranted" : "GeospatialNotEnabledByUser";
                return;
            }
#endif
            if (earthManager == null || extensions == null || !extensions.gameObject.activeInHierarchy ||
                ARSession.state != ARSessionState.SessionTracking)
            {
                EarthStateName = "SessionNotReady";
                return;
            }
            // Camera AR works independently while consent/permissions/session readiness are pending.
            // Enable only once the native session exists, then let Extensions apply the runtime config.
            if (!extensions.enabled) extensions.enabled = true;
            FeatureSupported support = earthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            if (support != FeatureSupported.Supported)
            {
                EarthStateName = support == FeatureSupported.Unsupported ? "GeospatialUnsupported" : "SessionNotReady";
                return;
            }
            if (_runtimeConfig != null)
            {
                _runtimeConfig.GeospatialMode = GeospatialMode.Enabled;
                _runtimeConfig.StreetscapeGeometryMode = StreetscapeGeometryMode.Enabled;
            }
            EarthState earthState = earthManager.EarthState;
            EarthStateName = earthState.ToString();
            bool tracking = earthState == EarthState.Enabled && earthManager.EarthTrackingState == TrackingState.Tracking;
            if (!tracking) return;
            GeospatialPose pose = earthManager.CameraGeospatialPose;
            if (!Finite(pose.Latitude) || !Finite(pose.Longitude) || pose.Latitude < -90 || pose.Latitude > 90)
            {
                EarthStateName = "InvalidGeospatialPose";
                return;
            }
            Position = new GeoPoint(pose.Latitude, pose.Longitude);
            Current = PositioningSample.FromEarth(true, true, pose.HorizontalAccuracy, pose.OrientationYawAccuracy);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private void RequestLocationPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermissions(new[] { Permission.CoarseLocation, Permission.FineLocation });
#endif
        }

        private void OnGUI()
        {
            if (!_showNotice) return;
            if (_noticeStyle == null)
            {
                int size = Mathf.Clamp(Screen.width / 28, 18, 38);
                _noticeStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = size };
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = size, padding = new RectOffset(16, 16, 14, 14) };
            }
            float width = Mathf.Min(Screen.width * 0.9f, 900);
            float height = Mathf.Min(Screen.height * 0.9f, _noticeStyle.fontSize * 17);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height), GUI.skin.box);
            GUILayout.Label("Fantasy Earth", _noticeStyle);
            GUILayout.Label("AR features use Google Play Services for AR. Google's privacy policy applies.", _noticeStyle);
            GUILayout.Label("To power this session, Google will process sensor data (e.g., camera and location).", _noticeStyle);
            if (GUILayout.Button("Google privacy policy", _buttonStyle)) Application.OpenURL("https://policies.google.com/privacy");
            if (GUILayout.Button("Learn about device data", _buttonStyle)) Application.OpenURL("https://support.google.com/ar?p=how-google-play-services-for-ar-handles-your-data");
            if (GUILayout.Button("Use location", _buttonStyle))
            {
                _consented = true; _showNotice = false;
                PlayerPrefs.SetInt(ConsentPreference, 1); PlayerPrefs.Save();
                RequestLocationPermission();
            }
            if (GUILayout.Button("Camera only", _buttonStyle)) _showNotice = false;
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (_runtimeConfig == null) return;
            if (extensions != null) extensions.ARCoreExtensionsConfig = _originalConfig;
            Destroy(_runtimeConfig);
        }
    }
}
