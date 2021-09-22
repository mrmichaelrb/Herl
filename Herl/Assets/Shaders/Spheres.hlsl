#ifndef SPHERES_HLSL
#define SPHERES_HLSL

#include "Common.hlsl"
#include "Compression.hlsl"
#include "Rendering.hlsl"

static const float FilamentSphereHitDeltaDistanceScale = 0.03;

bool IsInSphere(float3 position, float3 spherePosition, float sphereRadius)
{
  return (DistanceSquared(position, spherePosition) < LengthSquared(sphereRadius));
}

void HitSphereBothSides(
  float3 rayStart,
  float3 rayDirection,
  float3 spherePosition,
  float sphereRadius,
  inout float hitDistance,
  out float hitDistanceFar)
{
  hitDistanceFar = hitDistance;

  float3 displacement = rayStart - spherePosition;
  float b = dot(rayDirection, displacement);
  float c = LengthSquared(displacement) - LengthSquared(sphereRadius);
  float discriminant = (b * b) - c;

  // One or more roots
  if (discriminant >= 0.0)
  {
    float discriminantSqrt = sqrt(discriminant);
    float distance = -b - discriminantSqrt;
    float distanceFar = -b + discriminantSqrt;

    // If root is in front of ray, then the sphere has been hit
    if (distance > 0.0)
    {
      hitDistance = distance;
    }

    if (distanceFar > 0.0)
    {
      hitDistanceFar = distanceFar;
    }
  }
}

void HitSphere(
  float3 rayStart,
  float3 rayDirection,
  float3 spherePosition,
  float sphereRadius,
  inout float hitDistance)
{
  float hitDistanceFar;
  
  HitSphereBothSides(
    rayStart,
    rayDirection,
    spherePosition,
    sphereRadius,
    hitDistance,
    hitDistanceFar);
}

float TransparentSphereOpacity(
  float3 rayStart,
  float3 rayDirection,
  float sphereHitDistance,
  float3 spherePosition)
{
  float3 hitPosition = (sphereHitDistance * rayDirection) + rayStart;
  float3 surfaceNormal = normalize(spherePosition - hitPosition);
  float opacity = 1.0 - abs(dot(rayDirection, surfaceNormal));
  opacity *= opacity;
  opacity *= opacity;

  return opacity;
}

half3 GlowSphereColor(
  float3 rayDirection,
  float3 hitPosition,
  float3 spherePosition,
  half3 primaryColor,
  half3 secondaryColor)
{
  float3 surfaceInverseNormal = normalize(spherePosition - hitPosition);
  float lightProduct = dot(rayDirection, surfaceInverseNormal);
  lightProduct = lightProduct * lightProduct * lightProduct;

  return lerp(secondaryColor, primaryColor, lightProduct);
}

half3 DiffuseSphereColor(
  float3 hitPosition,
  float3 spherePosition,
  half3 sphereColor)
{
  float3 surfaceNormal = normalize(hitPosition - spherePosition);
  AddSunlightDiffuse(surfaceNormal, sphereColor);

  return sphereColor;
}

half3 DiffuseSphereInteriorColor(
  float3 hitPosition,
  float3 spherePosition,
  half3 sphereColor)
{
  return DiffuseSphereColor(spherePosition, hitPosition, sphereColor);
}

half3 DiffuseSphereColorWithNormalMap(
  half3 sphereColor,
  float3 surfaceNormal,
  sampler2D normalMap,
  float2 uv)
{
  float3 mapNormal = UnkpackNormal(SampleTexture(normalMap, uv));
  surfaceNormal = ApplyMapNormalUp(surfaceNormal, mapNormal);

  AddSunlightDiffuse(surfaceNormal, sphereColor);

  return sphereColor;
}

half3 DiffuseSphereColorWithNormalMap(
  float3 hitPosition,
  float3 spherePosition,
  half3 sphereColor,
  sampler2D normalMap,
  float2 uv)
{
  float3 surfaceNormal = normalize(hitPosition - spherePosition);

  return DiffuseSphereColorWithNormalMap(
    sphereColor,
    surfaceNormal,
    normalMap,
    uv);
}

