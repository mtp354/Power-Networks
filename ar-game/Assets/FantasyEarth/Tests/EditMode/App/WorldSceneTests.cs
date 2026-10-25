using System;
using FantasyEarth.App;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using Google.XR.ARCoreExtensions;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace FantasyEarth.Tests.App
{
    public class WorldSceneTests
    {
        private const string WorldPath = "Assets/FantasyEarth/Scenes/World.unity";

        [Test]
        public void BuildListContainsOnlyWorld()
        {
            Assert.AreEqual(1, EditorBuildSettings.scenes.Length);
            Assert.IsTrue(EditorBuildSettings.scenes[0].enabled);
            Assert.AreEqual(WorldPath, EditorBuildSettings.scenes[0].path);
        }

        [Test]
        public void WorldSceneHasWiredArCameraAndApplication()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldPath));
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(WorldPath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(WorldPath, OpenSceneMode.Additive);
            try
            {
                var session = Find<ARSession>(scene);
                var origin = Find<XROrigin>(scene);
                Assert.IsNotNull(origin.Camera);
                var cameraManager = origin.Camera.GetComponent<ARCameraManager>();
                Assert.IsNotNull(cameraManager);
                Assert.IsNotNull(origin.Camera.GetComponent<ARCameraBackground>());
                var extension = Find<ARCoreExtensions>(scene);
                Assert.IsFalse(extension.enabled, "Native extensions must wait for consent, permission and a running AR session.");
                Assert.AreSame(session, extension.Session);
                Assert.AreSame(origin, extension.Origin);
                Assert.AreSame(cameraManager, extension.CameraManager);
                Assert.IsNotNull(extension.ARCoreExtensionsConfig);
                Assert.AreEqual(GeospatialMode.Enabled, extension.ARCoreExtensionsConfig.GeospatialMode);
                Assert.AreEqual(StreetscapeGeometryMode.Enabled, extension.ARCoreExtensionsConfig.StreetscapeGeometryMode);
                var volume = Find<Volume>(scene);
                Assert.IsTrue(volume.isGlobal);
                Assert.IsNotNull(volume.sharedProfile);
                var cameraData = origin.Camera.GetUniversalAdditionalCameraData();
                Assert.IsTrue(cameraData.renderPostProcessing);
                Assert.AreNotEqual(0, cameraData.volumeLayerMask.value & (1 << volume.gameObject.layer));
                var motes = Find<ParticleSystem>(scene);
                Assert.AreEqual(ParticleSystemSimulationSpace.World, motes.main.simulationSpace);
                Assert.IsTrue(motes.transform.IsChildOf(origin.Camera.transform));
                References(Find<GeospatialSampler>(scene), "earthManager", "extensions");
                References(Find<ClimateService>(scene), "koppenGrid");
                References(Find<PaintedLookController>(scene), "library", "volume", "motes");
                References(Find<DiagnosticsOverlay>(scene), "presenter", "buildings");
                References(Find<LiveBuildingController>(scene), "sampler", "presenter", "origin", "layer", "buildingMaterial");
                Assert.IsNotNull(Find<ObservedBuildingLayer>(scene));
                Assert.IsNull(FindOptional<ARStreetscapeGeometryManager>(scene), "SDK manager must be created only after native tracking is ready.");
                References(Find<WorldPresenter>(scene), "sampler", "climate", "look", "logger");
                Assert.IsNotNull(Find<SessionLogger>(scene));
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void SamplerWithoutSessionDoesNotTrackOrThrow()
        {
            var go = new GameObject("Sampler test");
            try
            {
                var sampler = go.AddComponent<GeospatialSampler>();
                // EditMode does not schedule MonoBehaviour callbacks; invoke the managed sample path directly.
                Assert.DoesNotThrow(() => typeof(GeospatialSampler).GetMethod("Update",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(sampler, null));
                Assert.IsFalse(sampler.Current.IsTracking);
                Assert.IsNull(sampler.Position);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void LiveBuildingsWithoutSessionDoNotCreateAnUnsafeNativeManager()
        {
            var go=new GameObject("Unready live buildings test");
            try
            {
                var controller=go.AddComponent<LiveBuildingController>();
                Assert.DoesNotThrow(() => typeof(LiveBuildingController).GetMethod("Update",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller,null));
                Assert.AreEqual("WaitingForSession",controller.Status);
                Assert.AreEqual(0,controller.ObservedCount);
                Assert.IsNull(go.GetComponentInChildren<ARStreetscapeGeometryManager>());
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ApplyingBiomeDrivesProfileMistAndMotes()
        {
            var go = new GameObject("Look test");
            Color oldColor = Shader.GetGlobalColor("_FE_MistColor");
            float oldStrength = Shader.GetGlobalFloat("_FE_MistStrength");
            try
            {
                var controller = go.AddComponent<PaintedLookController>();
                var volume = go.AddComponent<Volume>();
                var motes = go.AddComponent<ParticleSystem>();
                var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>(
                    "Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
                Set(controller, "library", library); Set(controller, "volume", volume); Set(controller, "motes", motes);
                controller.Apply(Biome.Tropical);
                Assert.IsTrue(library.TryGet(Biome.Tropical, out var expected));
                Assert.AreSame(expected.Profile, volume.sharedProfile);
                Assert.AreEqual(expected.MistColor, Shader.GetGlobalColor("_FE_MistColor"));
                Assert.AreEqual(expected.MistStrength, Shader.GetGlobalFloat("_FE_MistStrength"));
                Assert.AreEqual(expected.MoteColor, motes.main.startColor.color);
                Assert.AreEqual(expected.MotesPerSecond, motes.emission.rateOverTime.constant);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                Shader.SetGlobalColor("_FE_MistColor", oldColor);
                Shader.SetGlobalFloat("_FE_MistStrength", oldStrength);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            T found=FindOptional<T>(scene);
            if (found!=null) return found;
            Assert.Fail("Missing " + typeof(T).Name);
            return null;
        }

        private static T FindOptional<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }

        private static void References(Component component, params string[] fields)
        {
            var serialized = new SerializedObject(component);
            foreach (string field in fields)
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, component.GetType().Name + "." + field);
        }

        private static void Set(Component component, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
