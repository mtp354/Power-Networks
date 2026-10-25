using System;
using System.Collections.Generic;
using System.IO;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildBuildingPreview
{
    public static string Build()
    {
        const string scenePath = "Assets/FantasyEarth/Scenes/BuildingPreview.unity";
        if (SceneManager.GetSceneByPath(scenePath).isLoaded) throw new InvalidOperationException("Close BuildingPreview before regenerating it.");
        Directory.CreateDirectory("Assets/FantasyEarth/Rendering/Buildings");
        Directory.CreateDirectory("Temp/building-previews");
        AssetDatabase.Refresh();
        Material building = MaterialAsset("StorybookBuilding", Shader.Find("FantasyEarth/Buildings"), Color.white);
        Material path = MaterialAsset("PreviewPath", Shader.Find("Universal Render Pipeline/Lit"), new Color(.66f, .56f, .39f));
        var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        Scene previous = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        RenderTexture beforeTarget = RenderTexture.active;
        Color beforeMist = Shader.GetGlobalColor("_FE_MistColor");
        float beforeStrength = Shader.GetGlobalFloat("_FE_MistStrength");
        var target = new RenderTexture(1360, 960, 24, RenderTextureFormat.ARGB32);
        target.Create();
        try
        {
            var sun = new GameObject("Preview Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(42, -35, 0); sun.cullingMask = 1 << 30;
            var camera = new GameObject("Building Preview Camera").AddComponent<Camera>();
            camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.32f, .45f, .54f); camera.orthographic = true;
            camera.nearClipPlane = .1f; camera.farClipPlane = 300; camera.allowHDR = true;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true; cameraData.volumeLayerMask = 1 << 30;
            var volume = new GameObject("Preview Biome Look").AddComponent<Volume>();
            volume.gameObject.layer = 30; volume.isGlobal = true;
            var groups = new GameObject[4];
            Color[] grounds = { new Color(.26f,.37f,.22f), new Color(.72f,.53f,.30f), new Color(.76f,.85f,.88f), new Color(.18f,.40f,.29f) };
            int views = 0, vertices = 0;
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                int b = (int)biome;
                var group = new GameObject(biome + " — synthetic buildings");
                group.layer = 30; group.transform.position = new Vector3((b % 2 == 0 ? -1 : 1) * 16, 0, (b < 2 ? -1 : 1) * 12);
                groups[b] = group;
                Material groundMaterial = MaterialAsset("PreviewGround_" + biome, Shader.Find("Universal Render Pipeline/Lit"), grounds[b]);
                Block(group.transform, "Preview ground", new Vector3(0,-.14f,0), new Vector3(28,.2f,20), groundMaterial);
                Block(group.transform, "Preview path", new Vector3(0,-.03f,-6), new Vector3(27,.08f,2.5f), path);
                for (int style = 0; style < 3; style++)
                {
                    BuildingFootprint footprint = Fixture((BuildingStyle)style);
                    var go = new GameObject(((BuildingStyle)style).ToString());
                    go.layer = 30; go.transform.SetParent(group.transform, false);
                    go.transform.localPosition = new Vector3((style-1)*8, 0, style == 1 ? 1.5f : 0);
                    go.AddComponent<ProceduralBuildingView>().Configure(footprint, biome, building);
                    vertices += go.GetComponent<MeshFilter>().sharedMesh.vertexCount; views++;
                }
            }
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                for (int i = 0; i < groups.Length; i++) groups[i].SetActive(i == (int)biome);
                // Rebuild immediately after activation; ExecuteAlways's next Update is otherwise deferred.
                foreach (var view in groups[(int)biome].GetComponentsInChildren<ProceduralBuildingView>()) view.Rebuild();
                var origin = groups[(int)biome].transform.position;
                camera.transform.position = origin + new Vector3(18, 14, -24);
                camera.transform.LookAt(origin + new Vector3(0, 3, 0)); camera.orthographicSize = 12.5f;
                if (!library.TryGet(biome, out var look)) throw new InvalidOperationException("Missing biome look.");
                volume.sharedProfile = look.Profile;
                Shader.SetGlobalColor("_FE_MistColor", look.MistColor);
                Shader.SetGlobalFloat("_FE_MistStrength", look.MistStrength * .35f);
                Render(camera, target, "Temp/building-previews/" + biome + ".png");
            }
            foreach (var group in groups)
            {
                group.SetActive(true);
                foreach (var view in group.GetComponentsInChildren<ProceduralBuildingView>()) view.Rebuild();
            }
            camera.transform.position = new Vector3(50, 42, -58);
            camera.transform.LookAt(new Vector3(0, 2, 0)); camera.orthographicSize = 31;
            library.TryGet(Biome.Temperate, out var temperate); volume.sharedProfile = temperate.Profile;
            Shader.SetGlobalColor("_FE_MistColor", temperate.MistColor);
            Shader.SetGlobalFloat("_FE_MistStrength", .025f);
            Render(camera, target, "Temp/building-previews/Overview.png");
            EditorSceneManager.SaveScene(preview, scenePath);
            AssetDatabase.SaveAssets();
            return "Created offline BuildingPreview with " + views + " deterministic buildings, " + vertices +
                " vertices, one shared building material; 4 biome renders and Overview are in Temp/building-previews. World build list preserved.";
        }
        finally
        {
            RenderTexture.active = beforeTarget;
            Shader.SetGlobalColor("_FE_MistColor", beforeMist); Shader.SetGlobalFloat("_FE_MistStrength", beforeStrength);
            UnityEngine.Object.DestroyImmediate(target);
            EditorSceneManager.CloseScene(preview, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static BuildingFootprint Fixture(BuildingStyle style)
    {
        PlanarPoint[] outline = style == BuildingStyle.StoneKeep ? new[]
        {
            new PlanarPoint(-3,-3), new PlanarPoint(3,-3), new PlanarPoint(3,0),
            new PlanarPoint(0,0), new PlanarPoint(0,3), new PlanarPoint(-3,3)
        } : style == BuildingStyle.TimberHall ? new[]
        {
            new PlanarPoint(-3,-3), new PlanarPoint(3,-3), new PlanarPoint(3,3), new PlanarPoint(-3,3)
        } : new[]
        {
            new PlanarPoint(-2.5,-3), new PlanarPoint(2.5,-3), new PlanarPoint(3,1), new PlanarPoint(0,3), new PlanarPoint(-3,1)
        };
        for (int i = 0; i < 100; i++)
        {
            var result = new BuildingFootprint("preview:building:" + i, outline, style == BuildingStyle.TimberHall ? 5.5 : 7);
            if (BuildingRecipe.Create(result, Biome.Temperate).Style == style) return result;
        }
        throw new InvalidOperationException("No deterministic preview fixture found.");
    }

    private static Material MaterialAsset(string name, Shader shader, Color color)
    {
        if (shader == null) throw new InvalidOperationException("Missing preview shader for " + name);
        string path = "Assets/FantasyEarth/Rendering/Buildings/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.shader = shader;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Block(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.layer = 30;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    private static void Render(Camera camera, RenderTexture target, string path)
    {
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try { image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
