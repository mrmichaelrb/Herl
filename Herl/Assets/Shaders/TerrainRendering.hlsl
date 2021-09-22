#ifndef TERRAINRENDERING_HLSL
#define TERRAINRENDERING_HLSL

#include "PowerBall.hlsl"
#include "Shadows.hlsl"
#include "Sky.hlsl"

static const float TerrainDistanceHintLastStepFactor = 0.95;
static const int TerrainHitDivisions = 8;
static const int TerrainHighDefSampleCount = 1;
static const float TerrainNormalSampleSizeFar = 10.0;
static const int TerrainDistanceHintUtilizationStep = 3;
static const int TerrainWaterReflectionMaxSteps = 64;
static const float TerrainHitWaterReflectionDeltaScale = 4.0;

static const float TerrainHighDefTreeHeightMin = 0.25;

uniform int TerrainStepsMax;
uniform float TerrainHitStepInitial;
uniform float TerrainHitDeltaHeightScale;
uniform float TerrainHitDeltaDistanceScale;
uniform float TerrainHitDeltaHintScale;

uniform float TerrainHighDefNoiseScale;
uniform float TerrainHighDefSampleVariation;
uniform float TerrainIridescenceFactor;
uniform float TerrainDistanceHintRendered;
uniform sampler2D TerrainDistanceHintTexture;

uniform int TerrainShadowStepsMax;
uniform float TerrainShadowHitStepInitial;
uniform float TerrainShadowHitStepDistanceMax;

uniform half3 WaterColor;
uniform float WaterFloor;
uniform float WaterBlendHeight;
uniform float WaterBlendCeiling;

uniform half3 BeachColor;
uniform float BeachHeight;
uniform float BeachCeiling;

uniform half3 LandColor;

uniform half3 GrassColor;
uniform half3 GrassColorVariation;
uniform float GrassCeiling;
uniform float GrassSlopeNormalMin;

uniform half3 TreeColor;
uniform float TreeHeight;
uniform float TreeLineFloor;
uniform float TreeLineHeight;
uniform float TreeNoiseScale;
uniform float TreeBranchNoiseScale;
uniform float TreeThicknessNoiseScale;
uniform float TreeThicknessFactor;

uniform half3 SnowColor;
uniform float SnowFloor;
uniform float SnowFadeHeight;
uniform float SnowReflectivity;
uniform float SnowShininess;
uniform float SnowSlopeNormalMin;

uniform half3 WaveColor;

float SampleSize(float hitDistance, float sampleSizeMin, float sampleSizeMax)
{
  float flattenRatio = saturate(hitDistance / AtmosphereCeiling);
  float sampleSize = lerp(sampleSizeMin, sampleSizeMax, flattenRatio);

  return sampleSize;
}

float TreeAltitudeFactor(float altitude)
{
  float altitudeFactor = (altitude - TreeLineFloor) / TreeLineHeight;
  altitudeFactor = saturate(altitudeFactor);

  return altitudeFactor;
}

float TreeBranchNoise(float2 location, float altitudeFactor)
{
  float treeBranchNoise = Float2NoiseTexture(location * TreeBranchNoiseScale);
  float treeThicknessNoise = Float2NoiseTexture(location * TreeThicknessNoiseScale);

  altitudeFactor = PowerCurve(altitudeFactor, 0.5, 2.0);

  return altitudeFactor * altitudeFactor * treeBranchNoise * treeThicknessNoise;
}

float TreeHeightWithBranches(float2 location, float treeBranchNoise)
{
  float treeNoise = Float2NoiseTexture(location * TreeNoiseScale);
  return treeNoise * treeBranchNoise * TreeHeight;
}

float TerrainHeightHighDef(float2 location, float treeBranchNoise)
{
  float height = TerrainHeight(location);

  float2x2 rotation = float2x2(TerrainInitialNoiseRotation);
  
  float treeSampleHeight = TreeHeightWithBranches(location, treeBranchNoise);
  height += treeSampleHeight;

  if (treeSampleHeight > TerrainHighDefTreeHeightMin)
  {
    return height;
  }

  float2 sampleLocation = location * TerrainHighDefNoiseScale;

  float variation = TerrainHighDefSampleVariation;

  height += variation * Float2NoiseTexture(sampleLocation);

  for (int i = 1; i < TerrainHighDefSampleCount; i++)
  {
    variation *= TerrainHighDefSampleVariation;
    sampleLocation = mul(rotation, sampleLocation);
    height += variation * Float2NoiseTexture(sampleLocation);
  }

  return height;
}

