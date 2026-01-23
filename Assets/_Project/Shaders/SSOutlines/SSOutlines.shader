Shader "Custom/SSOutlines"
{   
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _Thickness("Thickness", Float) = 0

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
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Assets/_Project/Shaders/SSOutlines/SSOutlinesInclude.hlsl"
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
            #define REQUIRE_NORMAL_TEXTURE

            CBUFFER_START(UnityPerMaterial)
            half _Thickness;
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
            CBUFFER_END

            half FineTuneEdgeDetection(half sobel, half strength, half thickness, half threshold)
            {
                return mul(pow(smoothstep(0, threshold, sobel), thickness), strength);
            }

            half SampleRawDepth(half2 uv)
            {
                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
            }

            half SampleEyeDepth(half2 uv)
            {
                if (unity_OrthoParams.w == 1.0)
                {
                    return LinearEyeDepth(ComputeWorldSpacePosition(uv, SampleRawDepth(uv), UNITY_MATRIX_I_VP), UNITY_MATRIX_V);
                }
                else
                {
                    return LinearEyeDepth(SampleRawDepth(uv), _ZBufferParams);
                }
            }

            half GetOutlinesBlendOpacity(half2 uv)
            {
                half uvRawDepth = SampleRawDepth(uv);
                half2 thickness = half2(_Thickness, mul(_Thickness, _ScreenParams.x / _ScreenParams.y)) / 100; 
                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, SampleEyeDepth(uv));
                
                half depthEdge;
                if (_EnableDepth)
                {
                    half depthSobel = DepthSobel_half(uv, thickness);
                    half3 vsnorm = GetViewSpaceNormals_half(uv);
                    half3 viewdir = ViewDirectionFromScreenUV_half(uv);
                    half depthThreshold = mul(uvRawDepth, lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))));
                    depthEdge = FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);
                }
                else
                {
                    depthEdge = 0;
                }

                half colorEdge;
                if (_EnableColor)
                {
                    half colorSobel = ColorSobel_half(uv, thickness);
                    half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                    if (uvRawDepth == 0)
                    {
                        colorEdge = 0;
                    }
                    else
                    {
                        colorEdge = FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);
                    }
                }
                else
                {
                    colorEdge = 0;
                }
                
                half normalsEdge;
                if (_EnableNormals)
                {
                    half normalsSobel = NormalsSobel_half(uv, thickness);
                    half normalsThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                    normalsEdge = FineTuneEdgeDetection(normalsSobel, _NormalsStrength, _NormalsThickness, normalsThreshold);
                }
                else 
                {
                    normalsEdge = 0;
                }
                
                half outline = max(depthEdge, max(colorEdge, normalsEdge));
                return mul(_Color.a, outline);
            }

            float4 Frag (Varyings input) : SV_Target
            {
                float4 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                half opacity = GetOutlinesBlendOpacity(input.texcoord);
                float4 color = lerp(base, _Color, opacity);
                return color;
            }
            
            ENDHLSL
        }
    }
}
