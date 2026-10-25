using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FantasyEarth.Fantasy;
using FantasyEarth.Rendering;
using FantasyEarth.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PreviewGroundProps
{
    public static string Run()
    {
        Directory.CreateDirectory("Temp/ground-prop-previews");
        var buildingMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/FantasyEarth/Rendering/Buildings/StorybookBuilding.mat");
        var library=AssetDatabase.LoadAssetAtPath<BiomeLookLibrary>("Assets/FantasyEarth/Rendering/BiomeLooks/BiomeLookLibrary.asset");
        if (buildingMaterial==null || library==null) throw new InvalidOperationException("Missing preview assets.");
        Scene previous=SceneManager.GetActiveScene();
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var materials=new List<Material>();
        RenderTexture oldTarget=RenderTexture.active;
        Color oldMist=Shader.GetGlobalColor("_FE_MistColor");
        float oldStrength=Shader.GetGlobalFloat("_FE_MistStrength");
        var target=new RenderTexture(1500,1100,24,RenderTextureFormat.ARGB32);
        try
        {
            target.Create();
            var frame=new LocalGeoFrame(new GeoPoint(0,0)); // Synthetic origin, unrelated to the device.
            var obstacle=new BuildingFootprint("synthetic-garden-building",new[] { new PlanarPoint(5,5),new PlanarPoint(13,5),
                new PlanarPoint(13,13),new PlanarPoint(5,13) },5);
            var groups=new GameObject[4];
            var propsByBiome=new List<IReadOnlyList<GroundProp>>();
            int vertices=0,props=0;
            Color[] grounds={new Color(.28f,.42f,.23f),new Color(.74f,.54f,.31f),new Color(.80f,.88f,.91f),new Color(.20f,.43f,.29f)};
            Material path=Material(new Color(.68f,.59f,.43f),materials);
            Material water=Material(new Color(.22f,.48f,.56f),materials);
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                int b=(int)biome;
                var group=new GameObject("Synthetic "+biome+" garden"); groups[b]=group; group.layer=30;
                group.transform.position=new Vector3((b%2==0 ? -1 : 1)*38,0,(b<2 ? -1 : 1)*38);
                Block(group.transform,"Eligible flat ground",new Vector3(0,-.17f,0),new Vector3(70,.25f,70),Material(grounds[b],materials));
                Block(group.transform,"Excluded path",new Vector3(0,-.025f,0),new Vector3(5,.05f,69),path);
                Block(group.transform,"Excluded water",new Vector3(0,-.02f,-27),new Vector3(69,.04f,14),water);
                var building=new GameObject("Synthetic building exclusion"); building.layer=30; building.transform.SetParent(group.transform,false);
                building.AddComponent<ProceduralBuildingView>().Configure(obstacle,biome,buildingMaterial);
                IReadOnlyList<GroundProp> planned=GroundPropPlanner.Generate(frame,32,_=>biome,
                    p=>Math.Abs(p.EastM)<2.5 || p.NorthM<-20 ? (double?)null : 0,new[] { obstacle });
                propsByBiome.Add(planned);
                var chunk=new GameObject("Shared seeded prop chunk"); chunk.layer=30; chunk.transform.SetParent(group.transform,false);
                var view=chunk.AddComponent<GroundPropChunkView>(); view.Configure(planned,buildingMaterial);
                view.SetPresentation(GroundPropView.Map,0,false);
                props+=planned.Count; vertices+=chunk.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }
            for (int b=1;b<propsByBiome.Count;b++)
                if (!propsByBiome[0].Select(p=>p.Id).SequenceEqual(propsByBiome[b].Select(p=>p.Id)))
                    throw new InvalidOperationException("Biome changed candidate identity or eligibility.");
            var light=new GameObject("Preview sun").AddComponent<Light>();
            light.type=LightType.Directional; light.intensity=1.3f; light.cullingMask=1<<30;
            light.transform.rotation=Quaternion.Euler(45,-40,0);
            var camera=new GameObject("Ground prop camera").AddComponent<Camera>();
            camera.cullingMask=1<<30; camera.orthographic=true; camera.orthographicSize=36;
            camera.nearClipPlane=.1f; camera.farClipPlane=500; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.31f,.43f,.52f);
            var cameraData=camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing=true; cameraData.volumeLayerMask=1<<30;
            var volume=new GameObject("Preview look").AddComponent<Volume>(); volume.gameObject.layer=30; volume.isGlobal=true;
            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                for (int i=0;i<4;i++) groups[i].SetActive(i==(int)biome);
                library.TryGet(biome,out var look); volume.sharedProfile=look.Profile;
                Shader.SetGlobalColor("_FE_MistColor",look.MistColor); Shader.SetGlobalFloat("_FE_MistStrength",.025f);
                Vector3 centre=groups[(int)biome].transform.position;
                camera.transform.position=centre+new Vector3(38,30,-48); camera.transform.LookAt(centre+Vector3.up*2);
                Render(camera,target,"Temp/ground-prop-previews/"+biome+".png");
            }
            foreach (var group in groups) group.SetActive(true);
            library.TryGet(Biome.Temperate,out var temperate); volume.sharedProfile=temperate.Profile;
            Shader.SetGlobalColor("_FE_MistColor",temperate.MistColor);
            camera.transform.position=new Vector3(115,100,-145); camera.transform.LookAt(Vector3.zero); camera.orthographicSize=95;
            Render(camera,target,"Temp/ground-prop-previews/Overview.png");
            return $"Rendered four synthetic biome gardens: {props} props, {vertices} chunk vertices, four prop meshes/shared material. Same geographic IDs in all gardens; path/water/building exclusions applied. Five renders, no scene/build-list changes.";
        }
        finally
        {
            RenderTexture.active=oldTarget;
            EditorSceneManager.CloseScene(scene,true);
            foreach (var material in materials) UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(target);
            Shader.SetGlobalColor("_FE_MistColor",oldMist); Shader.SetGlobalFloat("_FE_MistStrength",oldStrength);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static Material Material(Color color,List<Material> owned)
    {
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",.12f); owned.Add(material); return material;
    }
    private static void Block(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.layer=30;
        go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material; UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    private static void Render(Camera camera,RenderTexture target,string path)
    {
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination=target });
        RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try { image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
}
