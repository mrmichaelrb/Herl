#ifndef WATER_HLSL
#define WATER_HLSL

#include "Common.hlsl"
#include "Rendering.hlsl"
#include "Terrain.hlsl"
#include "TerrainRendering.hlsl"

static const int WaterSampleCount = 4;
static const float WaterNormalSampleSize = 0.1;
static const float WaterNormalSampleSizeFar = 100.0;

static const float2x2 WaterNoiseRotation = float2x2(2.366089, 3.20834, -3.028429, 2.472888);

uniform half3 WaterDeepColor;
uniform float WaterReflectivity;
uniform float WaterShininess;
uniform float WaterHeight;
uniform float WaterDeepFloor;
uniform float WaterWaveHeightRatio;
uniform float WaterWaveSpeed;
uniform float WaterInitialNoiseScale;
uniform float WaterNoiseScale;
uniform float WaterReflectionDistanceFar;
uniform sampler2D WaterReflectionTexture;
uniform sampler2D WaterReflectionDistanceTexture;

float WaterAltitude(float2 location)
{
  float altitude = WaterCeiling;
  float waveHeight = WaterHeight * WaterWaveHeightRatio;
  float waveShift = TIME_SECONDS * WaterWaveSpeed;

  location *= WaterInitialNoiseScale;
  altitude -= abs(sin((Float2NoiseTexture(location + waveShift) - 0.5) * Pi)) * waveHeight;

  for (int i = 0; i < WaterSampleCount; i++)
  {
    location = mul(WaterNoiseRotation, location) * WaterNoiseScale;
    waveHeight *= AlmostHalf;
    altitude -= abs(sin((Float2NoiseTexture(location + waveShift) - 0.5) * Pi)) * waveHeight;
  }

  return altitude;
}

float HeightAboveWater(float3 position)
{
  float2 location = position.xz - WorldPosition.xz;

  float altitude = WaterAltitude(location);

  return position.y - altitude - WorldPosition.y;
}

float3 WaterNormal(float3 position, float hitDistance)
{
  float2 location = position.xz - WorldPosition.xz;

  float sampleOffset = SampleSize(hitDistance, WaterNormalSampleSize, WaterNormalSampleSizeFar);

  float3 waterNormal = float3(0.0, WaterAltitude(location + float2(0.0, sampleOffset)), sampleOffset);
  float3 vector1 = waterNormal - float3(sampleOffset, WaterAltitude(location + float2(sampleOffset, 0.0)), 0.0);
  float3 vector2 = waterNormal - float3(0.0, WaterAltitude(location), 0.0);

  waterNormal = cross(vector1, vector2);
  waterNormal = normalize(waterNormal);

  return waterNormal;
}

void RenderSceneFromWaterReflection(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor);

float2 WaterReflectionUv(RenderState r)
{
  // Single-wide right eye texture for now
  return TransformEyeSpaceTex(r.ScreenUv);
}

half3 WaterReflection(
  RenderState r,
  float waterDistance,
  float3 waterPosition,
  float3 waterNormal)
{
  float2 reflectionUv = WaterReflectionUv(r) + waterNormal.xz;

  float hitDistance = SampleTexture(WaterReflectionDistanceTexture, reflectionUv).r;
  half4 fragColor;

  if (isinf(hitDistance))
  {
    fragColor = NoColor;
  }
  else
  {
    hitDistance -= waterDistance;
    fragColor = SampleTexture(WaterReflectionTexture, reflectionUv);
  }
  
  r.RayStart = waterPosition;
  r.RayDirection = reflect(r.RayDirection, waterNormal);

  RenderSceneFromWaterReflection(r, hitDistance, fragColor);

  return fragColor.rgb;
}

void RenderWater(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  if (r.RayDirection.y < 0.0)
  {
    float relativeWaterCeiling = WaterCeiling + WorldPosition.y;
    float waterHitDistance = DistanceToHorizontalPlane(r.RayStart, r.RayDirection, relativeWaterCeiling);

    if (waterHitDistance < hitDistance)
    {
      float3 waterPosition = (waterHitDistance * r.RayDirection) + r.RayStart;
      float3 waterNormal = WaterNormal(waterPosition, waterHitDistance);

      float waterAmount = saturate((hitDistance - waterHitDistance) / WaterHeight);
      waterAmount = sqrt(waterAmount);

      float waterDepth = TerrainHeight(waterPosition.xz) - WaterCeiling;
      float waterDepthRatio = saturate(waterDepth / WaterDeepFloor);
      waterDepthRatio = sqrt(waterDepthRatio);

      half3 depthColor = lerp(WaterColor, WaterDeepColor, waterDepthRatio);
      half3 reflectionColor = WaterReflection(r, waterHitDistance, waterPosition, waterNormal);
      half3 surfaceColor = lerp(depthColor, reflectionColor, WaterReflectivity);

      hitDistance = waterHitDistance;

      float meshShadow = ShadowMap(waterPosition, Up, hitDistance);
      float cloudShadow = CloudShadow(waterPosition);
      float shadow = min(meshShadow, cloudShadow);
      
      AddSunlightDiffuse(waterNormal, shadow, surfaceColor);
      AddSunlightSpecular(r.RayDirection, waterNormal, WaterReflectivity, WaterShininess, surfaceColor);
      AddPowerBallLight(waterPosition, waterNormal, surfaceColor);

      RenderColor(surfaceColor, waterAmount, fragColor);
    }
  }
}

#endif
