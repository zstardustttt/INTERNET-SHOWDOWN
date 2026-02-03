#ifndef SOBELOUTLINES_INCLUDED
#define SOBELOUTLINES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

static const half2 sobelSamplePointsHalf[8] = {
    half2(-0.71, 0.71), half2(0, 1), half2(0.71, 0.71),
    half2(-1, 0), half2(1, 0),
    half2(-0.71, -0.71), half2(0, -1), half2(0.71, -0.71),
};

static const half sobelXMatrixHalf[8] = {
    1, 0, -1,
    2, -2,
    1, 0, -1
};

static const half sobelYMatrixHalf[8] = {
    1, 2, 1,
    0, 0,
    -1, -2, -1
};

half DepthSobel_half(half2 UV, half2 Thickness, half sobelMask[8], half sobelDepths[8]) {
    half2 sobel = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half depth = sobelDepths[i] * sobelMask[i];
        sobel += depth * half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);
    }

    return length(sobel);
}

half ColorSobel_half(half2 UV, half2 Thickness, half sobelMask[8]) {
    half2 sobelR = 0;
    half2 sobelG = 0;
    half2 sobelB = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half3 rgb = SampleSceneColor(UV + sobelSamplePointsHalf[i] * Thickness) * sobelMask[i];
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelR += rgb.r * kernel;
        sobelG += rgb.g * kernel;
        sobelB += rgb.b * kernel;
    }

    return sqrt(
    max(
        mul(sobelR.x, sobelR.x) + mul(sobelR.y, sobelR.y), 
        max(
            mul(sobelG.x, sobelG.x) + mul(sobelG.y, sobelG.y), 
            mul(sobelB.x, sobelB.x) + mul(sobelB.y, sobelB.y)
        )
    ));
}

half3 GetViewSpaceNormals_half(half2 UV) {
    half3 worldNormal = SampleSceneNormals(UV);
    return mul((half3x3)UNITY_MATRIX_V, worldNormal);
}

half NormalsSobel_half(half2 UV, half2 Thickness, half sobelMask[8]) {
    half2 sobelX = 0;
    half2 sobelY = 0;
    half2 sobelZ = 0;

    [unroll] for (int i = 0; i < 8; i++) {
        half3 viewNormal = (GetViewSpaceNormals_half(UV + sobelSamplePointsHalf[i] * Thickness) + 1) / 2 * sobelMask[i];
        half2 kernel = half2(sobelXMatrixHalf[i], sobelYMatrixHalf[i]);

        sobelX += viewNormal.x * kernel;
        sobelY += viewNormal.y * kernel;
        sobelZ += viewNormal.z * kernel;
    }

    return sqrt(
    max(
        mul(sobelX.x, sobelX.x) + mul(sobelX.y, sobelX.y), 
        max(
            mul(sobelY.x, sobelY.x) + mul(sobelY.y, sobelY.y), 
            mul(sobelZ.x, sobelZ.x) + mul(sobelZ.y, sobelZ.y)
        )
    ));
}

half3 ViewDirectionFromScreenUV_half(half2 In) {
    half2 p11_22 = half2(unity_CameraProjection._11, unity_CameraProjection._22);
    return -normalize(half3((In * 2 - 1) / p11_22, -1));
}

#endif