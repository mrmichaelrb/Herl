#ifndef CIRCLES_HLSL
#define CIRCLES_HLSL

#include "Common.hlsl"

void HitCircle(
  float3 rayStart,
  float3 rayDirection,
  float3 circlePosition,
  float4 circleRotation,
  float circleRadius,
  out float3 localRayStart,
  out float3 localRayDirection,
  out float2 hitUV,
  out float hitRadiusRatioSquared,
  inout float hitDistance)
{
  localRayStart = rayStart - circlePosition;
  localRayStart = RotateVector(localRayStart, circleRotation);

  localRayDirection = RotateVector(rayDirection, circleRotation);

  float circleHitDistance = DistanceToZOriginPlane(localRayStart, localRayDirection);

  if ((circleHitDistance > 0.0) && (circleHitDistance < hitDistance))
  {
    float2 hitLocation = IntersectionOnZOriginPlane(localRayStart, localRayDirection);
    float2 hitLocationScaled = hitLocation / circleRadius;

    hitRadiusRatioSquared = LengthSquared(hitLocationScaled);

    if (hitRadiusRatioSquared < 1.0)
    {
      hitDistance = circleHitDistance;
      hitUV = float2(hitLocationScaled.x + 0.5, hitLocationScaled.y + 0.5);
    }
  }
}

void HitRing(
  float3 rayStart,
  float3 rayDirection,
  float3 ringPosition,
  float4 ringRotation,
  float innerRadius,
  float outerRadius,
  out float3 localRayStart,
  out float3 localRayDirection,
  out float2 hitUV,
  out float hitRadiusRatioSquared,
  inout float hitDistance)
{
  localRayStart = rayStart - ringPosition;
  localRayStart = RotateVector(localRayStart, ringRotation);

  localRayDirection = RotateVector(rayDirection, ringRotation);

  float ringHitDistance = DistanceToZOriginPlane(localRayStart, localRayDirection);

  if ((ringHitDistance > 0.0) && (ringHitDistance < hitDistance))
  {
    float2 hitLocation = IntersectionOnZOriginPlane(localRayStart, localRayDirection);

    float2 hitInnerLocationScaled = hitLocation / innerRadius;
    float2 hitOuterLocationScaled = hitLocation / outerRadius;

    float hitInnerRadiusRatioSquared = LengthSquared(hitInnerLocationScaled);
    float hitOuterRadiusRatioSquared = LengthSquared(hitOuterLocationScaled);

    if ((hitInnerRadiusRatioSquared > 1.0) && (hitOuterRadiusRatioSquared < 1.0))
    {
      hitDistance = ringHitDistance;
      hitUV = float2(hitOuterLocationScaled.x + 0.5, hitOuterLocationScaled.y + 0.5);
    }
  }
}

#endif
