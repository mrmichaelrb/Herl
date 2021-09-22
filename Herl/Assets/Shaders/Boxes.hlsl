#ifndef BOXES_HLSL
#define BOXES_HLSL

#include "Common.hlsl"
#include "Rendering.hlsl"

float DistanceToABBMinMaxNear(
  float3 rayStart,
  float3 rayDirection,
  float3 boxMin,
  float3 boxMax)
{
  float3 rayDirectionReciprical = 1.0f / rayDirection;

  float3 t1 = (boxMin - rayStart) * rayDirectionReciprical;
  float3 t2 = (boxMax - rayStart) * rayDirectionReciprical;

  float tMin = max(max(min(t1.x, t2.x), min(t1.y, t2.y)), min(t1.z, t2.z));
  
  return tMin;
}

float DistanceToAABBMinMaxFar(
  float3 rayStart,
  float3 rayDirection,
  float3 boxMin,
  float3 boxMax)
{
  float3 rayDirectionReciprical = 1.0f / rayDirection;

  float3 t1 = (boxMin - rayStart) * rayDirectionReciprical;
  float3 t2 = (boxMax - rayStart) * rayDirectionReciprical;

  float tMax = min(min(max(t1.x, t2.x), max(t1.y, t2.y)), max(t1.z, t2.z));

  return tMax;
}

float3 AABBIntersectionNormal(
  float3 boxPosition,
  float3 boxScale,
  float3 hitPosition)
{
  float3 hitNormal = (hitPosition - boxPosition) / boxScale;
  return PrimaryAxis(hitNormal);
}

void HitAABBBothSidesMinMax(
  float3 rayStart,
  float3 rayDirection,
  float3 boxMin,
  float3 boxMax,
  inout float hitDistance,
  out float hitDistanceFar)
{
  float3 rayDirectionReciprical = 1.0f / rayDirection;

  // Plane intersections
  float3 t1 = (boxMin - rayStart) * rayDirectionReciprical;
  float3 t2 = (boxMax - rayStart) * rayDirectionReciprical;

  // Nearest and farthest plane intersections
  float tMin = max(max(min(t1.x, t2.x), min(t1.y, t2.y)), min(t1.z, t2.z));
  float tMax = min(min(max(t1.x, t2.x), max(t1.y, t2.y)), max(t1.z, t2.z));

  if ((tMax > 0) && (tMin < tMax))
  {
    hitDistance = tMin;
    hitDistanceFar = tMax;
  }
}

void HitAABBBothSides(
  float3 rayStart,
  float3 rayDirection,
  float3 boxPosition,
  float3 boxScale,
  inout float hitDistance,
  out float hitDistanceFar)
{
  float3 boxMin = (boxScale * -0.5) + boxPosition;
  float3 boxMax = (boxScale * 0.5) + boxPosition;

  HitAABBBothSidesMinMax(
    rayStart,
    rayDirection,
    boxMin,
    boxMax,
    hitDistance,
    hitDistanceFar);
}

void HitAABB(
  float3 rayStart,
  float3 rayDirection,
  float3 boxPosition,
  float3 boxScale,
  inout float hitDistance)
{
  float hitDistanceFar;
  
  HitAABBBothSides(
    rayStart,
    rayDirection,
    boxPosition,
    boxScale,
    hitDistance,
    hitDistanceFar);
}

void HitAABBCube(
  float3 rayStart,
  float3 rayDirection,
  float3 cubePosition,
  float cubeScale,
  inout float hitDistance)
{
  HitAABB(
    rayStart,
    rayDirection,
    cubePosition,
    float3(cubeScale, cubeScale, cubeScale),
    hitDistance);
}

void HitBox(
  float3 rayStart,
  float3 rayDirection,
  float3 boxPosition,
  float4 boxRotation,
  float3 boxScale,
  out float3 localRayStart,
  out float3 localRayDirection,
  inout float hitDistance)
{
  localRayStart = rayStart - boxPosition;
  localRayStart = RotateVector(localRayStart, boxRotation);

  localRayDirection = RotateVector(rayDirection, boxRotation);
  
  HitAABB(
    localRayStart,
    localRayDirection,
    Origin,
    boxScale,
    hitDistance);
}

void HitCube(
  float3 rayStart,
  float3 rayDirection,
  float3 cubePosition,
  float4 cubeRotation,
  float cubeScale,
  out float3 localRayStart,
  out float3 localRayDirection,
  inout float hitDistance)
{
  HitBox(
    rayStart,
    rayDirection,
    cubePosition,
    cubeRotation,
    float3(cubeScale, cubeScale, cubeScale),
    localRayStart,
    localRayDirection,
    hitDistance);
}

void RenderDiffuseAABB(
  RenderState r,
  float3 boxPosition,
  float3 boxScale,
  half3 boxColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float boxHitDistance = hitDistance;

  HitAABB(
    r.RayStart,
    r.RayDirection,
    boxPosition,
    boxScale,
    boxHitDistance);
    
  if (boxHitDistance < hitDistance)
  {
    hitDistance = boxHitDistance;
  
    float3 hitPosition = (boxHitDistance * r.RayDirection) + r.RayStart;
    float3 surfaceNormal = AABBIntersectionNormal(boxPosition, boxScale, hitPosition);
    
    AddSunlightDiffuse(surfaceNormal, boxColor);
    RenderColor(boxColor, fragColor);
  }
}

void RenderDiffuseBox(
  RenderState r,
  float3 boxPosition,
  float4 boxRotation,
  float3 boxScale,
  half3 boxColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float boxHitDistance = hitDistance;
  float3 localRayStart;
  float3 localRayDirection;

  HitBox(
    r.RayStart,
    r.RayDirection,
    boxPosition,
    boxRotation,
    boxScale,
    localRayStart,
    localRayDirection,
    boxHitDistance);
    
  if (boxHitDistance < hitDistance)
  {
    hitDistance = boxHitDistance;
  
    float4 normalRotation = InvertQuaternion(boxRotation);
  
    float3 localHitPosition = (boxHitDistance * localRayDirection) + localRayStart;
    float3 surfaceNormal = AABBIntersectionNormal(Origin, boxScale, localHitPosition);
    surfaceNormal = RotateVector(surfaceNormal, normalRotation);
    
    AddSunlightDiffuse(surfaceNormal, boxColor);
    RenderColor(boxColor, fragColor);
  }
}

void RenderDiffuseCube(
  RenderState r,
  float3 cubePosition,
  float4 cubeRotation,
  float cubeScale,
  half3 cubeColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  RenderDiffuseBox(
    r,
    cubePosition,
    cubeRotation,
    float3(cubeScale, cubeScale, cubeScale),
    cubeColor,
    hitDistance,
    fragColor);
}

#endif
