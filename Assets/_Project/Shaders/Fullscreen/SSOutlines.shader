Shader "Custom/SSOutlines"
{   
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _Thickness("Thickness", Vector, 2) = (0, 0, 0, 0)

        _EnableDepth("EnableDepth", Float) = 0
        _DepthStrength("DepthStrength", Float) = 0
        _DepthThickness("DepthThickness", Float) = 0
        _DepthThreshold("DepthThreshold", Float) = 0

        _EnableColor("EnableColor", Float) = 0
        _ColorStrength("ColorStrength", Float) = 0
        _ColorThickness("ColorThickness", Float) = 0
        _ColorThreshold("ColorThreshold", Float) = 0

        _EnableNormals("EnableNormals", Float) = 0
        _NormalsStrength("NormalsStrength", Float) = 0
        _NormalsThickness("NormalsThickness", Float) = 0
        _NormalsThreshold("NormalsThreshold", Float) = 0
        
        _AcuteAngleStartDot("AcuteAngleStartDot", Float) = 0
        _AcuteDepthThreshold("AcuteDepthThreshold", Float) = 0
        _AdjustNearDepth("AdjustNearDepth", Float) = 0
        _AdjustFarDepth("AdjustFarDepth", Float) = 0
        _NormalsFarThreshold("NormalsFarThreshold", Float) = 0
        _ColorFarThreshold("ColorFarThreshold", Float) = 0
    }

    SubShader
    {
        HLSLINCLUDE
        #include "Assets/_Project/Shaders/Fullscreen/SSOutlinesInclude.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        Pass
        {
            Name "SSOutlinesShader"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            #define REQUIRE_DEPTH_TEXTURE

            CBUFFER_START(UnityPerMaterial)
            half2 _Thickness;
            half _DepthStrength;
            half _ColorStrength;
            half _DepthThickness;
            half _ColorThickness;
            half _DepthThreshold;
            half _ColorThreshold;
            half4 _Color;
            half _NormalsStrength;
            half _NormalsThickness;
            half _NormalsThreshold;
            half _AcuteAngleStartDot;
            half _AcuteDepthThreshold;
            half _AdjustFarDepth;
            half _AdjustNearDepth;
            half _ColorFarThreshold;
            half _NormalsFarThreshold;
            bool _EnableDepth;
            bool _EnableColor;
            bool _EnableNormals;

            TEXTURE2D(_FilteredOpaqueTexture);
            TEXTURE2D(_FilteredDepthTexture);
            TEXTURE2D(_FilteredNormalsTexture);
            CBUFFER_END

            half FineTuneEdgeDetection(half sobel, half strength, half thickness, half threshold)
            {
                return mul(pow(smoothstep(0, threshold, sobel), thickness), strength);
            }
            
            inline half SampleRawDepth(half2 uv)
            {
                return SAMPLE_TEXTURE2D(_FilteredDepthTexture, sampler_LinearClamp, uv);
            }

            half GetOutlinesBlendOpacity(half2 uv, half uvRawDepth, half sobelDepths[8])
            {
                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, LinearEyeDepth(uvRawDepth, _ZBufferParams));
                
                half outline = 0;
                if (_EnableDepth)
                {
                    half depthSobel = DepthSobel_half(uv, _Thickness, sobelDepths);
                    half3 vsnorm = GetViewSpaceNormals_half(uv, _FilteredNormalsTexture);
                    half3 viewdir = ViewDirectionFromScreenUV_half(uv);
                    half depthThreshold = mul(uvRawDepth, lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))));
                    outline += FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);
                }

                if (_EnableColor)
                {
                    half colorSobel = ColorSobel_half(uv, _Thickness, _FilteredOpaqueTexture);
                    half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                    if (uvRawDepth != 0)
                    {
                        outline += FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);
                    }
                }
                
                if (_EnableNormals)
                {
                    half normalsSobel = NormalsSobel_half(uv, _Thickness, _FilteredNormalsTexture);
                    half normalsThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                    outline += FineTuneEdgeDetection(normalsSobel, _NormalsStrength, _NormalsThickness, normalsThreshold);
                }
                
                return _Color.a * saturate(outline);
            }

            float3 Frag (Varyings input) : SV_Target
            {
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                half uvRawDepth = SampleRawDepth(input.texcoord);
                half sobelDepths[8];

                half maxDepth = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half depth = SampleRawDepth(input.texcoord + sobelSamplePointsHalf[i] * _Thickness);
                    sobelDepths[i] = depth;
                    if (depth > maxDepth) maxDepth = depth;
                }

                if (maxDepth >= SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord))
                {
                    half opacity = GetOutlinesBlendOpacity(input.texcoord, uvRawDepth, sobelDepths);
                    return lerp(base, _Color, opacity); 
                }

                return base; 
            }
            
            ENDHLSL
        }
    }
}
