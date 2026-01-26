using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Vector2 = UnityEngine.Vector2;

namespace Game.Graphics.Dither
{
    public class DitherRenderPass : ScriptableRenderPass
    {
        private static readonly int _checkerboardSizeID = Shader.PropertyToID("_checkerboard_size");
        private static readonly int _checkerboardScrollSpeedID = Shader.PropertyToID("_checkerboard_scroll_speed");
        private static readonly int _checkerboardIntensityID = Shader.PropertyToID("_checkerboard_intensity");

        private static readonly int _skyboxQuantizationID = Shader.PropertyToID("_skybox_quantization");
        private static readonly int _skyboxResolutionID = Shader.PropertyToID("_skybox_resolution");
        private static readonly int _skyboxDitherScaleID = Shader.PropertyToID("_skybox_dither_scale");
        private static readonly int _skyboxDitherIntensityID = Shader.PropertyToID("_skybox_dither_intensity");
        private static readonly int _skyboxContrastID = Shader.PropertyToID("_skybox_contrast");
        private static readonly int _skyboxBrightnessID = Shader.PropertyToID("_skybox_brightness");

        private DitherProperties _properties;
        private Material _material;

        public DitherRenderPass(Material material, DitherProperties properties)
        {
            _material = material;
            _properties = properties;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resData = frameData.Get<UniversalResourceData>();
            if (resData.isActiveTargetBackBuffer)
                return;

            var srcColor = resData.activeColorTexture;

            var dstDesc = srcColor.GetDescriptor(renderGraph);
            dstDesc.name = "_DitherTexture";
            var dstColor = renderGraph.CreateTexture(dstDesc);

            UpdateProperties();

            var param = new RenderGraphUtils.BlitMaterialParameters(srcColor, dstColor, _material, 0);
            renderGraph.AddBlitPass(param, "DitherPass");

            resData.cameraColor = dstColor;
        }

        private void UpdateProperties()
        {
            if (_material == null) return;

            _material.SetFloat(_checkerboardSizeID, _properties.checkerboardSize);
            _material.SetFloat(_checkerboardScrollSpeedID, _properties.checherboardScrollSpeed);
            _material.SetFloat(_checkerboardIntensityID, _properties.checkerboardIntensity);

            _material.SetFloat(_skyboxQuantizationID, _properties.skyboxQuantization);
            var resolution = new Vector2(_properties.skyboxPixelization * (Screen.width / Screen.height), _properties.skyboxPixelization);
            _material.SetVector(_skyboxResolutionID, resolution);
            _material.SetFloat(_skyboxDitherScaleID, _properties.skyboxDitherScale);
            _material.SetFloat(_skyboxDitherIntensityID, _properties.skyboxDitherIntensity);
            _material.SetFloat(_skyboxContrastID, _properties.skyboxContrast);
            _material.SetFloat(_skyboxBrightnessID, _properties.skyboxBrightness);
        }
    }
}