#ifndef SOBELOUTLINES_INCLUDED
#define SOBELOUTLINES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

static half2 sobelSamplePointsHalf[8] = {
    half2(-0.71, 0.71), half2(0, 1), half2(0.71, 0.71),
    half2(-1, 0), half2(1, 0),
    half2(-0.71, -0.71), half2(0, -1), half2(0.71, -0.71),
};

static half sobelXMatrixHalf[8] = {
    1, 0, -1,
    2, -2,
    1, 0, -1
};

static half sobelYMatrixHalf[8] = {
    1, 2, 1,
    0, 0,
    -1, -2, -1
};

half DepthSobel_half(half2 UV, half2 Thickness) {
    half2 sobel = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half depth = SampleSceneDepth(UV + sobelSamplePointsHalf[i] * Thickness);
        sobel += depth * half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);
    }

    return length(sobel);
}

half ColorSobel_half(half2 UV, half2 Thickness) {
    half2 sobelR = 0;
    half2 sobelG = 0;
    half2 sobelB = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half3 rgb = SampleSceneColor(UV + sobelSamplePointsHalf[i] * Thickness);
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelR += rgb.r * kernel;
        sobelG += rgb.g * kernel;
        sobelB += rgb.b * kernel;
    }

    return max(length(sobelR), max(length(sobelG), length(sobelB)));
}

half3 GetViewSpaceNormals_half(half2 UV) {
    half3 worldNormal = SampleSceneNormals(UV);
    return mul((half3x3)UNITY_MATRIX_V, worldNormal);
}

half NormalsSobel_half(half2 UV, half2 Thickness) {
    half2 sobelX = 0;
    half2 sobelY = 0;
    half2 sobelZ = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half3 viewNormal = (GetViewSpaceNormals_half(UV + sobelSamplePointsHalf[i] * Thickness) + 1) / 2;
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelX += viewNormal.x * kernel;
        sobelY += viewNormal.y * kernel;
        sobelZ += viewNormal.z * kernel;
    }

    return max(length(sobelX), max(length(sobelY), length(sobelZ)));
}

half3 ViewDirectionFromScreenUV_half(half2 In) {
    half2 p11_22 = half2(unity_CameraProjection._11, unity_CameraProjection._22);
    return -normalize(half3((In * 2 - 1) / p11_22, -1));
}

#endif