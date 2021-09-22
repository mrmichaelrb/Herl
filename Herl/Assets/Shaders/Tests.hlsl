#ifndef TESTS_HLSL
#define TESTS_HLSL

#include "Common.hlsl"
#include "Boxes.hlsl"
#include "Rendering.hlsl"
#include "Sky.hlsl"
#include "Spheres.hlsl"
#include "Tf.hlsl"

uniform sampler2D TestTexture;
uniform sampler2D TestNormalMap;

static const float TestDistance = -2000.0;
static const float TestSpacing = -500.0;
static const float TestAltitude = 1000.0;
static const half3 TestColor = half3(0.1, 0.4, 0.8);
static const float TestScale = 200.0f;
static const float TestReflectivity = 0.3;
static const float TestShininess = 8.0;
static const float TestLodBias = 0.0;
static const float TestTextureSize = 512.0;
static const float TestNormalMapSize = 512.0;
static const float2 TestSphereNormalMapScale = float2(3.0 * 8.0, 8.0);
static const float2 TestRectangleTextureScale = float2(10.0, 10.0);

void RenderTests(
  RenderState r,
  inout float terrainHitDistance,
  inout float hitDistance,
  inout half4 fragColor)
{
  float testlocation = TestDistance;

  RenderDiffuseSphere(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    TestScale * 0.5,
    TestColor,
    terrainHitDistance,
    fragColor);
    
  testlocation += TestSpacing;
  
  RenderDiffuseSpecularSphere(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    TestScale * 0.5,
    TestReflectivity,
    TestShininess,
    TestColor,
    terrainHitDistance,
    fragColor);
    
  testlocation += TestSpacing;
  
  RenderDiffuseSpecularSphereWithNormalMapUp(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    TestScale * 0.5,
    TestReflectivity,
    TestShininess,
    TestColor,
    0.0,
    TestNormalMap,
    TestNormalMapSize,
    TestSphereNormalMapScale,
    terrainHitDistance,
    fragColor);
    
  testlocation += TestSpacing;
  
  RenderDiffuseSpecularRectangleWithNormalMap(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    Planemo1Rotation,
    float2(TestScale, TestScale * 0.5),
    TestReflectivity,
    TestShininess,
    TestColor,
    TestLodBias,
    TestNormalMap,
    TestNormalMapSize,
    UvNoOffset,
    float2(50.0, 50.0),
    terrainHitDistance,
    fragColor);

  testlocation += TestSpacing;
  
  RenderUnlitTextureRectangle(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    Planemo2Rotation,
    float2(TestScale, TestScale * 0.5),
    TestLodBias,
    TestTexture,
    TestTextureSize,
    UvNoOffset,
    TestRectangleTextureScale,
    UvNoScale,
    terrainHitDistance,
    fragColor);

  testlocation += TestSpacing;
  
  RenderDiffuseAABB(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    float3(TestScale, TestScale * 0.5, TestScale * 0.25),
    TestColor,
    terrainHitDistance,
    fragColor);
    
  testlocation += TestSpacing;
  
  RenderDiffuseBox(
    r,
    WorldPosition + float3(testlocation, TestAltitude, 0.0),
    Planemo1Rotation,
    float3(TestScale, TestScale * 0.5, TestScale * 0.25),
    TestColor,
    terrainHitDistance,
    fragColor);
}

#endif