using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines
{
    public class SSOutlinesRenderPassData
    {
        public RendererListHandle rendererList;
    }

    public class SSOutlinesRenderPass : ScriptableRenderPass
    {
        public static readonly List<ShaderTagId> shaderTags = new()
        {
            new("UniversalForward"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit")
        };

        public static readonly Dictionary<(bool, bool, bool), int> _fragmentPasses = new()
        {
            {(true, true, true), 0},

            {(true, false, false), 1},
            {(false, true, false), 2},
            {(false, false, true), 3},

            {(true, true, false), 4},
            {(false, true, true), 5},
            {(true, false, true), 6},
        };

        private static readonly int _colorID = Shader.PropertyToID("_Color");
        private static readonly int _thicknessID = Shader.PropertyToID("_Thickness");

        private static readonly int _enableDepthID = Shader.PropertyToID("_EnableDepth");
        private static readonly int _depthStrengthID = Shader.PropertyToID("_DepthStrength");
        private static readonly int _depthThicknessID = Shader.PropertyToID("_DepthThickness");
        private static readonly int _depthThresholdID = Shader.PropertyToID("_DepthThreshold");

        private static readonly int _enableColorID = Shader.PropertyToID("_EnableColor");
        private static readonly int _colorStrengthID = Shader.PropertyToID("_ColorStrength");
        private static readonly int _colorThicknessID = Shader.PropertyToID("_ColorThickness");
        private static readonly int _colorThresholdID = Shader.PropertyToID("_ColorThreshold");

        private static readonly int _enableNormalsID = Shader.PropertyToID("_EnableNormals");
        private static readonly int _normalsStrengthID = Shader.PropertyToID("_NormalsStrength");
        private static readonly int _normalsThicknessID = Shader.PropertyToID("_NormalsThickness");
        private static readonly int _normalsThresholdID = Shader.PropertyToID("_NormalsThreshold");

        private static readonly int _acuteAngleStartDotID = Shader.PropertyToID("_AcuteAngleStartDot");
        private static readonly int _acuteDepthThresholdID = Shader.PropertyToID("_AcuteDepthThreshold");

        private static readonly int _adjustNearDepthID = Shader.PropertyToID("_AdjustNearDepth");
        private static readonly int _adjustFarDepthID = Shader.PropertyToID("_AdjustFarDepth");
        private static readonly int _normalsFarThresholdID = Shader.PropertyToID("_NormalsFarThreshold");
        private static readonly int _colorFarThresholdID = Shader.PropertyToID("_ColorFarThreshold");

        private static readonly int _outlinesMaskID = Shader.PropertyToID("_OutlinesMask");

        private SSOutlinesProperties _properties;
        private readonly Material _material;
        private readonly Material _outlinesMaskMaterial;

        public SSOutlinesRenderPass(Material material, SSOutlinesProperties properties)
        {
            _material = material;
            _properties = properties;

            var layerMaskShader = Shader.Find("Hidden/LayerMask");
            if (!layerMaskShader)
            {
                // Fallback
                layerMaskShader = Shader.Find("Unlit/Color");
            }
            _outlinesMaskMaterial = CoreUtils.CreateEngineMaterial(layerMaskShader);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;
            if (!_properties.enableDepth && !_properties.enableColor && !_properties.enableNormals) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            GetOutlinesMask(renderGraph, frameData, resourceData);
            UpdateProperties();

            var srcColor = resourceData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_SSOTexture";
            var dstColor = renderGraph.CreateTexture(dstDesc);

            var passIndex = _fragmentPasses[(_properties.enableDepth, _properties.enableColor, _properties.enableNormals)];
            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, passIndex);
            using var builder = renderGraph.AddBlitPass(param, "Screen Space Outlines Pass", returnBuilder: true);
            builder.UseTexture(resourceData.cameraOpaqueTexture);

            resourceData.cameraColor = dstColor;
        }

        private void GetOutlinesMask(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData)
        {
            if (!_outlinesMaskMaterial) return;

            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var cameraTextureDescriptor = cameraData.cameraTargetDescriptor;
            var renderTextureDescriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, cameraTextureDescriptor.colorFormat, 0, cameraTextureDescriptor.mipCount, RenderTextureReadWrite.Default);
            var outlinesMaskTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, renderTextureDescriptor, "_SSOMask", false);

            var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            drawSettings.overrideMaterial = _outlinesMaskMaterial;
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, _properties.layerMask);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);

            using var builder = renderGraph.AddRasterRenderPass<SSOutlinesRenderPassData>("Get Outlines Mask Texture", out var passData);
            var rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

            passData.rendererList = rendererListHandle;
            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
            builder.SetRenderAttachment(outlinesMaskTexture, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((SSOutlinesRenderPassData data, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(true, true, Color.black);
                context.cmd.DrawRendererList(data.rendererList);
                _material.SetTexture(_outlinesMaskID, outlinesMaskTexture);
            });
        }

        private void UpdateProperties()
        {
            _material.SetColor(_colorID, _properties.color);

            var thickness = new Vector2(_properties.thickness, _properties.thickness * ((float)Screen.width / Screen.height)) / 100f;
            _material.SetVector(_thicknessID, thickness);

            _material.SetFloat(_enableDepthID, _properties.enableDepth ? 1f : 0f);
            _material.SetFloat(_depthStrengthID, _properties.depthStrength);
            _material.SetFloat(_depthThicknessID, _properties.depthThickness);
            _material.SetFloat(_depthThresholdID, _properties.depthThreshold);

            _material.SetFloat(_enableColorID, _properties.enableColor ? 1f : 0f);
            _material.SetFloat(_colorStrengthID, _properties.colorStrength);
            _material.SetFloat(_colorThicknessID, _properties.colorThickness);
            _material.SetFloat(_colorThresholdID, _properties.colorThreshold);

            _material.SetFloat(_enableNormalsID, _properties.enableNormals ? 1f : 0f);
            _material.SetFloat(_normalsStrengthID, _properties.normalsStrength);
            _material.SetFloat(_normalsThicknessID, _properties.normalsThickness);
            _material.SetFloat(_normalsThresholdID, _properties.normalsThreshold);

            _material.SetFloat(_acuteAngleStartDotID, _properties.acuteAngleStartDot);
            _material.SetFloat(_acuteDepthThresholdID, _properties.acuteDepthThreshold);

            _material.SetFloat(_adjustNearDepthID, _properties.adjustNearDepth);
            _material.SetFloat(_adjustFarDepthID, _properties.adjustFarDepth);
            _material.SetFloat(_normalsFarThresholdID, _properties.normalsFarThreshold);
            _material.SetFloat(_colorFarThresholdID, _properties.colorFarThreshold);
        }
    }
}