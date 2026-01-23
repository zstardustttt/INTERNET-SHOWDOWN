Shader "Custom/Dither"
{   
    Properties
    {
        _checkerboard_size("Checkerboard Size", Float) = 75
        _checkerboard_scroll_speed("Checkerboard Scroll Speed", Float) = 0.2
        _checkerboard_intensity("Checkerboard Intensity", Range(0, 1)) = 0
        _skybox_quantization("Skybox Quantization", Float) = 0
        _skybox_pixelization("Skybox Pixelization", Float) = 0
        _skybox_dither_scale("Skybox Dither Scale", Float) = 0
        _skybox_dither_intensity("Skybox Dither Intensity", Float) = 0
        _skybox_contrast("Skybox Contrast", Float) = 0
        _skybox_brightness("Skybox Brightness", Float) = 0
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        static half DITHER_THRESHOLDS[16] = {
            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
            13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
            4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
            16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
        };
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        Pass
        {
            Name "Dither"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            #define REQUIRE_DEPTH_TEXTURE

            CBUFFER_START(UnityPerMaterial)
            half _skybox_quantization;
            half _skybox_pixelization;
            half _skybox_dither_scale;
            half _skybox_dither_intensity;
            half _skybox_contrast;
            half _skybox_brightness;
            half _checkerboard_size;
            half _checkerboard_scroll_speed;
            half _checkerboard_intensity;
            CBUFFER_END

            half SampleRawDepth(half2 uv)
            {
                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
            }

            half3 RGB_HSV(half3 In)
            {
                half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                half4 P = lerp(half4(In.bg, K.wz), half4(In.gb, K.xy), step(In.b, In.g));
                half4 Q = lerp(half4(P.xyw, In.r), half4(In.r, P.yzx), step(P.x, In.r));
                half D = Q.x - min(Q.w, Q.y);
                half  E = 1e-10;
                half V = (D == 0) ? Q.x : (Q.x + E);
                return half3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), V);
            }

            half3 HSV_RGB(half3 In)
            {
                half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                half3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
                return In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
            }

            half Dither(half In, half2 uv)
            {
                half2 scuv = uv * _ScreenParams.xy;
                uint index = (uint(scuv.x) % 4) * 4 + uint(scuv.y) % 4;
                return In - DITHER_THRESHOLDS[index];
            }

            half3 GetSkyboxColor(half2 uv)
            {
                half2 resolution = half2(mul(_skybox_pixelization, _ScreenParams.x / _ScreenParams.y), _skybox_pixelization);
                half2 pixeluv = round(uv * resolution) / resolution;
                if (SampleRawDepth(pixeluv) != 0)
                {
                    pixeluv = uv;
                }
                
                half3 colorHsv = RGB_HSV(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, pixeluv));
                half valuePosterizationSteps = _skybox_quantization / sqrt(colorHsv.z);
                half posterizedValue = floor(colorHsv.z * valuePosterizationSteps) / valuePosterizationSteps * 2;

                half2 ditheruv = uv / _skybox_dither_scale;
                half ditheredValue = Dither(colorHsv.z, ditheruv) * _skybox_dither_intensity;

                half value = pow(max(0, posterizedValue + ditheredValue), _skybox_contrast) * _skybox_brightness;
                return HSV_RGB(half3(colorHsv.x, colorHsv.y, value));
            }

            float3 GetEnviromentColor(half2 uv)
            {
                half2 checkerboardUv = uv + _TimeParameters.x * _checkerboard_scroll_speed;
                half2 checkerboardFrequency = half2(_ScreenParams.x / _ScreenParams.y, 1) * _checkerboard_size;
                int2 checker = frac(checkerboardFrequency * checkerboardUv) > 0.5;
                half checkerboard = checker.x ^ checker.y ? 1 - _checkerboard_intensity : 1;
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * checkerboard;
            }

            float3 Frag (Varyings input) : SV_Target
            {
                if (SampleRawDepth(input.texcoord) == 0)
                {
                    return GetSkyboxColor(input.texcoord);
                }
                else
                {
                    return GetEnviromentColor(input.texcoord);
                }
            }
            
            ENDHLSL
        }
    }
}
