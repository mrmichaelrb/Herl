#ifndef RENDERING_HLSL
#define RENDERING_HLSL

struct RenderState
{
  float2 ScreenUv;
  float3 RayStart;
  float3 RayDirection;
  half4 GeneratorCenterColor;
  half4 GeneratorAuraColor;
};

static const half3 SpectrumA = half3(0.5, 0.5, 0.5);
static const half3 SpectrumB = half3(0.5, 0.5, 0.5);
static const half3 SpectrumC = half3(1.0, 1.0, 1.0);
static const half3 SpectrumD = half3(0.0, 0.33, 0.67);

uniform float RenderDistanceMin;
uniform float RenderDistancePixelRatio;

uniform float TerrainFarClipDistance;

uniform half3 SunlightColor;
uniform half3 SunlightCelestialColor;
uniform half3 SunlightAmbientColor;
uniform float3 SunlightDirection;

#include "Common.hlsl"

float2 TransformEyeSpaceTex(float2 uv)
{
#if UNITY_SINGLE_PASS_STEREO
  float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
  uv = (uv - scaleOffset.zw) / scaleOffset.xy;
#endif
  return uv;
}

half3 ColorFromPalette(
  float t,
  half3 a,
  half3 b,
  half3 c,
  half3 d)
{
  return a + b * cos(TwoPi * (c * t + d));
}

half3 ColorFromSpectrum(float t)
{
  return ColorFromPalette(t, SpectrumA, SpectrumB, SpectrumC, SpectrumD);
}

void RenderColor(
  half4 color,
  inout half4 fragColor)
{
  fragColor.rgb = (color.rgb * color.a) + (fragColor.rgb * (1.0 - color.a));
  fragColor.a = (fragColor.a * (1.0 - color.a)) + color.a;
}

void RenderColor(
  half3 color,
  float alpha,
  inout half4 fragColor)
{
  half4 renderColor = half4(color.rgb, alpha);
  RenderColor(renderColor, fragColor);
}

void RenderColor(
  half3 color,
  inout half4 fragColor)
{
  fragColor.rgb = color;
  fragColor.a = 1.0;
}

void RenderColorAdditive(
  half4 color,
  inout half4 fragColor)
{
  fragColor.rgb = (color.rgb * color.a) + (fragColor.rgb * fragColor.a);
  fragColor.a = color.a + fragColor.a;
  fragColor = saturate(fragColor);
}

void RenderColorAdditive(
  half3 color,
  float alpha,
  inout half4 fragColor)
{
  half4 renderColor = half4(color.rgb, alpha);
  RenderColorAdditive(renderColor, fragColor);
}

void AverageLightDiffuse(
  float3 surfacePosition,
  float3 surfaceNormal,
  float3 lightPosition,
  half3 lightColor,
  float lightPower,
  float lightAttenuation,
  inout half3 renderColor)
{
  float3 lightDisplacement = surfacePosition - lightPosition;
  float3 lightDirection = normalize(lightDisplacement);
  float lightDistance = length(lightDisplacement);
  float lightProduct = saturate(-dot(lightDirection, surfaceNormal));

  half3 diffuseLight = (lightColor * lightPower * lightProduct) /
    pow(lightDistance, lightAttenuation);

  renderColor = (renderColor + diffuseLight) * 0.5;
}

void AddDirectionalLightDiffuse(
  float3 lightDirection,
  half3 lightColor,
  float3 surfaceNormal,
  inout half3 renderColor)
{
  float lightProduct = saturate(-dot(lightDirection, surfaceNormal));
  half3 diffuseLight = lightColor * lightProduct;

  renderColor *= diffuseLight;
}

void AddSunlightDiffuse(
  float3 surfaceNormal,
  float shadow,
  inout half3 renderColor)
{
  float sunlightProduct = saturate(-dot(SunlightDirection, surfaceNormal));
  sunlightProduct *= shadow;
  
  half3 diffuseSunlight = SunlightColor * sunlightProduct + SunlightAmbientColor;

  renderColor *= diffuseSunlight;
}