float HeightAboveTerrainWithTreeBranches(
  float3 position,
  out float treeBranchNoise,
  out float treeSampleHeight)
{
  float2 location = position.xz;
  float height = TerrainHeight(location);

  float altitudeFactor = TreeAltitudeFactor(height);

  if (altitudeFactor > 0.0)
  {
    float2 sampleLocation = location - WorldPosition.xz;
    treeBranchNoise = TreeBranchNoise(sampleLocation, altitudeFactor);
    treeSampleHeight = TreeHeightWithBranches(sampleLocation, treeBranchNoise);
    height += treeSampleHeight;
  }
  else
  {
    treeBranchNoise = 0.0;
    treeSampleHeight = 0.0;
  }

  return position.y - height - WorldPosition.y;
}

float RefinedTerrainHitDistance(
  RenderState r,
  float beforeDistance,
  float afterDistance,
  out float treeBranchNoise,
  out float treeSampleHeight)
{
  float halfway;

  for (int i = 0; i < TerrainHitDivisions; i++)
  {
    halfway = (beforeDistance + afterDistance) * 0.5;

    float3 stepPosition = (halfway * r.RayDirection) + r.RayStart;

    float halfwayHeight = HeightAboveTerrainWithTreeBranches(
      stepPosition,
      treeBranchNoise,
      treeSampleHeight);

    if (halfwayHeight < TerrainHitEpsilon)
    {
      beforeDistance = halfway;
    }
    else
    {
      afterDistance = halfway;
    }
  }

  return beforeDistance;
}

float TerrainDistanceHint(RenderState r)
{
  if (TerrainDistanceHintRendered)
  {
    return SampleTexture(TerrainDistanceHintTexture, r.ScreenUv).r;
  }
  else
  {
    return 0.0;
  }
}

bool RoughTerrainHitDistance(
  RenderState r,
  float maxDistance,
  inout float stepDistance,
  inout float lastStepDistance,
  out float treeBranchNoise,
  out float treeSampleHeight)
{
  bool hit = false;
  float hintDistance = -1.#INF;
  float relativeTerrainCeiling = TerrainCeiling + WorldPosition.y;
  float relativeWaterFloor = WaterFloor + WorldPosition.y;
  
  if (r.RayDirection.y < 0.0)
  {
    // Looking down above the terrain ceiling
    if (r.RayStart.y > relativeTerrainCeiling)
    {
      float terrainCeilingDistance = DistanceToHorizontalPlane(r.RayStart, r.RayDirection, relativeTerrainCeiling);
      stepDistance = terrainCeilingDistance;
      hintDistance = TerrainDistanceHint(r);

      if (hintDistance > stepDistance)
      {
        stepDistance = hintDistance;
        lastStepDistance = hintDistance * TerrainDistanceHintLastStepFactor;
      }
    }
  }
  else
  {
    // Looking up below the terrain ceiling
    if (r.RayStart.y < relativeTerrainCeiling)
    {
      float terrainCeilingDistance = DistanceToHorizontalPlane(r.RayStart, r.RayDirection, relativeTerrainCeiling);
      maxDistance = min(maxDistance, terrainCeilingDistance);
    }
    // Looking up above the terrain ceiling
    else
    {
      // This causes an invalid "use of potentially uninitialized variable" warning
      return false;
    }
  }

  float deltaHintScale = 1.0;

  for (int i = 0; i < TerrainStepsMax; i++)
  {
    if (stepDistance > maxDistance)
    {
      break;
    }
  
    float3 stepPosition = (stepDistance * r.RayDirection) + r.RayStart;

    float height = HeightAboveTerrainWithTreeBranches(
      stepPosition,
      treeBranchNoise,
      treeSampleHeight);

    if (height < TerrainHitEpsilon)
    {
      hit = true;
      break;
    }

    if (stepPosition.y < relativeWaterFloor)
    {
      break;
    }

    float heightDelta = height * TerrainHitDeltaHeightScale;
    float distanceDelta = stepDistance * TerrainHitDeltaDistanceScale;
    float stepDelta = heightDelta + distanceDelta;

    lastStepDistance = stepDistance;
    stepDistance = (stepDelta * deltaHintScale) + stepDistance;
    
    deltaHintScale = min(deltaHintScale + TerrainHitDeltaHintScale, 1.0);

    if (
      (i == TerrainDistanceHintUtilizationStep) &&
      (isinf(hintDistance)))
    {
      hintDistance = TerrainDistanceHint(r);

      if (hintDistance > stepDistance)
      {
        lastStepDistance = max(stepDistance, hintDistance * TerrainDistanceHintLastStepFactor);
        stepDistance = hintDistance;
        deltaHintScale = TerrainHitDeltaHintScale;
      }
    }
  }

  return hit;
}

