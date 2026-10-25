using System;
using System.Collections.Generic;
using System.IO;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewPaintedLook
{
    public static string Render()
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        var allocated = new List<UnityEngine.Object>();
        var beforeMist = Shader.GetGlobalColor("_FE_MistColor");
        float beforeStrength = Shader.GetGlobalFloat("_FE_MistStrength");
        RenderTexture beforeTarget = RenderTexture.active;
        try
        {
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 2.1f, -10);
            camera.transform.LookAt(new Vector3(0, 1.5f, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.29f, 0.48f, 0.65f);
            camera.cullingMask = 1 << 31;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.volumeLayerMask = 1 << 31;
            var target = new RenderTexture(960, 640, 24, RenderTextureFormat.ARGB32);
            target.Create();
            allocated.Add(target);

            var lamp = new GameObject("Preview Sun").AddComponent<Light>();
            lamp.type = LightType.Directional; lamp.intensity = 1.6f;
            lamp.transform.rotation = Quaternion.Euler(45, -30, 0);
            lamp.cullingMask = 1 << 31;
            for (int i = 0; i < 9; i++)
            {
                var block = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                block.layer = 31;
                block.transform.position = new Vector3((i - 4) * 1.3f, 0.6f + (i % 3) * 0.35f, i % 2);
                block.transform.localScale = new Vector3(1, 1.3f + i % 3, 1);
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = new Color(0.3f + i * 0.05f, 0.34f + i * 0.025f, 0.25f);
                block.GetComponent<Renderer>().sharedMaterial = material;
                allocated.Add(material);
            }
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.layer = 31;
            var groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.3f, 0.4f, 0.22f) };
            allocated.Add(groundMaterial);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            var volume = new GameObject("Preview Volume").AddComponent<Volume>();
            volume.gameObject.layer = 31; volume.isGlobal = true;
            var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>(
                "Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
            Directory.CreateDirectory("Temp/painterly-previews");
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                if (!library.TryGet(biome, out var look)) throw new InvalidOperationException("Missing biome look");
                volume.sharedProfile = look.Profile;
                Shader.SetGlobalColor("_FE_MistColor", look.MistColor);
                Shader.SetGlobalFloat("_FE_MistStrength", look.MistStrength);
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes("Temp/painterly-previews/" + biome + ".png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            return Path.GetFullPath("Temp/painterly-previews");
        }
        finally
        {
            RenderTexture.active = beforeTarget;
            Shader.SetGlobalColor("_FE_MistColor", beforeMist);
            Shader.SetGlobalFloat("_FE_MistStrength", beforeStrength);
            EditorSceneManager.CloseScene(preview, true);
            SceneManager.SetActiveScene(previous);
            foreach (var resource in allocated) UnityEngine.Object.DestroyImmediate(resource);
        }
    }
}
