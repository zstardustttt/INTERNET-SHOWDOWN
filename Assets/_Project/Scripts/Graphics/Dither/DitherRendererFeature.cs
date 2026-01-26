using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Graphics.Dither
{
    [Serializable]
    public struct DitherProperties
    {
        public float checkerboardSize;
        public float checherboardScrollSpeed;
        [Range(0f, 1f)] public float checkerboardIntensity;

        [Space(9)]
        public float skyboxQuantization;
        public float skyboxPixelization;
        public float skyboxDitherScale;
        public float skyboxDitherIntensity;
        public float skyboxContrast;
        public float skyboxBrightness;
    }

    public class DitherRendererFeature : ScriptableRendererFeature
    {
        public DitherProperties properties;
        public Shader shader;
        public RenderPassEvent injectionPoint;

        private Material _material;
        private DitherRenderPass _renderPass;

        public override void Create()
        {
            if (shader == null) return;

            _material = new(shader);
            _renderPass = new(_material, properties)
            {
                renderPassEvent = injectionPoint
            };

            _renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Normal);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_renderPass == null) return;
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(_renderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
    }
}