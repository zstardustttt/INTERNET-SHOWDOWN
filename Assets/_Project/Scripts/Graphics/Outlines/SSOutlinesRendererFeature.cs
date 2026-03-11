using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Outlines
{
    [Serializable]
    public struct SSOutlinesProperties
    {
        public Color color;
        public float thickness;
        public RenderingLayerMask renderingLayerMask;

        [Space(9)]
        public bool enableDepth;
        public float depthStrength;
        public float depthThickness;
        public float depthThreshold;

        [Space(9)]
        public bool enableColor;
        public float colorStrength;
        public float colorThickness;
        public float colorThreshold;

        [Space(9)]
        public bool enableNormals;
        public float normalsStrength;
        public float normalsThickness;
        public float normalsThreshold;

        [Space(9)]
        public float acuteAngleStartDot;
        public float acuteDepthThreshold;

        [Space(9)]
        public float adjustNearDepth;
        public float adjustFarDepth;
        public float normalsFarThreshold;
        public float colorFarThreshold;
    }

    public class SSOutlinesRendererFeature : ScriptableRendererFeature
    {
        public SSOutlinesProperties properties;
        public RenderPassEvent injectionPoint;

        private Material _material;
        private SSOutlinesRenderPass _renderPass;

        public override void Create()
        {
            var shader = Shader.Find("Hidden/SSOutlines");
            if (!shader) throw new("Screen Space Outlines shader couldn't be found!");
            _material = CoreUtils.CreateEngineMaterial(shader);

            _renderPass = new(_material, properties)
            {
                renderPassEvent = injectionPoint,
            };

            _renderPass.ConfigureInput(ScriptableRenderPassInput.Normal);
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
}