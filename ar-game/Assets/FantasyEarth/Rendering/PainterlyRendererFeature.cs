using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace FantasyEarth.Rendering
{
    public sealed class PainterlyRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material material;
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [SerializeField, Range(1, 4)] private int downsample = 2;
        private PainterlyPass _pass;

        public override void Create()
        {
            _pass = new PainterlyPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null || renderingData.cameraData.cameraType != CameraType.Game)
                return;
            _pass.Material = material;
            _pass.Downsample = Mathf.Clamp(downsample, 1, 4);
            _pass.renderPassEvent = passEvent;
            renderer.EnqueuePass(_pass);
        }

        private sealed class PainterlyPass : ScriptableRenderPass
        {
            public Material Material;
            public int Downsample;

            public PainterlyPass()
            {
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var camera = frameData.Get<UniversalCameraData>();
                if (camera.cameraType != CameraType.Game || resources.isActiveTargetBackBuffer || Material == null)
                    return;
                TextureHandle source = resources.activeColorTexture;
                if (!source.IsValid()) return;

                var fullDescriptor = source.GetDescriptor(renderGraph);
                fullDescriptor.name = "FantasyEarth Painted Camera";
                fullDescriptor.clearBuffer = false;
                fullDescriptor.msaaSamples = MSAASamples.None;
                var filteredDescriptor = fullDescriptor;
                filteredDescriptor.name = "FantasyEarth Kuwahara";
                filteredDescriptor.width = Mathf.Max(1, fullDescriptor.width / Downsample);
                filteredDescriptor.height = Mathf.Max(1, fullDescriptor.height / Downsample);
                filteredDescriptor.filterMode = FilterMode.Bilinear;
                TextureHandle filtered = renderGraph.CreateTexture(filteredDescriptor);
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, filtered, Material, 0),
                    "FantasyEarth Painterly");

                // Filter at half resolution on mobile, then restore the camera resource's dimensions.
                TextureHandle destination = renderGraph.CreateTexture(fullDescriptor);
                renderGraph.AddBlitPass(filtered, destination, Vector2.one, Vector2.zero,
                    passName: "FantasyEarth Painterly Upscale");
                resources.cameraColor = destination;
            }
        }
    }
}
