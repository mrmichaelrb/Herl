#ifndef MANDELBULB_HLSL
#define MANDELBULB_HLSL

#include "Common.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"

static const float MandelbulbScale = 0.8;
static const int MandelbulbIterations = 3;
static const float MandelbulbPower = 8.0;
static const float MandelbulbDetailLevel = 1.25;
static const float MandelbulbDetailRemoval = 2.0;
static const int MandelbulbHitSteps = 24;
static const float MandelbulbHitEpsilon = 0.001;
static const float MandelbulbEdgeEpsilon = 0.02;
static const float MandelbulbHitDeltaDistanceScale = 1.15;
static const float MandelbulbNormalSampleSize = 0.01;
static const float MandelbulbAnimationSpeed = 0.25;
static const half3 MandelbulbInteriorColor = half3(0.1, 0.1, 0.1);
static const float MandelbulbReflectivity = 0.3;
static const float MandelbulbShininess = 4.0;

float MandelbulbDistance(float3 position, inout float minimumR)
{
  float3 iterationPosition = position;
  float dr = 1.0;
  float r;
	
  for (int i = 0; i < MandelbulbIterations; i++)
  {
    r = length(iterationPosition);

    if (r > MandelbulbDetailLevel)
    {
      break;
    }

    float phi = AsinFast(iterationPosition.y / r) + (TIME_SECONDS * MandelbulbAnimationSpeed);
    float theta = atan(iterationPosition.z / iterationPosition.x);
    phi *= MandelbulbPower;
    theta *= MandelbulbPower;

    dr = pow(r, MandelbulbPower - MandelbulbDetailRemoval) * dr * MandelbulbPower;
    r = pow(r, MandelbulbPower);

    float sinePhi;
    float cosinePhi;
    float sineTheta;
    float cosineTheta;

    sincos(phi, sinePhi, cosinePhi);
    sincos(theta, sineTheta, cosineTheta);

    iterationPosition = r * float3(cosineTheta * cosinePhi, sinePhi, sineTheta * cosinePhi) + position;

    minimumR = min(minimumR, r);
  }

  return 0.5 * log(r) * r / dr;
}

float MandelbulbDistance(float3 position)
{
  float minimumR = 0.0;
  return MandelbulbDistance(position, minimumR);
}

float3 MandelbulbNormal(float3 position)
{
  return DistanceSurfaceNormal(MandelbulbDistance, position, MandelbulbNormalSampleSize);
}

half3 MandelbulbColor(
  float3 rayDirection,
  float3 position,
  float minimumR,
  half3 primaryColor,
  half3 secondaryColor
  )
{
  minimumR = saturate(minimumR);
  minimumR *= minimumR;

  half3 bulbColor = lerp(MandelbulbInteriorColor, primaryColor, minimumR);
  bulbColor = lerp(bulbColor, secondaryColor, minimumR);

  float3 surfaceNormal = MandelbulbNormal(position);

  AddSunlightDiffuse(surfaceNormal, bulbColor);
  AddSunlightSpecular(rayDirection, surfaceNormal, MandelbulbReflectivity, MandelbulbShininess, bulbColor);

  return bulbColor;
}

void RenderMandelbulbUp(
  inout RenderState r,
  float3 bulbPosition,
  float bulbRadius,
  half3 primaryColor,
  half3 secondaryColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float boundingSphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, bulbPosition, bulbRadius, boundingSphereHitDistance);

  if (boundingSphereHitDistance < hitDistance)
  {
    float radiusScaled = bulbRadius * MandelbulbScale;
  
    float3 localRayStart = r.RayStart - bulbPosition;
    localRayStart /= radiusScaled;

    float stepDistance = boundingSphereHitDistance / radiusScaled;
    float minDistance = 1.#INF;
    float minStepDistance;
    float minimumR = 1.#INF;

    for (int i = 0; i < MandelbulbHitSteps; i++)
    {
      float3 stepPosition = (stepDistance * r.RayDirection) + localRayStart;

      float distance = MandelbulbDistance(stepPosition, minimumR);

      if (distance < MandelbulbHitEpsilon)
      {
        hitDistance = stepDistance * radiusScaled;

        half3 bulbColor = MandelbulbColor(r.RayDirection, stepPosition, minimumR, primaryColor, secondaryColor);
        RenderColor(bulbColor, fragColor);

        return;
      }

      if (distance < minDistance)
      {
        minDistance = distance;
        minStepDistance = stepDistance;
      }

      stepDistance += distance * MandelbulbHitDeltaDistanceScale;
    }
    
    if (minDistance < MandelbulbEdgeEpsilon)
    {
      hitDistance = minStepDistance * radiusScaled;
      float3 stepPosition = (minStepDistance * r.RayDirection) + localRayStart;

      half3 bulbColor = MandelbulbColor(r.RayDirection, stepPosition, minimumR, primaryColor, secondaryColor);
      RenderColor(bulbColor, fragColor);
    }
  }
}

#endif
