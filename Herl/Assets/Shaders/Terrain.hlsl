#ifndef TERRAIN_HLSL
#define TERRAIN_HLSL

#include "Common.hlsl"

static const int TerrainHighResSampleCount = 2;
static const int TerrainLowResSampleCount = 4;
static const float TerrainNormalSampleSize = 0.3;

uniform float WaterCeiling;

uniform float TerrainHitEpsilon;
uniform float TerrainCeiling;
uniform float TerrainInitialNoiseScale;
uniform float TerrainNoiseScale;
uniform float TerrainInitialOffset;
uniform float TerrainInitialFactor;
uniform float4 TerrainInitialNoiseRotation;
uniform float TerrainTwistiness;
uniform float TerrainSampleVariation;
uniform float TerrainHeightAdjustment;

uniform float TerrainDistanceHintHeightOffset;
uniform float TerrainDistanceHintDistanceOffset;

uniform int TerrainCollisionMaxSteps;
uniform float TerrainCollisionStepDelta;

// Concentric waves of terrain when powerball hits a base
uniform float2 WaveCenter;
uniform float WaveDistanceSquared;
uniform float WaveWidthReciprical;
uniform float WaveIntensity;
uniform float WaveHeight;

// Cached for rendering peaks of terrain waves with colors
static float TerrainWave;

float WaveScale(float2 location, float2 waveCenter, float waveDistanceSquared, float waveWidthReciprical)
{
  float distanceSquared = DistanceSquared(location, waveCenter);

  float wave1 = saturate(abs(waveDistanceSquared - distanceSquared) * waveWidthReciprical);
  float wave2 = saturate(abs(waveDistanceSquared - (distanceSquared * 0.25)) * waveWidthReciprical);
  float wave3 = saturate(abs(waveDistanceSquared - (distanceSquared * 0.0625)) * waveWidthReciprical);

  return 3.0 - (wave1 + wave2 + wave3);
}

float TerrainHeight(float2 location)
{
  location -= WorldPosition.xz;
  float2 sampleLocation = location * TerrainInitialNoiseScale;
  
  float2x2 rotation = float2x2(TerrainInitialNoiseRotation);

  float noise = Float2NoiseTextureHiRes(sampleLocation * TerrainNoiseScale);
  float variation = (noise * TerrainInitialFactor);
  variation += TerrainInitialOffset;
  variation = variation * variation * TerrainCeiling;
  
  noise = Float2NoiseTextureHiRes(sampleLocation);
  float height = variation * noise;

  for (int i = 1; i < TerrainHighResSampleCount; i++)
  {
    sampleLocation = mul(rotation, sampleLocation);
    variation *= TerrainSampleVariation;
    noise = Float2NoiseTextureHiRes(sampleLocation);
    height += variation * noise;
    rotation._m00 += TerrainTwistiness * noise;
  }

  for (int j = 0; j < TerrainLowResSampleCount; j++)
  {
    sampleLocation = mul(rotation, sampleLocation);
    variation *= TerrainSampleVariation;
    noise = Float2NoiseTextureHiRes(sampleLocation);
    height += variation * noise;
    rotation._m00 += TerrainTwistiness * noise;
  }

  TerrainWave = WaveScale(location, WaveCenter, WaveDistanceSquared, WaveWidthReciprical);

  return (TerrainWave * WaveHeight) + (height * TerrainHeightAdjustment);
}

float HeightAboveTerrain(float3 position)
{
  float2 location = position.xz;
  float height = TerrainHeight(location);

  return position.y - height - WorldPosition.y;
}

float3 TerrainNormalOffset(float3 position, float sampleOffset)
{
  float2 location = position.xz;

  float sampleOffset2 = Sqrt2 * sampleOffset;
  float sampleOffset3 = -sampleOffset2;

  // Out in front, on circle radius
  float3 terrainNormal = float3(0.0, TerrainHeight(location + float2(0.0, sampleOffset)), sampleOffset);

  // Back right, but on circle radius
  float3 vector1 = terrainNormal -
    float3(sampleOffset2, TerrainHeight(location + float2(sampleOffset2, sampleOffset3)), sampleOffset3);

  // Back left, but on circle radius
  float3 vector2 = terrainNormal -
  float3(sampleOffset3, TerrainHeight(location + float2(sampleOffset3, sampleOffset3)), sampleOffset3);

  terrainNormal = cross(vector1, vector2);
  terrainNormal = normalize(terrainNormal);

  return terrainNormal;
}

float3 TerrainNormal(float3 position)
{
  return TerrainNormalOffset(position, TerrainNormalSampleSize);
}

float TerrainCollisionDistance(float3 rayStart, float3 rayDirection, float maxDistance)
{
  float collisionDistance = 1.#INF;
  float stepDistance = 0.0;
  float stepDelta = max(TerrainCollisionStepDelta, (maxDistance / TerrainCollisionMaxSteps));
  float relativeTerrainCeiling = TerrainCeiling + WorldPosition.y;
  float relativeWaterCeiling = WaterCeiling + WorldPosition.y;
  
  if ((rayStart.y > relativeTerrainCeiling) && (rayDirection.y < 0.0))
  {
    stepDistance = DistanceToHorizontalPlane(rayStart, rayDirection, relativeTerrainCeiling);
  }

  for (int stepIndex = 0; stepIndex < TerrainCollisionMaxSteps; stepIndex++)
  {
    if (stepDistance > maxDistance)
    {
      break;
    }

    float3 stepPosition = (stepDistance * rayDirection) + rayStart;

    if ((stepPosition.y > relativeTerrainCeiling) && (rayDirection.y > 0.0))
    {
      break;
    }

    float height = HeightAboveTerrain(stepPosition);

    if (height < TerrainHitEpsilon)
    {
      collisionDistance = stepDistance;
      break;
    }

    // Rendering until water floor
    // Physics until water ceiling
    if (stepPosition.y < relativeWaterCeiling)
    {
      break;
    }

    stepDistance += stepDelta;
  }

  return collisionDistance;
}

#endif
