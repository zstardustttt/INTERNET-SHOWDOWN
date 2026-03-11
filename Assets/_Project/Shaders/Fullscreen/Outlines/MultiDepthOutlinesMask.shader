Shader "Hidden/MultiDepthOutlinesMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Name "MultiDepthOutlinesMask"
        Tags { "LightMode"="UniversalForward" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
        };
        
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };
        ENDHLSL

        Pass
        {    
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half fragDepth = input.positionCS.z;
                return half4(fragDepth, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {    
            ColorMask G

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half fragDepth = input.positionCS.z;
                return half4(0, fragDepth, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {    
            ColorMask B

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half fragDepth = input.positionCS.z;
                return half4(0, 0, fragDepth, 0);
            }
            ENDHLSL
        }

        Pass
        {    
            ColorMask A

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz); 
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
} 