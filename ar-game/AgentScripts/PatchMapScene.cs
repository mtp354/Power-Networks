using System;
using FantasyEarth.App;
using FantasyEarth.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PatchMapScene
{
    public static string Run()
    {
        const string path="Assets/FantasyEarth/Scenes/World.unity";
        Scene previous=SceneManager.GetActiveScene(),scene=SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if (opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save World before adding map wiring.");
            var sampler=Find<GeospatialSampler>(scene); var presenter=Find<WorldPresenter>(scene);
            var camera=Find<Unity.XR.CoreUtils.XROrigin>(scene).Camera;
            var buildingMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
            var pack=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/FantasyEarth/Data/Maps/BrooklynWaterfront.bytes");
            var library=AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (pack==null || buildingMaterial==null || library==null || font==null) throw new InvalidOperationException("Missing map inputs.");
            const string folder="Assets/FantasyEarth/Rendering/Maps";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/FantasyEarth/Rendering","Maps");
            var marker=AssetDatabase.LoadAssetAtPath<Material>(folder+"/MapMarker.mat");
            if (marker==null)
            {
                Shader shader=Shader.Find("FantasyEarth/MapMarker");
                if (shader==null) throw new InvalidOperationException("Missing map marker shader.");
                marker=new Material(shader); AssetDatabase.CreateAsset(marker,folder+"/MapMarker.mat");
            }
            var map=presenter.GetComponent<MapPresenter>() ?? presenter.gameObject.AddComponent<MapPresenter>();
            Set(map,"arCamera",camera); Set(map,"mapPack",pack); Set(map,"sampler",sampler);
            Set(map,"buildings",Find<LiveBuildingController>(scene)); Set(map,"buildingMaterial",buildingMaterial);
            Set(map,"markerMaterial",marker); Set(map,"lookLibrary",library); Set(map,"uiFont",font);
            camera.cullingMask&=~(1<<NeighborhoodMapRenderer.MapLayer);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,path); AssetDatabase.SaveAssets();
            return "Patched World with offline Brooklyn map presenter, shared assets and runtime uGUI controls. Existing AR objects/configuration and sole-World build list preserved.";
        }
        finally
        { if (opened) EditorSceneManager.CloseScene(scene,true); if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); }
    }
    private static T Find<T>(Scene scene) where T:Component
    { foreach (var root in scene.GetRootGameObjects()) { T value=root.GetComponentInChildren<T>(true); if (value!=null) return value; } throw new InvalidOperationException("Missing "+typeof(T).Name); }
    private static void Set(Component component,string field,UnityEngine.Object value)
    { var data=new SerializedObject(component); data.FindProperty(field).objectReferenceValue=value; data.ApplyModifiedPropertiesWithoutUndo(); }
}
