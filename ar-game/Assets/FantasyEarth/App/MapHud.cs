using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FantasyEarth.App
{
    /// <summary>Small uGUI view handle and required data credits; no position readout.</summary>
    public sealed class MapHud : MonoBehaviour
    {
        private MapPresenter _map;
        private TMP_FontAsset _font;
        private GameObject _canvasRoot,_mapUi,_credits,_ownedEvents;
        private TextMeshProUGUI _status,_toggleText,_followText;
        private RectTransform _safe;
        private Rect _lastSafe;
        private readonly Color _ink=new Color(.96f,.91f,.76f);
        private readonly Color _panel=new Color(.10f,.16f,.17f,.94f);
        public bool CreditsVisible => _credits!=null && _credits.activeSelf;

        public void Initialize(MapPresenter map,TMP_FontAsset font)
        {
            _map=map; _font=font;
            _canvasRoot=new GameObject("Map controls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            _canvasRoot.transform.SetParent(transform,false);
            var canvas=_canvasRoot.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
            var scaler=_canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=.5f;
            var safe=new GameObject("Safe area",typeof(RectTransform)); safe.transform.SetParent(_canvasRoot.transform,false);
            _safe=(RectTransform)safe.transform; ApplySafeArea();
            if (FindFirstObjectByType<EventSystem>()==null)
            {
                _ownedEvents=new GameObject("Map input events",typeof(EventSystem),typeof(InputSystemUIInputModule));
                _ownedEvents.transform.SetParent(transform,false);
                _ownedEvents.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var toggle=Button(_safe,"View handle","Map",new Vector2(.5f,0),new Vector2(0,120),new Vector2(260,90),_map.ToggleMap);
            _toggleText=toggle.GetComponentInChildren<TextMeshProUGUI>();
            _mapUi=new GameObject("Map labels and attribution",typeof(RectTransform)); _mapUi.transform.SetParent(_safe,false); Stretch((RectTransform)_mapUi.transform);
            RectTransform header=Panel(_mapUi.transform,"Map title bar",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-80),new Vector2(0,160));
            Text(header,"Brooklyn waterfront",42,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,23),new Vector2(-320,58),TextAlignmentOptions.Center);
            _status=Text(header,"NYC preview",25,new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,-32),new Vector2(-280,50),TextAlignmentOptions.Center);
            Button(_mapUi.transform,"Credits button","Credits",new Vector2(1,1),new Vector2(-115,-68),new Vector2(170,72),()=>_credits.SetActive(true));
            var follow=Button(_mapUi.transform,"Follow or preview","Preview",new Vector2(1,0),new Vector2(-145,245),new Vector2(220,80),_map.Recenter);
            _followText=follow.GetComponentInChildren<TextMeshProUGUI>();
            var footer=Panel(_mapUi.transform,"Visible map attribution",new Vector2(0,0),new Vector2(1,0),new Vector2(0,32),new Vector2(0,64));
            Text(footer,"© OpenStreetMap contributors, Overture Maps Foundation",20,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-24,0),TextAlignmentOptions.Center);
            BuildCredits(); SetMapActive(false);
        }

        public void SetMapActive(bool active)
        { _mapUi.SetActive(active); _toggleText.text=active ? "Camera" : "Map"; if (_credits!=null) _credits.SetActive(false); }
        public void Refresh(string status,bool ready,bool hasPlayer)
        { _status.text=status; _followText.text=hasPlayer ? "Follow me" : "Preview"; }
        public void CloseCredits() { if (_credits!=null) _credits.SetActive(false); }

        private void BuildCredits()
        {
            var box=Panel(_safe,"Map and climate credits",new Vector2(.05f,.08f),new Vector2(.95f,.92f),Vector2.zero,Vector2.zero);
            _credits=box.gameObject;
            Text(box,"Map and climate credits",38,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-55),new Vector2(-100,70),TextAlignmentOptions.Center);
            Button(box,"Close credits","Close",new Vector2(1,1),new Vector2(-85,-55),new Vector2(135,65),()=>_credits.SetActive(false));
            var viewport=new GameObject("Credits viewport",typeof(RectTransform),typeof(RectMask2D)); viewport.transform.SetParent(box,false);
            var vr=(RectTransform)viewport.transform; vr.anchorMin=new Vector2(0,0); vr.anchorMax=new Vector2(1,1); vr.offsetMin=new Vector2(35,190); vr.offsetMax=new Vector2(-35,-115);
            var content=new GameObject("Credits text",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(ContentSizeFitter)); content.transform.SetParent(viewport.transform,false);
            var cr=(RectTransform)content.transform; cr.anchorMin=new Vector2(0,1); cr.anchorMax=Vector2.one; cr.pivot=new Vector2(.5f,1); cr.sizeDelta=Vector2.zero;
            var text=content.GetComponent<TextMeshProUGUI>(); text.font=_font; text.fontSize=27; text.color=_ink; text.textWrappingMode=TextWrappingModes.Normal;
            text.raycastTarget=false;
            text.text="BROOKLYN WATERFRONT PILOT\n\nMap data © OpenStreetMap contributors, Overture Maps Foundation.\n\n"+
                "Overture release: 2026-10-21.0\nBuilding footprints: OpenStreetMap and Microsoft ML Buildings. Height contributions: USGS Lidar. Transportation includes data from TomTom. Parks and water: OpenStreetMap.\n\n"+
                "The bundled derived map database is available under the Open Database License (ODbL) 1.0. Source metadata and reproducible conversion tools accompany the project.\n\n"+
                "Road widths can be class estimates. Missing building heights are marked as estimates in the data. This is a bounded pilot map with schematic terrain heights; it is not navigation guidance.\n\n"+
                "CLIMATE\nBeck, McVicar, Vergopolan et al. (2023), High-resolution Köppen-Geiger maps for 1901-2099 based on constrained CMIP6 projections. Scientific Data 10, 724.\n\n"+
                "1991-2020 climate classification, transformed to the bundled 0.1-degree grid. Creative Commons Attribution 4.0.\n\n"+
                "Fantasy buildings, props and avatar are generated by Fantasy Earth. Preview mode shows Brooklyn without inventing a player location.";
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.AddComponent<ScrollRect>(); scroll.viewport=vr; scroll.content=cr; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            // A transparent graphic receives drags without obscuring the panel.
            viewport.AddComponent<Image>().color=Color.clear;
            Button(box,"ODbL link","Map license",new Vector2(.25f,0),new Vector2(0,135),new Vector2(285,65),()=>Application.OpenURL("https://opendatacommons.org/licenses/odbl/1-0/"));
            Button(box,"Source attribution link","Map sources",new Vector2(.75f,0),new Vector2(0,135),new Vector2(285,65),()=>Application.OpenURL("https://docs.overturemaps.org/attribution/"));
            Button(box,"Climate license link","Climate license",new Vector2(.5f,0),new Vector2(0,55),new Vector2(360,65),()=>Application.OpenURL("https://creativecommons.org/licenses/by/4.0/"));
        }

        private RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            go.GetComponent<Image>().color=_panel;
            var rect=(RectTransform)go.transform; rect.anchorMin=min; rect.anchorMax=max; rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        private TextMeshProUGUI Text(Transform parent,string value,float size,Vector2 min,Vector2 max,Vector2 position,Vector2 dimensions,TextAlignmentOptions alignment)
        {
            var go=new GameObject(value.Length>30 ? "Text" : value,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform; rect.anchorMin=min; rect.anchorMax=max; rect.anchoredPosition=position; rect.sizeDelta=dimensions;
            var label=go.GetComponent<TextMeshProUGUI>(); label.font=_font; label.fontSize=size; label.color=_ink; label.text=value;
            label.alignment=alignment; label.textWrappingMode=TextWrappingModes.Normal; label.raycastTarget=false; return label;
        }
        private GameObject Button(Transform parent,string name,string label,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(parent,name,anchor,anchor,position,size); var button=rect.gameObject.AddComponent<Button>();
            button.targetGraphic=rect.GetComponent<Image>(); button.onClick.AddListener(action);
            Text(rect,label,30,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,TextAlignmentOptions.Center); return rect.gameObject;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
        private void Update() { if (_safe!=null && _lastSafe!=Screen.safeArea) ApplySafeArea(); }
        private void ApplySafeArea()
        {
            _lastSafe=Screen.safeArea;
            _safe.anchorMin=new Vector2(_lastSafe.xMin/Screen.width,_lastSafe.yMin/Screen.height);
            _safe.anchorMax=new Vector2(_lastSafe.xMax/Screen.width,_lastSafe.yMax/Screen.height);
            _safe.offsetMin=Vector2.zero; _safe.offsetMax=Vector2.zero;
        }
        private void OnDestroy() { if (_canvasRoot!=null) Destroy(_canvasRoot); if (_ownedEvents!=null) Destroy(_ownedEvents); }
    }
}
