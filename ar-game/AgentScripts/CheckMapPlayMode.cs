using System;
using System.Linq;
using FantasyEarth.App;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CheckMapPlayMode
{
    public static string Prepare()
    {
        var scene=SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the active scene before the map runtime check.");
        if (scene.path!="Assets/FantasyEarth/Scenes/World.unity") EditorSceneManager.OpenScene("Assets/FantasyEarth/Scenes/World.unity");
        return "World ready for Play Mode.";
    }
    public static string OpenMap()
    {
        var map=UnityEngine.Object.FindFirstObjectByType<MapPresenter>();
        if (map==null) throw new InvalidOperationException("Map presenter not running.");
        if (!map.MapActive) map.ToggleMap();
        return "Requested map view.";
    }
    public static object Inspect()
    {
        var map=UnityEngine.Object.FindFirstObjectByType<MapPresenter>();
        return new { active=map.MapActive,ready=map.Ready,hasPlayer=map.HasPlayer,status=map.Status,
            buildings=map.Renderer==null ? 0 : map.Renderer.BuildingCount,
            props=map.Renderer==null ? 0 : map.Renderer.PropCount,
            eventSystems=UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,
            buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Select(b=>b.name).ToArray(),
            text=UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Select(t=>t.text).ToArray() };
    }
    public static string Exercise()
    {
        var map=UnityEngine.Object.FindFirstObjectByType<MapPresenter>();
        if (!map.Ready || !map.MapActive) throw new InvalidOperationException("Map not ready.");
        map.Zoom(.8f); map.Pan(new Vector2(Screen.width*.5f,Screen.height*.5f),new Vector2(Screen.width*.55f,Screen.height*.53f)); map.Recenter();
        var credits=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Credits button");
        credits.onClick.Invoke();
        if (!map.InputBlocked) throw new InvalidOperationException("Credits panel did not open.");
        map.Escape();
        if (map.InputBlocked) throw new InvalidOperationException("Credits did not close.");
        map.ToggleMap();
        if (map.MapActive || !Camera.main.enabled) throw new InvalidOperationException("Camera rendering did not resume.");
        map.ToggleMap();
        if (!map.MapActive || !map.Ready) throw new InvalidOperationException("Map cache did not survive switching.");
        return "Exercised zoom, pan, recenter, credits open/close and map-camera-map switching without rebuilding.";
    }
    public static string Capture()
    {
        var gameType=typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        EditorWindow.GetWindow(gameType).Show();
        ScreenCapture.CaptureScreenshot("Temp/nyc-map-previews/MapUi.png");
        return "Queued Game view capture.";
    }
}
