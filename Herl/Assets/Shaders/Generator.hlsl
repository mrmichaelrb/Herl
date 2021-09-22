// Inspired by https://www.shadertoy.com/view/llVXRd

#ifndef GENERATOR_HLSL
#define GENERATOR_HLSL

#include "Common.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"

static const half3 GeneratorPodColor = half3(0.5, 0.5, 0.5);
static const half3 GeneratorFrameColor = half3(0.125, 0.125, 0.125);
static const half4 GeneratorCenterColor = half4(2.0, 2.0, 2.0, 0.5);
static const half4 GeneratorAuraColor = half4(4.0, 4.0, 4.0, 0.25);
static const float GeneratorPodIntensity = 2.0;
static const float GeneratorFrameReflectivity = 0.125;
static const int GeneratorHitSteps = 30;
static const float GeneratorRadiusSpacingFactor = 1.0925;
static const float GeneratorRadius = 3000.0 * GeneratorRadiusSpacingFactor;
static const float GeneratorRadiusReciprical = 1.0 / GeneratorRadius;
static const float GeneratorEpsilon = 0.001;
static const float GeneratorCenterEpsilon = 0.1;
static const float GeneratorScaleY = 2.0;
static const float GeneratorScaleYReciprical = 1.0 / GeneratorScaleY;
static const float GeneratorBoundingSphereRadius = GeneratorRadius * GeneratorScaleY;
static const int GeneratorComplexityLevel = 5;
static const float GeneratorInternalSpacing = 2.1;
static const float GeneratorInternalSpacingDivisor = 1.0 / pow(GeneratorInternalSpacing, GeneratorComplexityLevel);
static const float GeneratorPodScale = 0.75;
static const float GeneratorNormalSampleSize = 0.005;
static const float GeneratorFrameThickness = 0.075;
static const float GeneratorAuraDistanceMax = 0.125;
static const float GeneratorAuraDistanceMaxReciprical = 1.0 / GeneratorAuraDistanceMax;
static const float GeneratorAuraSpeed = 5.5;
static const float GeneratorAuraScale = 20.0;
static const float GeneratorAuraInitialIntensityScale = 0.25;
static const float2x2 GeneratorAuraRotation = float2x2(0.78, 1.21, -1.21, 0.78);
static const int GeneratorAuraSampleCount = 3;

uniform float3 GeneratorPosition;

float GeneratorPodDistanceFolded(float3 position, float3 foldedPosition)
{
  foldedPosition = abs(foldedPosition);
  return foldedPosition.x + foldedPosition.y + foldedPosition.z - (GeneratorPodScale * sin(position.y * Pi + TIME_SECONDS));
}

float GeneratorFrameDistanceFolded(float3 position, float3 foldedPosition)
{
  foldedPosition = abs(foldedPosition);
  foldedPosition.x -= 1.0;
  float3 frameVector = float3(-1.0, 1.0, 0.5);
  float product = saturate(dot(foldedPosition, frameVector) / LengthSquared(frameVector));
  float distance = length(frameVector * product - foldedPosition);
  return distance - GeneratorFrameThickness;
}

float3 GeneratorFold(float3 position)
{
  position = abs(position) * GeneratorInternalSpacing;

  if (position.x < position.y)
  {
    position.xy = position.yx;
  }

  if (position.x < position.z)
  {
    position.xz = position.zx;
  }

  if (position.y < position.z)
  {
    position.yz = position.zy;
  }

  position.x -= 1.0;

  return position;
}

float3 GeneratorFoldedPosition(float3 position)
{
  position.y *= GeneratorScaleYReciprical;

  for (int i = 0; i < GeneratorComplexityLevel; i++)
  {
    position = GeneratorFold(position);
  }

  return position;
}

float GeneratorDistance(float3 position)
{
  float3 foldedPosition = GeneratorFoldedPosition(position);

  float podDistance = GeneratorPodDistanceFolded(position, foldedPosition);
  float frameDistance = GeneratorFrameDistanceFolded(position, foldedPosition);

  float distance = min(podDistance, frameDistance);

  return distance * GeneratorInternalSpacingDivisor;
}

float GeneratorFrameDistance(float3 position)
{
  float3 foldedPosition = GeneratorFoldedPosition(position);

  float distance = GeneratorFrameDistanceFolded(position, foldedPosition);

  return distance * GeneratorInternalSpacingDivisor;
}

