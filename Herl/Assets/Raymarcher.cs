using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Camera))]
public class Raymarcher : MonoBehaviour
{
  // Closest possible (1 = closest, 0 = farthest)
  const float DisplayDistance = 0.0f;
  const float IrisDistance = 1.0f;

  const float IrisMaximum = 1.0f;
  const float IrisIncrement = 0.05f;

  // Allows display size some allowance to stay full sized during temporary
  // framerate drops
  const float MaximumIris = 1.05f;

  static readonly float s_sqrt2 = Mathf.Sqrt(2.0f);

  const int SquareVerticesCount = 4;
  const int OctagonVerticesCount = 8;
  const int IrisVerticesCount = 12;

  static readonly Vector3[] s_squareVertices = new Vector3[SquareVerticesCount]
  {
    new Vector3(-1.0f, -1.0f, DisplayDistance),
    new Vector3(1.0f, -1.0f, DisplayDistance),
    new Vector3(1.0f, 1.0f, DisplayDistance),
    new Vector3(-1.0f, 1.0f, DisplayDistance),
  };

  static readonly int[] s_squareIndices = new int[6]
  {
    0, 1, 2,
    2, 3, 0,
  };

  // Octagon formed from six (not eight) triangles
  static readonly int[] s_octagonIndices = new int[18]
  {
    // Main square
    0, 1, 2,
    2, 3, 0,

    // Top, bottom, and sides
    0, 4, 1,
    1, 5, 2,
    2, 6, 3,
    3, 7, 0,
  };

  // Opposite of octagon being rendered to
  static readonly int[] s_irisIndices = new int[36]
  {
    0, 8, 4,
    4, 8, 9,
    4, 9, 1,

    1, 9, 5,
    5, 9, 10,
    5, 10, 2,

    2, 10, 6,
    6, 10, 11,
    6, 11, 3,

    3, 11, 7,
    7, 11, 8,
    7, 8, 0,
  };

  static int s_propertyIdHashOffset;
  static int s_propertyIdHashOffsetStatic;

  static int s_propertyIdNoiseTextureSize;
  static int s_propertyIdNoiseTexelSize;
  static int s_propertyIdNoiseTextureScale;
  static int s_propertyIdNoiseTextureOffset;
  static int s_propertyIdNoiseTexture;

  static int s_propertyIdRenderDistancePixelRatio;
  static int s_propertyIdRenderDistanceMin;

  static int s_propertyIdWorldPosition;

  static int s_propertyIdTerrainStepsMax;
  static int s_propertyIdTerrainFarClipDistance;
  static int s_propertyIdTerrainHitEpsilon;
  static int s_propertyIdTerrainHitStepInitial;
  static int s_propertyIdTerrainHitDeltaHeightScale;
  static int s_propertyIdTerrainHitDeltaDistanceScale;
  static int s_propertyIdTerrainHitDeltaHintScale;
  static int s_propertyIdTerrainCeiling;
  static int s_propertyIdTerrainInitialNoiseScale;
  static int s_propertyIdTerrainNoiseScale;
  static int s_propertyIdTerrainInitialOffset;
  static int s_propertyIdTerrainInitialFactor;
  static int s_propertyIdTerrainSampleVariation;
  static int s_propertyIdTerrainTerrainInitialNoiseRotation;
  static int s_propertyIdTerrainTerrainTwistiness;
  static int s_propertyIdTerrainHeightAdjustment;
  static int s_propertyIdTerrainHighDefNoiseScale;
  static int s_propertyIdTerrainHighDefSampleVariation;
  static int s_propertyIdTerrainIridescenceFactor;

  static int s_propertyIdTerrainShadowStepsMax;
  static int s_propertyIdTerrainShadowHitStepInitial;
  static int s_propertyIdTerrainShadowHitStepDistanceMax;

  static int s_propertyIdWaterColor;
  static int s_propertyIdWaterDeepColor;
  static int s_propertyIdWaterCeiling;
  static int s_propertyIdWaterReflectivity;
  static int s_propertyIdWaterShininess;
  static int s_propertyIdWaterHeight;
  static int s_propertyIdWaterFloor;
  static int s_propertyIdWaterDeepFloor;
  static int s_propertyIdWaterBlendHeight;
  static int s_propertyIdWaterBlendCeiling;
  static int s_propertyIdWaterWaveHeightRatio;
  static int s_propertyIdWaterWaveSpeed;
  static int s_propertyIdWaterInitialNoiseScale;
  static int s_propertyIdWaterNoiseScale;

  static int s_propertyIdBeachColor;
  static int s_propertyIdBeachHeight;
  static int s_propertyIdBeachCeiling;

  static int s_propertyIdLandColor;

  static int s_propertyIdLGrassColor;
  static int s_propertyIdLGrassColorVariation;
  static int s_propertyIdLGrassCeiling;
  static int s_propertyIdLGrassSlopeNormalMin;

  static int s_propertyIdTreeColor;
  static int s_propertyIdTreeHeight;
  static int s_propertyIdTreeLineFloor;
  static int s_propertyIdTreeLineHeight;
  static int s_propertyIdTreeNoiseScale;
  static int s_propertyIdTreeBranchNoiseScale;
  static int s_propertyIdTreeThicknessNoiseScale;
  static int s_propertyIdTreeThicknessFactor;