half3 DiffuseSphereInteriorColorWithNormalMap(
  float3 hitPosition,
  float3 spherePosition,
  half3 sphereColor,
  sampler2D normalMap,
  float2 uv)
{
  float3 surfaceNormal = normalize(hitPosition - spherePosition);

  return DiffuseSphereColorWithNormalMap(
    sphereColor,
    surfaceNormal,
    normalMap,
    uv);
}

half3 DiffuseSpecularSphereColor(
  float3 rayDirection,
  float3 hitPosition,
  float3 spherePosition,
  float reflectivity,
  float shininess,
  half3 sphereColor)
{
  float3 surfaceNormal = normalize(hitPosition - spherePosition);

  AddSunlightDiffuse(surfaceNormal, sphereColor);
  AddSunlightSpecular(rayDirection, surfaceNormal, reflectivity, shininess, sphereColor);

  return sphereColor;
}

half3 DiffuseSpecularSphereInteriorColor(
  float3 rayDirection,
  float3 hitPosition,
  float3 spherePosition,
  float reflectivity,
  float shininess,
  half3 sphereColor)
{
  float3 surfaceInteriorNormal = normalize(spherePosition - hitPosition);

  AddSunlightDiffuse(surfaceInteriorNormal, sphereColor);
  AddSunlightSpecular(rayDirection, surfaceInteriorNormal, reflectivity, shininess, sphereColor);

  return sphereColor;
}

half3 DiffuseSpecularSphereColorWithNormalMap(
  float3 rayDirection,
  float hitDistance,
  float sphereRadius,
  float reflectivity,
  float shininess,
  half3 sphereColor,
  inout float3 surfaceNormal,
  float lodBias,
  sampler2D normalMap,
  float normalMapSize,
  float2 normalMapScale,
  float2 uv)
{
  float lod = MipMapLod(
    rayDirection,
    hitDistance,
    sphereRadius * 2.0,
    surfaceNormal,
    normalMapSize,
    normalMapScale);

  lod += lodBias;

  float3 mapNormal = UnkpackNormal(SampleTexture(normalMap, uv, lod));
  surfaceNormal = ApplyMapNormalUp(surfaceNormal, mapNormal);

  AddSunlightDiffuse(surfaceNormal, sphereColor);
  AddSunlightSpecular(rayDirection, surfaceNormal, reflectivity, shininess, sphereColor);

  return sphereColor;
}

half3 SunlightDiffuseNoAmbientMinnaertSphereColor(
  float3 rayDirection,
  float3 hitPosition,
  float3 spherePosition,
  float reflectivity,
  half3 sphereColor)
{
  float3 surfaceNormal = normalize(hitPosition - spherePosition);

  AddSunlightDiffuseNoAmbientMinnaert(rayDirection, surfaceNormal, reflectivity, sphereColor);

  return sphereColor;
}

float SphereMappingUpU(float x, float z)
{
  float u = (atan2(x, z) * -TwoPiUnderOne) - 0.25;
  return u;
}

float SphereMappingUpU(float3 surfaceNormal)
{
  return SphereMappingUpU(surfaceNormal.x, surfaceNormal.z);
}

float SphereMappingUpV(float y)
{
  float v = y * 0.5 + 0.5;
  return v;
}

float SphereMappingUpV(float3 surfaceNormal)
{
  return SphereMappingUpV(surfaceNormal.y);
}

float2 SphereMappingUp(float3 surfaceNormal)
{
  float u = SphereMappingUpU(surfaceNormal);
  float v = SphereMappingUpV(surfaceNormal);

  return float2(u, v);
}

float2 SphereMappingUp(float3 hitPosition, float3 spherePosition, float sphereRadius)
{
  float3 difference = hitPosition - spherePosition;

  float u = SphereMappingUpU(difference.x, difference.z);
  float v = SphereMappingUpV(difference.y / sphereRadius);

  return float2(u, v);
}

float2 SphereMapping(float3 surfaceNormal, float4 rotation)
{
  surfaceNormal = RotateVector(surfaceNormal, rotation);
  return SphereMappingUp(surfaceNormal);
}

void RenderAtmosphereFog(
  RenderState r,
  float hitDistance,
  float opacity,
  inout half4 fragColor);

