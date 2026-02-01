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

        private static readonly int _filteredOpaqueTextureID = Shader.PropertyToID("_FilteredOpaqueTexture");
        private static readonly int _filteredDepthTextureID = Shader.PropertyToID("_FilteredDepthTexture");
        private static readonly int _filteredNormalsTextureID = Shader.PropertyToID("_FilteredNormalsTexture");

        private SSOutlinesProperties _properties;
        private readonly Material _material;

        public SSOutlinesRenderPass(Material material, SSOutlinesProperties properties)
        {
            _material = material;
            _properties = properties;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
                return;

            var srcColor = resourceData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_SSOTexture";
            var dstColor = renderGraph.CreateTexture(dstDesc);

            GetFilteredTextures(renderGraph, frameData);
            UpdateProperties();

            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, 0);
            renderGraph.AddBlitPass(param, "Screen Space Outlines Pass");

            resourceData.cameraColor = dstColor;
        }

        private void GetFilteredTextures(RenderGraph renderGraph, ContextContainer frameData)
        {
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var cameraTextureDescriptor = cameraData.cameraTargetDescriptor;
            var renderTextureDescriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, cameraTextureDescriptor.colorFormat, 0, cameraTextureDescriptor.mipCount, RenderTextureReadWrite.Default);

            GetOpaqueAndDepthTextures(renderGraph, renderingData, cameraData, lightData, renderTextureDescriptor, cameraTextureDescriptor);

            if (_properties.enableNormals)
                GetNormalsTexture(renderGraph, renderingData, cameraData, lightData, renderTextureDescriptor);
        }

        private void GetOpaqueAndDepthTextures(RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, RenderTextureDescriptor renderTextureDescriptor, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all, _properties.layerMask);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);

            using var builder = renderGraph.AddRasterRenderPass<SSOutlinesRenderPassData>("Get Filtered Opaque And Depth Textures", out var passData);
            var rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

            passData.rendererList = rendererListHandle;
            var opaqueTexture = _properties.enableColor ?
                UniversalRenderer.CreateRenderGraphTexture(renderGraph, renderTextureDescriptor, "_SSOOpaqueTexture", false) :
                TextureHandle.nullHandle;

            var depthTextureDescriptor = new RenderTextureDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, RenderTextureFormat.Depth, cameraTextureDescriptor.depthBufferBits, cameraTextureDescriptor.mipCount, RenderTextureReadWrite.Default);
            var depthTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthTextureDescriptor, "_SSODepthTexture", false);

            builder.UseRendererList(passData.rendererList);
            if (_properties.enableColor) builder.SetRenderAttachment(opaqueTexture, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((SSOutlinesRenderPassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererList);
                if (_properties.enableColor) _material.SetTexture(_filteredOpaqueTextureID, opaqueTexture);
                _material.SetTexture(_filteredDepthTextureID, depthTexture);
            });
        }

        private void GetNormalsTexture(RenderGraph renderGraph, UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, RenderTextureDescriptor renderTextureDescriptor)
        {
            if (!_properties.getNormalsMaterial) return;

            var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            drawSettings.overrideMaterial = _properties.getNormalsMaterial;
            var filteringSettings = new FilteringSettings(RenderQueueRange.all, _properties.layerMask);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);

            using var builder = renderGraph.AddRasterRenderPass<SSOutlinesRenderPassData>("Get Filtered Normals Texture", out var passData);
            var rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

            passData.rendererList = rendererListHandle;
            var normalsTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, renderTextureDescriptor, "_SSONormalsTexture", false);

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(normalsTexture, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((SSOutlinesRenderPassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererList);
                _material.SetTexture(_filteredNormalsTextureID, normalsTexture);
            });
        }

        private void UpdateProperties()
        {
            if (_material == null) return;

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