using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines
{
    public class SimpleDepthSSOutlinesRendererFeature : ScriptableRendererFeature
    {
        public float thickness;
        public Color color;
        public Shader shader;
        public RenderPassEvent injectionPoint;

        private Material _material;
        private SimpleDepthSSOutlinesRenderPass _renderPass;

        public override void Create()
        {
            if (shader == null) return;

            _material = new(shader);
            _renderPass = new(_material, thickness, color)
            {
                renderPassEvent = injectionPoint
            };

            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null) return;
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }

    public class SimpleDepthSSOutlinesRenderPass : ScriptableRenderPass
    {
        private static readonly int _colorID = Shader.PropertyToID("_Color");
        private static readonly int _thicknessID = Shader.PropertyToID("_Thickness");

        private readonly float _thickness;
        private readonly Color _color;
        private readonly Material _material;

        public SimpleDepthSSOutlinesRenderPass(Material material, float thickness, Color color)
        {
            _material = material;
            _thickness = thickness;
            _color = color;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var srcColor = resourceData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_SimpleDepthSSOTexture";
            var dstColor = renderGraph.CreateTexture(dstDesc);
            UpdateProperties();

            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, 0);
            renderGraph.AddBlitPass(param, "Simple Depth Screen Space Outlines Pass");

            resourceData.cameraColor = dstColor;
        }

        private void UpdateProperties()
        {
            if (_material == null) return;

            _material.SetColor(_colorID, _color);

            var thickness = new Vector2(_thickness, _thickness * ((float)Screen.width / Screen.height)) / 100f;
            _material.SetVector(_thicknessID, thickness);
        }
    }
}