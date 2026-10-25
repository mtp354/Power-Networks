using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FantasyEarth.App
{
    public sealed class DiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private WorldPresenter presenter;
        private bool _visible;
        private bool _gestureHeld;
        private bool _touchEnabled;
        private GUIStyle _style;
        private string _text = "";
        private float _nextRefresh;

        private void Awake() { if (!Debug.isDebugBuild) enabled = false; }

        private void OnEnable()
        {
            if (!Debug.isDebugBuild) return;
            EnhancedTouchSupport.Enable();
            _touchEnabled = true;
        }

        private void OnDisable()
        {
            if (_touchEnabled) EnhancedTouchSupport.Disable();
            _touchEnabled = false;
        }

        private void Update()
        {
            if (!Debug.isDebugBuild) return;
            bool triple = Touch.activeTouches.Count == 3;
            if ((triple && !_gestureHeld) || (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame))
                _visible = !_visible;
            _gestureHeld = triple;
            if (!_visible || presenter == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.1f;
            var entry = presenter.LastEntry;
            _text = "Fantasy Earth — diagnostics\nEarth: " + entry.EarthState +
                "\nTracking: " + entry.IsTracking + "\nHorizontal: " + Accuracy(entry.HorizontalAccuracyM) + " m" +
                "\nYaw: " + Accuracy(entry.YawAccuracyDeg) + "°\nAlignment: " + entry.Alignment +
                "\nBlend: " + entry.RebuiltBlend.ToString("F2", CultureInfo.InvariantCulture) +
                "\nKöppen: " + entry.Koppen + "\nBiome: " + entry.Biome;
        }

        private static string Accuracy(double value) => double.IsNaN(value) || double.IsInfinity(value)
            ? "n/a" : value.ToString("F2", CultureInfo.InvariantCulture);

        private void OnGUI()
        {
            if (!Debug.isDebugBuild || !_visible) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.width / 30, 18, 36) };
            float width = Mathf.Min(Screen.width - 32, 760);
            GUILayout.BeginArea(new Rect(16, 32, width, _style.fontSize * 13), GUI.skin.box);
            GUILayout.Label(_text, _style);
            GUILayout.EndArea();
        }
    }
}
