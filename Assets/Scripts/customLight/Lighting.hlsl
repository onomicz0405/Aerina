#if !SHADERGRAPH_PREVIEW
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTERED_RENDERING
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile_fog
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma vertex ShadowPassVertex
#pragma fragment ShadowPassFragment
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#endif 

#ifndef MY_CUSTOM_LIGHTING
#define MY_CUSTOM_LIGHTING

#ifndef SHADERGRAPH_PREVIEW
#define CUSTOM_BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0
//called fifth
void CustomSampleShadow_GetTexelAreas_Tent_3x3(real offset, out real4 computedArea, out real4 computedAreaUncut)
{
    // Compute the exterior areas
    real offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
    computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
    computedAreaUncut.w = computedArea.w = offset01SquaredHalved;

    // Compute the middle areas
    // For Y : We find the area in Y of as if the left section of the isoceles triangle would
    // intersect the axis between Y and Z (ie where offset = 0).
    computedAreaUncut.y = SampleShadow_GetTriangleTexelArea(1.5 - offset);
    // This area is superior to the one we are looking for if (offset < 0) thus we need to
    // subtract the area of the triangle defined by (0,1.5-offset), (0,1.5+offset), (-offset,1.5).
    real clampedOffsetLeft = min(offset, 0);
    real areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
    computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;

    // We do the same for the Z but with the right part of the isoceles triangle
    computedAreaUncut.z = SampleShadow_GetTriangleTexelArea(1.5 + offset);
    real clampedOffsetRight = max(offset, 0);
    real areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
    computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
}

//called fourth
void CustomSampleShadow_GetTexelWeights_Tent_7x7(real offset, out real4 texelsWeightsA, out real4 texelsWeightsB)
{
    // See _UnityInternalGetAreaPerTexel_3TexelTriangleFilter for details.
    real4 computedArea_From3texelTriangle;
    real4 computedAreaUncut_From3texelTriangle;
    CustomSampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);

    // Triangle slope is 45 degree thus we can almost reuse the result of the 3 texel wide computation.
    // the 7 texel wide triangle can be seen as the 3 texel wide one but shifted up by two unit/texel.
    // 0.081632 is 1/(the triangle area)
    texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
    texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
    texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
    texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
    texelsWeightsB.x = 0.081632 * (computedArea_From3texelTriangle.z + 2);
    texelsWeightsB.y = 0.081632 * (computedAreaUncut_From3texelTriangle.z + 1);
    texelsWeightsB.z = 0.081632 * (computedAreaUncut_From3texelTriangle.z);
    texelsWeightsB.w = 0.081632 * (computedArea_From3texelTriangle.w);
}

//called third
void CustomSampleShadow_ComputeSamples_Tent_7x7(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[16], out real2 fetchesUV[16]) 
{
    // tent base is 7x7 base thus covering from 49 to 64 texels, thus we need 16 bilinear PCF fetches
    real2 tentCenterInTexelSpace = coord.xy * shadowMapTexture_TexelSize.zw;
    real2 centerOfFetchesInTexelSpace = floor(tentCenterInTexelSpace + 0.5);
    real2 offsetFromTentCenterToCenterOfFetches = tentCenterInTexelSpace - centerOfFetchesInTexelSpace;

    // find the weight of each texel based on the area of a 45 degree slop tent above each of them.
    real4 texelsWeightsU_A, texelsWeightsU_B;
    real4 texelsWeightsV_A, texelsWeightsV_B;
    CustomSampleShadow_GetTexelWeights_Tent_7x7(offsetFromTentCenterToCenterOfFetches.x, texelsWeightsU_A, texelsWeightsU_B);
    CustomSampleShadow_GetTexelWeights_Tent_7x7(offsetFromTentCenterToCenterOfFetches.y, texelsWeightsV_A, texelsWeightsV_B);

    // each fetch will cover a group of 2x2 texels, the weight of each group is the sum of the weights of the texels
    real4 fetchesWeightsU = real4(texelsWeightsU_A.xz, texelsWeightsU_B.xz) + real4(texelsWeightsU_A.yw, texelsWeightsU_B.yw);
    real4 fetchesWeightsV = real4(texelsWeightsV_A.xz, texelsWeightsV_B.xz) + real4(texelsWeightsV_A.yw, texelsWeightsV_B.yw);

    // move the PCF bilinear fetches to respect texels weights
    real4 fetchesOffsetsU = real4(texelsWeightsU_A.yw, texelsWeightsU_B.yw) / fetchesWeightsU.xyzw + real4(-3.5, -1.5, 0.5, 2.5);
    real4 fetchesOffsetsV = real4(texelsWeightsV_A.yw, texelsWeightsV_B.yw) / fetchesWeightsV.xyzw + real4(-3.5, -1.5, 0.5, 2.5);
    fetchesOffsetsU *=1;
    fetchesOffsetsV *=1;
    fetchesOffsetsU *= shadowMapTexture_TexelSize.xxxx;
    fetchesOffsetsV *= shadowMapTexture_TexelSize.yyyy;

    real2 bilinearFetchOrigin = centerOfFetchesInTexelSpace * shadowMapTexture_TexelSize.xy;

    fetchesUV[0] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.x);
    fetchesUV[1] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.x);
    fetchesUV[2] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.x);
    fetchesUV[3] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.x);
    fetchesUV[4] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.y);
    fetchesUV[5] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.y);
    fetchesUV[6] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.y);
    fetchesUV[7] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.y);
    fetchesUV[8] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.z);
    fetchesUV[9] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.z);
    fetchesUV[10] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.z);
    fetchesUV[11] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.z);
    fetchesUV[12] = bilinearFetchOrigin + real2(fetchesOffsetsU.x, fetchesOffsetsV.w);
    fetchesUV[13] = bilinearFetchOrigin + real2(fetchesOffsetsU.y, fetchesOffsetsV.w);
    fetchesUV[14] = bilinearFetchOrigin + real2(fetchesOffsetsU.z, fetchesOffsetsV.w);
    fetchesUV[15] = bilinearFetchOrigin + real2(fetchesOffsetsU.w, fetchesOffsetsV.w);

    fetchesWeights[0] = fetchesWeightsU.x * fetchesWeightsV.x;
    fetchesWeights[1] = fetchesWeightsU.y * fetchesWeightsV.x;
    fetchesWeights[2] = fetchesWeightsU.z * fetchesWeightsV.x;
    fetchesWeights[3] = fetchesWeightsU.w * fetchesWeightsV.x;
    fetchesWeights[4] = fetchesWeightsU.x * fetchesWeightsV.y;
    fetchesWeights[5] = fetchesWeightsU.y * fetchesWeightsV.y;
    fetchesWeights[6] = fetchesWeightsU.z * fetchesWeightsV.y;
    fetchesWeights[7] = fetchesWeightsU.w * fetchesWeightsV.y;
    fetchesWeights[8] = fetchesWeightsU.x * fetchesWeightsV.z;
    fetchesWeights[9] = fetchesWeightsU.y * fetchesWeightsV.z;
    fetchesWeights[10] = fetchesWeightsU.z * fetchesWeightsV.z;
    fetchesWeights[11] = fetchesWeightsU.w * fetchesWeightsV.z;
    fetchesWeights[12] = fetchesWeightsU.x * fetchesWeightsV.w;
    fetchesWeights[13] = fetchesWeightsU.y * fetchesWeightsV.w;
    fetchesWeights[14] = fetchesWeightsU.z * fetchesWeightsV.w;
    fetchesWeights[15] = fetchesWeightsU.w * fetchesWeightsV.w;
}