  static int s_propertyIdSnowColor;
  static int s_propertyIdSnowFloor;
  static int s_propertyIdSnowFadeHeight;
  static int s_propertyIdSnowReflectivity;
  static int s_propertyIdSnowShininess;
  static int s_propertyIdSnowSlopeNormalMin;

  static int s_propertyIdAtmosphereAttenuatedColor;
  static int s_propertyIdAtmosphereCeiling;
  static int s_propertyIdAtmosphereOrbSize;
  static int s_propertyIdAtmosphereHardness;
  static int s_propertyIdAtmosphereCelestialOrbSize;
  static int s_propertyIdAtmosphereCelestialHardness;

  static int s_propertyIdCloudColor;
  static int s_propertyIdCloudFarClipDistance;
  static int s_propertyIdCloudFloor;
  static int s_propertyIdCloudCeiling;
  static int s_propertyIdCloudHeightReciprical;
  static int s_propertyIdCloudHeightPiReciprical;
  static int s_propertyIdCloudLayer;
  static int s_propertyIdCloudNoiseScale;
  static int s_propertyIdCloudNoiseLocationFactor;
  static int s_propertyIdCloudOffset;
  static int s_propertyIdCloudDensity;
  static int s_propertyIdCloudHardness;
  static int s_propertyIdCloudCover;
  static int s_propertyIdCloudMaximumObscurance;

  static int s_propertyIdSun1Direction;
  static int s_propertyIdSun1Color;
  static int s_propertyIdSun1Size;
  static int s_propertyIdSun1Hardness;

  static int s_propertyIdSunlightColor;
  static int s_propertyIdSunlightCelestialColor;
  static int s_propertyIdSunlightAmbientColor;
  static int s_propertyIdSunlightDirection;

  static int s_propertyIdTfNormalMap;
  static int s_propertyIdTf1Position;
  static int s_propertyIdTf2Position;

  static int s_propertyIdGeneratorPosition;

  static int s_propertyIdArbiterPosition;

  static int s_propertyIdPowerBallPosition;
  static int s_propertyIdPowerBallRotation;
  static int s_propertyIdPowerBallGlowRadius;
  static int s_propertyIdPowerBallRadius;
  static int s_propertyIdPowerBallRadiusReciprical;
  static int s_propertyIdPowerBallProgressInverse;

  static int s_propertyIdNebulaPosition;
  static int s_propertyIdNebulaRotation;
  static int s_propertyIdNebulaScale;
  static int s_propertyIdNebulaTexture;

  static int s_propertyIdPlanemo1Position;
  static int s_propertyIdPlanemo1Rotation;
  static int s_propertyIdPlanemo1Radius;
  static int s_propertyIdPlanemo1Texture;

  static int s_propertyIdPlanemo2Position;
  static int s_propertyIdPlanemo2Rotation;
  static int s_propertyIdPlanemo2Radius;
  static int s_propertyIdPlanemo2Texture;

  static int s_propertyIdWaveCenter;
  static int s_propertyIdWaveColor;
  static int s_propertyIdWaveDistanceSquared;
  static int s_propertyIdWaveWidthReciprical;
  static int s_propertyIdWaveIntensity;
  static int s_propertyIdWaveHeight;

  public Material DisplayMaterial;
  public Material IrisMaterial;

  public float DisplaySize = 1.0f;

  public bool AutomaticIris = true;

  [Range(0.0f, 1.0f)]
  public float AutomaticIrisSpeed = 0.05f;

  [Range(0.0f, 1.0f)]
  public float MinimumIris = 0.2f;

  [Range(1.0f / 90.0f, 1.0f / 25.0f)]
  public float TargetDeltaTime = 1.0f / 75.0f;

  World _world;
  Camera _camera;
  Mesh _displayMesh;
  Mesh _irisMesh;
  float _lastSize;

  readonly Vector3[] _octagonVertices = new Vector3[OctagonVerticesCount];
  readonly Vector3[] _irisVertices = new Vector3[IrisVerticesCount];

  bool _render = true;

  bool _previousSmallerIris;
  bool _nextLargerIris;

  void InitializeIrisVertices()
  {
    _irisVertices[8].Set(-1.5f, -1.5f, IrisDistance);
    _irisVertices[9].Set(1.5f, -1.5f, IrisDistance);
    _irisVertices[10].Set(1.5f, 1.5f, IrisDistance);
    _irisVertices[11].Set(-1.5f, 1.5f, IrisDistance);
  }

