using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines.MultiDepthOutlines
{
    public class MultiDepthOutlinesFeature : ScriptableRendererFeature
    {
        public RenderPassEvent injectionPoint;
        public float thickness;

        [Space(9)]
        public RenderingLayerMask mask1;
        public Color color1;

        [Space(9)]
        public RenderingLayerMask mask2;
        public Color color2;

        [Space(9)]
        public RenderingLayerMask mask3;
        public Color color3;

        [Space(9)]
        public RenderingLayerMask transparentMask;
        public float transparentOpacity;

        private Material _maskMaterial;

        private CreateMaskTextureRenderPass _createMaskTexturePass;
        private MultiDepthOutlinesChannelRenderPass _channelPass0;
        private MultiDepthOutlinesChannelRenderPass _channelPass1;
        private MultiDepthOutlinesChannelRenderPass _channelPass2;
        private MultiDepthOutlinesChannelRenderPass _transparentPass;
        private MultiDepthOutlinesBlitRenderPass _blitPass;

        public override void Create()
        {
            var maskShader = Shader.Find("Hidden/MultiDepthOutlinesMask");
            if (!maskShader) throw new("Multi Depth Outlines Mask Shader couldn't be found!");
            _maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);

            _createMaskTexturePass = new()
            {
                renderPassEvent = injectionPoint,
            };

            _channelPass0 = new(_maskMaterial, mask1, 0, false)
            {
                renderPassEvent = injectionPoint
            };

            _channelPass1 = new(_maskMaterial, mask2, 1, false)
            {
                renderPassEvent = injectionPoint
            };

            _channelPass2 = new(_maskMaterial, mask3, 2, false)
            {
                renderPassEvent = injectionPoint
            };

            _transparentPass = new(_maskMaterial, transparentMask, 3, true)
            {
                renderPassEvent = injectionPoint
            };

            _blitPass = new(thickness, color1, color2, color3, transparentOpacity)
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_createMaskTexturePass == null) return;

            renderer.EnqueuePass(_createMaskTexturePass);
            renderer.EnqueuePass(_channelPass0);
            renderer.EnqueuePass(_channelPass1);
            renderer.EnqueuePass(_channelPass2);
            renderer.EnqueuePass(_transparentPass);
            renderer.EnqueuePass(_blitPass);
        }

        protected override void Dispose(bool disposing)
        {
            if (Application.isPlaying) Destroy(_maskMaterial);
            else DestroyImmediate(_maskMaterial);
        }
    }
}