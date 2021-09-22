#ifndef RECTANGLES_HLSL
#define RECTANGLES_HLSL

#include "Common.hlsl"
#include "Compression.hlsl"
#include "Rendering.hlsl"

void HitRectangle(
  float3 rayStart,
  float3 rayDirection,
  float3 rectanglePosition,
  float4 rectangleRotation,
  float2 rectangleScale,
  out float3 localRayStart,
  out float3 localRayDirection,
  out float2 hitUV,
  inout float hitDistance)
{
  localRayStart = rayStart - rectanglePosition;
  localRayStart = RotateVector(localRayStart, rectangleRotation);

  localRayDirection = RotateVector(rayDirection, rectangleRotation);

  float rectangleHitDistance = DistanceToZOriginPlane(localRayStart, localRayDirection);

  if ((rectangleHitDistance > 0.0) && (rectangleHitDistance < hitDistance))
  {
    float2 hitLocation = IntersectionOnZOriginPlane(localRayStart, localRayDirection);
    float2 hitLocationScaled = hitLocation / rectangleScale;

    if (all(abs(hitLocationScaled) < 0.5))
    {
      hitDistance = rectangleHitDistance;
      hitUV = hitLocationScaled + 0.5;
    }
  }
}

void RenderUnlitTextureRectangle(
  RenderState r,
  float3 rectanglePosition,
  float4 rectangleRotation,
  float2 rectangleScale,
  float lodBias,
  sampler2D rectangleTexture,
  float textureSize,
  float2 textureOffset,
  float2 textureScale,
  float2 backfaceTextureScale,
  inout float hitDistance,
  inout half4 fragColor)
{
  float rectangleHitDistance = hitDistance;
  float3 localRayStart;
  float3 localRayDirection;
  float2 uv;

  HitRectangle(
    r.RayStart,
    r.RayDirection,
    rectanglePosition,
    rectangleRotation,
    rectangleScale,
    localRayStart,
    localRayDirection,
    uv,
    rectangleHitDistance);

  if (rectangleHitDistance < hitDistance)
  {
    hitDistance = rectangleHitDistance;

    uv += textureOffset;
    uv *= textureScale;

    float4 normalRotation = InvertQuaternion(rectangleRotation);

    float surfaceSize = max(rectangleScale.x, rectangleScale.y);
    float3 surfaceNormal = RotateVector(Forward, normalRotation);

    float lod = MipMapLod(
      r.RayDirection,
      hitDistance,
      surfaceSize,
      surfaceNormal,
      textureSize,
      textureScale);

    lod += lodBias;

    float surfaceProduct = dot(r.RayDirection, surfaceNormal);

    if (surfaceProduct < 0.0)
    {
      uv *= backfaceTextureScale;
    }

    half4 rectangleColor = SampleTexture(rectangleTexture, uv, lod);

    RenderColor(rectangleColor, fragColor);
  }
}

void RenderDiffuseSpecularRectangleWithNormalMap(
  RenderState r,
  float3 rectanglePosition,
  float4 rectangleRotation,
  float2 rectangleScale,
  float reflectivity,
  float shininess,
  half3 rectangleColor,
  float lodBias,
  sampler2D normalMap,
  float normalMapSize,
  float2 normalMapOffset,
  float2 normalMapScale,
  inout float hitDistance,
  inout half4 fragColor)
{
  float rectangleHitDistance = hitDistance;
  float3 localRayStart;
  float3 localRayDirection;
  float2 uv;

  HitRectangle(
    r.RayStart,
    r.RayDirection,
    rectanglePosition,
    rectangleRotation,
    rectangleScale,
    localRayStart,
    localRayDirection,
    uv,
    rectangleHitDistance);

  if (rectangleHitDistance < hitDistance)
  {
    hitDistance = rectangleHitDistance;

    uv += normalMapOffset;
    uv *= normalMapScale;

    float4 normalRotation = InvertQuaternion(rectangleRotation);

    float surfaceSize = max(rectangleScale.x, rectangleScale.y);
    float3 surfaceNormal = RotateVector(Forward, normalRotation);
    float3 surfaceTangent = RotateVector(Right, normalRotation);

    float lod = MipMapLod(
      r.RayDirection,
      hitDistance,
      surfaceSize,
      surfaceNormal,
      normalMapSize,
      normalMapScale);

    lod += lodBias;

    float3 mapNormal = UnkpackNormal(SampleTexture(normalMap, uv, lod));
    surfaceNormal = ApplyMapNormal(surfaceNormal, surfaceTangent, mapNormal);

    AddSunlightDiffuse(surfaceNormal, rectangleColor);
    AddSunlightSpecular(r.RayDirection, surfaceNormal, reflectivity, shininess, rectangleColor);

    RenderColor(rectangleColor, fragColor);
  }
}

#endif
