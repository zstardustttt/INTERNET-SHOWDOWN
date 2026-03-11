Shader "Custom/Dither"
{   
    Properties
    {
        _checkerboard_size("Checkerboard Size", Vector) = (0, 0, 0, 0)
        _checkerboard_scroll_speed("Checkerboard Scroll Speed", Float) = 0.2
        _checkerboard_intensity_inverted("Checkerboard Intensity Inverted", Range(0, 1)) = 0
        _skybox_quantization("Skybox Quantization", Float) = 0
        _skybox_resolution("Skybox Resolution", Vector, 2) = (0, 0, 0, 0)
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

        static const half DITHER_THRESHOLDS[16] = {
            1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
            13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
            4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
            16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
        };

        static const half4 RGB_HSV_K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
        static const half RGB_HSV_E = 1e-10;
        static const half4 HSV_RGB_K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
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
            half2 _skybox_resolution;
            half _skybox_dither_scale;
            half _skybox_dither_intensity;
            half _skybox_contrast;
            half _skybox_brightness;
            half2 _checkerboard_size;
            half _checkerboard_scroll_speed;
            half _checkerboard_intensity_inverted;
            CBUFFER_END

            inline half SampleRawDepth(half2 uv)
            {
                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
            }

            half3 RGB_HSV(half3 In)
            {
                half4 P = lerp(half4(In.bg, RGB_HSV_K.wz), half4(In.gb, RGB_HSV_K.xy), step(In.b, In.g));
                half4 Q = lerp(half4(P.xyw, In.r), half4(In.r, P.yzx), step(P.x, In.r));
                half D = Q.x - min(Q.w, Q.y);
                half V = (D == 0) ? Q.x : (Q.x + RGB_HSV_E);
                return half3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + RGB_HSV_E)), D / (Q.x + RGB_HSV_E), V);
            }

            half3 HSV_RGB(half3 In)
            {
                half3 P = abs(frac(In.xxx + HSV_RGB_K.xyz) * 6.0 - HSV_RGB_K.www);
                return In.z * lerp(HSV_RGB_K.xxx, saturate(P - HSV_RGB_K.xxx), In.y);
            }

            half Dither(half In, half2 uv)
            {
                half2 scuv = uv * _ScreenParams.xy;
                uint index = (uint(scuv.x) % 4) * 4 + uint(scuv.y) % 4;
                return In - DITHER_THRESHOLDS[index];
            }

            half3 GetSkyboxColor(half2 uv)
            {
                half2 pixeluv = round(uv * _skybox_resolution) / _skybox_resolution;
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
                int2 checker = frac(_checkerboard_size * checkerboardUv) > 0.5;
                half checkerboard = checker.x ^ checker.y ? lerp(1, _checkerboard_intensity_inverted, abs(uv.x * 2 - 1)) : 1;
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv) * checkerboard;
            }

            float3 Frag (Varyings input) : SV_Target
            {
                UNITY_BRANCH
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