  void InitializeShaderPropertyIds()
  {
    s_propertyIdHashOffset = Shader.PropertyToID("HashOffset");
    s_propertyIdHashOffsetStatic = Shader.PropertyToID("HashOffsetStatic");

    s_propertyIdNoiseTextureSize = Shader.PropertyToID("NoiseTextureSize");
    s_propertyIdNoiseTexelSize = Shader.PropertyToID("NoiseTexelSize");
    s_propertyIdNoiseTextureScale = Shader.PropertyToID("NoiseTextureScale");
    s_propertyIdNoiseTextureOffset = Shader.PropertyToID("NoiseTextureOffset");
    s_propertyIdNoiseTexture = Shader.PropertyToID("NoiseTexture");

    s_propertyIdRenderDistancePixelRatio = Shader.PropertyToID("RenderDistancePixelRatio");
    s_propertyIdRenderDistanceMin = Shader.PropertyToID("RenderDistanceMin");

    s_propertyIdWorldPosition = Shader.PropertyToID("WorldPosition");

    s_propertyIdTerrainStepsMax = Shader.PropertyToID("TerrainStepsMax");
    s_propertyIdTerrainFarClipDistance = Shader.PropertyToID("TerrainFarClipDistance");
    s_propertyIdTerrainHitEpsilon = Shader.PropertyToID("TerrainHitEpsilon");
    s_propertyIdTerrainHitStepInitial = Shader.PropertyToID("TerrainHitStepInitial");
    s_propertyIdTerrainHitDeltaHeightScale = Shader.PropertyToID("TerrainHitDeltaHeightScale");
    s_propertyIdTerrainHitDeltaDistanceScale = Shader.PropertyToID("TerrainHitDeltaDistanceScale");
    s_propertyIdTerrainHitDeltaHintScale = Shader.PropertyToID("TerrainHitDeltaHintScale");
    s_propertyIdTerrainCeiling = Shader.PropertyToID("TerrainCeiling");
    s_propertyIdTerrainInitialNoiseScale = Shader.PropertyToID("TerrainInitialNoiseScale");
    s_propertyIdTerrainNoiseScale = Shader.PropertyToID("TerrainNoiseScale");
    s_propertyIdTerrainInitialOffset = Shader.PropertyToID("TerrainInitialOffset");
    s_propertyIdTerrainInitialFactor = Shader.PropertyToID("TerrainInitialFactor");
    s_propertyIdTerrainSampleVariation = Shader.PropertyToID("TerrainSampleVariation");
    s_propertyIdTerrainTerrainInitialNoiseRotation = Shader.PropertyToID("TerrainInitialNoiseRotation");
    s_propertyIdTerrainTerrainTwistiness = Shader.PropertyToID("TerrainTwistiness");
    s_propertyIdTerrainHeightAdjustment = Shader.PropertyToID("TerrainHeightAdjustment");
    s_propertyIdTerrainHighDefNoiseScale = Shader.PropertyToID("TerrainHighDefNoiseScale");
    s_propertyIdTerrainHighDefSampleVariation = Shader.PropertyToID("TerrainHighDefSampleVariation");
    s_propertyIdTerrainIridescenceFactor = Shader.PropertyToID("TerrainIridescenceFactor");

    s_propertyIdTerrainShadowStepsMax = Shader.PropertyToID("TerrainShadowStepsMax");
    s_propertyIdTerrainShadowHitStepInitial = Shader.PropertyToID("TerrainShadowHitStepInitial");
    s_propertyIdTerrainShadowHitStepDistanceMax = Shader.PropertyToID("TerrainShadowHitStepDistanceMax");

    s_propertyIdWaterColor = Shader.PropertyToID("WaterColor");
    s_propertyIdWaterDeepColor = Shader.PropertyToID("WaterDeepColor");
    s_propertyIdWaterCeiling = Shader.PropertyToID("WaterCeiling");
    s_propertyIdWaterReflectivity = Shader.PropertyToID("WaterReflectivity");
    s_propertyIdWaterShininess = Shader.PropertyToID("WaterShininess");
    s_propertyIdWaterHeight = Shader.PropertyToID("WaterHeight");
    s_propertyIdWaterFloor = Shader.PropertyToID("WaterFloor");
    s_propertyIdWaterDeepFloor = Shader.PropertyToID("WaterDeepFloor");
    s_propertyIdWaterBlendHeight = Shader.PropertyToID("WaterBlendHeight");
    s_propertyIdWaterBlendCeiling = Shader.PropertyToID("WaterBlendCeiling");
    s_propertyIdWaterWaveHeightRatio = Shader.PropertyToID("WaterWaveHeightRatio");
    s_propertyIdWaterWaveSpeed = Shader.PropertyToID("WaterWaveSpeed");
    s_propertyIdWaterInitialNoiseScale = Shader.PropertyToID("WaterInitialNoiseScale");
    s_propertyIdWaterNoiseScale = Shader.PropertyToID("WaterNoiseScale");

    s_propertyIdBeachColor = Shader.PropertyToID("BeachColor");
    s_propertyIdBeachHeight = Shader.PropertyToID("BeachHeight");
    s_propertyIdBeachCeiling = Shader.PropertyToID("BeachCeiling");

    s_propertyIdLandColor = Shader.PropertyToID("LandColor");

    s_propertyIdLGrassColor = Shader.PropertyToID("GrassColor");
    s_propertyIdLGrassColorVariation = Shader.PropertyToID("GrassColorVariation");
    s_propertyIdLGrassCeiling = Shader.PropertyToID("GrassCeiling");
    s_propertyIdLGrassSlopeNormalMin = Shader.PropertyToID("GrassSlopeNormalMin");

    s_propertyIdTreeColor = Shader.PropertyToID("TreeColor");
    s_propertyIdTreeHeight = Shader.PropertyToID("TreeHeight");
    s_propertyIdTreeLineFloor = Shader.PropertyToID("TreeLineFloor");
    s_propertyIdTreeLineHeight = Shader.PropertyToID("TreeLineHeight");
    s_propertyIdTreeNoiseScale = Shader.PropertyToID("TreeNoiseScale");
    s_propertyIdTreeBranchNoiseScale = Shader.PropertyToID("TreeBranchNoiseScale");
    s_propertyIdTreeThicknessNoiseScale = Shader.PropertyToID("TreeThicknessNoiseScale");
    s_propertyIdTreeThicknessFactor = Shader.PropertyToID("TreeThicknessFactor");

    s_propertyIdSnowColor = Shader.PropertyToID("SnowColor");
    s_propertyIdSnowFloor = Shader.PropertyToID("SnowFloor");
    s_propertyIdSnowFadeHeight = Shader.PropertyToID("SnowFadeHeight");
    s_propertyIdSnowReflectivity = Shader.PropertyToID("SnowReflectivity");
    s_propertyIdSnowShininess = Shader.PropertyToID("SnowShininess");
    s_propertyIdSnowSlopeNormalMin = Shader.PropertyToID("SnowSlopeNormalMin");

    s_propertyIdAtmosphereAttenuatedColor = Shader.PropertyToID("AtmosphereAttenuatedColor");
    s_propertyIdAtmosphereCeiling = Shader.PropertyToID("AtmosphereCeiling");
    s_propertyIdAtmosphereOrbSize = Shader.PropertyToID("AtmosphereOrbSize");
    s_propertyIdAtmosphereHardness = Shader.PropertyToID("AtmosphereHardness");
    s_propertyIdAtmosphereCelestialOrbSize = Shader.PropertyToID("AtmosphereCelestialOrbSize");
    s_propertyIdAtmosphereCelestialHardness = Shader.PropertyToID("AtmosphereCelestialHardness");

    s_propertyIdCloudColor = Shader.PropertyToID("CloudColor");
    s_propertyIdCloudFarClipDistance = Shader.PropertyToID("CloudFarClipDistance");
    s_propertyIdCloudFloor = Shader.PropertyToID("CloudFloor");
    s_propertyIdCloudCeiling = Shader.PropertyToID("CloudCeiling");
    s_propertyIdCloudHeightReciprical = Shader.PropertyToID("CloudHeightReciprical");
    s_propertyIdCloudHeightPiReciprical = Shader.PropertyToID("CloudHeightPiReciprical");
    s_propertyIdCloudLayer = Shader.PropertyToID("CloudLayer");
    s_propertyIdCloudNoiseScale = Shader.PropertyToID("CloudNoiseScale");
    s_propertyIdCloudNoiseLocationFactor = Shader.PropertyToID("CloudNoiseLocationFactor");
    s_propertyIdCloudOffset = Shader.PropertyToID("CloudOffset");
    s_propertyIdCloudDensity = Shader.PropertyToID("CloudDensity");
    s_propertyIdCloudHardness = Shader.PropertyToID("CloudHardness");
    s_propertyIdCloudCover = Shader.PropertyToID("CloudCover");
    s_propertyIdCloudMaximumObscurance = Shader.PropertyToID("CloudMaximumObscurance");

    s_propertyIdSun1Direction = Shader.PropertyToID("Sun1Direction");
    s_propertyIdSun1Color = Shader.PropertyToID("Sun1Color");
    s_propertyIdSun1Size = Shader.PropertyToID("Sun1Size");
    s_propertyIdSun1Hardness = Shader.PropertyToID("Sun1Hardness");

    s_propertyIdSunlightColor = Shader.PropertyToID("SunlightColor");
    s_propertyIdSunlightCelestialColor = Shader.PropertyToID("SunlightCelestialColor");
    s_propertyIdSunlightAmbientColor = Shader.PropertyToID("SunlightAmbientColor");
    s_propertyIdSunlightDirection = Shader.PropertyToID("SunlightDirection");

    s_propertyIdNebulaPosition = Shader.PropertyToID("NebulaPosition");
    s_propertyIdNebulaRotation = Shader.PropertyToID("NebulaRotation");
    s_propertyIdNebulaScale = Shader.PropertyToID("NebulaScale");
    s_propertyIdNebulaTexture = Shader.PropertyToID("NebulaTexture");

    s_propertyIdPlanemo1Position = Shader.PropertyToID("Planemo1Position");
    s_propertyIdPlanemo1Rotation = Shader.PropertyToID("Planemo1Rotation");
    s_propertyIdPlanemo1Radius = Shader.PropertyToID("Planemo1Radius");
    s_propertyIdPlanemo1Texture = Shader.PropertyToID("Planemo1Texture");

    s_propertyIdPlanemo2Position = Shader.PropertyToID("Planemo2Position");
    s_propertyIdPlanemo2Rotation = Shader.PropertyToID("Planemo2Rotation");
    s_propertyIdPlanemo2Radius = Shader.PropertyToID("Planemo2Radius");
    s_propertyIdPlanemo2Texture = Shader.PropertyToID("Planemo2Texture");

    s_propertyIdGeneratorPosition = Shader.PropertyToID("GeneratorPosition");

    s_propertyIdArbiterPosition = Shader.PropertyToID("ArbiterPosition");

    s_propertyIdPowerBallPosition = Shader.PropertyToID("PowerBallPosition");
    s_propertyIdPowerBallRotation = Shader.PropertyToID("PowerBallRotation");
    s_propertyIdPowerBallGlowRadius = Shader.PropertyToID("PowerBallGlowRadius");
    s_propertyIdPowerBallRadius = Shader.PropertyToID("PowerBallRadius");
    s_propertyIdPowerBallRadiusReciprical = Shader.PropertyToID("PowerBallRadiusReciprical");
    s_propertyIdPowerBallProgressInverse = Shader.PropertyToID("PowerBallProgressInverse");

    s_propertyIdTfNormalMap = Shader.PropertyToID("TfNormalMap");
    s_propertyIdTf1Position = Shader.PropertyToID("Tf1Position");
    s_propertyIdTf2Position = Shader.PropertyToID("Tf2Position");

    s_propertyIdWaveCenter = Shader.PropertyToID("WaveCenter");
    s_propertyIdWaveColor = Shader.PropertyToID("WaveColor");
    s_propertyIdWaveDistanceSquared = Shader.PropertyToID("WaveDistanceSquared");
    s_propertyIdWaveWidthReciprical = Shader.PropertyToID("WaveWidthReciprical");
    s_propertyIdWaveIntensity = Shader.PropertyToID("WaveIntensity");
    s_propertyIdWaveHeight = Shader.PropertyToID("WaveHeight");
  }

