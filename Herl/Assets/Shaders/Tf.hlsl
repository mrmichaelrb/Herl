#ifndef TF_HLSL
#define TF_HLSL

#include "Common.hlsl"
#include "Cylinders.hlsl"
#include "Mandelbulb.hlsl"
#include "Rendering.hlsl"
#include "Spheres.hlsl"

static const half3 Tf1PrimaryColor = half3(0.0, 2.0, 2.0);
static const half3 Tf1SecondaryColor = half3(0.0, 0.0, 4.0);
static const half3 Tf2PrimaryColor = half3(1.8, 0.2, 1.8);
static const half3 Tf2SecondaryColor = half3(4.0, 0.0, 0.0);
static const float Tf1SphereRadius = 400.0;
static const float Tf2SphereRadius = 400.0;
static const half3 TfShellColor = half3(0.1, 0.1, 0.1);
static const float TfShellInitialIntensity = 0.01;
static const float TfShellIntensity = 1.5;
static const int TfShells = 16;
static const int TfShellMaxIntensity = 12;
static const float TfShellRadius = 0.95;
static const float TfShellInitialSpeed = 1.0;
static const float TfShellSpeed = 1.07;
static const float TfShellReflectivity = 0.3;
static const float TfShellShininess = 8.0;
static const float TfShellBottomReflectivity = TfShellReflectivity * 0.5;
static const float TfShellBottomShininess = TfShellShininess * 2.0;
static const float TfShellTopHole = 0.96;
static const float TfShellStripWidth = 22.5;
static const float TfShellStripSpace = 2.5;
static const int TfShellHoles = 3;
static const float TfShellHoleRadius = 0.9;
static const float TfShellHoleAngleIncrement = TwoPi / TfShellHoles;
static const float TfShellBitPatternNoiseScale = 1024.0;
static const float TfShellBitPatternNoiseLimit = 0.08;
static const float TfShellNormalMapSize = 512.0;
static const float2 TfShellNormalMapScale = float2(3.0 * 8.0, 8.0);
static const float TfCoreRadius = 0.3;
static const float TfCylinderOffset = 0.0;
static const float TfCylinderRadius = 60.0;
static const float TfCylinderNoiseSpeed = 256.0;
static const float TfCylinderNoiseScale = 0.0003;
static const int TfCylinderNoiseSampleCount = 2;
static const float TfCylinderNoiseDensity = 0.45;
static const float TfCylinderNoiseLocationFactor = 2.01;
static const float TfShieldDensity = 0.4;
static const half3 TfShieldColor = half3(0.0, 1.0, 0.0);

uniform sampler2D TfNormalMap;
uniform float3 Tf1Position;
uniform float3 Tf2Position;

void RenderTfCylinder(
  RenderState r,
  float3 tfPosition,
  half3 primaryColor,
  half3 secondaryColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float cylinderHitDistance = hitDistance;
  float hitDistanceCenter;

  HitCylinderAxisAlignedInfinite(
    r.RayStart,
    r.RayDirection,
    tfPosition,
    Down,
    TfCylinderRadius,
    cylinderHitDistance,
    hitDistanceCenter);

  if (cylinderHitDistance < hitDistance)
  {
    float3 hitPosition = (cylinderHitDistance * r.RayDirection) + r.RayStart;

    if (hitPosition.y < (tfPosition.y + TfCylinderOffset))
    {
      hitDistance = cylinderHitDistance;

      float3 noisePosition = (TIME_SECONDS * TfCylinderNoiseSpeed) + hitPosition - WorldPosition;

      float noise = SmokeNoise(
        noisePosition * TfCylinderNoiseScale,
        TfCylinderNoiseSampleCount,
        TfCylinderNoiseDensity,
        TfCylinderNoiseLocationFactor);

      half3 cylinderColor = lerp(primaryColor, secondaryColor, noise);
      cylinderColor = lerp(cylinderColor, secondaryColor, (hitDistanceCenter / TfCylinderRadius));

      RenderColor(cylinderColor, fragColor);
    }
  }
}

void RenderTfCylinders(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  RenderTfCylinder(
    r,
    Tf1Position,
    Tf1PrimaryColor,
    Tf1SecondaryColor,
    hitDistance,
    fragColor);

  RenderTfCylinder(
    r,
    Tf2Position,
    Tf2PrimaryColor,
    Tf2SecondaryColor,
    hitDistance,
    fragColor);
}

