// Inspired by https://www.shadertoy.com/view/llVXRd

#ifndef POWERBALL_HLSL
#define POWERBALL_HLSL

#include "Cylinders.hlsl"
#include "Generator.hlsl"
#include "Rendering.hlsl"
#include "Sky.hlsl"
#include "Spheres.hlsl"

static const half3 PowerBallColor = half3(1.5, 1.5, 1.5);
static const float PowerBallBrightness = 250.0;
static const float PowerBallAttenuation = 1.1;
static const int PowerBallHitSteps = 32;
static const float PowerBallGlowDensity = 0.3;
static const float PowerBallHitEpsilon = 0.01;
static const float PowerBallHitDeltaDistanceScale = 0.9;
static const float PowerBallAnimationScale = 0.8;
static const float PowerBallAnimationOffset = 30.0;
static const float PowerBallAnimationScaleSpeed = 2.0;
static const float PowerBallAnimationSubdivisionsMin = 3.0;
static const float PowerBallAnimationSubdivisionsMax = 4.2;
static const float PowerBallAnimationSubdivisionsSpeed = 2.0;
static const float PowerBallAnimationColorSpeed = 1.0;
static const float PowerBallEdgeLength = 1.0 / ((Sqrt3 / 12.0) * (3.0 + sqrt(5.0)));
static const float PowerBallFaceRadius = (1.0 / 6.0) * Sqrt3 * PowerBallEdgeLength;
static const float PowerBallCospin = cos(Pi / 5.0);
static const float PowerBallSCospin = sqrt(0.75 - PowerBallCospin * PowerBallCospin);
static const float3 PowerBallFoldingPlane = float3(-0.5, -PowerBallCospin, PowerBallSCospin);
static const float3 PowerBallFacePlane = normalize(float3(0.0, PowerBallSCospin, PowerBallCospin));
static const float3 PowerBallVPlane = float3(1.0, 0.0, 0.0);
static const float3 PowerBallUPlane = cross(PowerBallVPlane, PowerBallFacePlane);
static const float2x2 PowerBallHexMatrix = float2x2(1.0, 0.0, SqrtOneThird, 2.0 * SqrtOneThird);
static const float2x2 PowerBallSphereMatrix = float2x2(1.0, 0.0, -0.5, 0.5 * Sqrt3);

uniform float3 PowerBallPosition;
uniform float4 PowerBallRotation;
uniform float PowerBallGlowRadius;
uniform float PowerBallRadius;
uniform float PowerBallRadiusReciprical;
uniform float PowerBallProgressInverse;

float3 PowerBallIntersection(float3 positionNormal, float3 planeNormal, float planeOffset)
{
  float denominator = dot(positionNormal, planeNormal);
  float t = (dot(Origin, planeNormal) + planeOffset) / -denominator;
  return positionNormal * t;
}

float2 PowerBallUV(float3 position)
{
  float3 positionNormal = normalize(position);
  float3 intersection = PowerBallIntersection(positionNormal, PowerBallFacePlane, -1.0);
  return float2(dot(intersection, PowerBallUPlane), dot(intersection, PowerBallVPlane));
}

float3 PowerBallFoldedPosition(float3 position)
{
  position = abs(position);
  position = reflect(position, PowerBallFoldingPlane);
  position.xy = abs(position.xy);
  position = reflect(position, PowerBallFoldingPlane);
  position.xy = abs(position.xy);
  position = reflect(position, PowerBallFoldingPlane);
  return position;
}

void PowerBallUVPoints(
  float2 uv,
  out float2 a,
  out float2 b,
  out float2 c,
  out float2 center,
  out float2 ab,
  out float2 bc,
  out float2 ca)
{
  float2 pTri = mul(uv, PowerBallHexMatrix);
  float2 Pi = floor(pTri);
  float2 pf = frac(pTri);

  float split1 = step(pf.y, pf.x);
  float split2 = step(pf.x, pf.y);

  a = float2(split1, 1.0);
  b = float2(1.0, split2);
  c = float2(0.0, 0.0);

  a += Pi;
  b += Pi;
  c += Pi;

  a = mul(a, PowerBallSphereMatrix);
  b = mul(b, PowerBallSphereMatrix);
  c = mul(c, PowerBallSphereMatrix);

  center = (a + b + c) * 0.3333333;

  ab = (a + b) * 0.5;
  bc = (b + c) * 0.5;
  ca = (c + a) * 0.5;
}

