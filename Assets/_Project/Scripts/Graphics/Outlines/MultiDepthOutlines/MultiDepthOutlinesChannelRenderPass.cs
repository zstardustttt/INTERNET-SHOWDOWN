using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines.MultiDepthOutlines
{
    public class MultiDepthOutlinesRenderPassData
    {
        public RendererListHandle rendererList;
    }

    public class MultiDepthOutlinesChannelRenderPass : ScriptableRenderPass
    {
        private static readonly int _maskID = Shader.PropertyToID("_MultiDepthOutlinesMask");

        public static readonly List<ShaderTagId> shaderTags = new()
        {
            new("UniversalForward"),
            new("UniversalForwardOnly"),
            new("SRPDefaultUnlit")
        };

        private readonly Material _maskMaterial;
        private readonly RenderingLayerMask _renderingLayerMask;
        private readonly int _passIndex;
        private readonly bool _setGlobalTexture;

        public MultiDepthOutlinesChannelRenderPass(Material maskMaterial, RenderingLayerMask renderingLayerMask, int passIndex, bool setGlobalTexture)
        {
            _maskMaterial = maskMaterial;
            _renderingLayerMask = renderingLayerMask;
            _passIndex = passIndex;
            _setGlobalTexture = setGlobalTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_maskMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var outlinesData = frameData.Get<MultiDepthOutlinesData>();

            var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, cameraData.defaultOpaqueSortFlags);
            drawSettings.overrideMaterial = _maskMaterial;
            drawSettings.overrideMaterialPassIndex = _passIndex;

            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: _renderingLayerMask);
            var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filteringSettings);

            using var builder = renderGraph.AddRasterRenderPass<MultiDepthOutlinesRenderPassData>($"Get Multi Depth Outlines Mask Channel {_passIndex}", out var passData);
            var rendererListHandle = renderGraph.CreateRendererList(rendererListParams);

            passData.rendererList = rendererListHandle;
            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(outlinesData.maskTexture, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resourceData.cameraDepthTexture, AccessFlags.Read);
            builder.SetRenderFunc((MultiDepthOutlinesRenderPassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererList);
            });
            if (_setGlobalTexture) builder.SetGlobalTextureAfterPass(outlinesData.maskTexture, _maskID);
        }
    }
}