void RenderTransparentSphere(
  RenderState r,
  float sphereHitDistance,
  float3 spherePosition,
  float sphereDensity,
  half3 sphereColor,
  inout half4 fragColor)
{
  float opacity = TransparentSphereOpacity(
      r.RayStart,
      r.RayDirection,
      sphereHitDistance,
      spherePosition);

  RenderColor(sphereColor, opacity * sphereDensity, fragColor);
  RenderAtmosphereFog(r, sphereHitDistance, opacity, fragColor);
}

void RenderTransparentSphere(
  RenderState r,
  float hitDistance,
  float3 spherePosition,
  float sphereRadius,
  float sphereDensity,
  half3 sphereColor,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;
  float sphereHitDistanceFar;

  HitSphereBothSides(
    r.RayStart,
    r.RayDirection,
    spherePosition,
    sphereRadius,
    sphereHitDistance,
    sphereHitDistanceFar);

  if (sphereHitDistanceFar < hitDistance)
  {
    RenderTransparentSphere(
      r,
      sphereHitDistanceFar,
      spherePosition,
      sphereDensity,
      sphereColor,
      fragColor);
  }

  if (sphereHitDistance < hitDistance)
  {
    RenderTransparentSphere(
      r,
      sphereHitDistance,
      spherePosition,
      sphereDensity,
      sphereColor,
      fragColor);
  }
}

void RenderTransparentSphereBack(
  RenderState r,
  float hitDistance,
  float3 spherePosition,
  float sphereRadius,
  float sphereDensity,
  half3 sphereColor,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;
  float sphereHitDistanceFar;

  HitSphereBothSides(
    r.RayStart,
    r.RayDirection,
    spherePosition,
    sphereRadius,
    sphereHitDistance,
    sphereHitDistanceFar);

  if (sphereHitDistanceFar < hitDistance)
  {
    RenderTransparentSphere(
      r,
      sphereHitDistanceFar,
      spherePosition,
      sphereDensity,
      sphereColor,
      fragColor);
  }
}

void RenderFogSphere(
  RenderState r,
  float hitDistance,
  float3 spherePosition,
  float sphereRadius,
  float sphereDensity,
  half3 sphereColor,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;
  float sphereHitDistanceFar;

  HitSphereBothSides(
    r.RayStart,
    r.RayDirection,
    spherePosition,
    sphereRadius,
    sphereHitDistance,
    sphereHitDistanceFar);

  if (
    (sphereHitDistance < hitDistance) ||
    IsInSphere(r.RayStart, spherePosition, sphereRadius)
    )
  {
    if (sphereHitDistance == hitDistance)
    {
      sphereHitDistance = RenderDistanceMin;
    }
    
    sphereHitDistanceFar = min(sphereHitDistanceFar, hitDistance);
    float intersectionDistance = sphereHitDistanceFar - sphereHitDistance;
    float opacity = saturate((intersectionDistance * intersectionDistance) / (sphereRadius * sphereRadius));

    RenderColor(sphereColor, opacity * sphereDensity, fragColor);
    RenderAtmosphereFog(r, sphereHitDistance, opacity, fragColor);
  }
}

void RenderFilamentSphere(
  RenderState r,
  float hitDistance,
  float3 spherePosition,
  float sphereRadius,
  int sampleCount,
  int noiseSampleCount,
  float noiseScale,
  float noiseDensity,
  float innerEpsilon,
  half3 innerColor,
  float outerEpsilon,
  half3 outerColor,
  float animationSpeed,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;
  float sphereHitDistanceFar;

  HitSphereBothSides(
    r.RayStart,
    r.RayDirection,
    spherePosition,
    sphereRadius,
    sphereHitDistance,
    sphereHitDistanceFar);

  if (
    (sphereHitDistance < hitDistance) ||
    IsInSphere(r.RayStart, spherePosition, sphereRadius)
    )
  {
    if (sphereHitDistance == hitDistance)
    {
      sphereHitDistance = RenderDistanceMin;
    }

    sphereHitDistanceFar = min(sphereHitDistanceFar, hitDistance);

    float3 noiseStart = r.RayStart - WorldPosition;
    float noiseDensityReciprical = 1.0 / noiseDensity;
    float totalDensity = 0.0;

    half4 accumulatedColor = NoColor;

    float stepDistance = sphereHitDistance;

    for (int i = 0; i < sampleCount; i++)
    {
      float3 noisePosition = (stepDistance * r.RayDirection) + noiseStart;

      float filamentDistance = FilamentNoise(noisePosition * noiseScale, noiseSampleCount, animationSpeed);
      filamentDistance *= noiseDensityReciprical;

      if (filamentDistance < outerEpsilon)
      {
        float localDensity = outerEpsilon - filamentDistance;
        float weighingFactor = (localDensity * -totalDensity) + localDensity;
        totalDensity += weighingFactor;

        if (filamentDistance < innerEpsilon)
        {
          RenderColor(innerColor, totalDensity, accumulatedColor);
        }
        else
        {
          RenderColor(outerColor, totalDensity, accumulatedColor);
        }
      }

      stepDistance += filamentDistance * FilamentSphereHitDeltaDistanceScale * sphereRadius;
      
      if ((stepDistance > sphereHitDistanceFar) || (accumulatedColor.a >= 1.0))
      {
        break;
      }
    }

    accumulatedColor.a = saturate(accumulatedColor.a);

    RenderColor(accumulatedColor, fragColor);
    RenderAtmosphereFog(r, sphereHitDistance, accumulatedColor.a, fragColor);
  }
}