  void UpdateMeshes(bool useIris)
  {
    float size = Mathf.Clamp01(DisplaySize);

    if (size != _lastSize)
    {
      // Vertices in X and Y in viewport space:
      //
      // (-1,-1)---(1,-1)
      // |              |
      // |     (0,0)    |
      // |              |
      // (-1, 1)---(1, 1)
      //
      // Texture coordinates in screen space (640x480 example):
      //
      // (0,480)---(640,480)
      // |                 |
      // (0,0)-------(640,0)
      //
      // Normals in are in directional vectors from camera into world
      //
      // Indices for main quad:
      //
      // 0-1
      // |/|
      // 3-2
      Vector3[] vertices;
      int[] indices;
      int vertexCount;

      if (useIris)
      {
        float aspect = _camera.aspect;
        float xSize;
        float ySize;

        if (aspect > 1)
        {
          // Wider
          xSize = size / aspect;
          ySize = size;
        }
        else
        {
          // Taller
          xSize = size;
          ySize = size * aspect;
        }

        vertices = _octagonVertices;
        indices = s_octagonIndices;
        vertexCount = OctagonVerticesCount;

        _octagonVertices[0].Set(-xSize, -ySize, DisplayDistance);
        _octagonVertices[1].Set(xSize, -ySize, DisplayDistance);
        _octagonVertices[2].Set(xSize, ySize, DisplayDistance);
        _octagonVertices[3].Set(-xSize, ySize, DisplayDistance);

        _octagonVertices[4].Set(0.0f, -s_sqrt2 * ySize, DisplayDistance);
        _octagonVertices[5].Set(s_sqrt2 * xSize, 0.0f, DisplayDistance);
        _octagonVertices[6].Set(0.0f, s_sqrt2 * ySize, DisplayDistance);
        _octagonVertices[7].Set(-s_sqrt2 * xSize, 0.0f, DisplayDistance);

        for (int i = 0; i < vertexCount; i++)
        {
          _irisVertices[i].Set(_octagonVertices[i].x, _octagonVertices[i].y, IrisDistance);
        }
      }
      else
      {
        vertices = s_squareVertices;
        indices = s_squareIndices;
        vertexCount = SquareVerticesCount;
      }

      _displayMesh.Clear(false);
      _displayMesh.vertices = vertices;
      _displayMesh.SetTriangles(indices, 0, false);
      _displayMesh.bounds = Common.NeverCullBounds;

      if (useIris)
      {
        _irisMesh.Clear(false);
        _irisMesh.vertices = _irisVertices;
        _irisMesh.SetTriangles(s_irisIndices, 0, false);
        _irisMesh.bounds = Common.NeverCullBounds;
      }

      _lastSize = size;
    }
  }