float3 PowerBallSpherePosition(float2 uv)
{
  return normalize((PowerBallUPlane * uv.x) + (PowerBallVPlane * uv.y) + PowerBallFacePlane);
}

void PowerBallGeodesicPoints(
  float3 position,
  float subdivisions,
  out float3 a,
  out float3 b,
  out float3 c,
  out float3 center,
  out float3 ab,
  out float3 bc,
  out float3 ca)
{
  float2 uv = PowerBallUV(position);

  float uvScale = (subdivisions / PowerBallFaceRadius) * 0.5;
  float uvScaleReciprical = 1.0 / uvScale;

  float2 uvA;
  float2 uvB;
  float2 uvC;
  float2 uvCenter;
  float2 uvAB;
  float2 uvBC;
  float2 uvCA;

  PowerBallUVPoints(uv * uvScale, uvA, uvB, uvC, uvCenter, uvAB, uvBC, uvCA);

  a = PowerBallSpherePosition(uvA * uvScaleReciprical);
  b = PowerBallSpherePosition(uvB * uvScaleReciprical);
  c = PowerBallSpherePosition(uvC * uvScaleReciprical);
  center = PowerBallSpherePosition(uvCenter * uvScaleReciprical);
  ab = PowerBallSpherePosition(uvAB * uvScaleReciprical);
  bc = PowerBallSpherePosition(uvBC * uvScaleReciprical);
  ca = PowerBallSpherePosition(uvCA * uvScaleReciprical);
}

float PowerBallHexHeight(float3 centerPosition, float subdivisions)
{
  float scale = TIME_SECONDS * PowerBallAnimationScaleSpeed * Pi;
  scale -= subdivisions;

  float blend = dot(centerPosition, PowerBallFacePlane);
  blend = cos(blend * PowerBallAnimationOffset + scale) * 0.5 + 0.5;

  float height = lerp(PowerBallAnimationScale, 1.0, blend);

  return height;
}

float PowerBallHexDistance(
  float3 position,
  float3 edgeA,
  float3 edgeB,
  float height,
  out float edgeDistance)
{
  float edgeADistance = dot(position, edgeA);
  float edgeBDistance = dot(position, edgeB);
  edgeDistance = max(edgeADistance, -edgeBDistance);

  float outerDistance = length(position) - height;
  float distance = max(edgeDistance, outerDistance);

  return distance;
}

float PowerBallDistance(
  float3 position,
  out float3 closestHexCenter,
  out float closestEdgeDistance)
{
  position = PowerBallFoldedPosition(position);

  float selection = cos(TIME_SECONDS * PowerBallAnimationSubdivisionsSpeed * Pi) * 0.5 + 0.5;
  float subdivisions = lerp(PowerBallAnimationSubdivisionsMin, PowerBallAnimationSubdivisionsMax, selection);

  float3 a;
  float3 b;
  float3 c;
  float3 center;
  float3 ab;
  float3 bc;
  float3 ca;

  PowerBallGeodesicPoints(position, subdivisions, a, b, c, center, ab, bc, ca);

  float3 edgeAB = normalize(cross(center, ab));
  float3 edgeBC = normalize(cross(center, bc));
  float3 edgeCA = normalize(cross(center, ca));

  float height = PowerBallHexHeight(b, subdivisions);

  float edgeDistance;
  float closestDistance = PowerBallHexDistance(position, edgeAB, edgeBC, height, edgeDistance);
  closestHexCenter = b;
  closestEdgeDistance = edgeDistance;

  height = PowerBallHexHeight(c, subdivisions);

  float distance = PowerBallHexDistance(position, edgeBC, edgeCA, height, edgeDistance);

  if (distance < closestDistance)
  {
    closestDistance = distance;
    closestHexCenter = c;
    closestEdgeDistance = edgeDistance;
  }

  height = PowerBallHexHeight(a, subdivisions);

  distance = PowerBallHexDistance(position, edgeCA, edgeAB, height, edgeDistance);

  if (distance < closestDistance)
  {
    closestDistance = distance;
    closestHexCenter = a;
    closestEdgeDistance = edgeDistance;
  }

  return closestDistance;
}