void RenderDiffuseSphere(
  RenderState r,
  float3 spherePosition,
  float sphereRadius,
  half3 sphereColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, spherePosition, sphereRadius, sphereHitDistance);

  if (sphereHitDistance < hitDistance)
  {
    hitDistance = sphereHitDistance;

    float3 hitPosition = (sphereHitDistance * r.RayDirection) + r.RayStart;

    sphereColor = DiffuseSphereColor(hitPosition, spherePosition, sphereColor);

    RenderColor(sphereColor, fragColor);
  }
}

void RenderDiffuseSpecularSphere(
  RenderState r,
  float3 spherePosition,
  float sphereRadius,
  float reflectivity,
  float shininess,
  half3 sphereColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, spherePosition, sphereRadius, sphereHitDistance);

  if (sphereHitDistance < hitDistance)
  {
    hitDistance = sphereHitDistance;

    float3 hitPosition = (sphereHitDistance * r.RayDirection) + r.RayStart;

    sphereColor = DiffuseSpecularSphereColor(
      r.RayDirection,
      hitPosition,
      spherePosition,
      reflectivity,
      shininess,
      sphereColor);

    RenderColor(sphereColor, fragColor);
  }
}

void RenderDiffuseNoAmbientMinnaertSphere(
  RenderState r,
  float3 spherePosition,
  float sphereRadius,
  float reflectivity,
  half3 sphereColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, spherePosition, sphereRadius, sphereHitDistance);

  if (sphereHitDistance < hitDistance)
  {
    hitDistance = sphereHitDistance;

    float3 hitPosition = (sphereHitDistance * r.RayDirection) + r.RayStart;

    sphereColor = SunlightDiffuseNoAmbientMinnaertSphereColor(
      r.RayDirection,
      hitPosition,
      spherePosition,
      reflectivity,
      sphereColor);

    RenderColor(sphereColor, fragColor);
  }
}

void RenderDiffuseSpecularSphereWithNormalMapUp(
  RenderState r,
  float3 spherePosition,
  float sphereRadius,
  float reflectivity,
  float shininess,
  half3 sphereColor,
  float lodBias,
  sampler2D normalMap,
  float normalMapSize,
  float2 normalMapScale,
  inout float hitDistance,
  inout half4 fragColor)
{
  float sphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, spherePosition, sphereRadius, sphereHitDistance);

  if (sphereHitDistance < hitDistance)
  {
    hitDistance = sphereHitDistance;

    float3 hitPosition = (sphereHitDistance * r.RayDirection) + r.RayStart;
    float3 surfaceNormal = normalize(hitPosition - spherePosition);

    float2 uv = SphereMappingUp(surfaceNormal);
    uv *= normalMapScale;

    sphereColor = DiffuseSpecularSphereColorWithNormalMap(
      r.RayDirection,
      hitDistance,
      sphereRadius,
      reflectivity,
      shininess,
      sphereColor,
      surfaceNormal,
      lodBias,
      normalMap,
      normalMapSize,
      normalMapScale,
      uv);

    RenderColor(sphereColor, fragColor);
  }
}

#endif
