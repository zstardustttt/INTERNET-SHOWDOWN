using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines.MultiDepthOutlines
{
    public class MultiDepthOutlinesBlitRenderPass : ScriptableRenderPass
    {
        private static readonly int _color1ID = Shader.PropertyToID("_Color1");
        private static readonly int _color2ID = Shader.PropertyToID("_Color2");
        private static readonly int _color3ID = Shader.PropertyToID("_Color3");
        private static readonly int _opacityID = Shader.PropertyToID("_Opacity");
        private static readonly int _thicknessID = Shader.PropertyToID("_Thickness");

        private readonly float _thickness;
        private readonly Color _color1;
        private readonly Color _color2;
        private readonly Color _color3;
        private readonly float _transparentOpacity;

        private readonly Material _material;

        public MultiDepthOutlinesBlitRenderPass(float thickness, Color color1, Color color2, Color color3, float transparentOpacity)
        {
            var shader = Shader.Find("Hidden/MultiDepthOutlines");
            if (!shader) throw new("Multi Depth Outlines Shader couldn't be found!");
            _material = CoreUtils.CreateEngineMaterial(shader);

            _thickness = thickness;
            _color1 = color1;
            _color2 = color2;
            _color3 = color3;
            _transparentOpacity = transparentOpacity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!_material) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            var outlinesData = frameData.Get<MultiDepthOutlinesData>();
            UpdateProperties();

            var srcColor = resourceData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_MultiDepthOutlinesBlit";
            var dstColor = renderGraph.CreateTexture(dstDesc);

            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, 0);
            using var builder = renderGraph.AddBlitPass(param, "Multi Depth Outlines Blit", returnBuilder: true);
            builder.UseTexture(outlinesData.maskTexture);

            resourceData.cameraColor = dstColor;
        }

        private void UpdateProperties()
        {
            _material.SetColor(_color1ID, _color1);
            _material.SetColor(_color2ID, _color2);
            _material.SetColor(_color3ID, _color3);
            _material.SetFloat(_opacityID, _transparentOpacity);

            var thickness = new Vector2(_thickness, _thickness * ((float)Screen.width / Screen.height)) / 100f;
            _material.SetVector(_thicknessID, thickness);
        }
    }
}