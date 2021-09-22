#ifndef SKY_HLSL
#define SKY_HLSL

#include "Common.hlsl"
#include "Rectangles.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"
#include "Terrain.hlsl"

static const float CelestialDistance = 256.0 * 1024.0;

static const float AtmosphereFogExponent = 1.125;

static const int CloudNoiseSampleCount = 4;
static const float CloudMinSteps = 24.0;
static const float CloudMaxSteps = 32.0;
static const float CloudStepDistance = 1.0 / 64.0;
static const float CloudMaxAcceleration = 32.0;
static const float CloudWaterReflectionSteps = 8.0;
static const float CloudWaterReflectionMaxFactor = 1.0;

static const float PlanemoTextureSize = 1024.0;
static const float PlanemoLodBias = -1.0;

static const float NebulaTextureSize = 512.0;
static const float NebulaLodBias = -1.0;

static const half3 StarColor = half3(0.25, 0.25, 0.25);
static const float StarNoiseScale = 350.0;
static const float StarNoiseThreshhold = 0.99;

static const half3 SpaceColor = half3(0.0, 0.0, 0.0);

uniform half3 AtmosphereAttenuatedColor;
uniform float AtmosphereCeiling;
uniform float AtmosphereOrbSize;
uniform float AtmosphereHardness;
uniform float AtmosphereCelestialOrbSize;
uniform float AtmosphereCelestialHardness;

uniform half3 CloudColor;
uniform float CloudFarClipDistance;
uniform float CloudFloor;
uniform float CloudCeiling;
uniform float CloudHeightReciprical;
uniform float CloudHeightPiReciprical;
uniform float CloudLayer;
uniform float CloudNoiseScale;
uniform float CloudNoiseLocationFactor;
uniform float2 CloudOffset;
uniform float CloudDensity;
uniform float CloudHardness;
uniform float CloudCover;
uniform float CloudMaximumObscurance;

uniform float3 Sun1Direction;
uniform half3 Sun1Color;
uniform float Sun1Size;
uniform float Sun1Hardness;

uniform float3 NebulaPosition;
uniform float4 NebulaRotation;
uniform float2 NebulaScale;
uniform sampler2D NebulaTexture;

uniform float3 Planemo1Position;
uniform float4 Planemo1Rotation;
uniform float Planemo1Radius;
uniform sampler2D Planemo1Texture;

uniform float3 Planemo2Position;
uniform float4 Planemo2Rotation;
uniform float Planemo2Radius;
uniform sampler2D Planemo2Texture;

void RenderAtmosphere(RenderState r, inout half4 fragColor)
{
  RenderOrb(
    r,
    Down,
    AtmosphereAttenuatedColor,
    AtmosphereOrbSize,
    AtmosphereHardness,
    fragColor);
}

void RenderAtmosphereFogCelestial(RenderState r, inout half4 fragColor)
{
  RenderOrbAdditive(
    r,
    Down,
    AtmosphereAttenuatedColor,
    AtmosphereOrbSize,
    AtmosphereHardness,
    fragColor);

  RenderOrbReversed(
    r,
    Up,
    AtmosphereAttenuatedColor,
    AtmosphereCelestialOrbSize,
    AtmosphereCelestialHardness,
    fragColor);
}

void RenderAtmosphereFog(
  RenderState r,
  float hitDistance,
  float opacity,
  inout half4 fragColor)
{
  if (hitDistance > CelestialDistance)
  {
    RenderAtmosphereFogCelestial(r, fragColor);
  }
  else
  {
    float relativeAtmosphereCeiling = AtmosphereCeiling + WorldPosition.y;
    float atmosphereDistance = DistanceToHorizontalPlane(r.RayStart, r.RayDirection, relativeAtmosphereCeiling);

    if (r.RayDirection.y >= 0.0)
    {
      atmosphereDistance = min(hitDistance, atmosphereDistance);
    }
    else
    {
      if (hitDistance < atmosphereDistance)
      {
        atmosphereDistance = 0.0;
      }
      else
      {
        atmosphereDistance = max(0.0, atmosphereDistance);
        atmosphereDistance = hitDistance - atmosphereDistance;
      }
    }

    float fogAmount = saturate(atmosphereDistance / TerrainFarClipDistance);
    fogAmount = pow(fogAmount, AtmosphereFogExponent);

    RenderColor(AtmosphereAttenuatedColor, fogAmount * opacity, fragColor);
  }
}

void RenderPlanemo(
  RenderState r,
  float3 planemoPosition,
  float4 planemoRotation,
  float planemoRadius,
  sampler2D planemoTexture,
  inout float hitDistance,
  inout half4 fragColor)
{
  float planemoHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, planemoPosition, planemoRadius, planemoHitDistance);

  if (planemoHitDistance < hitDistance)
  {
    hitDistance = planemoHitDistance;

    float3 hitPosition = (planemoHitDistance * r.RayDirection) + r.RayStart;
    float3 surfaceNormal = normalize(hitPosition - planemoPosition);

    float2 uv = SphereMapping(surfaceNormal, planemoRotation);

    float lod = MipMapLod(
      r.RayDirection,
      hitDistance,
      planemoRadius * 2.0,
      surfaceNormal,
      PlanemoTextureSize,
      UvNoScale);

    lod += PlanemoLodBias;

    half3 planemoColor = SampleTexture(planemoTexture, uv, lod).rgb;
    AddSunlightCelestialDiffuseNoAmbient(surfaceNormal, planemoColor);

    RenderColor(planemoColor, fragColor);
  }
}

