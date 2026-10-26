using System;
using System.IO;
using FantasyEarth.Data;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewBrooklynMap
{
    public static string Run()
    {
        var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/FantasyEarth/Data/Maps/BrooklynWaterfront.bytes");
        var region=MapPackReader.Read(asset.text);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
        var library=AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        Scene previous=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var renderer=new GameObject("Brooklyn waterfront preview").AddComponent<NeighborhoodMapRenderer>();
        var target=new RenderTexture(1600,1200,24,RenderTextureFormat.ARGB32);
        RenderTexture before=RenderTexture.active;
        Color oldMist=Shader.GetGlobalColor("_FE_MistColor"); float oldStrength=Shader.GetGlobalFloat("_FE_MistStrength");
        try
        {
            var build=renderer.Build(region,material); while (build.MoveNext()) { }
            var centre=region.Buildings.Frame.Project(region.DefaultCenter).Planar;
            for (int x=-1;x<=1;x++) for (int z=-1;z<=1;z++) renderer.AddPropsNear(new FantasyEarth.World.PlanarPoint(centre.EastM+x*128,centre.NorthM+z*128));
            var sun=new GameObject("Map sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.cullingMask=1<<NeighborhoodMapRenderer.MapLayer; sun.intensity=1.3f; sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var camera=new GameObject("Map camera").AddComponent<Camera>(); camera.cullingMask=1<<NeighborhoodMapRenderer.MapLayer;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.19f,.31f,.36f);
            camera.orthographic=true; camera.farClipPlane=6000; camera.nearClipPlane=.1f;
            var cameraData=camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true;
            cameraData.volumeLayerMask=1<<NeighborhoodMapRenderer.MapLayer;
            library.TryGet(Biome.Temperate,out var look);
            var volume=new GameObject("Map look").AddComponent<Volume>(); volume.isGlobal=true;
            volume.gameObject.layer=NeighborhoodMapRenderer.MapLayer; volume.sharedProfile=look.Profile;
            Shader.SetGlobalColor("_FE_MistColor",look.MistColor); Shader.SetGlobalFloat("_FE_MistStrength",0);
            target.Create(); Directory.CreateDirectory("Temp/nyc-map-previews");
            Render(camera,target,new Vector3((float)centre.EastM,0,(float)centre.NorthM),240,"Waterfront");
            Render(camera,target,new Vector3(0,0,0),850,"Overview");
            return $"Rendered Brooklyn map: {renderer.BuildingCount} real building parts, {region.Surfaces.Count} road/park/water surfaces, {renderer.PropCount} seeded props, {renderer.VertexCount} vertices. No scene saved.";
        }
        finally
        {
            RenderTexture.active=before; renderer.Clear(); EditorSceneManager.CloseScene(scene,true);
            UnityEngine.Object.DestroyImmediate(target); Shader.SetGlobalColor("_FE_MistColor",oldMist); Shader.SetGlobalFloat("_FE_MistStrength",oldStrength);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }
    private static void Render(Camera camera,RenderTexture target,Vector3 centre,float zoom,string name)
    {
        camera.orthographicSize=zoom; camera.transform.rotation=Quaternion.Euler(55,0,0);
        camera.transform.position=centre-camera.transform.forward*zoom*3;
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
        RenderTexture.active=target; var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes("Temp/nyc-map-previews/"+name+".png",image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
