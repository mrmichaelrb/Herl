#ifndef CYLINDERS_HLSL
#define CYLINDERS_HLSL

void HitCylinderAxisAlignedInfiniteBothSides(
  float3 rayStart,
  float3 rayDirection,
  float3 cylinderPosition,
  float3 cylinderAxis,
  float cylinderRadius,
  inout float hitDistance,
  out float hitDistanceFar,
  out float hitDistanceCenter)
{
  float3 displacement = rayStart - cylinderPosition;
  float3 normalCross = cross(rayDirection, cylinderAxis);
  float3 normal = normalize(normalCross);
  float distance = abs(dot(displacement, normal));

  if (distance < cylinderRadius)
  {
    float normalLength = length(normalCross);
    float3 b = cross(displacement, cylinderAxis);
    float t = dot(b, -normal) / normalLength;

    if (t > 0.0)
    {
      float3 c = normalize(cross(normal, cylinderAxis));
      float s = abs(sqrt((cylinderRadius * cylinderRadius) - (distance * distance)) / dot(rayDirection, c));
   
      hitDistance = t - s;
      hitDistanceFar = t + s;
      hitDistanceCenter = distance;
    }
  }
}

void HitCylinderAxisAlignedInfinite(
  float3 rayStart,
  float3 rayDirection,
  float3 cylinderPosition,
  float3 cylinderAxis,
  float cylinderRadius,
  inout float hitDistance,
  out float hitDistanceCenter)
{
  float hitDistanceFar;

  HitCylinderAxisAlignedInfiniteBothSides(
    rayStart,
    rayDirection,
    cylinderPosition,
    cylinderAxis,
    cylinderRadius,
    hitDistance,
    hitDistanceFar,
    hitDistanceCenter);
}

#endif
