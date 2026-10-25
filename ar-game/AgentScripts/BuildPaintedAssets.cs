using System;
using System.IO;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BuildPaintedAssets
{
    private const string Root = "Assets/FantasyEarth/Rendering";

    public static string Build()
    {
        Directory.CreateDirectory(Root + "/BiomeLooks");
        AssetDatabase.Refresh();
        var shader = Shader.Find("FantasyEarth/Painterly");
        if (shader == null) throw new InvalidOperationException("Painterly shader was not imported");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Painterly.mat");
        if (material == null)
        {
            material = new Material(shader) { name = "Painterly" };
            AssetDatabase.CreateAsset(material, Root + "/Painterly.mat");
        }
        material.shader = shader;
        material.SetFloat("_Radius", 4);
        EditorUtility.SetDirty(material);

        var library = LoadOrCreate<BiomeLookLibrary>(Root + "/BiomeLooks/BiomeLookLibrary.asset");
        var biomes = (Biome[])Enum.GetValues(typeof(Biome));
        library.Entries = new BiomeLookLibrary.Entry[biomes.Length];
        foreach (Biome biome in biomes)
        {
            Color tint, mist, mote;
            float strength, rate, saturation, contrast;
            switch (biome)
            {
                case Biome.Desert:
                    tint = new Color(1f, 0.82f, 0.62f); mist = new Color(1f, 0.67f, 0.32f);
                    mote = new Color(1f, 0.64f, 0.28f); strength = 0.045f; rate = 4; saturation = 8; contrast = 14;
                    break;
                case Biome.Snowy:
                    tint = new Color(0.78f, 0.88f, 1f); mist = new Color(0.76f, 0.87f, 1f);
                    mote = new Color(0.71f, 0.88f, 1f); strength = 0.18f; rate = 12; saturation = -8; contrast = 5;
                    break;
                case Biome.Tropical:
                    tint = new Color(0.78f, 1f, 0.91f); mist = new Color(0.33f, 0.82f, 0.72f);
                    mote = new Color(0.48f, 1f, 0.72f); strength = 0.12f; rate = 16; saturation = 22; contrast = 12;
                    break;
                default:
                    tint = new Color(1f, 0.94f, 0.82f); mist = new Color(1f, 0.81f, 0.49f);
                    mote = new Color(1f, 0.79f, 0.32f); strength = 0.08f; rate = 9; saturation = 7; contrast = 9;
                    break;
            }
            var profile = LoadOrCreate<VolumeProfile>(Root + "/BiomeLooks/" + biome + "_Volume.asset");
            var color = Override<ColorAdjustments>(profile);
            color.postExposure.Override(0.12f);
            color.colorFilter.Override(tint);
            color.saturation.Override(saturation);
            color.contrast.Override(contrast);
            var bloom = Override<Bloom>(profile);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(false);
            var vignette = Override<Vignette>(profile);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.55f);
            Override<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);
            foreach (var component in profile.components) EditorUtility.SetDirty(component);

            var look = LoadOrCreate<BiomeLook>(Root + "/BiomeLooks/" + biome + ".asset");
            look.Profile = profile; look.MistColor = mist; look.MistStrength = strength;
            look.MoteColor = mote; look.MotesPerSecond = rate;
            EditorUtility.SetDirty(look);
            library.Entries[(int)biome] = new BiomeLookLibrary.Entry { Biome = biome, Look = look };
        }
        EditorUtility.SetDirty(library);

        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-Performant-Renderer.asset");
        PainterlyRendererFeature feature = null;
        foreach (var candidate in renderer.rendererFeatures)
            if (candidate is PainterlyRendererFeature found) feature = found;
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<PainterlyRendererFeature>();
            feature.name = "PainterlyRendererFeature";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);
        }
        feature.SetActive(true);
        var settings = new SerializedObject(feature);
        settings.FindProperty("material").objectReferenceValue = material;
        settings.FindProperty("downsample").intValue = 2;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        var rendererSettings = new SerializedObject(renderer);
        var map = rendererSettings.FindProperty("m_RendererFeatureMap");
        map.arraySize = renderer.rendererFeatures.Count;
        for (int i = 0; i < renderer.rendererFeatures.Count; i++)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out string guid, out long id);
            map.GetArrayElementAtIndex(i).longValue = id;
        }
        rendererSettings.ApplyModifiedPropertiesWithoutUndo();
        renderer.SetDirty();
        EditorUtility.SetDirty(renderer);
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-Performant.asset");
        pipeline.supportsHDR = true;
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
        return "Created four biome looks/profiles, painterly material and renderer feature; HDR enabled.";
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component))
        {
            component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);
        }
        return component;
    }
}
