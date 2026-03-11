Shader "Hidden/MultiDepthOutlines"
{   
    Properties
    {
        _Color1("Color1", Color) = (0, 0, 0, 1)
        _Color2("Color2", Color) = (0, 0, 0, 1)
        _Color3("Color3", Color) = (0, 0, 0, 1)
        _Opacity("Opacity", Float) = 1
        _Thickness("Thickness", Vector, 2) = (0, 0, 0, 0)
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

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
            Name "MultiDepthOutlines"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
            half3 _Color1;
            half3 _Color2;
            half3 _Color3;
            half _Opacity;
            half2 _Thickness;
            CBUFFER_END

            TEXTURE2D(_MultiDepthOutlinesMask);

            float3 Frag (Varyings input) : SV_Target
            {
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                //return lerp(base, SAMPLE_TEXTURE2D(_MultiDepthOutlinesMask, sampler_LinearClamp, input.texcoord).a, 0.5);
                
                half maxR = 0;
                half maxG = 0;
                half maxB = 0;
                half minA = 1;
                
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 uv = input.texcoord + sobelSamplePointsHalf[i] * _Thickness;
                    half4 mask = SAMPLE_TEXTURE2D(_MultiDepthOutlinesMask, sampler_LinearClamp, uv).rgba;

                    maxR = max(mask.r, maxR);
                    maxG = max(mask.g, maxG);
                    maxB = max(mask.b, maxB);
                    minA = min(mask.a, minA);
                }

                half dominant = max(maxR, max(maxG, maxB));
                UNITY_BRANCH
                if (dominant == 0)
                {
                    return base;
                }
                
                half opacityMult = saturate(floor(minA) + _Opacity);
                half3 mask = SAMPLE_TEXTURE2D(_MultiDepthOutlinesMask, sampler_LinearClamp, input.texcoord).rgb;
                if (dominant == maxR)
                {
                    half opacity = 1 - ceil(mask.r);
                    return lerp(base, _Color1, opacity * opacityMult);
                }
                
                if (dominant == maxG)
                {
                    half opacity = 1 - ceil(mask.g);
                    return lerp(base, _Color2, opacity * opacityMult);
                }

                if (dominant == maxB)
                {
                    half opacity = 1 - ceil(mask.b);
                    return lerp(base, _Color3, opacity * opacityMult);
                }

                return base;
            }
            
            ENDHLSL
        }
    }
}
