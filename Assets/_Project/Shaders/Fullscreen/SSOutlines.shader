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
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

        static const half2 sobelSamplePoints[8] = {
            half2(-0.71, 0.71), half2(0, 1), half2(0.71, 0.71),
            half2(-1, 0), half2(1, 0),
            half2(-0.71, -0.71), half2(0, -1), half2(0.71, -0.71),
        };

        static const half2 sobelMatrix[8] = {
            half2(1, 1), half2(0, 2), half2(-1, 1),
            half2(2, 0), half2(-2, 0),
            half2(1, -1), half2(0, -2), half2(-1, -1)
        };

        inline half FineTuneEdgeDetection(half sobel, half strength, half thickness, half threshold)
        {
            return mul(pow(smoothstep(0, threshold, sobel), thickness), strength);
        }

        inline half GetTripleSobel(half2 x, half2 y, half2 z)
        {
            return sqrt(max(dot(x, x), max(dot(y, y), dot(z, z))));
        }

        half3 GetViewSpaceNormals(half2 UV) {
            half3 worldNormal = SampleSceneNormals(UV);
            return mul((half3x3)UNITY_MATRIX_V, worldNormal);
        }

        half3 ViewDirectionFromScreenUV(half2 In) {
            half2 p11_22 = half2(unity_CameraProjection._11, unity_CameraProjection._22);
            return -normalize(half3((In * 2 - 1) / p11_22, -1));
        }

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

            TEXTURE2D(_OutlinesMask);
        CBUFFER_END
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        Blend Off
        ZTest Off
        ZWrite Off
        Name "SSOutlinesShader"
        Pass
        {
            // Everything
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;
                
                half2 depthSobelRaw = 0;

                half2 colorSobelRawR = 0;
                half2 colorSobelRawG = 0;
                half2 colorSobelRawB = 0;

                half2 normalSobelRawX = 0;
                half2 normalSobelRawY = 0;
                half2 normalSobelRawZ = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];
                    
                    depthSobelRaw += sobelMask.g * sobelMask.r * kernel;
                    
                    half3 sobelOpaque = SampleSceneColor(sobelUv) * sobelMask.r;
                    colorSobelRawR += sobelOpaque.r * kernel;
                    colorSobelRawG += sobelOpaque.g * kernel;
                    colorSobelRawB += sobelOpaque.b * kernel;
                    
                    half3 sobelNormal = (GetViewSpaceNormals(sobelUv) + 1) / 2 * sobelMask.r;
                    normalSobelRawX += sobelNormal.x * kernel;
                    normalSobelRawY += sobelNormal.y * kernel;
                    normalSobelRawZ += sobelNormal.z * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }
                
                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half depthSobel = length(depthSobelRaw);
                half colorSobel = GetTripleSobel(colorSobelRawR, colorSobelRawG, colorSobelRawB);
                half normalSobel = GetTripleSobel(normalSobelRawX, normalSobelRawY, normalSobelRawZ);

                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);

                half3 vsnorm = GetViewSpaceNormals(input.texcoord) * mask.r;
                half3 viewdir = ViewDirectionFromScreenUV(input.texcoord);
                half depthThreshold = lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))) * mask.g;
                half depthOutlineOpacity = FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);
                
                half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                half colorOutlineOpacity = ceil(mask.g) * FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);

                half normalThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                half normalOutlineOpacity = FineTuneEdgeDetection(normalSobel, _NormalsStrength, _NormalsThickness, normalThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * saturate(depthOutlineOpacity + colorOutlineOpacity + normalOutlineOpacity)
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Depth only
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;
                
                half2 depthSobelRaw = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord + sobelSamplePoints[i] * _Thickness).rg;
                    depthSobelRaw += sobelMask.g * sobelMask.r * sobelMatrix[i];

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }
                

                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half depthSobel = length(depthSobelRaw);
                half3 vsnorm = GetViewSpaceNormals(input.texcoord) * mask.r;
                half3 viewdir = ViewDirectionFromScreenUV(input.texcoord);
                half depthThreshold = lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))) * mask.g;
                half depthOutlineOpacity = FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * depthOutlineOpacity
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Color only
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;

                half2 colorSobelRawR = 0;
                half2 colorSobelRawG = 0;
                half2 colorSobelRawB = 0;

                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];
                    
                    half3 sobelOpaque = SampleSceneColor(sobelUv) * sobelMask.r;
                    colorSobelRawR += sobelOpaque.r * kernel;
                    colorSobelRawG += sobelOpaque.g * kernel;
                    colorSobelRawB += sobelOpaque.b * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }
                

                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half colorSobel = GetTripleSobel(colorSobelRawR, colorSobelRawG, colorSobelRawB);
                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);
                half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                half colorOutlineOpacity = ceil(mask.g) * FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * colorOutlineOpacity
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Normals only
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;

                half2 normalSobelRawX = 0;
                half2 normalSobelRawY = 0;
                half2 normalSobelRawZ = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];
                    
                    half3 sobelNormal = (GetViewSpaceNormals(sobelUv) + 1) / 2 * sobelMask.r;
                    normalSobelRawX += sobelNormal.x * kernel;
                    normalSobelRawY += sobelNormal.y * kernel;
                    normalSobelRawZ += sobelNormal.z * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }
                

                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half normalSobel = GetTripleSobel(normalSobelRawX, normalSobelRawY, normalSobelRawZ);
                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);
                half normalThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                half normalOutlineOpacity = FineTuneEdgeDetection(normalSobel, _NormalsStrength, _NormalsThickness, normalThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * normalOutlineOpacity
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Depth & Color
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;
                
                half2 depthSobelRaw = 0;

                half2 colorSobelRawR = 0;
                half2 colorSobelRawG = 0;
                half2 colorSobelRawB = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];
                    
                    depthSobelRaw += sobelMask.g * sobelMask.r * kernel;
                    
                    half3 sobelOpaque = SampleSceneColor(sobelUv) * sobelMask.r;
                    colorSobelRawR += sobelOpaque.r * kernel;
                    colorSobelRawG += sobelOpaque.g * kernel;
                    colorSobelRawB += sobelOpaque.b * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }


                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half depthSobel = length(depthSobelRaw);
                half colorSobel = GetTripleSobel(colorSobelRawR, colorSobelRawG, colorSobelRawB);

                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);

                half3 vsnorm = GetViewSpaceNormals(input.texcoord) * mask.r;
                half3 viewdir = ViewDirectionFromScreenUV(input.texcoord);
                half depthThreshold = lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))) * mask.g;
                half depthOutlineOpacity = FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);
                
                half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                half colorOutlineOpacity = ceil(mask.g) * FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * saturate(depthOutlineOpacity + colorOutlineOpacity)
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Color & Normals
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;

                half2 colorSobelRawR = 0;
                half2 colorSobelRawG = 0;
                half2 colorSobelRawB = 0;

                half2 normalSobelRawX = 0;
                half2 normalSobelRawY = 0;
                half2 normalSobelRawZ = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];

                    half3 sobelOpaque = SampleSceneColor(sobelUv) * sobelMask.r;
                    colorSobelRawR += sobelOpaque.r * kernel;
                    colorSobelRawG += sobelOpaque.g * kernel;
                    colorSobelRawB += sobelOpaque.b * kernel;
                    
                    half3 sobelNormal = (GetViewSpaceNormals(sobelUv) + 1) / 2 * sobelMask.r;
                    normalSobelRawX += sobelNormal.x * kernel;
                    normalSobelRawY += sobelNormal.y * kernel;
                    normalSobelRawZ += sobelNormal.z * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }


                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half colorSobel = GetTripleSobel(colorSobelRawR, colorSobelRawG, colorSobelRawB);
                half normalSobel = GetTripleSobel(normalSobelRawX, normalSobelRawY, normalSobelRawZ);

                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);
                
                half colorThreshold = lerp(_ColorThreshold, _ColorFarThreshold, depthAdjust);
                half colorOutlineOpacity = ceil(mask.g) * FineTuneEdgeDetection(colorSobel, _ColorStrength, _ColorThickness, colorThreshold);

                half normalThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                half normalOutlineOpacity = FineTuneEdgeDetection(normalSobel, _NormalsStrength, _NormalsThickness, normalThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * saturate(colorOutlineOpacity + normalOutlineOpacity)
                ); 
            }
            
            ENDHLSL
        }

        Pass
        {
            // Depth & Normals
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 Frag (Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, input.texcoord).rg;
                float3 base = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                
                half minMask = mask.r;
                half minDepth = mask.g;
                
                half2 depthSobelRaw = 0;

                half2 normalSobelRawX = 0;
                half2 normalSobelRawY = 0;
                half2 normalSobelRawZ = 0;
                [unroll] for (int i = 0; i < 8; i++) 
                {
                    half2 sobelUv = input.texcoord + sobelSamplePoints[i] * _Thickness;
                    half2 sobelMask = SAMPLE_TEXTURE2D(_OutlinesMask, sampler_LinearClamp, sobelUv).rg;
                    half2 kernel = sobelMatrix[i];
                    
                    depthSobelRaw += sobelMask.g * sobelMask.r * kernel;
                    
                    half3 sobelNormal = (GetViewSpaceNormals(sobelUv) + 1) / 2 * sobelMask.r;
                    normalSobelRawX += sobelNormal.x * kernel;
                    normalSobelRawY += sobelNormal.y * kernel;
                    normalSobelRawZ += sobelNormal.z * kernel;

                    minMask = min(sobelMask.r, minMask);
                    minDepth = min(sobelMask.g, minDepth);
                }
                

                if (floor(minMask) == 0 && minDepth != 0)
                {
                    return base;
                }

                half depthSobel = length(depthSobelRaw);
                half normalSobel = GetTripleSobel(normalSobelRawX, normalSobelRawY, normalSobelRawZ);

                half depthAdjust = smoothstep(_AdjustNearDepth, _AdjustFarDepth, mask.g * _ProjectionParams.z);

                half3 vsnorm = GetViewSpaceNormals(input.texcoord) * mask.r;
                half3 viewdir = ViewDirectionFromScreenUV(input.texcoord);
                half depthThreshold = lerp(_DepthThreshold, _AcuteDepthThreshold, smoothstep(_AcuteAngleStartDot, 1, 1 - dot(vsnorm, viewdir))) * mask.g;
                half depthOutlineOpacity = FineTuneEdgeDetection(depthSobel, _DepthStrength, _DepthThickness, depthThreshold);

                half normalThreshold = lerp(_NormalsThreshold, _NormalsFarThreshold, depthAdjust);
                half normalOutlineOpacity = FineTuneEdgeDetection(normalSobel, _NormalsStrength, _NormalsThickness, normalThreshold);
                
                return lerp
                (
                    base, 
                    _Color.rgb, 
                    _Color.a * saturate(depthOutlineOpacity + normalOutlineOpacity)
                ); 
            }
            
            ENDHLSL
        }
    }
}