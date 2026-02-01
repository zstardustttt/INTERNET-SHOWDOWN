Shader "Custom/SimpleDepthSSOutlines"
{   
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _Thickness("Thickness", Vector, 2) = (0, 0, 0, 0)
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        static const half2 sobelSamplePointsHalf[8] = {
            half2(-0.71, 0.71), half2(0, 1), half2(0.71, 0.71),
            half2(-1, 0), half2(1, 0),
            half2(-0.71, -0.71), half2(0, -1), half2(0.71, -0.71),
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
            Name "SimpleDepthSSOutlines"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            #define REQUIRE_DEPTH_TEXTURE

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half2 _Thickness;
            CBUFFER_END

            float3 Frag (Varyings input) : SV_Target
            {
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                half depth = ceil(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord));
                if (depth != 0)
                {
                    return base;
                }

                half opacity = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 uv = input.texcoord + sobelSamplePointsHalf[i] * _Thickness;
                    opacity += ceil(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv));
                }

                return lerp(base, _Color, saturate(opacity) * _Color.a);
            }
            
            ENDHLSL
        }
    }
}