half3 PowerBallPositionColor(float3 position, float3 hexCenter, float edgeDistance)
{
  float edgeColorSelection =
    (dot(hexCenter, PowerBallFacePlane) * 8.0) +
    length(position) +
    (TIME_SECONDS * PowerBallAnimationColorSpeed);

  half3 edgeColor = ColorFromSpectrum(edgeColorSelection);

  return lerp(edgeColor, PowerBallColor, edgeDistance * -16.0);
}

void RenderPowerBall(
  RenderState r,
  float3 ballPosition,
  float4 ballRotation,
  inout float hitDistance,
  inout half4 fragColor)
{
  float boundingSphereHitDistance = hitDistance;

  HitSphere(r.RayStart, r.RayDirection, ballPosition, PowerBallRadius, boundingSphereHitDistance);

  if (boundingSphereHitDistance < hitDistance)
  {
    float3 localRayStart = r.RayStart - ballPosition;
    localRayStart *= PowerBallRadiusReciprical;
    localRayStart = RotateVector(localRayStart, ballRotation);

    float3 localRayDirection = RotateVector(r.RayDirection, ballRotation);

    float stepDistance = boundingSphereHitDistance * PowerBallRadiusReciprical;

    for (int i = 0; i < PowerBallHitSteps; i++)
    {
      float3 stepPosition = (stepDistance * localRayDirection) + localRayStart;

      float3 hexCenter;
      float edgeDistance;
      float distance = PowerBallDistance(stepPosition, hexCenter, edgeDistance);

      if (distance < PowerBallHitEpsilon)
      {
        hitDistance = stepDistance * PowerBallRadius;

        half3 ballColor = PowerBallPositionColor(stepPosition, hexCenter, edgeDistance);
        RenderColor(ballColor, fragColor);

        break;
      }

      stepDistance += distance * PowerBallHitDeltaDistanceScale;
    }
  }
}

void RenderPowerBallGlow(
  RenderState r,
  float hitDistance,
  inout half4 fragColor)
{
  RenderFogSphere(
    r,
    hitDistance,
    PowerBallPosition,
    PowerBallGlowRadius,
    PowerBallGlowDensity,
    PowerBallColor,
    fragColor);
}

void RenderPowerBallSpawningBeam(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  if (PowerBallProgressInverse > 0.0)
  {
    float cylinderHitDistance = hitDistance;
    float hitDistanceCenter;

    HitCylinderAxisAlignedInfinite(
      r.RayStart,
      r.RayDirection,
      GeneratorPosition,
      Down,
      PowerBallRadius,
      cylinderHitDistance,
      hitDistanceCenter);

    if (cylinderHitDistance < hitDistance)
    {
      float3 hitPosition = (cylinderHitDistance * r.RayDirection) + r.RayStart;

      if ((hitPosition.y >= PowerBallPosition.y) && (hitPosition.y < GeneratorPosition.y))
      {
        hitDistance = cylinderHitDistance;

        float opacity = 1.0 - saturate(hitDistanceCenter / PowerBallGlowRadius);
        opacity *= PowerBallProgressInverse;

        RenderColor(PowerBallColor, opacity, fragColor);
      }
    }
  }
}

void AddPowerBallLight(
  float3 surfacePosition,
  float3 surfaceNormal,
  inout half3 renderColor)
{
  AverageLightDiffuse(
    surfacePosition,
    surfaceNormal,
    PowerBallPosition,
    PowerBallColor,
    PowerBallBrightness,
    PowerBallAttenuation,
    renderColor);
}

#endif