  float GetDistancePixelRatio()
  {
    float verticalFov = _camera.fieldOfView * Mathf.Deg2Rad;
    float cameraHeightSquare = Mathf.Tan(verticalFov * 0.5f);
    float horizontalFov = Mathf.Atan(cameraHeightSquare * _camera.aspect) * 2.0f;

    float verticalRatio = 2.0f * Mathf.Sin(verticalFov * 0.5f) / _camera.scaledPixelHeight;
    float horizontalRatio = 2.0f * Mathf.Sin(horizontalFov * 0.5f) / _camera.scaledPixelWidth;

    return Mathf.Min(verticalRatio, horizontalRatio);
  }

  void UpdateShaderUniforms()
  {
    float relativeAltitude = transform.position.y;
    float terrainFarClipDistance = _world.GetTerrainFarClipDistance(relativeAltitude);
    float atmosphereOrbSize = _world.GetAtmosphereOrbSize(relativeAltitude);
    float atmosphereCelestialOrbSize = _world.GetAtmosphereCelestialOrbSize(relativeAltitude);
    float cloudFarClipDistance = _world.GetCloudFarClipDistance(relativeAltitude);

    Shader.SetGlobalFloat(s_propertyIdRenderDistancePixelRatio, GetDistancePixelRatio());
    Shader.SetGlobalFloat(s_propertyIdRenderDistanceMin, _world.RenderDistanceMin);

    Shader.SetGlobalFloat(s_propertyIdHashOffset, _world.HashOffset);
    Shader.SetGlobalFloat(s_propertyIdHashOffsetStatic, _world.HashOffsetStatic);

    Shader.SetGlobalFloat(s_propertyIdNoiseTextureSize, _world.NoiseTextureSize);
    Shader.SetGlobalFloat(s_propertyIdNoiseTexelSize, _world.NoiseTexelSize);
    Shader.SetGlobalFloat(s_propertyIdNoiseTextureScale, _world.NoiseTextureScale);
    Shader.SetGlobalVector(s_propertyIdNoiseTextureOffset, _world.NoiseTextureOffset);
    Shader.SetGlobalTexture(s_propertyIdNoiseTexture, _world.NoiseTexture);

    Shader.SetGlobalVector(s_propertyIdWorldPosition, _world.transform.position);

    Shader.SetGlobalInt(s_propertyIdTerrainStepsMax, _world.TerrainStepsMax);
    Shader.SetGlobalFloat(s_propertyIdTerrainFarClipDistance, terrainFarClipDistance);
    Shader.SetGlobalFloat(s_propertyIdTerrainHitEpsilon, _world.TerrainHitEpsilon);
    Shader.SetGlobalFloat(s_propertyIdTerrainHitStepInitial, _world.TerrainHitStepInitial);
    Shader.SetGlobalFloat(s_propertyIdTerrainHitDeltaHeightScale, _world.TerrainHitDeltaHeightScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainHitDeltaDistanceScale, _world.TerrainHitDeltaDistanceScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainHitDeltaHintScale, _world.TerrainHitDeltaHintScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainCeiling, _world.TerrainCeiling);
    Shader.SetGlobalFloat(s_propertyIdTerrainInitialNoiseScale, _world.TerrainInitialNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainNoiseScale, _world.TerrainNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainInitialOffset, _world.TerrainInitialOffset);
    Shader.SetGlobalFloat(s_propertyIdTerrainInitialFactor, _world.TerrainInitialFactor);
    Shader.SetGlobalFloat(s_propertyIdTerrainSampleVariation, _world.TerrainSampleVariation);
    Shader.SetGlobalVector(s_propertyIdTerrainTerrainInitialNoiseRotation, _world.TerrainInitialNoiseRotation.ToVector());
    Shader.SetGlobalFloat(s_propertyIdTerrainTerrainTwistiness, _world.TerrainTwistiness);
    Shader.SetGlobalFloat(s_propertyIdTerrainHeightAdjustment, _world.TerrainHeightAdjustment);
    Shader.SetGlobalFloat(s_propertyIdTerrainHighDefNoiseScale, _world.TerrainHighDefNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTerrainHighDefSampleVariation, _world.TerrainHighDefSampleVariation);
    Shader.SetGlobalFloat(s_propertyIdTerrainIridescenceFactor, _world.TerrainIridescenceFactor);

    Shader.SetGlobalInt(s_propertyIdTerrainShadowStepsMax, _world.TerrainShadowStepsMax);
    Shader.SetGlobalFloat(s_propertyIdTerrainShadowHitStepInitial, _world.TerrainShadowHitStepInitial);
    Shader.SetGlobalFloat(s_propertyIdTerrainShadowHitStepDistanceMax, _world.TerrainShadowHitStepDistanceMax);

    Shader.SetGlobalColor(s_propertyIdWaterColor, _world.WaterColor);
    Shader.SetGlobalColor(s_propertyIdWaterDeepColor, _world.WaterDeepColor);
    Shader.SetGlobalFloat(s_propertyIdWaterCeiling, _world.WaterCeiling);
    Shader.SetGlobalFloat(s_propertyIdWaterReflectivity, _world.WaterReflectivity);
    Shader.SetGlobalFloat(s_propertyIdWaterShininess, _world.WaterShininess);
    Shader.SetGlobalFloat(s_propertyIdWaterHeight, _world.WaterHeight);
    Shader.SetGlobalFloat(s_propertyIdWaterFloor, _world.WaterFloor);
    Shader.SetGlobalFloat(s_propertyIdWaterDeepFloor, _world.WaterDeepFloor);
    Shader.SetGlobalFloat(s_propertyIdWaterBlendHeight, _world.WaterBlendHeight);
    Shader.SetGlobalFloat(s_propertyIdWaterBlendCeiling, _world.WaterBlendCeiling);
    Shader.SetGlobalFloat(s_propertyIdWaterWaveHeightRatio, _world.WaterWaveHeightRatio);
    Shader.SetGlobalFloat(s_propertyIdWaterWaveSpeed, _world.WaterWaveSpeed);
    Shader.SetGlobalFloat(s_propertyIdWaterInitialNoiseScale, _world.WaterInitialNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdWaterNoiseScale, _world.WaterNoiseScale);

    Shader.SetGlobalColor(s_propertyIdBeachColor, _world.BeachColor);
    Shader.SetGlobalFloat(s_propertyIdBeachHeight, _world.BeachHeight);
    Shader.SetGlobalFloat(s_propertyIdBeachCeiling, _world.BeachCeiling);

    Shader.SetGlobalColor(s_propertyIdLandColor, _world.LandColor);

    Shader.SetGlobalColor(s_propertyIdLGrassColor, _world.GrassColor);
    Shader.SetGlobalColor(s_propertyIdLGrassColorVariation, _world.GrassColorVariation);
    Shader.SetGlobalFloat(s_propertyIdLGrassCeiling, _world.GrassCeiling);
    Shader.SetGlobalFloat(s_propertyIdLGrassSlopeNormalMin, _world.GrassSlopeNormalMin);

    Shader.SetGlobalColor(s_propertyIdTreeColor, _world.TreeColor);
    Shader.SetGlobalFloat(s_propertyIdTreeHeight, _world.TreeHeight);
    Shader.SetGlobalFloat(s_propertyIdTreeLineFloor, _world.TreeLineFloor);
    Shader.SetGlobalFloat(s_propertyIdTreeLineHeight, _world.TreeLineHeight);
    Shader.SetGlobalFloat(s_propertyIdTreeNoiseScale, _world.TreeNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTreeBranchNoiseScale, _world.TreeBranchNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTreeThicknessNoiseScale, _world.TreeThicknessNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdTreeThicknessFactor, _world.TreeThicknessFactor);

    Shader.SetGlobalColor(s_propertyIdSnowColor, _world.SnowColor);
    Shader.SetGlobalFloat(s_propertyIdSnowFloor, _world.SnowFloor);
    Shader.SetGlobalFloat(s_propertyIdSnowFadeHeight, _world.SnowFadeHeight);
    Shader.SetGlobalFloat(s_propertyIdSnowReflectivity, _world.SnowReflectivity);
    Shader.SetGlobalFloat(s_propertyIdSnowShininess, _world.SnowShininess);
    Shader.SetGlobalFloat(s_propertyIdSnowSlopeNormalMin, _world.SnowSlopeNormalMin);

    Shader.SetGlobalColor(s_propertyIdAtmosphereAttenuatedColor, _world.AtmosphereAttenuatedColor);
    Shader.SetGlobalFloat(s_propertyIdAtmosphereCeiling, _world.AtmosphereCeiling);
    Shader.SetGlobalFloat(s_propertyIdAtmosphereOrbSize, atmosphereOrbSize);
    Shader.SetGlobalFloat(s_propertyIdAtmosphereHardness, _world.AtmosphereHardness);
    Shader.SetGlobalFloat(s_propertyIdAtmosphereCelestialOrbSize, atmosphereCelestialOrbSize);
    Shader.SetGlobalFloat(s_propertyIdAtmosphereCelestialHardness, _world.AtmosphereCelestialHardness);

    Shader.SetGlobalColor(s_propertyIdCloudColor, _world.CloudColor);
    Shader.SetGlobalFloat(s_propertyIdCloudFarClipDistance, cloudFarClipDistance);
    Shader.SetGlobalFloat(s_propertyIdCloudFloor, _world.CloudFloor);
    Shader.SetGlobalFloat(s_propertyIdCloudCeiling, _world.CloudCeiling);
    Shader.SetGlobalFloat(s_propertyIdCloudHeightReciprical, _world.CloudHeightReciprical);
    Shader.SetGlobalFloat(s_propertyIdCloudHeightPiReciprical, _world.CloudHeightPiReciprical);
    Shader.SetGlobalFloat(s_propertyIdCloudLayer, _world.CloudLayer);
    Shader.SetGlobalFloat(s_propertyIdCloudNoiseScale, _world.CloudNoiseScale);
    Shader.SetGlobalFloat(s_propertyIdCloudNoiseLocationFactor, _world.CloudNoiseLocationFactor);
    Shader.SetGlobalVector(s_propertyIdCloudOffset, _world.CloudOffset);
    Shader.SetGlobalFloat(s_propertyIdCloudDensity, _world.CloudDensity);
    Shader.SetGlobalFloat(s_propertyIdCloudHardness, _world.CloudHardness);
    Shader.SetGlobalFloat(s_propertyIdCloudCover, _world.CloudCover);
    Shader.SetGlobalFloat(s_propertyIdCloudMaximumObscurance, _world.CloudMaximumObscurance);

    Shader.SetGlobalVector(s_propertyIdSun1Direction, _world.Sun1.Direction);
    Shader.SetGlobalColor(s_propertyIdSun1Color, _world.Sun1.Color * _world.Sun1.Intensity);
    Shader.SetGlobalFloat(s_propertyIdSun1Size, _world.Sun1.Size);
    Shader.SetGlobalFloat(s_propertyIdSun1Hardness, _world.Sun1.Hardness);

    Shader.SetGlobalVector(s_propertyIdNebulaPosition, _world.Nebula.transform.position);
    Shader.SetGlobalVector(s_propertyIdNebulaRotation, _world.Nebula.transform.rotation.normalized.xyzw());
    Shader.SetGlobalVector(s_propertyIdNebulaScale, _world.Nebula.transform.localScale.xy());
    Shader.SetGlobalTexture(s_propertyIdNebulaTexture, _world.NebulaTexture);

    Shader.SetGlobalVector(s_propertyIdPlanemo1Position, _world.Planemo1.transform.position);
    Shader.SetGlobalFloat(s_propertyIdPlanemo1Radius, _world.Planemo1.transform.localScale.x * 0.5f);
    Shader.SetGlobalVector(s_propertyIdPlanemo1Rotation, _world.Planemo1.transform.rotation.normalized.xyzw());
    Shader.SetGlobalTexture(s_propertyIdPlanemo1Texture, _world.Planemo1Texture);

    Shader.SetGlobalVector(s_propertyIdPlanemo2Position, _world.Planemo2.transform.position);
    Shader.SetGlobalFloat(s_propertyIdPlanemo2Radius, _world.Planemo2.transform.localScale.x * 0.5f);
    Shader.SetGlobalVector(s_propertyIdPlanemo2Rotation, _world.Planemo2.transform.rotation.normalized.xyzw());
    Shader.SetGlobalTexture(s_propertyIdPlanemo2Texture, _world.Planemo2Texture);

    Shader.SetGlobalVector(s_propertyIdGeneratorPosition, _world.Generator.transform.position);

    Shader.SetGlobalVector(s_propertyIdArbiterPosition, _world.Arbiter.transform.position);

    Shader.SetGlobalVector(s_propertyIdPowerBallPosition, _world.PowerBall.transform.position);
    Shader.SetGlobalVector(s_propertyIdPowerBallRotation, _world.PowerBall.transform.rotation.normalized.xyzw());
    Shader.SetGlobalFloat(s_propertyIdPowerBallGlowRadius, _world.PowerBall.Radius);
    Shader.SetGlobalFloat(s_propertyIdPowerBallRadius, _world.PowerBall.SolidRadius);
    Shader.SetGlobalFloat(s_propertyIdPowerBallRadiusReciprical, 1.0f / _world.PowerBall.SolidRadius);
    Shader.SetGlobalFloat(s_propertyIdPowerBallProgressInverse, _world.PowerBall.ProgressInverse);

    Shader.SetGlobalColor(s_propertyIdSunlightColor, _world.SunlightLight.color);
    Shader.SetGlobalColor(s_propertyIdSunlightCelestialColor, _world.SunlightCelestialColor);
    Shader.SetGlobalColor(s_propertyIdSunlightAmbientColor, RenderSettings.ambientLight);
    Shader.SetGlobalVector(s_propertyIdSunlightDirection, _world.Sun1.LightDirection);

    Shader.SetGlobalTexture(s_propertyIdTfNormalMap, _world.TfNormalMap);
    Shader.SetGlobalVector(s_propertyIdTf1Position, _world.Tf1.transform.position);
    Shader.SetGlobalVector(s_propertyIdTf2Position, _world.Tf2.transform.position);

    Shader.SetGlobalVector(s_propertyIdWaveCenter, _world.WaveCenter);
    Shader.SetGlobalColor(s_propertyIdWaveColor, _world.WaveColor);
    Shader.SetGlobalFloat(s_propertyIdWaveDistanceSquared, _world.WaveDistanceSquared);
    Shader.SetGlobalFloat(s_propertyIdWaveWidthReciprical, _world.WaveWidthReciprical);
    Shader.SetGlobalFloat(s_propertyIdWaveIntensity, _world.WaveIntensity);
    Shader.SetGlobalFloat(s_propertyIdWaveHeight, _world.WaveHeight);
  }

