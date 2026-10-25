using System;
using System.IO;
using FantasyEarth.App;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class BuildWorldScene
{
    public static string Build()
    {
        Scene current = SceneManager.GetActiveScene();
        if (current.isDirty) throw new InvalidOperationException("Save the current scene before regenerating World.");
        Directory.CreateDirectory("Assets/FantasyEarth/Scenes");
        Directory.CreateDirectory("Assets/FantasyEarth/Rendering/Particles");
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var session = new GameObject("AR Session").AddComponent<ARSession>();
        session.gameObject.AddComponent<ARInputManager>();
        var origin = new GameObject("XR Origin (Mobile AR)").AddComponent<XROrigin>();
        origin.Origin = origin.gameObject;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = 0;
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(origin.transform, false);
        origin.CameraFloorOffsetObject = offset;
        var camera = new GameObject("AR Camera").AddComponent<Camera>();
        camera.transform.SetParent(offset.transform, false);
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100;
        camera.allowHDR = true;
        origin.Camera = camera;
        var cameraManager = camera.gameObject.AddComponent<ARCameraManager>();
        camera.gameObject.AddComponent<ARCameraBackground>();
        var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask = 1;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        var driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
        var position = new InputAction("AR Position", InputActionType.Value, expectedControlType: "Vector3");
        position.AddBinding("<HandheldARInputDevice>/devicePosition");
        position.AddBinding("<XRHMD>/centerEyePosition");
        var rotation = new InputAction("AR Rotation", InputActionType.Value, expectedControlType: "Quaternion");
        rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
        rotation.AddBinding("<XRHMD>/centerEyeRotation");
        var tracking = new InputAction("AR Tracking", InputActionType.Value, expectedControlType: "Integer");
        tracking.AddBinding("<HandheldARInputDevice>/trackingState");
        tracking.AddBinding("<XRHMD>/trackingState");
        driver.positionInput = new InputActionProperty(position);
        driver.rotationInput = new InputActionProperty(rotation);
        driver.trackingStateInput = new InputActionProperty(tracking);
        var earth = origin.gameObject.AddComponent<AREarthManager>();
        var extensions = new GameObject("ARCore Extensions").AddComponent<ARCoreExtensions>();
        extensions.enabled = false;
        extensions.Session = session;
        extensions.Origin = origin;
        extensions.CameraManager = cameraManager;
        extensions.ARCoreExtensionsConfig = AssetDatabase.LoadAssetAtPath<ARCoreExtensionsConfig>(
            "Assets/FantasyEarth/App/ARCoreExtensionsConfig.asset");

        var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>(
            "Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        if (!library.TryGet(Biome.Temperate, out var temperate)) throw new InvalidOperationException("Temperate look missing");
        var volume = new GameObject("Biome Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = temperate.Profile;
        var motes = new GameObject("Motes").AddComponent<ParticleSystem>();
        motes.transform.SetParent(camera.transform, false);
        motes.transform.localPosition = new Vector3(0, 0, 2.5f);
        motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = motes.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7, 11);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.04f);
        main.startColor = temperate.MoteColor;
        main.maxParticles = 220;
        var shape = motes.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7, 4, 4);
        var emission = motes.emission;
        emission.rateOverTime = temperate.MotesPerSecond;
        var noise = motes.noise;
        noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.25f;
        noise.scrollSpeed = 0.035f; noise.quality = ParticleSystemNoiseQuality.Low;
        var overLife = motes.colorOverLifetime;
        overLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0.7f, 0.75f), new GradientAlphaKey(0, 1) });
        overLife.color = gradient;
        var particleRenderer = motes.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = CreateMoteMaterial();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        var app = new GameObject("App");
        var sampler = app.AddComponent<GeospatialSampler>();
        var climate = app.AddComponent<ClimateService>();
        var look = app.AddComponent<PaintedLookController>();
        var logger = app.AddComponent<SessionLogger>();
        var overlay = app.AddComponent<DiagnosticsOverlay>();
        var presenter = app.AddComponent<WorldPresenter>();
        Set(sampler, "earthManager", earth); Set(sampler, "extensions", extensions);
        Set(climate, "koppenGrid", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/FantasyEarth/Data/koppen_1991_2020_0p1.bytes"));
        Set(look, "library", library); Set(look, "volume", volume); Set(look, "motes", motes);
        Set(presenter, "sampler", sampler); Set(presenter, "climate", climate); Set(presenter, "look", look); Set(presenter, "logger", logger);
        Set(overlay, "presenter", presenter);
        const string path = "Assets/FantasyEarth/Scenes/World.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets();
        return "World scene created with AR camera, Geospatial, four-biome look controller, motes and development diagnostics. Only World is enabled in Build Settings.";
    }

    private static Material CreateMoteMaterial()
    {
        const string root = "Assets/FantasyEarth/Rendering/Particles/";
        if (!File.Exists(root + "Mote.png"))
        {
            var image = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                image.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 2)));
            }
            image.Apply();
            File.WriteAllBytes(root + "Mote.png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(root + "Mote.png");
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(root + "Mote.png");
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(root + "Mote.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Mote" };
            AssetDatabase.CreateAsset(material, root + "Mote.mat");
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Mote.png"));
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 2);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Set(Component component, string field, UnityEngine.Object value)
    {
        if (value == null) throw new InvalidOperationException("Cannot assign null to " + component.GetType().Name + "." + field);
        var serialized = new SerializedObject(component);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
