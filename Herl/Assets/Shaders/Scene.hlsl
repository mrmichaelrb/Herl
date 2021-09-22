#ifndef SCENE_HLSL
#define SCENE_HLSL

#include "Common.hlsl"
#include "Arbiter.hlsl"
#include "Cylinders.hlsl"
#include "Generator.hlsl"
#include "PowerBall.hlsl"
#include "Rectangles.hlsl"
#include "Rendering.hlsl"
#include "Sky.hlsl"
#include "Spheres.hlsl"
#include "Terrain.hlsl"
#include "TerrainRendering.hlsl"
#include "Tests.hlsl"
#include "Tf.hlsl"
#include "TfMiniSphereGrid.hlsl"
#include "Water.hlsl"

// Comment out defines to disable parts of the scene
//#define SCENE_RENDER_TESTS
#define SCENE_RENDER_ARBITER
#define SCENE_RENDER_CLOUDS
#define SCENE_RENDER_GENERATOR
#define SCENE_RENDER_POWER_BALL
#define SCENE_RENDER_TERRAIN
#define SCENE_RENDER_TFS
#define SCENE_RENDER_WATER_REFLECTION

void RenderSceneStage1(
  RenderState r,
  inout float terrainHitDistance,
  inout float hitDistance,
  inout half4 fragColor)
{
  // Render opaque objects near the terrain (in increasing level of processing expense)
  // Simpler objects can obscure and prevent complex objects from rendering
#ifdef SCENE_RENDER_TFS
  RenderTfSpheres(r, terrainHitDistance, fragColor);
  RenderTfCylinders(r, terrainHitDistance, fragColor);
  RenderTfMiniSphereGrids(r, terrainHitDistance, fragColor);
#endif

#ifdef SCENE_RENDER_POWER_BALL
  RenderPowerBall(r, PowerBallPosition, PowerBallRotation, terrainHitDistance, fragColor);
#endif

#ifdef SCENE_RENDER_TESTS
  RenderTests(r, terrainHitDistance, hitDistance, fragColor);
#endif
}

void RenderSceneStage2(
  inout RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  // Render opaque objects in space
#ifdef SCENE_RENDER_GENERATOR
  RenderGenerator(
    r,
    GeneratorPosition,
    hitDistance,
    fragColor);
#endif

  RenderPlanemos(r, hitDistance, fragColor);
}

void RenderSceneStage3(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  // Limit rendering to terrain clip distance
  float terrainHitDistance = min(hitDistance, TerrainFarClipDistance);

  // Render transparent objects near the terrain
#ifdef SCENE_RENDER_ARBITER
  RenderArbiter(r, terrainHitDistance, ArbiterPosition, fragColor);
#endif

#ifdef SCENE_RENDER_TFS
  RenderTfShields(r, terrainHitDistance, fragColor);
#endif

#ifdef SCENE_RENDER_POWER_BALL
  RenderPowerBallGlow(r, terrainHitDistance, fragColor);
  RenderPowerBallSpawningBeam(r, terrainHitDistance, fragColor);
#endif

  // Render transparent objects in space
#ifdef SCENE_RENDER_GENERATOR
  RenderGeneratorGlow(
    r,
    hitDistance,
    GeneratorPosition,
    fragColor);
#endif
}

void RenderScene(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  // Limit rendering to terrain clip distance
  float terrainHitDistance = min(hitDistance, TerrainFarClipDistance);

  RenderSceneStage1(r, terrainHitDistance, hitDistance, fragColor);

#ifdef SCENE_RENDER_TERRAIN
  RenderTerrain(r, terrainHitDistance, fragColor);
#endif

  RenderWater(r, terrainHitDistance, fragColor);

  // Allow rendering for objects farther away than the terrain
  if (terrainHitDistance < TerrainFarClipDistance)
  {
    hitDistance = terrainHitDistance;
  }

  RenderSceneStage2(r, hitDistance, fragColor);

  if (isinf(hitDistance))
  {
    RenderBackground(r, fragColor);
  }
  else
  {
    RenderAtmosphereFog(r, hitDistance, 1.0, fragColor);
  }

#ifdef SCENE_RENDER_CLOUDS
  RenderClouds(r, hitDistance, fragColor);
#endif

  RenderSceneStage3(r, hitDistance, fragColor);
}

void RenderSceneFromWaterReflection(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
#ifdef SCENE_RENDER_WATER_REFLECTION
  // Limit rendering to terrain clip distance
  float terrainHitDistance = min(hitDistance, TerrainFarClipDistance);

  RenderSceneStage1(r, terrainHitDistance, hitDistance, fragColor);

#ifdef SCENE_RENDER_TERRAIN
  RenderTerrainFromWaterReflection(r, terrainHitDistance, fragColor);
#endif

  // Allow rendering for objects farther away than the terrain
  if (terrainHitDistance < TerrainFarClipDistance)
  {
    hitDistance = terrainHitDistance;
  }

  RenderSceneStage2(r, hitDistance, fragColor);

  if (isinf(hitDistance))
  {
    RenderBackgroundFromWaterReflection(r, fragColor);
  }
  else
  {
    RenderAtmosphereFog(r, hitDistance, 1.0, fragColor);
  }

#ifdef SCENE_RENDER_CLOUDS
  RenderCloudsFromWaterReflection(r, hitDistance, fragColor);
#endif

  RenderSceneStage3(r, hitDistance, fragColor);
#endif
}

#endif
