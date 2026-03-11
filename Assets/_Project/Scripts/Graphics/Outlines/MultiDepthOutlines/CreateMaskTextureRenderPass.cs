using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines.MultiDepthOutlines
{
    public class MultiDepthOutlinesData : ContextItem
    {
        public TextureHandle maskTexture;

        public override void Reset()
        {
            maskTexture = TextureHandle.nullHandle;
        }
    }

    public class CreateMaskTextureRenderPass : ScriptableRenderPass
    {
        private class PassData { }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            var textureDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            textureDesc.name = "_MultiDepthOutlinesMask";
            textureDesc.format = GraphicsFormat.R16G16B16A16_SFloat;
            textureDesc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            textureDesc.msaaSamples = MSAASamples.None;
            var maskTexture = renderGraph.CreateTexture(textureDesc);

            var outlinesData = frameData.Create<MultiDepthOutlinesData>();
            outlinesData.maskTexture = maskTexture;

            using var builder = renderGraph.AddRasterRenderPass<PassData>("Create Multi Depth Outlines Mask Texture", out _);
            builder.SetRenderAttachment(maskTexture, 0, AccessFlags.WriteAll);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                context.cmd.ClearRenderTarget(true, true, Color.black);
            });
        }
    }
}