float GeneratorDistanceWithComponent(float3 position, out bool hitPod)
{
  float3 foldedPosition = GeneratorFoldedPosition(position);

  float podDistance = GeneratorPodDistanceFolded(position, foldedPosition);
  float frameDistance = GeneratorFrameDistanceFolded(position, foldedPosition);

  float distance;

  if (podDistance < frameDistance)
  {
    distance = podDistance;
    hitPod = true;
  }
  else
  {
    distance = frameDistance;
    hitPod = false;
  }

  return distance * GeneratorInternalSpacingDivisor;
}

float3 GeneratorNormal(float3 position)
{
  return DistanceSurfaceNormal(GeneratorDistance, position, GeneratorNormalSampleSize);
}

float3 GeneratorFrameNormal(float3 position)
{
  return DistanceSurfaceNormal(GeneratorFrameDistance, position, GeneratorNormalSampleSize);
}

half3 GeneratorPositionPodColor(float3 surfacePosition)
{
  return ColorFromSpectrum(frac(TIME_SECONDS + surfacePosition.y)) * GeneratorPodIntensity;
}

float GeneratorAuraIntensity(float2 uv, float timeOffset)
{
  float intensity = 0.0;
  float insensityScale = GeneratorAuraInitialIntensityScale;

  for (int i = 0; i < GeneratorAuraSampleCount; i++)
  {
    intensity += sin(uv.x + sin(timeOffset + uv.y)) * insensityScale;
    insensityScale *= 0.5;
    uv = mul(uv, GeneratorAuraRotation);
  }

  return intensity;
}

void RenderGenerator(
  inout RenderState r,
  float3 position,
  inout float hitDistance,
  inout half4 fragColor)
{
  float generatorHitDistance;

  // Inside bounding sphere
  if (DistanceSquared(r.RayStart, position) < (GeneratorBoundingSphereRadius * GeneratorBoundingSphereRadius))
  {
    generatorHitDistance = 0.0;
  }
  // Hit test for bounding sphere
  else
  {
    generatorHitDistance = hitDistance;
    HitSphere(r.RayStart, r.RayDirection, position, GeneratorBoundingSphereRadius, generatorHitDistance);
  }

  if (generatorHitDistance < hitDistance)
  {
    float3 localRayStart = r.RayStart - position;
    localRayStart *= GeneratorRadiusReciprical;

    float3 stepPosition;
    float distance;
    float minDistance = 1.#INF;

    float stepDistance = generatorHitDistance * GeneratorRadiusReciprical;

    for (int i = 0; i < GeneratorHitSteps; i++)
    {
      stepPosition = (stepDistance * r.RayDirection) + localRayStart;

      bool hitPod;
      distance = GeneratorDistanceWithComponent(stepPosition, hitPod);

      minDistance = min(distance, minDistance);
      generatorHitDistance = stepDistance * GeneratorRadius;

      if (distance < GeneratorEpsilon)
      {
        if (generatorHitDistance < hitDistance)
        {
          hitDistance = generatorHitDistance;

          half3 surfaceColor;

          if (hitPod)
          {
            surfaceColor = GeneratorPositionPodColor(stepPosition);
          }
          else
          {
            surfaceColor = GeneratorFrameColor;

            float3 surfaceNormal = GeneratorFrameNormal(stepPosition);
            AddSunlightDiffuseNoAmbientMinnaert(r.RayDirection, surfaceNormal, GeneratorFrameReflectivity, surfaceColor);
          }

          RenderColor(surfaceColor, fragColor);
        }

        return;
      }

      stepDistance += distance;
    }

    if (distance < GeneratorCenterEpsilon)
    {
      if (generatorHitDistance < hitDistance)
      {
        r.GeneratorCenterColor = (GeneratorCenterColor * -length(stepPosition)) + GeneratorCenterColor;
      }
    }
    else if (minDistance < GeneratorAuraDistanceMax)
    {
      float3 auraDirection = normalize(localRayStart) + r.RayDirection;
      auraDirection = normalize(auraDirection);
      float2 uv = -float2(atan2(auraDirection.z, auraDirection.x), auraDirection.y);

      float auraIntensity = GeneratorAuraIntensity(uv * GeneratorAuraScale, TIME_SECONDS * GeneratorAuraSpeed);
      auraIntensity = lerp(auraIntensity, 0.0, minDistance * GeneratorAuraDistanceMaxReciprical);

      r.GeneratorAuraColor = GeneratorAuraColor * auraIntensity;
    }
  }
}

void RenderGeneratorGlow(
  RenderState r,
  float hitDistance,
  float3 position,
  inout half4 fragColor)
{
  if (DistanceSquared(r.RayStart, position) < (hitDistance * hitDistance))
  {
    RenderColor(r.GeneratorCenterColor, fragColor);
    RenderColor(r.GeneratorAuraColor, fragColor);
  }
}

#endif