void AddSunlightDiffuse(
  float3 surfaceNormal,
  inout half3 renderColor)
{
  AddSunlightDiffuse(surfaceNormal, 1.0, renderColor);
}

void AddSunlightDiffuseNoAmbient(
  float3 surfaceNormal,
  inout half3 renderColor)
{
  AddDirectionalLightDiffuse(
    SunlightDirection,
    SunlightColor,
    surfaceNormal,
    renderColor);
}

void AddSunlightCelestialDiffuseNoAmbient(
  float3 surfaceNormal,
  inout half3 renderColor)
{
  AddDirectionalLightDiffuse(
    SunlightDirection,
    SunlightCelestialColor,
    surfaceNormal,
    renderColor);
}

void AddDirectionalLightSpecularPhong(
  float3 rayDirection,
  float3 lightDirection,
  half3 lightColor,
  float3 surfaceNormal,
  float reflectivity,
  float shininess,
  inout half3 renderColor)
{
  float3 reflectionDirection = reflect(lightDirection, surfaceNormal);
  float reflectionProduct = saturate(-dot(rayDirection, reflectionDirection));
  half3 specularColor = lightColor * reflectivity * NanToZero(pow(reflectionProduct, shininess));

  renderColor += specularColor;
}

void AddDirectionalLightSpecularBlinnPhong(
  float3 rayDirection,
  float3 lightDirection,
  half3 lightColor,
  float3 surfaceNormal,
  float reflectivity,
  float shininess,
  inout half3 renderColor)
{
  float3 halfDirection = normalize(lightDirection + rayDirection);
  float halfProduct = abs(dot(halfDirection, surfaceNormal));
  half3 specularColor = lightColor * reflectivity * pow(halfProduct, shininess);

  renderColor += specularColor;
}

void AddDirectionalLightSpecular(
  float3 rayDirection,
  float3 lightDirection,
  half3 lightColor,
  float3 surfaceNormal,
  float reflectivity,
  float shininess,
  inout half3 renderColor)
{
  AddDirectionalLightSpecularBlinnPhong(
    rayDirection,
    lightDirection,
    lightColor,
    surfaceNormal,
    reflectivity,
    shininess,
    renderColor);
}

void AddSunlightSpecular(
  float3 rayDirection,
  float3 surfaceNormal,
  float reflectivity,
  float shininess,
  inout half3 renderColor)
{
  AddDirectionalLightSpecular(
    rayDirection,
    SunlightDirection,
    SunlightColor,
    surfaceNormal,
    reflectivity,
    shininess,
    renderColor);
}

void AddSunlightDiffuseNoAmbientMinnaert(
  float3 rayDirection,
  float3 surfaceNormal,
  float reflectivity,
  inout half3 renderColor)
{
  float sunlightProduct = saturate(-dot(SunlightDirection, surfaceNormal));
  float minnaertProduct = saturate(dot(rayDirection, -surfaceNormal)) * reflectivity;

  renderColor *= minnaertProduct + sunlightProduct;
}

float MipMapLod(
  float3 rayDirection,
  float hitDistance,
  float surfaceSize,
  float3 surfaceNormal,
  float textureSize,
  float2 textureScale)
{
  // Diameter of pixel in world units at the distance being textured
  float pixelDiameter = hitDistance * RenderDistancePixelRatio;
  
  float surfaceProduct = abs(dot(rayDirection, surfaceNormal));
  float f = (textureSize * pixelDiameter) / (surfaceProduct * surfaceSize);

  float du = textureScale.x * f;
  float dv = textureScale.y * f;

  float dmin = min(du, dv);

  float lod = log2(dmin * 2.0);

  return lod;
}

