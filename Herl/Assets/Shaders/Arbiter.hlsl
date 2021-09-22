#ifndef ARBITER_HLSL
#define ARBITER_HLSL

#include "Common.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"

static const float ArbiterRadius = 800.0;
static const int ArbiterFilamentSampleCount = 16;
static const int ArbiterFilamentNoiseSampleCount = 6;
static const float ArbiterFilamentNoiseScale = 0.005;
static const float ArbiterFilamentDensity = 0.125;
static const float ArbiterFilamentInnerEpsilon = 0.01;
static const half3 ArbiterFilamentInnerColor = half3(2.0, 2.0, 2.0);
static const float ArbiterFilamentOuterEpsilon = 0.1;
static const half3 ArbiterFilamentOuterColor = half3(0.0, 2.0, 2.0);
static const float ArbiterFilamentSpeed = 0.25;
static const float ArbiterSphereDensity = 0.2;
static const half3 ArbiterSphereColor = half3(1.0, 1.0, 1.0);

uniform float3 ArbiterPosition;

void RenderArbiter(
  inout RenderState r,
  float hitDistance,
  float3 position,
  inout half4 fragColor)
{
  RenderFilamentSphere(
    r,
    hitDistance,
    position,
    ArbiterRadius,
    ArbiterFilamentSampleCount,
    ArbiterFilamentNoiseSampleCount,
    ArbiterFilamentNoiseScale,
    ArbiterFilamentDensity,
    ArbiterFilamentInnerEpsilon,
    ArbiterFilamentInnerColor,
    ArbiterFilamentOuterEpsilon,
    ArbiterFilamentOuterColor,
    ArbiterFilamentSpeed,
    fragColor);

  RenderTransparentSphere(
    r,
    hitDistance,
    position,
    ArbiterRadius,
    ArbiterSphereDensity,
    ArbiterSphereColor,
    fragColor);
}

#endif