bool RoughTerrainHitDistanceFromWaterReflection(
  RenderState r,
  float maxDistance,
  inout float stepDistance,
  out float treeBranchNoise,
  out float treeSampleHeight)
{
  bool hit = false;
  float relativeTerrainCeiling = TerrainCeiling + WorldPosition.y;

  float terrainCeilingDistance = DistanceToHorizontalPlane(r.RayStart, r.RayDirection, relativeTerrainCeiling);
  maxDistance = min(maxDistance, terrainCeilingDistance);

  for (int i = 0; i < TerrainWaterReflectionMaxSteps; i++)
  {
    if (stepDistance > maxDistance)
    {
      break;
    }
  
    float3 stepPosition = (stepDistance * r.RayDirection) + r.RayStart;

    float height = HeightAboveTerrainWithTreeBranches(
      stepPosition,
      treeBranchNoise,
      treeSampleHeight);

    if (height < TerrainHitEpsilon)
    {
      hit = true;
      break;
    }

    float heightDelta = height * TerrainHitDeltaHeightScale;
    float distanceDelta = stepDistance * TerrainHitDeltaDistanceScale;
    float stepDelta = distanceDelta + heightDelta;

    stepDistance = (stepDelta * TerrainHitWaterReflectionDeltaScale) + stepDistance;
  }

  return hit;
}

float3 TerrainNormalHighDef(float3 position, float hitDistance, float treeBranchNoise)
{
  float2 location = position.xz;

  float sampleOffset = SampleSize(hitDistance, TerrainNormalSampleSize, TerrainNormalSampleSizeFar);

  float3 terrainNormal = float3(0.0, TerrainHeightHighDef(location + float2(0.0, sampleOffset), treeBranchNoise), sampleOffset);
  float3 vector1 = terrainNormal - float3(sampleOffset, TerrainHeightHighDef(location + float2(sampleOffset, 0.0), treeBranchNoise), 0.0);
  float3 vector2 = terrainNormal - float3(0.0, TerrainHeightHighDef(location, treeBranchNoise), 0.0);

  terrainNormal = cross(vector1, vector2);
  terrainNormal = normalize(terrainNormal);

  return terrainNormal;
}

float TerrainShadow(float3 position)
{
  float3 localRayStart = position;
  float3 localRayDirection = Sun1Direction;

  float stepDistance = TerrainShadowHitStepInitial;
  
  bool hit = false;
  float relativeTerrainCeiling = TerrainCeiling + WorldPosition.y;
  
  float terrainCeilingDistance = DistanceToHorizontalPlane(localRayStart, localRayDirection, relativeTerrainCeiling);
  float maxDistance = min(TerrainShadowHitStepDistanceMax, terrainCeilingDistance);

  for (int i = 0; i < TerrainShadowStepsMax; i++)
  {
    float3 stepPosition = (stepDistance * localRayDirection) + localRayStart;

    float height = HeightAboveTerrain(stepPosition);

    if (height < TerrainHitEpsilon)
    {
      hit = true;
      break;
    }

    float heightDelta = height * TerrainHitDeltaHeightScale;
    float distanceDelta = stepDistance * TerrainHitDeltaDistanceScale;
    float stepDelta = heightDelta + distanceDelta;

    stepDistance += stepDelta;
    
    if (stepDistance > maxDistance)
    {
      break;
    }
  }

  if (hit)
  {
    return 0.0;
  }
  else
  {
    return 1.0;
  }
}

