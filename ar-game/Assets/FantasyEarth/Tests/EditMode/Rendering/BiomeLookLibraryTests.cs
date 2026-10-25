using System;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FantasyEarth.Tests.Rendering
{
    public class BiomeLookLibraryTests
    {
        [Test]
        public void LibraryHasLookForEveryBiome()
        {
            var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>(
                "Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
            Assert.IsNotNull(library);
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                Assert.IsTrue(library.TryGet(biome, out var look), biome.ToString());
                Assert.IsNotNull(look.Profile);
                Assert.That(look.MistStrength, Is.InRange(0f, 1f));
                Assert.IsTrue(look.Profile.TryGet<Bloom>(out var bloom));
                Assert.IsTrue(bloom.threshold.overrideState);
                Assert.Less(bloom.threshold.value, 1f);
                Assert.IsTrue(look.Profile.TryGet<ColorAdjustments>(out var color));
                Assert.IsTrue(color.colorFilter.overrideState);
                Assert.IsTrue(look.Profile.TryGet<Vignette>(out var vignette));
                Assert.IsTrue(vignette.intensity.overrideState);
            }
        }

        [Test]
        public void RendererHasPainterlyFeatureWithMaterial()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Settings/URP-Performant-Renderer.asset");
            Assert.IsNotNull(renderer);
            PainterlyRendererFeature found = null;
            foreach (var feature in renderer.rendererFeatures)
                if (feature is PainterlyRendererFeature painterly) found = painterly;
            Assert.IsNotNull(found);
            Assert.IsTrue(found.isActive);
            var material = new SerializedObject(found).FindProperty("material").objectReferenceValue as Material;
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.shader);
            Assert.AreEqual("FantasyEarth/Painterly", material.shader.name);
            Assert.IsTrue(material.shader.isSupported);
        }

        [Test]
        public void MissingBiomeReturnsFalse()
        {
            var library = ScriptableObject.CreateInstance<BiomeLookLibrary>();
            try
            {
                Assert.IsFalse(library.TryGet(Biome.Temperate, out var look));
                Assert.IsNull(look);
            }
            finally { UnityEngine.Object.DestroyImmediate(library); }
        }
    }
}
