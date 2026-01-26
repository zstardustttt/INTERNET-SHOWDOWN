Shader "Custom/Speedlines"
{   
    Properties
    {
        _alpha("alpha", Float) = 0
        [NoScaleOffset]_speedlines_rt("speedlines_rt", 2D) = "white" {}
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        Pass
        {
            Name "Speedlines"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
            float _alpha;
            TEXTURE2D(_speedlines_rt);
            SAMPLER(sampler_speedlines_rt);
            CBUFFER_END

            float4 Frag (Varyings input) : SV_Target
            {
                half speedlines = SAMPLE_TEXTURE2D(_speedlines_rt, sampler_speedlines_rt, input.texcoord) * _alpha;
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                return color + speedlines;
            }
            
            ENDHLSL
        }
    }
}