  private void Init()
  {
    if (_world == null)
    {
      _world = FindObjectOfType<World>();
    }

    if (_camera == null)
    {
      _camera = GetComponent<Camera>();
    }

    if (_irisMesh == null)
    {
      _irisMesh = new Mesh();
    }

    if (_displayMesh == null)
    {
      _displayMesh = new Mesh();
    }

    _lastSize = 0;

    InitializeIrisVertices();
    InitializeShaderPropertyIds();

    _camera.depthTextureMode = DepthTextureMode.Depth;
  }

  void Awake()
  {
    Init();
  }

  private void Start()
  {
    // TODO: Why is this so different in the HTC Vive?
    if (VirtualReality.IsOpenVrHmdPresent())
    {
      DisplaySize = 0.35f;
    }
  }

  void OnEnable()
  {
    Init();
  }

  void Update ()
  {
    if (Input.GetButtonDown("Raymarcher"))
    {
      _render = !_render;
    }

    if (!_render)
    {
      return;
    }

    bool smallerIris = Common.InputGetAxisButtonDown("Iris", true, ref _previousSmallerIris);
    bool largerIris = Common.InputGetAxisButtonDown("Iris", false, ref _nextLargerIris);

    if (smallerIris)
    {
      DisplaySize -= IrisIncrement;

      if (DisplaySize < MinimumIris)
      {
        DisplaySize = IrisMaximum;
      }
    }
    else if (largerIris)
    {
      DisplaySize += IrisIncrement;

      if (DisplaySize > IrisMaximum)
      {
        DisplaySize = MinimumIris;
      }
    }

    if (AutomaticIris)
    {
      if (Time.deltaTime > TargetDeltaTime)
      {
        DisplaySize -= AutomaticIrisSpeed * Time.deltaTime;

        if (DisplaySize < MinimumIris)
        {
          DisplaySize = MinimumIris;
        }
      }
      else
      {
        DisplaySize += AutomaticIrisSpeed * Time.deltaTime;

        if (DisplaySize > IrisMaximum)
        {
          DisplaySize = IrisMaximum;
        }
      }
    }

    UpdateShaderUniforms();

    float displaySize = DisplaySize;

    if (!XRSettings.enabled)
    {
      displaySize = 1.0f;
    }

    if (displaySize > 0.0f)
    {
      bool useIris = (displaySize < 1.0f);

      UpdateMeshes(useIris);

      Graphics.DrawMesh(_displayMesh, Matrix4x4.identity, DisplayMaterial, 0, _camera);

      if (useIris)
      {
        Graphics.DrawMesh(_irisMesh, Matrix4x4.identity, IrisMaterial, 0, _camera);
      }
    }
  }
}
