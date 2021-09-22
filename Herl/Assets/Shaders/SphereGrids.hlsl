#ifndef SPHEREGRIDS_HLSL
#define SPHEREGRIDS_HLSL

#include "Common.hlsl"
#include "Boxes.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"
#include "Terrain.hlsl"

interface IGridSphere
{
  void Render(
    inout RenderState r,
    float3 position,
    float radius,
    inout float hitDistance,
    inout half4 fragColor);
};

// Assumptions:
// ringAltitude == (outerMin.y + outerMax.y) * 0.5
// sphereRadius == (outerMax.y - outerMin.y) * 0.5
void RenderSphereGridWithBoundingBoxes(
  RenderState r,
  int maxSteps,
  float gridAltitude,
  float3 outerMin,
  float3 outerMax,
  float sphereSpacing,
  float sphereRadius,
  IGridSphere gridSphere,
  inout float hitDistance,
  inout half4 fragColor)
{
  float maxDistance;
  float gridHitDistance;

  if (all(r.RayStart > outerMin) && all(r.RayStart < outerMax))
  {
    gridHitDistance = 0.0;
    maxDistance = DistanceToAABBMinMaxFar(
      r.RayStart,
      r.RayDirection,
      outerMin,
      outerMax);
  }
  else
  {
    gridHitDistance = hitDistance;
    HitAABBBothSidesMinMax(
      r.RayStart,
      r.RayDirection,
      outerMin,
      outerMax,
      gridHitDistance,
      maxDistance);
  }

  if (gridHitDistance < hitDistance)
  {
    float stepDistance = gridHitDistance;
    maxDistance = min(maxDistance, hitDistance);
    float stepDelta = SquareStep(r.RayDirection.xz) * sphereSpacing * 0.5f;

    for (int i = 0; i < maxSteps; i++)
    {
      if (stepDistance > maxDistance)
      {
        break;
      }
      
      float2 stepLocation = (stepDistance * r.RayDirection.xz) + r.RayStart.xz;

      float2 sphereLocation = stepLocation / sphereSpacing;
      sphereLocation = round(sphereLocation);
      sphereLocation *= sphereSpacing;

      float3 spherePosition = float3(sphereLocation.x, gridAltitude, sphereLocation.y);
      float sphereHitDistance = hitDistance;

      gridSphere.Render(
        r,
        spherePosition,
        sphereRadius,
        sphereHitDistance,
        fragColor);

      if (sphereHitDistance < hitDistance)
      {
        hitDistance = sphereHitDistance;
        break;
      }

      stepDistance += stepDelta;
    }
  }
}

void RenderSphereRingWithBoundingBoxes(
  RenderState r,
  int maxSteps,
  float ringAltitude,
  float3 outerMin,
  float3 outerMax,
  float2 innerMin,
  float2 innerMax,
  float sphereSpacing,
  float sphereRadius,
  IGridSphere gridSphere,
  inout float hitDistance,
  inout half4 fragColor)
{
  float maxDistance;
  float ringHitDistance;

  if (all(r.RayStart > outerMin) && all(r.RayStart < outerMax))
  {
    ringHitDistance = 0.0;
    maxDistance = DistanceToAABBMinMaxFar(
      r.RayStart,
      r.RayDirection,
      outerMin,
      outerMax);
  }
  else
  {
    ringHitDistance = hitDistance;
    HitAABBBothSidesMinMax(
      r.RayStart,
      r.RayDirection,
      outerMin,
      outerMax,
      ringHitDistance,
      maxDistance);
  }

  if (ringHitDistance < hitDistance)
  {
    float stepDistance = ringHitDistance;
    maxDistance = min(maxDistance, hitDistance);
    float stepDelta = SquareStep(r.RayDirection.xz) * sphereSpacing * 0.5f;

    for (int i = 0; i < maxSteps; i++)
    {
      if (stepDistance > maxDistance)
      {
        break;
      }
      
      float2 stepLocation = (stepDistance * r.RayDirection.xz) + r.RayStart.xz;

      float2 sphereLocation = stepLocation / sphereSpacing;
      sphereLocation = round(sphereLocation);
      sphereLocation *= sphereSpacing;

      if (any(sphereLocation < innerMin) || any(sphereLocation > innerMax))
      {
        float3 spherePosition = float3(sphereLocation.x, ringAltitude, sphereLocation.y);
        float sphereHitDistance = hitDistance;

        gridSphere.Render(
        r,
        spherePosition,
        sphereRadius,
        sphereHitDistance,
        fragColor);

        if (sphereHitDistance < hitDistance)
        {
          hitDistance = sphereHitDistance;
          break;
        }
      }

      stepDistance += stepDelta;
    }
  }
}

void RenderSphereGrid(
  RenderState r,
  float3 ringPosition,
  int outerRings,
  float sphereSpacing,
  float sphereRadius,
  IGridSphere gridSphere,
  inout float hitDistance,
  inout half4 fragColor)
{
  float outerSize = (sphereSpacing * outerRings) + sphereRadius;
  float3 outerMin = float3(-outerSize, -sphereRadius, -outerSize);
  float3 outerMax = float3(outerSize, sphereRadius, outerSize);
  outerMin += ringPosition;
  outerMax += ringPosition;

  RenderSphereGridWithBoundingBoxes(
    r,
    outerRings * 2,
    ringPosition.y,
    outerMin,
    outerMax,
    sphereSpacing,
    sphereRadius,
    gridSphere,
    hitDistance,
    fragColor);
}

void RenderSphereRing(
  RenderState r,
  float3 ringPosition,
  int outerRings,
  int innerEmptyRings,
  float sphereSpacing,
  float sphereRadius,
  IGridSphere gridSphere,
  inout float hitDistance,
  inout half4 fragColor)
{
  float outerSize = (sphereSpacing * outerRings) + sphereRadius;
  float3 outerMin = float3(-outerSize, -sphereRadius, -outerSize);
  float3 outerMax = float3(outerSize, sphereRadius, outerSize);
  outerMin += ringPosition;
  outerMax += ringPosition;

  float innerSize = (sphereSpacing * innerEmptyRings) - sphereRadius;
  float2 innerMin = float2(-innerSize, -innerSize);
  float2 innerMax = float2(innerSize, innerSize);
  innerMin += ringPosition.xz;
  innerMax += ringPosition.xz;

  RenderSphereRingWithBoundingBoxes(
    r,
    (outerRings - innerEmptyRings) * 2,
    ringPosition.y,
    outerMin,
    outerMax,
    innerMin,
    innerMax,
    sphereSpacing,
    sphereRadius,
    gridSphere,
    hitDistance,
    fragColor);
}

#endif