void RenderPlanemos(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  RenderPlanemo(
    r,
    Planemo1Position,
    Planemo1Rotation,
    Planemo1Radius,
    Planemo1Texture,
    hitDistance,
    fragColor);

  RenderPlanemo(
    r,
    Planemo2Position,
    Planemo2Rotation,
    Planemo2Radius,
    Planemo2Texture,
    hitDistance,
    fragColor);
}

void RenderSuns(RenderState r, inout half4 fragColor)
{
  RenderOrb(r, Sun1Direction, Sun1Color, Sun1Size, Sun1Hardness, fragColor);
}

void RenderStars(RenderState r, inout half4 fragColor)
{
  float3 rayDirectionAbs = abs(r.RayDirection);
  float2 starMapLocation;

  if ((rayDirectionAbs.x > rayDirectionAbs.y) && (rayDirectionAbs.x > rayDirectionAbs.z))
  {
    starMapLocation = r.RayDirection.yz / r.RayDirection.x;
  }
  else if ((rayDirectionAbs.y > rayDirectionAbs.x) && (rayDirectionAbs.y > rayDirectionAbs.z))
  {
    starMapLocation = r.RayDirection.zx / r.RayDirection.y;
  }
  else
  {
    starMapLocation = r.RayDirection.xy / r.RayDirection.z;
  }

  float starNoise = Float2Hash(trunc(starMapLocation * StarNoiseScale), HashOffsetStatic);

  if (starNoise > StarNoiseThreshhold)
  {
    RenderColor(StarColor, fragColor);
  }
}

void RenderNebula(RenderState r, inout half4 fragColor)
{
  float hitDistance = 1.#INF;

  RenderUnlitTextureRectangle(
    r,
    NebulaPosition,
    NebulaRotation,
    NebulaScale,
    NebulaLodBias,
    NebulaTexture,
    NebulaTextureSize,
    UvNoOffset,
    UvNoScale,
    UvNoScale,
    hitDistance,
    fragColor);
}

void RenderSpace(RenderState r, inout half4 fragColor)
{
  RenderColor(SpaceColor, fragColor);
  RenderNebula(r, fragColor);
  RenderStars(r, fragColor);
}

void RenderBackground(RenderState r, inout half4 fragColor)
{
  RenderSpace(r, fragColor);
  RenderAtmosphere(r, fragColor);
  RenderSuns(r, fragColor);
}

void RenderBackgroundFromWaterReflection(RenderState r, inout half4 fragColor)
{
  RenderColor(AtmosphereAttenuatedColor, fragColor);
  RenderSuns(r, fragColor);
}

float CloudNoise(float3 position)
{
  float rounding = sin((position.y - CloudFloor) * CloudHeightPiReciprical);

  position *= CloudNoiseScale;

  float density = CloudDensity;
  float noise = 0.0;

  for (int i = 0; i < CloudNoiseSampleCount; i++)
  {
    noise += Float3NoiseTexture(position) * density;
    density *= 0.5;
    position *= CloudNoiseLocationFactor;
  }

  return saturate((noise - CloudCover) * CloudHardness) * rounding;
}

void RenderClouds(
  float3 rayStart,
  float3 rayDirection,
  float cloudSteps,
  int maxSteps,
  float stepDistance,
  float maxDistance,
  float fadeFactor,
  inout half4 fragColor)
{
  float stepAcceleration = 2.0 * (maxDistance - stepDistance) / (cloudSteps * cloudSteps);
  stepAcceleration = min(stepAcceleration, CloudMaxAcceleration);
  float stepDelta = stepAcceleration;
  float stepWeight = maxSteps / cloudSteps;

  float accumulatedBrightness = 0.0;
  float accumulatedOpacity = 0.0;
  int stepCount;

  for (stepCount = 0; stepCount < cloudSteps; stepCount++)
  {
    if (stepDistance >= maxDistance)
    {
      break;
    }
  
    float3 stepPosition = (stepDistance * rayDirection) + rayStart;

    float cloudNoise = CloudNoise(stepPosition);
    float sunlightProduct = saturate((stepPosition.y - CloudFloor) * CloudHeightReciprical);

    accumulatedOpacity += cloudNoise * stepWeight;
    accumulatedBrightness += sunlightProduct * accumulatedOpacity;
   
    stepDistance += stepDelta;
    stepDelta += stepAcceleration;
  }

  if (stepCount > 0)
  {
    float fade = 1.0 - (saturate(stepDistance / CloudFarClipDistance) * fadeFactor);
    accumulatedOpacity *= fade * fade;
    accumulatedOpacity = min(accumulatedOpacity, CloudMaximumObscurance);

    accumulatedBrightness = saturate(accumulatedBrightness / stepCount);
    half3 accumulatedColor = (CloudColor * accumulatedBrightness) + SunlightAmbientColor;

    RenderColor(accumulatedColor, accumulatedOpacity, fragColor);
  }
}

