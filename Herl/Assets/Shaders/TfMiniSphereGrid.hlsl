#ifndef TFMINISPHEREGRID_HLSL
#define TFMINISPHEREGRID_HLSL

#include "Mandelbulb.hlsl"
#include "SphereGrids.hlsl"
#include "Tf.hlsl"

static const int TfMiniSphereOuterRings = 32;
static const float TfMiniSphereSphereSpacing = 256.0;
static const float TfMiniSphereSphereRadius = 16.0;

static const int TfMiniSphereNoiseSampleCount = 2;
static const int TfMiniSphereNoiseSampleCountFar = 1;
static const float TfMiniSphereNoiseScale = 0.0006;
static const float TfMiniSphereNoiseSpeed = 32.0;
static const float TfMiniSphereNoiseDensity = 0.45;
static const float TfMiniSphereNoiseLocationFactor = 1.62;
static const float TfMiniSphereColorFactor = 0.25;

class Tf1MiniSphere : IGridSphere
{
  void Render(
    inout RenderState r,
    float3 position,
    float radius,
    inout float hitDistance,
    inout half4 fragColor)
  {
    RenderTfSphere(
      r,
      position,
      radius,
      Tf1PrimaryColor,
      Tf1SecondaryColor,
      hitDistance,
      fragColor);
  }
};

class Tf2MiniSphere : IGridSphere
{
  void Render(
    inout RenderState r,
    float3 position,
    float radius,
    inout float hitDistance,
    inout half4 fragColor)
  {
    RenderMandelbulbUp(
      r,
      position,
      radius,
      Tf2PrimaryColor,
      Tf2SecondaryColor,
      hitDistance,
      fragColor);
  }
};

void RenderTfMiniSphereGrids(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  Tf1MiniSphere tf1MiniSphere;
  Tf2MiniSphere tf2MiniSphere;

  RenderSphereGrid(
    r,
    float3(Tf1Position.x, Tf1Position.y + Tf1SphereRadius, Tf1Position.z),
    TfMiniSphereOuterRings,
    TfMiniSphereSphereSpacing,
    TfMiniSphereSphereRadius,
    tf1MiniSphere,
    hitDistance,
    fragColor);

  RenderSphereGrid(
    r,
    float3(Tf2Position.x, Tf2Position.y + Tf2SphereRadius, Tf2Position.z),
    TfMiniSphereOuterRings,
    TfMiniSphereSphereSpacing,
    TfMiniSphereSphereRadius,
    tf2MiniSphere,
    hitDistance,
    fragColor);
}

#endif