// Fake, but fast
// Result is not normalized
float3 AddMapNormalNoiseGrid(
  float3 mapNormal,
  float2 uv,
  float gridResolution,
  float gridWidth,
  float intensity)
{
  uv *= gridResolution;

  float2 gridDistance = frac(uv);
  float2 gridPosition = floor(uv);
  
  if (gridDistance.x < gridWidth)
  {
    float s0 = round(Float2Rand(gridPosition));
    float s1 = round(Float2Rand(gridPosition + float2(-1.0, 0.0)));
    float alter = (s1 - s0) * intensity;
    mapNormal.z -= abs(alter);
    mapNormal.x += alter;
  }

  if (gridDistance.y < gridWidth)
  {
    float s0 = round(Float2Rand(gridPosition));
    float s1 = round(Float2Rand(gridPosition + float2(0.0, -1.0)));
    float alter = (s1 - s0) * intensity;
    mapNormal.z -= abs(alter);
    mapNormal.y += alter;
  }

  return mapNormal;
}

float3 ApplyMapNormal(
  float3 worldNormal,
  float3 worldTangent,
  float3 mapNormal)
{
  float3 bitangent = cross(worldNormal, worldTangent);
  float3x3 tbn = float3x3(worldTangent, bitangent, worldNormal);

  return mul(mapNormal, tbn);
}

float3 ApplyMapNormalUp(
  float3 worldNormal,
  float3 mapNormal)
{
  float3 worldTangent = normalize(cross(Up, worldNormal));
  return ApplyMapNormal(worldNormal, worldTangent, mapNormal);
}

float SmokeNoise(
  float3 position,
  int sampleCount,
  float density,
  float locationFactor)
{
  float noise = 0.0;

  for (int i = 0; i < sampleCount; i++)
  {
    noise += (sin(Float3NoiseTexture(position) * TwoPi) * 0.5 + 0.5) *
      density;

    density *= 0.5;
    position *= locationFactor;
  }

  return noise;
}

float FilamentNoise(
  float3 position,
  int sampleCount,
  float animationSpeed)
{
  float normalizer = 1.0 / Sqrt2;
  float density = 2.0;
  float densityReciprical = 1.0 / density;
  float noise = 0.0;
  float animationOffset = TIME_SECONDS * animationSpeed;

  for (int i = 0; i < sampleCount; i++)
  {
    noise += densityReciprical *
      (cos((position.x + animationOffset) * density) +
        sin((position.y + animationOffset) * density));

    density *= 2.0;
    densityReciprical *= 0.5;

    position.xy += float2(position.y, -position.x);
    position.xy *= normalizer;
    position.xz += float2(position.z, -position.x);
    position.xz *= normalizer;
  }

  return abs(noise);
}

void RenderOrb(
  RenderState r,
  float3 orbDirection,
  float3 orbColor,
  float orbSize,
  float orbHardness,
  inout half4 fragColor)
{
  float orbAmount = saturate(dot(r.RayDirection, orbDirection));
  orbAmount = saturate(pow(orbAmount + orbSize, orbHardness));

  RenderColor(orbColor, orbAmount, fragColor);
}

void RenderOrbReversed(
  RenderState r,
  float3 orbDirection,
  float3 orbColor,
  float orbSize,
  float orbHardness,
  inout half4 fragColor)
{
  float orbAmount = saturate(dot(r.RayDirection, orbDirection));
  orbAmount = 1.0 - saturate(pow(orbAmount + orbSize, orbHardness));

  RenderColor(orbColor, orbAmount, fragColor);
}

void RenderOrbAdditive(
  RenderState r,
  float3 orbDirection,
  float3 orbColor,
  float orbSize,
  float orbHardness,
  inout half4 fragColor)
{
  float orbAmount = saturate(dot(r.RayDirection, orbDirection));
  orbAmount = saturate(pow(orbAmount + orbSize, orbHardness));

  RenderColorAdditive(orbColor, orbAmount, fragColor);
}

float PixelRand(
  RenderState r)
{
  return Float3Rand((r.RayDirection * 1024.0) + r.RayStart);
}

#endif