void RenderClouds(RenderState r, float hitDistance, inout half4 fragColor)
{
  float3 localRayStart = r.RayStart - WorldPosition;
  localRayStart.xz += CloudOffset;

  float altitude = localRayStart.y;
  float floorDistance = DistanceToHorizontalPlane(localRayStart, r.RayDirection, CloudFloor);
  float ceilingDistance = DistanceToHorizontalPlane(localRayStart, r.RayDirection, CloudCeiling);

  float maxDistance;
  float stepDistance;
  float cloudSteps;
  float fadeFactor;

  if (r.RayDirection.y > 0.0)
  {
    // Looking up below the clouds
    if (altitude < CloudFloor)
    {
      float cloudVerticalDistance = CloudFloor - altitude;
      cloudSteps = CloudMaxSteps - (cloudVerticalDistance * CloudStepDistance);
      
      // Add randomization to prevent banding
      cloudSteps += PixelRand(r);
      cloudSteps = clamp(cloudSteps, CloudMinSteps, CloudMaxSteps);

      maxDistance = min(hitDistance, CloudFarClipDistance);
      stepDistance = min(maxDistance, floorDistance);
      maxDistance = min(maxDistance, ceilingDistance);
      fadeFactor = saturate(cloudVerticalDistance * CloudHeightReciprical);
    }
    // Looking up from inside the clouds
    else if (altitude < CloudCeiling)
    {
      cloudSteps = CloudMaxSteps;
      stepDistance = 0.0;
      maxDistance = min(hitDistance, ceilingDistance);
      fadeFactor = 0.0;
    }
    // Looking up above the clouds
    else
    {
      return;
    }
  }
  else
  {
    // Looking down at the clouds
    if (altitude > CloudCeiling)
    {
      float cloudVerticalDistance = altitude - CloudCeiling;
      cloudSteps = CloudMaxSteps - (cloudVerticalDistance * CloudStepDistance);
      
      // Add randomization to prevent layers
      cloudSteps += PixelRand(r);
      cloudSteps = clamp(cloudSteps, CloudMinSteps, CloudMaxSteps);
      
      maxDistance = min(hitDistance, CloudFarClipDistance);
      stepDistance = min(maxDistance, ceilingDistance);
      maxDistance = min(maxDistance, floorDistance);
      fadeFactor = saturate(cloudVerticalDistance * CloudHeightReciprical);
    }
    // Looking down from inside the clouds
    else if (altitude > CloudFloor)
    {
      cloudSteps = CloudMaxSteps;
      stepDistance = 0.0;
      maxDistance = min(hitDistance, floorDistance);
      fadeFactor = 0.0f;
    }
    // Looking down below the clouds
    else
    {
      return;
    }
  }

  RenderClouds(
    localRayStart,
    r.RayDirection,
    cloudSteps,
    CloudMaxSteps,
    stepDistance,
    maxDistance,
    fadeFactor,
    fragColor);
}

void RenderCloudsFromWaterReflection(RenderState r, float hitDistance, inout half4 fragColor)
{
  float3 localRayStart = r.RayStart - WorldPosition;
  localRayStart.xz += CloudOffset;

  float floorDistance = DistanceToHorizontalPlane(localRayStart, r.RayDirection, CloudFloor);
  float ceilingDistance = DistanceToHorizontalPlane(localRayStart, r.RayDirection, CloudCeiling);

  float maxDistance = min(hitDistance, CloudFarClipDistance);
  float stepDistance;
  float cloudSteps;

  cloudSteps = CloudWaterReflectionSteps;
  stepDistance = floorDistance;
  maxDistance = min(maxDistance, ceilingDistance);

  RenderClouds(
    localRayStart,
    r.RayDirection,
    cloudSteps,
    CloudMaxSteps * CloudWaterReflectionMaxFactor,
    stepDistance,
    maxDistance,
    1.0,
    fragColor);
}

float CloudShadow(float3 position)
{
  float3 localRayStart = position - WorldPosition;
  localRayStart.xz += CloudOffset;
  
  float3 localRayDirection = Sun1Direction;

  float floorDistance = DistanceToHorizontalPlane(localRayStart, localRayDirection, CloudFloor);
  float ceilingDistance = DistanceToHorizontalPlane(localRayStart, localRayDirection, CloudCeiling);

  float maxDistance = CloudFarClipDistance;
  float stepDistance;
  float cloudSteps;

  cloudSteps = CloudWaterReflectionSteps;
  stepDistance = floorDistance;
  maxDistance = min(maxDistance, ceilingDistance);

  half4 cloudColor = NoColor;
  
  RenderClouds(
    localRayStart,
    localRayDirection,
    cloudSteps,
    CloudMaxSteps * CloudWaterReflectionMaxFactor,
    stepDistance,
    maxDistance,
    1.0,
    cloudColor);
  
  float cloudShadow = 1.0 - cloudColor.a;
  
  return sqrt(cloudShadow);
}

#endif