half3 TerrainColor(
  RenderState r,
  float3 terrainPosition,
  float3 terrainNormal,
  float hitDistance,
  float treeSampleHeight,
  bool includeShadows)
{
  half3 fragColor;

  float altitude = terrainPosition.y - WorldPosition.y;
  float slopeNormal = terrainNormal.y;

  // Trees
  if (treeSampleHeight > TreeThicknessFactor)
  {
    fragColor = TreeColor;
  }
  else
  {
    // Dirt and rocks
    fragColor = LandColor;

    // Grass
    if ((altitude < GrassCeiling) && (slopeNormal > GrassSlopeNormalMin))
    {
      float grassAmount = 1.0 - ((GrassCeiling - altitude) / GrassCeiling);
      half3 blendColor = (GrassColorVariation * grassAmount) + GrassColor;

      fragColor = lerp(blendColor, fragColor, grassAmount * grassAmount);
    }

    // Beach
    if (altitude < BeachCeiling)
    {
      float beachAmount = (BeachCeiling - altitude) / BeachHeight;

      fragColor = lerp(fragColor, BeachColor, beachAmount * beachAmount);
    }

    // Snow
    if ((altitude > SnowFloor) && (slopeNormal > SnowSlopeNormalMin))
    {
      float snowFade = saturate((altitude - SnowFloor) / SnowFadeHeight);

      fragColor = lerp(fragColor, SnowColor, snowFade);
      AddSunlightSpecular(r.RayDirection, terrainNormal, SnowReflectivity, SnowShininess, fragColor);
    }
  }

  // Underwater
  float waterAmount = saturate(((WaterBlendCeiling - altitude) / WaterBlendHeight) - 0.25);
  fragColor = lerp(fragColor, WaterColor, waterAmount );

  fragColor.rb += abs(reflect(terrainNormal, r.RayDirection).xz) * TerrainIridescenceFactor;

  float shadow = 1.0;
  
  if (includeShadows)
  {
    float meshShadow = ShadowMap(terrainPosition, terrainNormal, hitDistance);
    float cloudShadow = CloudShadow(terrainPosition);
    float terrainShadow = TerrainShadow(terrainPosition);
    
    shadow = min(min(meshShadow, cloudShadow), terrainShadow);
  }
  
  AddSunlightDiffuse(terrainNormal, shadow, fragColor);
  AddPowerBallLight(terrainPosition, terrainNormal, fragColor);

  float terrainWaveScale = TerrainWave * TerrainWave;
  terrainWaveScale *= terrainWaveScale;

  fragColor += (WaveColor * WaveIntensity * 0.125) * terrainWaveScale;

  return fragColor;
}

void RenderTerrain(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  float stepDistance = TerrainHitStepInitial;
  float lastStepDistance = 0.0;
  float treeBranchNoise;
  float treeSampleHeight;

  bool hit;

  hit = RoughTerrainHitDistance(
    r,
    hitDistance,
    stepDistance,
    lastStepDistance,
    treeBranchNoise,
    treeSampleHeight);

  if (hit)
  {
    float terrainHitDistance = RefinedTerrainHitDistance(
      r,
      stepDistance,
      lastStepDistance,
      treeBranchNoise,
      treeSampleHeight);

    float3 terrainPosition = (terrainHitDistance * r.RayDirection) + r.RayStart;
    float relativeWaterFloor = WaterFloor + WorldPosition.y;

    if (terrainPosition.y > relativeWaterFloor)
    {
      hitDistance = terrainHitDistance;

      float3 terrainNormal = TerrainNormalHighDef(terrainPosition, hitDistance, treeBranchNoise);

      half3 terrainColor = TerrainColor(
        r,
        terrainPosition,
        terrainNormal,
        hitDistance,
        treeSampleHeight,
        true);

      RenderColor(terrainColor, fragColor);
    }
  }
}

void RenderTerrainFromWaterReflection(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  float stepDistance = TerrainHitStepInitial;
  float treeBranchNoise;
  float treeSampleHeight;

  bool hit;

  hit = RoughTerrainHitDistanceFromWaterReflection(
    r,
    hitDistance,
    stepDistance,
    treeBranchNoise,
    treeSampleHeight);

  if (hit)
  {
    hitDistance = stepDistance;

    float3 terrainPosition = (hitDistance * r.RayDirection) + r.RayStart;
    float3 terrainNormal = TerrainNormalHighDef(terrainPosition, hitDistance, treeBranchNoise);

    half3 terrainColor = TerrainColor(
      r,
      terrainPosition,
      terrainNormal,
      hitDistance,
      treeSampleHeight,
      false);

    RenderColor(terrainColor, fragColor);
  }
}

#endif
