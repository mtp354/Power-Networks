using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch=UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace FantasyEarth.App
{
    public sealed class MapControls : MonoBehaviour
    {
        private MapPresenter _map;
        private bool _touchEnabled,_single,_handle,_mousePan;
        private Vector2 _start,_previous;
        private float _pinch;
        public void Initialize(MapPresenter map) => _map=map;
        private void OnEnable() { EnhancedTouchSupport.Enable(); _touchEnabled=true; }
        private void OnDisable() { if (_touchEnabled) EnhancedTouchSupport.Disable(); _touchEnabled=false; _single=false; _pinch=0; }
        private void Update()
        {
            if (_map==null) return;
            if (Keyboard.current!=null && Keyboard.current.tabKey.wasPressedThisFrame) _map.ToggleMap();
            if (Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame) _map.Escape();
            if (_map.InputBlocked) { _single=false; _pinch=0; return; }
            var touches=Touch.activeTouches;
            if (touches.Count>=3) { _single=false; _pinch=0; return; }
            if (touches.Count==2)
            {
                _single=false;
                float distance=Vector2.Distance(touches[0].screenPosition,touches[1].screenPosition);
                if (_map.MapActive && _pinch>0 && distance>10) _map.Zoom(_pinch/distance);
                _pinch=distance; return;
            }
            if (_pinch>0) { _pinch=0; _single=false; return; }
            if (touches.Count==1)
            {
                var touch=touches[0]; Vector2 position=touch.screenPosition;
                if (touch.phase==UnityEngine.InputSystem.TouchPhase.Began)
                { _start=_previous=position; _single=true; _handle=position.y<Screen.height*.22f; }
                else if (_single && touch.phase==UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    if (_map.MapActive && !_handle && _start.y<Screen.height*.86f) _map.Pan(_previous,position);
                    _previous=position;
                }
                else if (_single && touch.phase==UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    Vector2 delta=position-_start;
                    if (_handle && Mathf.Abs(delta.y)>Mathf.Max(70,Screen.height*.045f) && Mathf.Abs(delta.y)>Mathf.Abs(delta.x)*1.5f &&
                        ((_map.MapActive && delta.y<0) || (!_map.MapActive && delta.y>0))) _map.ToggleMap();
                    _single=false;
                }
                else if (touch.phase==UnityEngine.InputSystem.TouchPhase.Canceled) _single=false;
                return;
            }
            _single=false;
            if (Mouse.current==null || !_map.MapActive) return;
            Vector2 mouse=Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.wasPressedThisFrame) { _mousePan=mouse.y>Screen.height*.22f && mouse.y<Screen.height*.86f; _previous=mouse; }
            if (_mousePan && Mouse.current.leftButton.isPressed) { _map.Pan(_previous,mouse); _previous=mouse; }
            if (Mouse.current.leftButton.wasReleasedThisFrame) _mousePan=false;
            float scroll=Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll)>.1f) _map.Zoom(Mathf.Exp(-scroll*.001f));
        }
    }
}
