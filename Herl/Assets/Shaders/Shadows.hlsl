#ifndef SHADOWS_HLSL
#define SHADOWS_HLSL

#include "Rendering.hlsl"

uniform float MainLightShadowRendered;
uniform float MainLightShadowDistance;
uniform sampler2D MainLightShadowTexture;

half4 ComputeCascadeWeights(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    half4 weights = half4(distances2 < _CascadeShadowSplitSphereRadii);

    return weights;
}

half ComputeCascadeIndex(half4 weights)
{
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return 4 - dot(weights, half4(4, 3, 2, 1));
}

float4 TransformWorldToShadowCoord(float3 positionWS, half4 weights)
{
#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    half cascadeIndex = ComputeCascadeIndex(weights);
    return mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0));
#else
    return mul(_MainLightWorldToShadow[0], float4(positionWS, 1.0));
#endif
}

float MainLightShadow(float2 uv)
{
  return SampleTexture(MainLightShadowTexture, uv.xy).r;
}

void AddShadowCascade(float3 position, int index, half weight, inout float shadowSample, inout float sampleCount)
{
  if (weight > 0.0)
  {
    float4 shadowCoord = mul(_MainLightWorldToShadow[index], float4(position, 1.0));

    if ((all(shadowCoord.xy > 0.0)) && (all(shadowCoord.xy < 1.0)))
    {
      shadowSample += MainLightShadow(shadowCoord.xy);
      sampleCount++;
    }
  }
}

float ShadowMapSingleCascade(float3 position, float distance)
{
  float shadow = 1.0;
  float4 shadowCoord = TransformWorldToShadowCoord(position);

  if (!BEYOND_SHADOW_FAR(shadowCoord))
  {
    float aboveShadow = SAMPLE_TEXTURE2D_SHADOW(
      _MainLightShadowmapTexture,
      sampler_MainLightShadowmapTexture,
      shadowCoord.xyz);

    if (!aboveShadow)
    {
      float shadowSample = MainLightShadow(shadowCoord.xy);
      float shadowAmount = saturate(distance / MainLightShadowDistance);
      shadow = lerp(shadowSample, 1.0, shadowAmount);
    }
  }

  return shadow;
}

float ShadowMapMultipleCascades(float3 position, float distance)
{
  float shadow = 1.0;

  half4 weights = ComputeCascadeWeights(position);
  float4 shadowCoord = TransformWorldToShadowCoord(position, weights);

  if (!BEYOND_SHADOW_FAR(shadowCoord))
  {
    float aboveShadow = SAMPLE_TEXTURE2D_SHADOW(
      _MainLightShadowmapTexture,
      sampler_MainLightShadowmapTexture,
      shadowCoord.xyz);

    if (!aboveShadow)
    {
      float shadowSample = 0.0;
      float sampleCount = 0.0;

      AddShadowCascade(position, 0, weights.x, shadowSample, sampleCount);
      AddShadowCascade(position, 1, weights.y, shadowSample, sampleCount);
      AddShadowCascade(position, 2, weights.z, shadowSample, sampleCount);
      AddShadowCascade(position, 3, weights.w, shadowSample, sampleCount);

      shadowSample /= sampleCount;

      float shadowAmount = saturate(distance / MainLightShadowDistance);
      shadow = lerp(shadowSample, 1.0, shadowAmount);
    }
  }

  return shadow;
}

float ShadowMap(float3 position, float3 surfaceNormal, float distance)
{
  if (
    (MainLightShadowRendered) &&
    (distance < MainLightShadowDistance) &&
    (dot(surfaceNormal, SunlightDirection) < 0.0)
    )
  {
#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    return ShadowMapMultipleCascades(position, distance);
#else
    return ShadowMapSingleCascade(position, distance);
#endif
  }
  else
  {
    return 1.0;
  }
}

#endif
