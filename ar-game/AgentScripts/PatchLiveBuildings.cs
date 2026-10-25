using System;
using FantasyEarth.App;
using FantasyEarth.Rendering;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PatchLiveBuildings
{
    public static string Run()
    {
        const string path="Assets/FantasyEarth/Scenes/World.unity";
        Scene previous=SceneManager.GetActiveScene();
        Scene scene=SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if (opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("Save World before patching its live building wiring.");
            var sampler=Find<GeospatialSampler>(scene);
            var presenter=Find<WorldPresenter>(scene);
            var origin=Find<XROrigin>(scene);
            var extension=Find<ARCoreExtensions>(scene);
            var overlay=Find<DiagnosticsOverlay>(scene);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
            if (material==null) throw new InvalidOperationException("Missing shared building material.");
            var host=presenter.gameObject;
            var layer=host.GetComponent<ObservedBuildingLayer>() ?? host.AddComponent<ObservedBuildingLayer>();
            var controller=host.GetComponent<LiveBuildingController>() ?? host.AddComponent<LiveBuildingController>();
            Set(controller,"sampler",sampler); Set(controller,"presenter",presenter);
            Set(controller,"origin",origin); Set(controller,"layer",layer); Set(controller,"buildingMaterial",material);
            Set(overlay,"buildings",controller);
            extension.ARCoreExtensionsConfig.StreetscapeGeometryMode=StreetscapeGeometryMode.Enabled;
            EditorUtility.SetDirty(extension.ARCoreExtensionsConfig);
            origin.Camera.farClipPlane=250;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene,path);
            AssetDatabase.SaveAssets();
            return "Patched existing World references with a live building controller/layer and shared material. Native manager is runtime-only; build list and existing object IDs preserved.";
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene,true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static T Find<T>(Scene scene) where T:Component
    {
        foreach (var root in scene.GetRootGameObjects())
        { T result=root.GetComponentInChildren<T>(true); if (result!=null) return result; }
        throw new InvalidOperationException("Missing "+typeof(T).Name);
    }
    private static void Set(Component component,string field,UnityEngine.Object value)
    {
        var serialized=new SerializedObject(component);
        serialized.FindProperty(field).objectReferenceValue=value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
