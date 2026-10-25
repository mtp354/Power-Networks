using System;
using System.Collections.Generic;
using System.IO;
using FantasyEarth.Data;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Local evidence only: requires the separately downloaded public sample in ignored Temp.</summary>
public static class PreviewBuildingTile
{
    public static string Build()
    {
        const string output = "Temp/building-data-preview";
        BuildingCatalog catalog = BuildingTileReader.Read(File.ReadAllText("Temp/boston-building-tile.json"));
        if (catalog.Buildings.Count == 0) throw new InvalidOperationException("Sample has no supported buildings.");
        var matcher = new BuildingFootprintMatcher(catalog);
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
        if (material == null) throw new InvalidOperationException("The shared building material is missing.");
        var library = AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        if (library == null || !library.TryGet(Biome.Temperate, out var look)) throw new InvalidOperationException("Missing biome look.");
        Directory.CreateDirectory(output);
        Scene previous = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        var meshes = new List<Mesh>();
        RenderTexture previousTarget = RenderTexture.active;
        Color previousMist = Shader.GetGlobalColor("_FE_MistColor");
        float previousStrength = Shader.GetGlobalFloat("_FE_MistStrength");
        var target = new RenderTexture(1600, 1100, 24, RenderTextureFormat.ARGB32);
        Material groundMaterial = null;
        try
        {
            target.Create();
            var bounds = new Bounds();
            int vertices = 0, matched = 0, ambiguous = 0;
            int[] styles = new int[3], heights = new int[4];
            bool first = true;
            foreach (var building in catalog.Buildings)
            {
                var footprint = building.Footprint;
                var match = matcher.Match(footprint);
                if (match.Status == BuildingMatchStatus.Matched && match.Building.Footprint.StableId == footprint.StableId) matched++;
                else if (match.Status == BuildingMatchStatus.Ambiguous) ambiguous++;
                else throw new InvalidOperationException("A canonical footprint failed its identity round trip.");
                BuildingRecipe recipe = BuildingRecipe.Create(footprint, Biome.Temperate);
                Mesh mesh = BuildingMeshBuilder.Create(footprint, recipe);
                meshes.Add(mesh); vertices += mesh.vertexCount;
                if (first) { bounds = mesh.bounds; first = false; } else bounds.Encapsulate(mesh.bounds);
                styles[(int)recipe.Style]++; heights[(int)building.HeightSource]++;
                var go = new GameObject("Public sample building");
                go.layer = 30;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMaterial.SetColor("_BaseColor", new Color(.31f,.39f,.27f));
            groundMaterial.SetFloat("_Smoothness", .05f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Preview ground"; ground.layer = 30;
            ground.transform.position = new Vector3(bounds.center.x,-.2f,bounds.center.z);
            ground.transform.localScale = new Vector3(bounds.size.x+60,.3f,bounds.size.z+60);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var sun = new GameObject("Sample sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.25f;
            sun.cullingMask = 1 << 30; sun.transform.rotation = Quaternion.Euler(42,-35,0);
            var camera = new GameObject("Sample preview camera").AddComponent<Camera>();
            camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.32f,.45f,.54f);
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 3000;
            float radius = Mathf.Max(bounds.extents.magnitude,50);
            camera.transform.position = bounds.center + new Vector3(.85f,.9f,-1.25f)*radius*2;
            camera.transform.LookAt(bounds.center);
            camera.orthographicSize = radius*1.05f;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true; cameraData.volumeLayerMask = 1 << 30;
            var volume = new GameObject("Sample look").AddComponent<Volume>();
            volume.gameObject.layer = 30; volume.isGlobal = true; volume.sharedProfile = look.Profile;
            Shader.SetGlobalColor("_FE_MistColor",look.MistColor);
            Shader.SetGlobalFloat("_FE_MistStrength",0);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
            RenderTexture.active = target;
            var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                File.WriteAllBytes(output+"/Boston.png",image.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
            string result = $"Read {catalog.Buildings.Count} public building parts; correct identity matches={matched}, " +
                $"ambiguous={ambiguous}; meshes={meshes.Count}, vertices={vertices}; " +
                $"styles timber/keep/spire={styles[0]}/{styles[1]}/{styles[2]}; " +
                $"heights reported/floors/default={heights[1]}/{heights[2]}/{heights[3]}. " +
                "Rendered Temp/building-data-preview/Boston.png. No scene or build settings saved.";
            File.WriteAllText(output+"/evidence.txt",result+"\n"+catalog.Attribution+"\nhttps://docs.overturemaps.org/attribution/\n");
            return result;
        }
        finally
        {
            RenderTexture.active = previousTarget;
            Shader.SetGlobalColor("_FE_MistColor",previousMist);
            Shader.SetGlobalFloat("_FE_MistStrength",previousStrength);
            EditorSceneManager.CloseScene(preview,true);
            foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
            if (groundMaterial != null) UnityEngine.Object.DestroyImmediate(groundMaterial);
            UnityEngine.Object.DestroyImmediate(target);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
}