void RenderTfSphere(
  RenderState r,
  float3 tfPosition,
  float tfSphereRadius,
  half3 primaryColor,
  half3 secondaryColor,
  inout float hitDistance,
  inout half4 fragColor)
{
  float shellRadius = tfSphereRadius;
  float shellSpeed = TfShellInitialSpeed;

  float shellHitDistance = hitDistance;
  float shellHitDistanceFar;

  HitSphereBothSides(
    r.RayStart,
    r.RayDirection,
    tfPosition,
    shellRadius,
    shellHitDistance,
    shellHitDistanceFar);

  if (shellHitDistance < hitDistance)
  {
    half3 shellIntensityPrimary = primaryColor * TfShellInitialIntensity;
    half3 shellIntensitySecondary = secondaryColor * TfShellInitialIntensity;
    float shellReverser = 1.0;

    for (int shellIndex = 0; shellIndex < TfShells; shellIndex++)
    {
      float3 hitPosition = (shellHitDistance * r.RayDirection) + r.RayStart;
      float3 hitPositionFar = (shellHitDistanceFar * r.RayDirection) + r.RayStart;

      float shellTopHoleHeight = (shellRadius * TfShellTopHole) + tfPosition.y;

      float shellAngle = TIME_SECONDS * shellSpeed * shellReverser;
      float holeAngle = shellAngle;
      shellReverser = -shellReverser;

      float holeRadius = shellRadius * TfShellHoleRadius;

      bool nearHitInHole = hitPosition.y > shellTopHoleHeight;
      bool farHitInHole = hitPositionFar.y > shellTopHoleHeight;

      nearHitInHole = nearHitInHole || IsOnStrip(hitPosition.y, TfShellStripWidth, TfShellStripSpace);
      farHitInHole = farHitInHole || IsOnStrip(hitPositionFar.y, TfShellStripWidth, TfShellStripSpace);

      if ((!nearHitInHole) || (!farHitInHole))
      {
        for (int holeIndex = 0; holeIndex < TfShellHoles; holeIndex++)
        {
          float sineHoleAngle;
          float cosineHoleAngle;
          sincos(holeAngle, sineHoleAngle, cosineHoleAngle);

          float holeX = (cosineHoleAngle * shellRadius) + tfPosition.x;
          float holeZ = (sineHoleAngle * shellRadius) + tfPosition.z;
          float3 holePosition = float3(holeX, tfPosition.y, holeZ);

          nearHitInHole = nearHitInHole || IsInSphere(hitPosition, holePosition, holeRadius);
          farHitInHole = farHitInHole || IsInSphere(hitPositionFar, holePosition, holeRadius);

          holeAngle += TfShellHoleAngleIncrement;
        }
      }

      if (!nearHitInHole)
      {
        float2 uv = SphereMappingUp(hitPosition, tfPosition, shellRadius);
        uv.x -= (shellAngle * TwoPiUnderOne);

        float2 uvBitPattern = floor(uv * TfShellBitPatternNoiseScale);

        if (Float2Rand(uvBitPattern) > TfShellBitPatternNoiseLimit)
        {
          hitDistance = shellHitDistance;

          half3 shellColor;

          if (shellIndex > TfShellMaxIntensity)
          {
            shellColor = shellIntensitySecondary;
          }
          else
          {
            uv *= TfShellNormalMapScale;

            float3 surfaceNormal = normalize(hitPosition - tfPosition);

            shellColor = shellIntensitySecondary +
              DiffuseSpecularSphereColorWithNormalMap(
                r.RayDirection,
                hitDistance,
                shellRadius,
                TfShellReflectivity,
                TfShellShininess,
                TfShellColor,
                surfaceNormal,
                NoBias,
                TfNormalMap,
                TfShellNormalMapSize,
                TfShellNormalMapScale,
                uv);

            AddDirectionalLightSpecular(
              r.RayDirection,
              Up,
              primaryColor,
              surfaceNormal,
              TfShellBottomReflectivity,
              TfShellBottomShininess,
              shellColor);
          }

          RenderColor(shellColor, fragColor);

          break;
        }
      }

      if (!farHitInHole)
      {
        float2 uv = SphereMappingUp(hitPositionFar, tfPosition, shellRadius);
        uv.x -= (shellAngle * TwoPiUnderOne);

        float2 uvBitPattern = floor(uv * TfShellBitPatternNoiseScale);

        if (Float2Rand(uvBitPattern) > TfShellBitPatternNoiseLimit)
        {
          if (shellHitDistanceFar < hitDistance)
          {
            hitDistance = shellHitDistanceFar;

            half3 shellColor;

            if (shellIndex >= TfShellMaxIntensity)
            {
              shellColor = shellIntensityPrimary;
            }
            else
            {
              shellColor = shellIntensityPrimary +
                DiffuseSpecularSphereInteriorColor(
                  r.RayDirection,
                  hitPosition,
                  tfPosition,
                  TfShellReflectivity,
                  TfShellShininess,
                  TfShellColor);
            }

            RenderColor(shellColor, fragColor);
          }
        }
      }

      shellRadius *= TfShellRadius;
      shellSpeed *= TfShellSpeed;
      shellIntensityPrimary *= TfShellIntensity;
      shellIntensitySecondary *= TfShellIntensity;

      shellHitDistance = hitDistance;

      HitSphereBothSides(
        r.RayStart,
        r.RayDirection,
        tfPosition,
        shellRadius,
        shellHitDistance,
        shellHitDistanceFar);

      if (shellHitDistance >= hitDistance)
      {
        break;
      }
    }

    float coreHitDistance = hitDistance;

    HitSphere(r.RayStart, r.RayDirection, tfPosition, tfSphereRadius * TfCoreRadius, coreHitDistance);

    if (coreHitDistance < hitDistance)
    {
      hitDistance = coreHitDistance;

      float3 hitCorePosition = (coreHitDistance * r.RayDirection) + r.RayStart;

      half3 coreColor = GlowSphereColor(
        r.RayDirection,
        hitCorePosition,
        tfPosition,
        primaryColor,
        secondaryColor);

      RenderColor(coreColor, fragColor);
    }
  }
}

void RenderTfSpheres(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  RenderTfSphere(
    r,
    Tf1Position,
    Tf1SphereRadius * TfShellRadius,
    Tf1PrimaryColor,
    Tf1SecondaryColor,
    hitDistance,
    fragColor);

  RenderMandelbulbUp(
    r,
    Tf2Position,
    Tf2SphereRadius * TfShellRadius,
    Tf2PrimaryColor,
    Tf2SecondaryColor,
    hitDistance,
    fragColor);
}

void RenderTfShields(
  RenderState r,
  inout float hitDistance,
  inout half4 fragColor)
{
  RenderTransparentSphereBack(
    r,
    hitDistance,
    Tf1Position,
    Tf1SphereRadius,
    TfShieldDensity,
    TfShieldColor,
    fragColor);

  RenderTransparentSphereBack(
    r,
    hitDistance,
    Tf2Position,
    Tf2SphereRadius,
    TfShieldDensity,
    TfShieldColor,
    fragColor);
}

#endif