//called second
real CustomSampleShadowMapFiltered(TEXTURE2D_SHADOW_PARAM(ShadowMap, sampler_ShadowMap), float4 shadowCoord, ShadowSamplingData samplingData)
{
    real attenuation;

    float fetchesWeights[16];
    float2 fetchesUV[16];
    CustomSampleShadow_ComputeSamples_Tent_7x7(samplingData.shadowmapSize, shadowCoord.xy, fetchesWeights, fetchesUV);
    //7x7 requires 16 drops
    attenuation = fetchesWeights[0] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[0].xy, shadowCoord.z));
    attenuation += fetchesWeights[1] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[1].xy, shadowCoord.z));
    attenuation += fetchesWeights[2] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[2].xy, shadowCoord.z));
    attenuation += fetchesWeights[3] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[3].xy, shadowCoord.z));
    attenuation += fetchesWeights[4] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[4].xy, shadowCoord.z));
    attenuation += fetchesWeights[5] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[5].xy, shadowCoord.z));
    attenuation += fetchesWeights[6] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[6].xy, shadowCoord.z));
    attenuation += fetchesWeights[7] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[7].xy, shadowCoord.z));
    attenuation += fetchesWeights[8] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[8].xy, shadowCoord.z));
    attenuation += fetchesWeights[9] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[9].xy, shadowCoord.z));
    attenuation += fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[10].xy, shadowCoord.z));
    attenuation += fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[11].xy, shadowCoord.z));
    attenuation += fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[12].xy, shadowCoord.z));
    attenuation += fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[13].xy, shadowCoord.z));
    attenuation += fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[14].xy, shadowCoord.z));
    attenuation += fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(ShadowMap, sampler_ShadowMap, float3(fetchesUV[15].xy, shadowCoord.z));

    return CUSTOM_BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}
//called first
float CustomShadows_float(float4 shadowCoord)
{
    real attenuation;
    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    attenuation = CustomSampleShadowMapFiltered(TEXTURE2D_SHADOW_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoord, shadowSamplingData);
    return attenuation;
}
#endif
//main light shader graph node
void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float DistanceAtten, out float ShadowAtten, out float ShadowFade)
{
#if SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0);
    Color = float3(1, 1, 1);
    DistanceAtten = 1;
    ShadowAtten = 1;
    ShadowFade = 1;
#else
    Light light = GetMainLight(TransformWorldToShadowCoord(WorldPos));
    Direction = light.direction;
    Color = light.color;
    DistanceAtten = light.distanceAttenuation;
    ShadowAtten = light.shadowAttenuation;
    ShadowFade = CustomShadows_float(TransformWorldToShadowCoord(WorldPos));
#endif
    
}

void AdditionalLight_float(float3 WorldPos, float3 WorldNormal, float LightFadeOutRate, float2 Smoothing, out float3 LightsResult, out float3 ColorResult)
{
    LightsResult = 0.0f;
    ColorResult = 0.0f;
#ifndef SHADERGRAPH_PREVIEW
    int lightCount = GetAdditionalLightsCount();
    float3 TotalLight = (0,0,0);
    float3 TotalColor = (0,0,0);
    for (int i = 0; i < lightCount; ++i) 
    {
        Light light = GetAdditionalLight(i, WorldPos);

        //calculate light values
        float3 newLight = smoothstep(Smoothing.x, Smoothing.y, dot(light.direction, WorldNormal));
        float3 currentLight = TotalLight;
        float lightFallOff = min(max(sqrt(light.distanceAttenuation * 100)/LightFadeOutRate, 0), 0.8);
        TotalLight = max(currentLight, newLight - (1 - lightFallOff));

        //calculate colors
        float3 newColor = light.color * lightFallOff * newLight;
        float3 currentColor = TotalColor;
        TotalColor = max(currentColor, newColor - (1 - 0.6));

    }
    LightsResult = TotalLight;
    ColorResult = TotalColor;

#endif
    
}


#endif