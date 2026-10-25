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

public static class PreviewLiveBuildings
{
    public static string Run()
    {
        Directory.CreateDirectory("Temp/live-building-previews");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
        var library=AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        if (material==null || library==null) throw new InvalidOperationException("Missing rendering assets.");
        Scene previous=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var sources=new List<Mesh>();
        var layers=new List<ObservedBuildingLayer>();
        RenderTexture beforeTarget=RenderTexture.active;
        Color beforeMist=Shader.GetGlobalColor("_FE_MistColor");
        float beforeStrength=Shader.GetGlobalFloat("_FE_MistStrength");
        var target=new RenderTexture(1500,960,24,RenderTextureFormat.ARGB32);
        Material groundMaterial=null;
        try
        {
            target.Create();
            for (int style=0;style<3;style++)
            {
                var outline=style==1 ? new[] { new PlanarPoint(-4,-4),new PlanarPoint(4,-4),new PlanarPoint(4,0),
                    new PlanarPoint(0,0),new PlanarPoint(0,4),new PlanarPoint(-4,4) } :
                    new[] { new PlanarPoint(-3,-4),new PlanarPoint(3,-4),new PlanarPoint(3,4),new PlanarPoint(-3,4) };
                string id=null;
                for (int attempt=0;attempt<100;attempt++)
                {
                    string candidate="synthetic-session:"+style+":"+attempt;
                    if ((int)BuildingRecipe.Create(new BuildingFootprint(candidate,outline,9),Biome.Temperate).Style==style)
                    { id=candidate; break; }
                }
                if (id==null) throw new InvalidOperationException("Missing synthetic style fixture.");
                var footprint=new BuildingFootprint(id,outline,9);
                Mesh source=Proxy(footprint); sources.Add(source);
                var group=new GameObject("Synthetic observed building"); group.layer=30;
                group.transform.position=new Vector3((style-1)*13,0,0);
                var host=new GameObject("Preview render copy");
                var layer=host.AddComponent<ObservedBuildingLayer>(); layers.Add(layer);
                layer.Configure(group.transform,material);
                if (!layer.TryUpsert(id,source,Pose.identity,Biome.Temperate,Vector3.zero)) throw new InvalidOperationException("Proxy rejected.");
                foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>(true)) renderer.gameObject.layer=30;
            }
            groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMaterial.SetColor("_BaseColor",new Color(.30f,.40f,.27f));
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.layer=30;
            ground.transform.position=new Vector3(0,-.25f,0); ground.transform.localScale=new Vector3(47,.3f,20);
            ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var sun=new GameObject("Preview sun").AddComponent<Light>();
            sun.type=LightType.Directional; sun.intensity=1.3f; sun.cullingMask=1<<30;
            sun.transform.rotation=Quaternion.Euler(45,-40,0);
            var camera=new GameObject("Observed building camera").AddComponent<Camera>();
            camera.cullingMask=1<<30; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.31f,.43f,.52f); camera.orthographic=true;
            camera.orthographicSize=19; camera.farClipPlane=250;
            camera.transform.position=new Vector3(24,20,-34); camera.transform.LookAt(new Vector3(0,4,0));
            var cameraData=camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing=true; cameraData.volumeLayerMask=1<<30;
            library.TryGet(Biome.Temperate,out var look);
            var volume=new GameObject("Preview look").AddComponent<Volume>();
            volume.gameObject.layer=30; volume.isGlobal=true; volume.sharedProfile=look.Profile;
            Shader.SetGlobalColor("_FE_MistColor",look.MistColor); Shader.SetGlobalFloat("_FE_MistStrength",.03f);
            float[] blends={0,.5f,1};
            foreach (float blend in blends)
            {
                foreach (var layer in layers) layer.SetPresentation(blend,true);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
                RenderTexture.active=target;
                var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                try
                {
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                    File.WriteAllBytes("Temp/live-building-previews/Blend"+Mathf.RoundToInt(blend*100)+".png",image.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            foreach (var source in sources)
                if (source.normals.Length!=0) throw new InvalidOperationException("Borrowed source was modified.");
            return "Rendered 3 synthetic observed building surfaces at blend 0/0.5/1; borrowed geometry and absent source normals preserved. No scene saved.";
        }
        finally
        {
            RenderTexture.active=beforeTarget;
            foreach (var layer in layers) if (layer!=null) layer.Clear();
            EditorSceneManager.CloseScene(scene,true);
            foreach (var source in sources) UnityEngine.Object.DestroyImmediate(source);
            if (groundMaterial!=null) UnityEngine.Object.DestroyImmediate(groundMaterial);
            UnityEngine.Object.DestroyImmediate(target);
            Shader.SetGlobalColor("_FE_MistColor",beforeMist); Shader.SetGlobalFloat("_FE_MistStrength",beforeStrength);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static Mesh Proxy(BuildingFootprint footprint)
    {
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        Func<PlanarPoint,float,Vector3> point=(p,y)=>new Vector3((float)p.EastM,y,(float)p.NorthM);
        Action<Vector3,Vector3,Vector3> face=(a,b,c)=>
        { int start=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2); };
        for (int i=0;i<footprint.Outline.Count;i++)
        {
            var a=footprint.Outline[i]; var b=footprint.Outline[(i+1)%footprint.Outline.Count];
            face(point(a,0),point(a,9),point(b,9)); face(point(a,0),point(b,9),point(b,0));
        }
        int[] roof=FootprintTriangulator.Triangulate(footprint);
        for (int i=0;i<roof.Length;i+=3)
            face(point(footprint.Outline[roof[i]],9),point(footprint.Outline[roof[i+2]],9),point(footprint.Outline[roof[i+1]],9));
        var mesh=new Mesh { name="Synthetic SDK-like surface" };
        mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
        return mesh;
    }
}
