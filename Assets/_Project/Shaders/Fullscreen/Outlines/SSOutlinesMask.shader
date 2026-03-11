Shader "Hidden/SSOutlinesMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "SSOutlinesMask"
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" 
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half2 frag(Varyings input) : SV_Target
            {
                half2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                half sceneDepth = Linear01Depth(SampleSceneDepth(screenUV), _ZBufferParams);
                half fragDepth = Linear01Depth(input.positionCS.z, _ZBufferParams);
                return half2(fragDepth > sceneDepth ? 0 : 1, fragDepth);
            }
            ENDHLSL
        }
    }
} 