using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using float2 = UnityEngine.Vector2;
using float3 = UnityEngine.Vector3;

public class World : MonoBehaviour
{
  struct TerrainHeightLocation
  {
    public const int ElementSize = 2;

    public float2 Location;

    public void Set(float2 location)
    {
      Location = location;
    }
  };

  struct PointWorldTest
  {
    public const int ElementSize = 6;

    public float3 Position;
    public float3 Velocity;

    public void Set(float3 position, float3 velocity)
    {
      Position = position;
      Velocity = velocity;
    }
  };

  struct SphereWorldTest
  {
    public const int ElementSize = 7;

    public float3 Position;
    public float3 Velocity;
    public float Radius;

    public void Set(float3 position, float3 velocity, float radius)
    {
      Position = position;
      Velocity = velocity;
      Radius = radius;
    }
  }

  // These constants must match the ones defined in the shaders
  const int TerrainHighResSampleCount = 2;
  const int TerrainLowResSampleCount = 4;

  const int SeedInitial = 0;
  const int SeedStep = 1;

  const float HashOffsetMinimum = 10.0f;
  const float HashOffsetMaximum = 30.0f;

  const float AtmosphereDensity = 5.0f;
  const float AtmosphereDragMin = 0.2f;

  const float CloudAtmosphereRatio = 0.6f;
  const float CloudTerrainRatioBelowClouds = 1.5f;
  const float CloudTerrainRatioAboveClouds = 1.0f;

  const float TransitionSpeed = 0.05f;
  const float TreeTransition = 5.0f;

  const int TerrainVariationSampleCount = TerrainHighResSampleCount + TerrainLowResSampleCount - 1;
  const float TerrainHeightAdjustmentFactor = 0.95f;

  const float TerrainDistanceHintHeightMinOffset = 4.0f;
  // Minimum visually appealing hint height factor based on tree height
  const float TerrainDistanceHintTreeHeightFactor = 0.3f;

  const float WaterDepthTerrainFactor = -0.05f;

  const float WaveSpeed = 1000.0f;
  const float WaveHeightInitial = 125.0f;
  const float WaveWidth = 250.0f;
  const float WaveDistanceMax = 10000.0f;

  // 32 threads per warp/wavefront
  //  public const int MissileCount = 256;
  //  public const int RlCount = 64;
  public const int MissileCount = 512;
  public const int RlCount = 128;

  static readonly Vector2 NoiseTextureBaseOffset = new Vector2(23.0f, 29.0f);

  static int s_propertyIdFixedDeltaTime;

  static int s_propertyIdHashOffset;
  static int s_propertyIdHashOffsetStatic;

  static int s_propertyIdNoiseTextureSize;
  static int s_propertyIdNoiseTexelSize;
  static int s_propertyIdNoiseTextureScale;
  static int s_propertyIdNoiseTextureOffset;
  static int s_propertyIdNoiseTexture;

  static int s_propertyIdWorldPosition;

  static int s_propertyIdTerrainHitEpsilon;
  static int s_propertyIdTerrainCeiling;
  static int s_propertyIdTerrainInitialNoiseScale;
  static int s_propertyIdTerrainNoiseScale;
  static int s_propertyIdTerrainInitialOffset;
  static int s_propertyIdTerrainInitialFactor;
  static int s_propertyIdTerrainSampleVariation;
  static int s_propertyIdTerrainTerrainInitialNoiseRotation;
  static int s_propertyIdTerrainTerrainTwistiness;
  static int s_propertyIdTerrainHeightAdjustment;
  static int s_propertyIdTerrainCollisionMaxSteps;
  static int s_propertyIdTerrainCollisionStepDelta;

  static int s_propertyIdWaterCeiling;

  static int s_propertyIdWaveCenter;
  static int s_propertyIdWaveDistanceSquared;
  static int s_propertyIdWaveWidthReciprical;
  static int s_propertyIdWaveIntensity;
  static int s_propertyIdWaveHeight;

  static int s_propertyIdTerrainLocations;
  static int s_propertyIdTerrainHeights;

  static int s_propertyIdPointTests;
  static int s_propertyIdPointCollisions;

  static int s_propertyIdSphereTests;
  static int s_propertyIdSphereCollisions;

  public ComputeShader TerrainComputeShader;
  public GameObject WaterSurfacePlane;
  public Light SunlightLight;
  public GameObject Nebula;
  public Texture2D NebulaTexture;
  public GameObject Planemo1;
  public Texture2D Planemo1Texture;
  public GameObject Planemo2;
  public Texture2D Planemo2Texture;

  public int Seed = SeedInitial;
  public float HashOffset;
  public float HashOffsetStatic;

  [Range(1, 10)]
  public int NoiseTexturePower = 8;

  public float AltitudeMax;
  public float RenderDistanceMin;

  public int TerrainStepsMax = 200;
  public float TerrainFarClipInitialDistance;
  public float TerrainFarClipAltitudeFactor;
  public float TerrainHitEpsilon;
  public float TerrainHitStepInitial;
  public float TerrainHitDeltaHeightScale;
  public float TerrainHitDeltaDistanceScale;
  public float TerrainHitDeltaHintScale;
  public float TerrainDistanceHintDistanceOffset;

  public float TerrainCeiling;
  public float TerrainInitialNoiseScale;
  public float TerrainNoiseScale;
  public float TerrainInitialOffset;
  public float TerrainSampleVariation;
  public Matrix2x2 TerrainInitialNoiseRotation;
  public float TerrainTwistiness;
  public float TerrainHighDefNoiseScale;
  public float TerrainHighDefSampleVariation;
  public int TerrainCollisionMaxSteps;
  public float TerrainCollisionStepDelta;
  float _terrainIridescence;

  public int TerrainShadowStepsMax;
  public float TerrainShadowHitStepInitial;
  public float TerrainShadowHitStepDistanceMax;

  public Color WaterColor;
  public Color WaterDeepColor;
  public float WaterCeiling;
  public float WaterReflectivity;
  public float WaterShininess;
  public float WaterHeight;
  public float WaterBlendHeight;
  public float WaterWaveHeightRatio;
  public float WaterWaveSpeed;
  public float WaterInitialNoiseScale;
  public float WaterNoiseScale;

  public Color BeachColor;
  public float BeachHeight;

  public Color LandColor;

  public Color GrassColor;
  public Color GrassColorVariation;
  public float GrassCeiling;
  public float GrassSlopeNormalMin;

  public Color TreeColor;
  public float TreeHeight;
  public float TreeLineFloor;
  public float TreeLineHeight;
  public float TreeNoiseScale;
  public float TreeBranchNoiseScale;
  public float TreeThicknessNoiseScale;
  public float TreeThicknessFactor;

  public Color SnowColor;
  public float SnowFloor;
  public float SnowFadeHeight;
  public float SnowReflectivity;
  public float SnowShininess;
  public float SnowSlopeNormalMin;

  Color _atmosphereAttenuatedColor;
  public Color AtmosphereColor;
  public float AtmosphereCeiling;
  public float AtmosphereTransitionHeight;
  public float AtmosphereSpaceSize;
  public float AtmosphereSpaceHeight;
  public float AtmosphereSpaceFactor;
  public float AtmosphereHardness;
  public float AtmosphereCelestialSize;
  public float AtmosphereCelestialHardness;

  public Color CloudColor;
  public float CloudFloor;
  public float CloudHeight;
  public float CloudNoiseScale;
  public float CloudNoiseLocationFactor;
  public Vector2 CloudOffset;
  public Vector2 CloudVelocity;
  public float CloudDensity;
  public float CloudHardness;
  public float CloudCover;
  public float CloudMaximumObscurance;

  public float SunlightColorIntensity;
  public float SunlightAmbientIntensity;
  Color _sunlightCelestialColor;
  public Sun Sun1;
  public Sun Sun2;

  public GameObject Generator;
  public GameObject Arbiter;
  public GameObject PowerBallObject;
  public Texture2D TfNormalMap;
  public GameObject Tf1Object;
  public GameObject Tf2Object;
  public GameObject Missile1Template;
  public GameObject Missile2Template;
  public GameObject Rl1Template;
  public GameObject Rl2Template;

  Texture2D _noiseTexture;

  Rigidbody _waterSurfaceRigidbody;

  float _transitionProgress;
  float _transitionSpeed;
  World _team1World;
  World _team2World;

  List<GameObject> _activePlayers = new List<GameObject>();
  List<GameObject>[] _activeTeamPlayers = new List<GameObject>[Common.TeamCount];
  List<GameObject>[] _activeOpposingTeamPlayers = new List<GameObject>[Common.TeamCount];

  PowerBall _powerBall;

  Tf _tf1;
  Tf _tf2;

  Vector2 _waveCenter;
  Color _waveColor;
  float _waveDistance = 0.0f;

  int _kernelIdComputeTerrainHeights;
  int _kernelIdComputePointCollisions;
  int _kernelIdComputeSphereCollisions;

  GameObjectPool<Missile> _missile1Pool;
  GameObjectPool<Missile> _missile2Pool;
  GameObjectPool<RlController> _rl1Pool;
  GameObjectPool<RlController> _rl2Pool;

  List<TerrainHeightSampler> _terrainHeightSamplers = new List<TerrainHeightSampler>();
  List<PointWorldCollider> _pointColliders = new List<PointWorldCollider>();
  List<SphereWorldCollider> _sphereColliders = new List<SphereWorldCollider>();

  TerrainHeightSampler[] _terrainHeightSamplersDispatched;
  int _lastTerrainHeightSamplersCount = 0;
  TerrainHeightLocation[] _terrainLocationsArray;
  ComputeBuffer _terrainLocationsBuffer;
  ComputeBuffer _terrainHeightsBuffer;
  AsyncGPUReadbackRequest _terrainHeightsBufferRequest;
  int _terrainHeightsBufferRequestFrame;

  PointWorldCollider[] _pointCollidersDispatched;
  int _lastPointCollidersCount = 0;
  PointWorldTest[] _pointsTestsArray;
  ComputeBuffer _pointTestsBuffer;
  ComputeBuffer _pointCollisionsBuffer;
  AsyncGPUReadbackRequest _pointCollisionsBufferRequest;
  int _pointCollisionsBufferRequestFrame;

  SphereWorldCollider[] _sphereCollidersDispatched;
  int _lastSphereCollidersCount = 0;
  SphereWorldTest[] _spheresTestsArray;
  ComputeBuffer _sphereTestsBuffer;
  ComputeBuffer _sphereCollisionsBuffer;
  AsyncGPUReadbackRequest _sphereCollisionsBufferRequest;
  int _sphereCollisionsBufferRequestFrame;

  bool _transformTerranPressed;
  bool _transformAlienPressed;

  bool _previousWorldPressed;
  bool _nextWorldPressed;

  bool _rlSpawn = false;
  float _rlSpawnLast = 0.0f;
  int _rlSpawnTeam = 0;

  public Vector3 GetActualPosition(Vector3 floatingPosition)
  {
    return floatingPosition - transform.position;
  }

  public Vector3 GetFloatingPosition(Vector3 actualPosition)
  {
    return actualPosition + transform.position;
  }

  public int NoiseTextureSize
  {
    get
    {
      return 1 << NoiseTexturePower;
    }
  }

  public float NoiseTexelSize
  {
    get
    {
      return 1.0f / NoiseTextureSize;
    }
  }

  public float NoiseTextureScale
  {
    get
    {
      // Noise scales were emperically decided with a noise resolution of
      // 256, so adjust if the noise resolution changes
      return 256.0f / (1 << NoiseTexturePower);
    }
  }

  public Vector2 NoiseTextureOffset
  {
    get
    {
      return NoiseTextureBaseOffset * NoiseTexelSize;
    }
  }

  public Texture2D NoiseTexture
  {
    get
    {
      return _noiseTexture;
    }
  }

  public float TerrainInitialFactor
  {
    get
    {
      return 1.0f - TerrainInitialOffset;
    }
  }

  public float TerrainIridescenceFactor
  {
    get
    {
      return Mathf.Pow(_terrainIridescence, 4.0f);
    }
  }

  // Multiplied by the maximum calculated terrain height to approximate the
  // desired terrain ceiling
  public float TerrainHeightAdjustment
  {
    get
    {
      float waveFactor = (TerrainCeiling - WaveHeightInitial) / TerrainCeiling;

      // Simplification of the reciprical of the finite sum of sample variations
      // 1 / sum(from k=0 to n-1 of r^k) == (r - 1) / (r^n - 1)
      float variationFactor = (TerrainSampleVariation - 1.0f) / (Mathf.Pow(TerrainSampleVariation, TerrainVariationSampleCount) - 1.0f);

      return waveFactor * variationFactor * TerrainHeightAdjustmentFactor;
    }
  }

  public float TerrainDistanceHintHeightOffset
  {
    get
    {
      return TerrainDistanceHintHeightMinOffset + (TreeHeight * TerrainDistanceHintTreeHeightFactor);
    }
  }

  public float WaterFloor
  {
    get
    {
      return WaterCeiling - WaterHeight;
    }
  }

  public float WaterDeepFloor
  {
    get
    {
      return (TerrainCeiling - WaterCeiling) * WaterDepthTerrainFactor;
    }
  }

  public float WaterBlendCeiling
  {
    get
    {
      return WaterCeiling + WaterBlendHeight;
    }
  }

  public float BeachCeiling
  {
    get
    {
      return WaterFloor + BeachHeight;
    }
  }

  public float CloudHeightReciprical
  {
    get
    {
      return 1.0f / CloudHeight;
    }
  }

  public float CloudHeightPiReciprical
  {
    get
    {
      return Mathf.PI / CloudHeight;
    }
  }

  public float CloudCeiling
  {
    get
    {
      return CloudFloor + CloudHeight;
    }
  }

  public float CloudLayer
  {
    get
    {
      return (CloudHeight * 0.5f) + CloudFloor;
    }
  }

  public Color AtmosphereAttenuatedColor
  {
    get
    {
      return _atmosphereAttenuatedColor;
    }
  }

  public Color SunlightCelestialColor
  {
    get
    {
      return _sunlightCelestialColor;
    }
  }

  public PowerBall PowerBall
  {
    get
    {
      return _powerBall;
    }
  }

  public Tf Tf1
  {
    get
    {
      return _tf1;
    }
  }

  public Tf Tf2
  {
    get
    {
      return _tf2;
    }
  }

  public Vector2 WaveCenter
  {
    get
    {
      return _waveCenter;
    }
  }

  public Color WaveColor
  {
    get
    {
      return _waveColor;
    }
  }

  public float WaveDistanceSquared
  {
    get
    {
      return _waveDistance * _waveDistance;
    }
  }

  public float WaveWidthReciprical
  {
    get
    {
      float denominator = WaveWidth + (WaveWidth * _waveDistance);
      return 1.0f / denominator;
    }
  }

  public float WaveIntensity
  {
    get
    {
      return 1.0f - (_waveDistance / WaveDistanceMax);
    }
  }

  public float WaveHeight
  {
    get
    {
      if (_waveDistance > 0.0f)
      {
        return WaveHeightInitial * WaveIntensity;
      }
      else
      {
        return 0.0f;
      }
    }
  }

  public void AddActivePlayer(GameObject player, int team)
  {
    _activePlayers.Add(player);
    _activeTeamPlayers[team].Add(player);

    for (int opposingTeamIndex = 0; opposingTeamIndex < Common.TeamCount; opposingTeamIndex++)
    {
      if (opposingTeamIndex != team)
      {
        _activeOpposingTeamPlayers[opposingTeamIndex].Add(player);
      }
    }
  }

  public void RemoveActivePlayer(GameObject player, int team)
  {
    _activePlayers.Remove(player);
    _activeTeamPlayers[team].Remove(player);

    for (int opposingTeamIndex = 0; opposingTeamIndex < Common.TeamCount; opposingTeamIndex++)
    {
      if (opposingTeamIndex != team)
      {
        _activeOpposingTeamPlayers[opposingTeamIndex].Remove(player);
      }
    }
  }

  public List<GameObject> GetActivePlayers(int team, bool opposing)
  {
    if (team == Common.TeamNone)
    {
      return _activePlayers;
    }
    else
    {
      if (opposing)
      {
        return _activeOpposingTeamPlayers[team];
      }
      else
      {
        return _activeTeamPlayers[team];
      }
    }
  }

  public GameObject GetNearestActivePlayer(Vector3 position, int team, bool opposing, out Vector3 displacement, out float distanceSquared)
  {
    GameObject nearestPlayer = null;
    displacement = Vector3.zero;
    distanceSquared = float.MaxValue;

    List<GameObject> activePlayers = GetActivePlayers(team, opposing);

    foreach (GameObject player in activePlayers)
    {
      displacement = player.transform.position - position;
      float sqrMagnitude = displacement.sqrMagnitude;

      if (sqrMagnitude < distanceSquared)
      {
        nearestPlayer = player;
        distanceSquared = sqrMagnitude;
      }
    }

    return nearestPlayer;
  }

  public GameObject GetNearestActivePlayer(Vector3 position, int team, bool opposing, Vector3 direction, float directionProductMin, out Vector3 displacement, out float distanceSquared)
  {
    GameObject nearestPlayer = null;
    displacement = Vector3.zero;
    distanceSquared = float.MaxValue;

    List<GameObject> activePlayers = GetActivePlayers(team, opposing);

    foreach (GameObject player in activePlayers)
    {
      displacement = player.transform.position - position;
      float sqrMagnitude = displacement.sqrMagnitude;

      if (sqrMagnitude < distanceSquared)
      {
        Vector3 playerDirection = displacement.normalized;
        float playerDirectionProduct = Vector3.Dot(direction, playerDirection);

        if (playerDirectionProduct > directionProductMin)
        {
          nearestPlayer = player;
          distanceSquared = sqrMagnitude;
        }
      }
    }

    return nearestPlayer;
  }

  public GameObjectPool<Missile> GetMissilePool(int team)
  {
    if (team == 0)
    {
      return _missile1Pool;
    }
    else
    {
      return _missile2Pool;
    }
  }

  public GameObjectPool<RlController> GetRlPool(int team)
  {
    if (team == 0)
    {
      return _rl1Pool;
    }
    else
    {
      return _rl2Pool;
    }
  }

  public Tf GetTeamTf(int team)
  {
    if (team == 0)
    {
      return _tf1;
    }
    else
    {
      return _tf2;
    }
  }

  public static World CreateWorld(int team)
  {
    // Create World object which is not attached to a game object and is just
    // used to hold properties. This will cause the following warning in Unity:
    //   "You are trying to create a MonoBehaviour using the 'new' keyword.
    //   This is not allowed. MonoBehaviours can only be added using
    //   AddComponent(). Alternatively, your script can inherit from
    //   ScriptableObject or no base class at all"
    // The warning can be ignored.
    // An alternative implementation could encapsulate these properties in a
    // separate class, but then the properties would not be available in the
    // editor for manual adjustment during development.
    World w = new World();

    w.HashOffset = Random.Range(HashOffsetMinimum, HashOffsetMaximum);

    // Team terran
    if (team == 0)
    {
      w.TerrainCeiling = 1000.0f;
      w.TerrainNoiseScale = Random.Range(0.22f, 0.3f);
      w.TerrainSampleVariation = -0.3f;
      w.TerrainTwistiness = 0.0f;
      w.TerrainHighDefSampleVariation = -0.7f;
      w._terrainIridescence = 0.0f;

      w.WaterColor = Random.ColorHSV(0.47f, 0.62f, 1.0f, 1.0f, 0.28f, 0.38f);
      w.WaterDeepColor = Color.Lerp(Color.black, w.WaterColor, 0.1f);
      w.WaterCeiling = Random.Range(-1.0f, 7.0f);

      w.BeachColor = Random.ColorHSV(0.05f, 0.16f, 0.0f, 0.86f, 0.09f, 0.47f);

      w.LandColor = Random.ColorHSV(0.0f, 1.0f, 0.12f, 0.12f, 0.2f, 0.5f);

      w.GrassColor = Random.ColorHSV(0.27f, 0.32f, 1.0f, 1.0f, 0.08f, 0.11f);
      w.GrassColorVariation = Random.ColorHSV(0.0f, 0.16f, 0.75f, 1.0f, 0.12f, 0.16f);
      w.GrassCeiling = Random.Range(300.0f, 450.0f);

      w.TreeHeight = 6.0f;
      w.TreeLineHeight = 200.0f;

      w.SnowColor = new Color(1.2f, 1.2f, 1.5f);
      w.SnowFloor = 400.0f;
      w.SnowFadeHeight = 200.0f;
      w.SnowReflectivity = 0.0f;
      w.SnowShininess = 0.0f;

      w.AtmosphereColor = new Color(0.1f, 0.475f, 0.941f);
      w.AtmosphereCeiling = 6400.0f;

      w.CloudColor = new Color(1.0f, 1.0f, 1.0f);
      w.CloudFloor = w.AtmosphereCeiling * CloudAtmosphereRatio;
      w.CloudHeight = 400.0f;
      w.CloudNoiseScale = 0.000003f;
      w.CloudNoiseLocationFactor = 2.7f;
    }
    // Team alien
    else
    {
      w.TerrainCeiling = Random.Range(900.0f, 1100.0f);
      w.TerrainNoiseScale = Random.Range(0.1f, 0.18f);
      w.TerrainSampleVariation = Random.Range(-0.25f, -0.26f);
      w.TerrainTwistiness = 0.1f;
      w.TerrainHighDefSampleVariation = -0.25f;
      w._terrainIridescence = 1.0f;

      w.WaterColor = Random.ColorHSV(0.0f, 1.0f, 0.77f, 1.0f, 0.08f, 0.18f);
      w.WaterColor = Common.RandomColorExcludeHue(w.WaterColor, 0.4f, 0.7f);
      w.WaterDeepColor = Random.ColorHSV(0.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.5f);
      w.WaterCeiling = Random.Range(30.0f, 50.0f);

      w.BeachColor = Random.ColorHSV(0.58f, 1.0f, 0.77f, 1.0f, 0.0f, 0.7f);
      w.BeachColor = Common.RandomColorExcludeHue(w.BeachColor, 0.4f, 0.7f);

      w.LandColor = new Color(0.0f, 1.0f, 0.4f);

      w.GrassColor = Random.ColorHSV(0.0f, 1.0f, 0.7f, 1.0f, 0.2f, 1.0f);
      w.GrassColor = Common.RandomColorExcludeHue(w.GrassColor, 0.16f, 0.46f);
      w.GrassColorVariation = Random.ColorHSV(0.0f, 1.0f, 0.7f, 1.0f, 0.2f, 1.0f);
      w.GrassColorVariation = Common.RandomColorExcludeHue(w.GrassColorVariation, 0.16f, 0.46f);

      w.GrassCeiling = Random.Range(50.0f, 100.0f);

      w.TreeHeight = 0.0f;
      w.TreeLineHeight = 0.0f;

      w.SnowColor = new Color(0.0f, 0.28f, 0.66f);
      w.SnowFloor = 20.0f;
      w.SnowFadeHeight = 200.0f;
      w.SnowReflectivity = 0.5f;
      w.SnowShininess = 32.0f;

      w.AtmosphereColor = Random.ColorHSV(0.81f, 1.0f, 0.54f, 0.78f, 0.78f, 1.0f);
      w.AtmosphereCeiling = Random.Range(4700.0f, 5200.0f);

      w.CloudColor = Random.ColorHSV(0.0f, 1.0f, 0.55f, 0.55f, 1.0f, 1.0f);
      w.CloudFloor = w.AtmosphereCeiling * CloudAtmosphereRatio;
      w.CloudHeight = Random.Range(500.0f, 700.0f);
      w.CloudNoiseScale = 0.0000015f;
      w.CloudNoiseLocationFactor = 2.0f;
    }

    return w;
  }

  public void LerpUnclamped(World a, World b, float t)
  {
    float treeTransition = Mathf.Clamp01(t * TreeTransition);

    HashOffset = Mathf.LerpUnclamped(a.HashOffset, b.HashOffset, t);
    HashOffsetStatic = a.HashOffset;
    TerrainCeiling = Mathf.LerpUnclamped(a.TerrainCeiling, b.TerrainCeiling, t);
    TerrainNoiseScale = Mathf.LerpUnclamped(a.TerrainNoiseScale, b.TerrainNoiseScale, t);
    TerrainSampleVariation = Mathf.LerpUnclamped(a.TerrainSampleVariation, b.TerrainSampleVariation, t);
    TerrainTwistiness = Mathf.LerpUnclamped(a.TerrainTwistiness, b.TerrainTwistiness, t);
    TerrainHighDefSampleVariation = Mathf.LerpUnclamped(a.TerrainHighDefSampleVariation, b.TerrainHighDefSampleVariation, t);
    _terrainIridescence = Mathf.LerpUnclamped(a._terrainIridescence, b._terrainIridescence, t);
    WaterColor = Color.LerpUnclamped(a.WaterColor, b.WaterColor, t);
    WaterDeepColor = Color.LerpUnclamped(a.WaterDeepColor, b.WaterDeepColor, t);
    WaterCeiling = Mathf.LerpUnclamped(a.WaterCeiling, b.WaterCeiling, t);
    BeachColor = Color.LerpUnclamped(a.BeachColor, b.BeachColor, t);
    LandColor = Color.LerpUnclamped(a.LandColor, b.LandColor, t);
    GrassColor = Color.LerpUnclamped(a.GrassColor, b.GrassColor, t);
    GrassColorVariation = Color.LerpUnclamped(a.GrassColorVariation, b.GrassColorVariation, t);
    GrassCeiling = Mathf.LerpUnclamped(a.GrassCeiling, b.GrassCeiling, t);
    TreeHeight = Mathf.LerpUnclamped(a.TreeHeight, b.TreeHeight, treeTransition);
    TreeLineHeight = Mathf.LerpUnclamped(a.TreeLineHeight, b.TreeLineHeight, treeTransition);
    SnowColor = Color.LerpUnclamped(a.SnowColor, b.SnowColor, t);
    SnowFloor = Mathf.LerpUnclamped(a.SnowFloor, b.SnowFloor, t);
    SnowFadeHeight = Mathf.LerpUnclamped(a.SnowFadeHeight, b.SnowFadeHeight, t);
    SnowReflectivity = Mathf.LerpUnclamped(a.SnowReflectivity, b.SnowReflectivity, t);
    SnowShininess = Mathf.LerpUnclamped(a.SnowShininess, b.SnowShininess, t);
    AtmosphereColor = Color.LerpUnclamped(a.AtmosphereColor, b.AtmosphereColor, t);
    AtmosphereCeiling = Mathf.LerpUnclamped(a.AtmosphereCeiling, b.AtmosphereCeiling, t);
    CloudColor = Color.LerpUnclamped(a.CloudColor, b.CloudColor, t);
    CloudFloor = Mathf.LerpUnclamped(a.CloudFloor, b.CloudFloor, t);
    CloudHeight = Mathf.LerpUnclamped(a.CloudHeight, b.CloudHeight, t);
    CloudNoiseScale = Mathf.LerpUnclamped(a.CloudNoiseScale, b.CloudNoiseScale, t);
    CloudNoiseLocationFactor = Mathf.LerpUnclamped(a.CloudNoiseLocationFactor, b.CloudNoiseLocationFactor, t);
  }

  public void UpdateTeamProgress()
  {
    LerpUnclamped(_team1World, _team2World, _transitionProgress);
  }

  public float GetAbsoluteAltitude(float relativeAltitude)
  {
    return relativeAltitude - transform.position.y;
  }

  public float GetRelativeAltitude(float altitude)
  {
    return altitude + transform.position.y;
  }

  public float GetAtmosphericDrag(float relativeAltitude)
  {
    float atmosphereRatio = (AtmosphereCeiling - GetAbsoluteAltitude(relativeAltitude)) / AtmosphereCeiling;
    atmosphereRatio = Mathf.Max(0.0f, atmosphereRatio);
    atmosphereRatio = atmosphereRatio * atmosphereRatio;

    float drag = atmosphereRatio * AtmosphereDensity;
    drag = Mathf.Max(AtmosphereDragMin, drag);

    return drag;
  }

  public float GetTerrainFarClipDistance(float relativeAltitude)
  {
    return (GetAbsoluteAltitude(relativeAltitude) * TerrainFarClipAltitudeFactor) + TerrainFarClipInitialDistance;
  }

  public float GetAtmosphereOrbSize(float relativeAltitude)
  {
    float altitude = GetAbsoluteAltitude(relativeAltitude);

    float atmosphereTransitionRatio = Mathf.Clamp01((AtmosphereCeiling - altitude) / AtmosphereTransitionHeight);
    float spaceRatio = Mathf.Clamp01((AtmosphereCeiling + AtmosphereSpaceHeight - altitude) / AtmosphereSpaceHeight);
    float spaceSize = AtmosphereSpaceSize - (AtmosphereSpaceFactor * (1.0f - spaceRatio));
    float orbSize = atmosphereTransitionRatio * (1.0f - AtmosphereSpaceSize) + spaceSize;
    return orbSize;
  }

  public float GetAtmosphereCelestialOrbSize(float relativeAltitude)
  {
    float atmosphereTransitionFloor = AtmosphereCeiling - AtmosphereTransitionHeight;
    float orbRatio = Mathf.Clamp01((atmosphereTransitionFloor - GetAbsoluteAltitude(relativeAltitude)) / atmosphereTransitionFloor);
    float orbSize = 1.0f - (orbRatio * (1.0f - AtmosphereCelestialSize));
    return orbSize;
  }

  public float GetCloudFarClipDistance(float relativeAltitude)
  {
    float altitude = GetAbsoluteAltitude(relativeAltitude);
    float cloudTerrainRatio;

    if (altitude >= CloudCeiling)
    {
      cloudTerrainRatio = CloudTerrainRatioAboveClouds;
    }
    else
    {
      cloudTerrainRatio = CloudTerrainRatioBelowClouds;
    }

    return GetTerrainFarClipDistance(relativeAltitude) * cloudTerrainRatio;
  }

  void InitializeTerrainComputeShaderPropertyIds()
  {
    s_propertyIdFixedDeltaTime = Shader.PropertyToID("FixedDeltaTime");

    s_propertyIdHashOffset = Shader.PropertyToID("HashOffset");
    s_propertyIdHashOffsetStatic = Shader.PropertyToID("HashOffsetStatic");

    s_propertyIdNoiseTextureSize = Shader.PropertyToID("NoiseTextureSize");
    s_propertyIdNoiseTexelSize = Shader.PropertyToID("NoiseTexelSize");
    s_propertyIdNoiseTextureScale = Shader.PropertyToID("NoiseTextureScale");
    s_propertyIdNoiseTextureOffset = Shader.PropertyToID("NoiseTextureOffset");
    s_propertyIdNoiseTexture = Shader.PropertyToID("NoiseTexture");

    s_propertyIdWorldPosition = Shader.PropertyToID("WorldPosition");

    s_propertyIdTerrainHitEpsilon = Shader.PropertyToID("TerrainHitEpsilon");
    s_propertyIdTerrainCeiling = Shader.PropertyToID("TerrainCeiling");
    s_propertyIdTerrainInitialNoiseScale = Shader.PropertyToID("TerrainInitialNoiseScale");
    s_propertyIdTerrainNoiseScale = Shader.PropertyToID("TerrainNoiseScale");
    s_propertyIdTerrainInitialOffset = Shader.PropertyToID("TerrainInitialOffset");
    s_propertyIdTerrainInitialFactor = Shader.PropertyToID("TerrainInitialFactor");
    s_propertyIdTerrainSampleVariation = Shader.PropertyToID("TerrainSampleVariation");
    s_propertyIdTerrainTerrainInitialNoiseRotation = Shader.PropertyToID("TerrainInitialNoiseRotation");
    s_propertyIdTerrainTerrainTwistiness = Shader.PropertyToID("TerrainTwistiness");
    s_propertyIdTerrainHeightAdjustment = Shader.PropertyToID("TerrainHeightAdjustment");
    s_propertyIdTerrainCollisionMaxSteps = Shader.PropertyToID("TerrainCollisionMaxSteps");
    s_propertyIdTerrainCollisionStepDelta = Shader.PropertyToID("TerrainCollisionStepDelta");

    s_propertyIdWaterCeiling = Shader.PropertyToID("WaterCeiling");

    s_propertyIdWaveCenter = Shader.PropertyToID("WaveCenter");
    s_propertyIdWaveDistanceSquared = Shader.PropertyToID("WaveDistanceSquared");
    s_propertyIdWaveWidthReciprical = Shader.PropertyToID("WaveWidthReciprical");
    s_propertyIdWaveIntensity = Shader.PropertyToID("WaveIntensity");
    s_propertyIdWaveHeight = Shader.PropertyToID("WaveHeight");

    s_propertyIdTerrainLocations = Shader.PropertyToID("TerrainLocations");
    s_propertyIdTerrainHeights = Shader.PropertyToID("TerrainHeights");

    s_propertyIdPointTests = Shader.PropertyToID("PointTests");
    s_propertyIdPointCollisions = Shader.PropertyToID("PointCollisions");

    s_propertyIdSphereTests = Shader.PropertyToID("SphereTests");
    s_propertyIdSphereCollisions = Shader.PropertyToID("SphereCollisions");
  }

  private void UpdateTerrainComputeShaderUniforms()
  {
    TerrainComputeShader.SetFloat(s_propertyIdFixedDeltaTime, Time.fixedDeltaTime);

    TerrainComputeShader.SetFloat(s_propertyIdHashOffset, HashOffset);
    TerrainComputeShader.SetFloat(s_propertyIdHashOffsetStatic, HashOffsetStatic);

    TerrainComputeShader.SetFloat(s_propertyIdNoiseTextureSize, NoiseTextureSize);
    TerrainComputeShader.SetFloat(s_propertyIdNoiseTexelSize, NoiseTexelSize);
    TerrainComputeShader.SetFloat(s_propertyIdNoiseTextureScale, NoiseTextureScale);
    TerrainComputeShader.SetVector(s_propertyIdNoiseTextureOffset, NoiseTextureOffset);

    TerrainComputeShader.SetTexture(_kernelIdComputeTerrainHeights, s_propertyIdNoiseTexture, _noiseTexture);
    TerrainComputeShader.SetTexture(_kernelIdComputePointCollisions, s_propertyIdNoiseTexture, _noiseTexture);
    TerrainComputeShader.SetTexture(_kernelIdComputeSphereCollisions, s_propertyIdNoiseTexture, _noiseTexture);

    TerrainComputeShader.SetVector(s_propertyIdWorldPosition, transform.position);

    TerrainComputeShader.SetFloat(s_propertyIdTerrainHitEpsilon, TerrainHitEpsilon);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainCeiling, TerrainCeiling);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainInitialNoiseScale, TerrainInitialNoiseScale);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainNoiseScale, TerrainNoiseScale);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainInitialOffset, TerrainInitialOffset);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainInitialFactor, TerrainInitialFactor);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainSampleVariation, TerrainSampleVariation);
    TerrainComputeShader.SetVector(s_propertyIdTerrainTerrainInitialNoiseRotation, TerrainInitialNoiseRotation.ToVector());
    TerrainComputeShader.SetFloat(s_propertyIdTerrainTerrainTwistiness, TerrainTwistiness);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainHeightAdjustment, TerrainHeightAdjustment);
    TerrainComputeShader.SetInt(s_propertyIdTerrainCollisionMaxSteps, TerrainCollisionMaxSteps);
    TerrainComputeShader.SetFloat(s_propertyIdTerrainCollisionStepDelta, TerrainCollisionStepDelta);

    TerrainComputeShader.SetFloat(s_propertyIdWaterCeiling, WaterCeiling);

    TerrainComputeShader.SetVector(s_propertyIdWaveCenter, _waveCenter);
    TerrainComputeShader.SetFloat(s_propertyIdWaveDistanceSquared, WaveDistanceSquared);
    TerrainComputeShader.SetFloat(s_propertyIdWaveWidthReciprical, WaveWidthReciprical);
    TerrainComputeShader.SetFloat(s_propertyIdWaveIntensity, WaveIntensity);
    TerrainComputeShader.SetFloat(s_propertyIdWaveHeight, WaveHeight);
  }

  [System.Obsolete("Use TerrainHeightSampler instead.")]
  public float[] GetTerrainHeights(Vector2[] locations)
  {
    int count = locations.Length;

    ComputeBuffer terrainLocations = new ComputeBuffer(count, 2 * sizeof(float));
    ComputeBuffer terrainHeights = new ComputeBuffer(count, sizeof(float));

    terrainLocations.SetData(locations);

    TerrainComputeShader.SetBuffer(_kernelIdComputeTerrainHeights, s_propertyIdTerrainLocations, terrainLocations);
    TerrainComputeShader.SetBuffer(_kernelIdComputeTerrainHeights, s_propertyIdTerrainHeights, terrainHeights);

    int dispatchSize = Common.GetGpuDispatchSize(count);
    TerrainComputeShader.Dispatch(_kernelIdComputeTerrainHeights, dispatchSize, 1, 1);

    float[] result = new float[count];
    terrainHeights.GetData(result);

    terrainLocations.Release();
    terrainHeights.Release();

    return result;
  }

  public void AddTerrainHeightSampler(TerrainHeightSampler sampler)
  {
    _terrainHeightSamplers.Add(sampler);
  }

  public void RemoveTerrainHeightSampler(TerrainHeightSampler sampler)
  {
    _terrainHeightSamplers.Remove(sampler);
  }

  void ReleaseTerrainHeightSamplersBuffers()
  {
    if (_terrainLocationsBuffer != null)
    {
      _terrainLocationsBuffer.Release();
      _terrainLocationsBuffer = null;
    }

    if (_terrainHeightsBuffer != null)
    {
      _terrainHeightsBuffer.Release();
      _terrainHeightsBuffer = null;
    }
  }

  void PerformTerrainHeightSamples()
  {
    HandleTerrainHeightSamples();

    if (_terrainHeightsBufferRequestFrame == Time.renderedFrameCount)
    {
      return;
    }

    if (_terrainHeightSamplersDispatched == null)
    {
      int count = _terrainHeightSamplers.Count;

      if (count > 0)
      {
        _terrainHeightSamplersDispatched = _terrainHeightSamplers.ToArray();

        if (_lastTerrainHeightSamplersCount != count)
        {
          ReleaseTerrainHeightSamplersBuffers();

          _terrainLocationsArray = new TerrainHeightLocation[count];
          _terrainLocationsBuffer = new ComputeBuffer(count, TerrainHeightLocation.ElementSize * sizeof(float));
          _terrainHeightsBuffer = new ComputeBuffer(count, TerrainHeightSampler.ElementSize * sizeof(float));

          _lastTerrainHeightSamplersCount = count;
        }

        int sampleIndex = 0;
        foreach (TerrainHeightSampler sample in _terrainHeightSamplersDispatched)
        {
          _terrainLocationsArray[sampleIndex].Set(sample.RequestedLocation);
          sampleIndex++;
        }

        _terrainLocationsBuffer.SetData(_terrainLocationsArray);

        TerrainComputeShader.SetBuffer(_kernelIdComputeTerrainHeights, s_propertyIdTerrainLocations, _terrainLocationsBuffer);
        TerrainComputeShader.SetBuffer(_kernelIdComputeTerrainHeights, s_propertyIdTerrainHeights, _terrainHeightsBuffer);

        int dispatchSize = Common.GetGpuDispatchSize(count);
        TerrainComputeShader.Dispatch(_kernelIdComputeTerrainHeights, dispatchSize, 1, 1);

        _terrainHeightsBufferRequest = AsyncGPUReadback.Request(_terrainHeightsBuffer);
        _terrainHeightsBufferRequestFrame = Time.renderedFrameCount;
      }
    }
  }

  void HandleTerrainHeightSamples()
  {
    if (_terrainHeightSamplersDispatched != null)
    {
      if (_terrainHeightsBufferRequest.done)
      {
        if (!_terrainHeightsBufferRequest.hasError)
        {
          NativeArray<float> data = _terrainHeightsBufferRequest.GetData<float>();

          int sampleIndex = 0;
          foreach (TerrainHeightSampler sample in _terrainHeightSamplersDispatched)
          {
            sample.RecordSample(_terrainLocationsArray[sampleIndex].Location, data[sampleIndex]);
            sampleIndex++;
          }
        }
        else
        {
          _lastTerrainHeightSamplersCount = 0;
        }

        _terrainHeightSamplersDispatched = null;
      }
    }
  }

  public void AddPointCollider(PointWorldCollider collider)
  {
    _pointColliders.Add(collider);
  }

  public void RemovePointCollider(PointWorldCollider collider)
  {
    _pointColliders.Remove(collider);
  }

  void ReleasePointCollisionsBuffers()
  {
    if (_pointTestsBuffer != null)
    {
      _pointTestsBuffer.Release();
      _pointTestsBuffer = null;
    }

    if (_pointCollisionsBuffer != null)
    {
      _pointCollisionsBuffer.Release();
      _pointCollisionsBuffer = null;
    }
  }

  void PerformPointCollisions()
  {
    HandlePointCollisions();

    if (_pointCollisionsBufferRequestFrame == Time.renderedFrameCount)
    {
      return;
    }

    if (_pointCollidersDispatched == null)
    {
      int count = _pointColliders.Count;

      if (count > 0)
      {
        _pointCollidersDispatched = _pointColliders.ToArray();

        if (_lastPointCollidersCount != count)
        {
          ReleasePointCollisionsBuffers();

          _pointsTestsArray = new PointWorldTest[count];
          _pointTestsBuffer = new ComputeBuffer(count, PointWorldTest.ElementSize * sizeof(float));
          _pointCollisionsBuffer = new ComputeBuffer(count, WorldCollision.ElementSize * sizeof(float));

          _lastPointCollidersCount = count;
        }

        int pointIndex = 0;
        foreach (PointWorldCollider point in _pointCollidersDispatched)
        {
          _pointsTestsArray[pointIndex].Set(point.Position, point.Velocity);
          pointIndex++;
        }

        _pointTestsBuffer.SetData(_pointsTestsArray);

        TerrainComputeShader.SetBuffer(_kernelIdComputePointCollisions, s_propertyIdPointTests, _pointTestsBuffer);
        TerrainComputeShader.SetBuffer(_kernelIdComputePointCollisions, s_propertyIdPointCollisions, _pointCollisionsBuffer);

        int dispatchSize = Common.GetGpuDispatchSize(count);
        TerrainComputeShader.Dispatch(_kernelIdComputePointCollisions, dispatchSize, 1, 1);

        _pointCollisionsBufferRequest = AsyncGPUReadback.Request(_pointCollisionsBuffer);
        _pointCollisionsBufferRequestFrame = Time.renderedFrameCount;
      }
    }
  }

  void HandlePointCollisions()
  {
    if (_pointCollidersDispatched != null)
    {
      if (_pointCollisionsBufferRequest.done)
      {
        if (!_pointCollisionsBufferRequest.hasError)
        {
          NativeArray<float> data = _pointCollisionsBufferRequest.GetData<float>();

          int pointIndex = 0;
          foreach (PointWorldCollider point in _pointCollidersDispatched)
          {
            WorldCollision collision = new WorldCollision(data, pointIndex, point.Velocity);

            if (collision.StaticCollision || collision.DynamicCollision)
            {
              point.OnWorldCollision(collision);
            }

            pointIndex++;
          }
        }
        else
        {
          _lastPointCollidersCount = 0;
        }

        _pointCollidersDispatched = null;
      }
    }
  }

  public void AddSphereCollider(SphereWorldCollider collider)
  {
    _sphereColliders.Add(collider);
  }

  public void RemoveSphereCollider(SphereWorldCollider collider)
  {
    _sphereColliders.Remove(collider);
  }

  void ReleaseSphereCollisionsBuffers()
  {
    if (_sphereTestsBuffer != null)
    {
      _sphereTestsBuffer.Release();
      _sphereTestsBuffer = null;
    }

    if (_sphereCollisionsBuffer != null)
    {
      _sphereCollisionsBuffer.Release();
      _sphereCollisionsBuffer = null;
    }
  }

  void PerformSphereCollisions()
  {
    HandleSphereCollisions();

    if (_sphereCollisionsBufferRequestFrame == Time.renderedFrameCount)
    {
      return;
    }

    if (_sphereCollidersDispatched == null)
    {
      int count = _sphereColliders.Count;

      if (count > 0)
      {
        _sphereCollidersDispatched = _sphereColliders.ToArray();

        if (_lastSphereCollidersCount != count)
        {
          ReleaseSphereCollisionsBuffers();

          _spheresTestsArray = new SphereWorldTest[count];
          _sphereTestsBuffer = new ComputeBuffer(count, SphereWorldTest.ElementSize * sizeof(float));
          _sphereCollisionsBuffer = new ComputeBuffer(count, WorldCollision.ElementSize * sizeof(float));

          _lastSphereCollidersCount = count;
        }

        int sphereIndex = 0;
        foreach (SphereWorldCollider sphere in _sphereCollidersDispatched)
        {
          _spheresTestsArray[sphereIndex].Set(sphere.Position, sphere.Velocity, sphere.Radius);
          sphereIndex++;
        }

        _sphereTestsBuffer.SetData(_spheresTestsArray);

        TerrainComputeShader.SetBuffer(_kernelIdComputeSphereCollisions, s_propertyIdSphereTests, _sphereTestsBuffer);
        TerrainComputeShader.SetBuffer(_kernelIdComputeSphereCollisions, s_propertyIdSphereCollisions, _sphereCollisionsBuffer);

        int dispatchSize = Common.GetGpuDispatchSize(count);
        TerrainComputeShader.Dispatch(_kernelIdComputeSphereCollisions, dispatchSize, 1, 1);

        _sphereCollisionsBufferRequest = AsyncGPUReadback.Request(_sphereCollisionsBuffer);
        _sphereCollisionsBufferRequestFrame = Time.renderedFrameCount;
      }
    }
  }

  void HandleSphereCollisions()
  {
    if (_sphereCollidersDispatched != null)
    {
      if (_sphereCollisionsBufferRequest.done)
      {
        if (!_sphereCollisionsBufferRequest.hasError)
        {
          NativeArray<float> data = _sphereCollisionsBufferRequest.GetData<float>();

          int sphereIndex = 0;
          foreach (SphereWorldCollider sphere in _sphereCollidersDispatched)
          {
            WorldCollision collision = new WorldCollision(data, sphereIndex, sphere.Velocity);

            if (collision.StaticCollision || collision.DynamicCollision)
            {
              sphere.OnWorldCollision(collision);
            }

            sphereIndex++;
          }
        }
        else
        {
          _lastSphereCollidersCount = 0;
        }

        _sphereCollidersDispatched = null;
      }
    }
  }

  void ResetNoiseTexture()
  {
    int size = NoiseTextureSize;

    if (_noiseTexture == null)
    {
      _noiseTexture = new Texture2D(size, size, TextureFormat.R8, false, false);
    }
    else
    {
      _noiseTexture.Resize(size, size);
    }
  }

  void Reset()
  {
    Random.InitState(Seed);
    ResetNoiseTexture();
    Common.Randomize8BitTexture(_noiseTexture);
    _team1World = CreateWorld(0);
    _team2World = CreateWorld(1);
    UpdateTeamProgress();
  }

  void Awake()
  {
    _waterSurfaceRigidbody = WaterSurfacePlane.GetComponent<Rigidbody>();

    for (int i = 0; i < Common.TeamCount; i++)
    {
      _activeTeamPlayers[i] = new List<GameObject>();
      _activeOpposingTeamPlayers[i] = new List<GameObject>();
    }

    _powerBall = PowerBallObject.GetComponent<PowerBall>();

    _tf1 = Tf1Object.GetComponent<Tf>();
    _tf2 = Tf2Object.GetComponent<Tf>();

    _kernelIdComputeTerrainHeights = TerrainComputeShader.FindKernel("ComputeTerrainHeights");
    _kernelIdComputePointCollisions = TerrainComputeShader.FindKernel("ComputePointCollisions");
    _kernelIdComputeSphereCollisions = TerrainComputeShader.FindKernel("ComputeSphereCollisions");

    InitializeTerrainComputeShaderPropertyIds();

    _missile1Pool = new GameObjectPool<Missile>(MissileCount, Missile1Template);
    _missile2Pool = new GameObjectPool<Missile>(MissileCount, Missile2Template);

    _rl1Pool = new GameObjectPool<RlController>(RlCount, Rl1Template);
    _rl2Pool = new GameObjectPool<RlController>(RlCount, Rl2Template);
  }

  void Start()
  {
    AltitudeMax = 15000.0f;
    RenderDistanceMin = 10.0f;

    TerrainFarClipInitialDistance = 9000.0f;
    TerrainFarClipAltitudeFactor = 11.0f;
    TerrainHitEpsilon = 1.0f;
    TerrainHitStepInitial = 8.0f;
    TerrainHitDeltaHeightScale = 0.31f;
    TerrainHitDeltaDistanceScale = 0.015f;
    TerrainHitDeltaHintScale = 1.0f / 16.0f;
    TerrainDistanceHintDistanceOffset = 0.01f;
    TerrainCeiling = 1000.0f;
    TerrainInitialNoiseScale = 0.000005f;
    TerrainNoiseScale = 0.25f;
    TerrainInitialOffset = 0.15f;
    TerrainSampleVariation = -0.4f;
    TerrainInitialNoiseRotation = new Matrix2x2(2.366089f, 3.20834f, -3.028429f, 2.472888f);
    TerrainTwistiness = 0.0f;
    TerrainHighDefNoiseScale = 0.04f;
    TerrainHighDefSampleVariation = -0.7f;
    TerrainCollisionMaxSteps = 8;
    TerrainCollisionStepDelta = 1.0f;
    _terrainIridescence = 0.0f;

    WaterColor = new Color(0.017f, 0.076f, 0.162f);
    WaterCeiling = 0.0f;
    WaterReflectivity = 0.4f;
    WaterShininess = 64.0f;
    WaterHeight = 6.0f;
    WaterBlendHeight = 1.0f;
    WaterWaveHeightRatio = 0.02f;
    WaterWaveSpeed = 0.0125f;
    WaterInitialNoiseScale = 0.0025f;
    WaterNoiseScale = 0.3331f;

    BeachColor = new Color(0.09f, 0.067f, 0.02f);
    BeachHeight = 32.0f;

    LandColor = new Color(0.51f, 0.491f, 0.357f);

    GrassColor = new Color(0.03f, 0.134f, 0.0f);
    GrassColorVariation = new Color(0.15f, 0.06f, 0.0f);
    GrassCeiling = 320.0f;
    GrassSlopeNormalMin = 0.5f;

    TreeColor = new Color(0.0f, 0.071f, 0.0f);
    TreeHeight = 6.0f;
    TreeLineFloor = 5.0f;
    TreeLineHeight = 200.0f;
    TreeNoiseScale = 0.01f;
    TreeBranchNoiseScale = 0.08f;
    TreeThicknessNoiseScale = 0.00002f;
    TreeThicknessFactor = 0.45f;

    SnowColor = new Color(1.1f, 1.1f, 1.2f);
    SnowFloor = 400.0f;
    SnowFadeHeight = 200.0f;
    SnowReflectivity = 0.3f;
    SnowShininess = 64.0f;
    SnowSlopeNormalMin = 0.42f;

    AtmosphereColor = new Color(0.23f, 0.334f, 0.941f);
    AtmosphereCeiling = 4500.0f;
    AtmosphereTransitionHeight = 800.0f;
    AtmosphereSpaceSize = 0.93f;
    AtmosphereSpaceHeight = 9000.0f;
    AtmosphereSpaceFactor = 0.035f;
    AtmosphereHardness = 90.0f;
    AtmosphereCelestialSize = 0.88f;
    AtmosphereCelestialHardness = 40.0f;

    CloudColor = new Color(0.6f, 0.6f, 0.6f);
    CloudFloor = 1200.0f;
    CloudHeight = 300.0f;
    CloudNoiseScale = 0.001f;
    CloudNoiseLocationFactor = 2.7f;
    CloudOffset = new Vector2(0.0f, 0.0f);
    CloudVelocity = new Vector2(70.0f, 70.0f);
    CloudDensity = 0.6f;
    CloudHardness = 0.5f;
    CloudCover = 0.44f;
    CloudMaximumObscurance = 0.8f;

    SunlightColorIntensity = 0.0f;
    SunlightAmbientIntensity = 0.2f;

    Sun1.Color = new Color(1.0f, 1.0f, 0.4f);
    Sun1.Size = 0.00125f;
    Sun1.Hardness = 512.0f;
    Sun1.Intensity = 4.0f;
    Sun1.Angle = 58.0f;
    Sun1.AngleVelocity = 0.0f;

    Reset();

    _transitionProgress = 1.0f;
    _transitionSpeed = -TransitionSpeed;
  }

  protected virtual void OnDisable()
  {
    ReleaseTerrainHeightSamplersBuffers();
    ReleasePointCollisionsBuffers();
    ReleaseSphereCollisionsBuffers();
  }

  void UpdateLighting()
  {
    Sun1.Rotate(Time.deltaTime);

    Color diffuseColor = Sun1.Color;
    SunlightLight.transform.rotation = Sun1.Rotation;

    float diffuseGrayscale = diffuseColor.grayscale;
    Color diffuseGrayscaleColor = new Color(diffuseGrayscale, diffuseGrayscale, diffuseGrayscale);
    diffuseColor = Color.LerpUnclamped(diffuseGrayscaleColor, diffuseColor, SunlightColorIntensity);

    Color ambientColor = diffuseColor * SunlightAmbientIntensity;

    float sunTheta = Vector3.Dot(SunlightLight.transform.forward, Vector3.down);
    float skyLuminance = Mathf.Clamp01(sunTheta + 0.25f);

    _sunlightCelestialColor = diffuseColor;
    SunlightLight.color = diffuseColor * skyLuminance;
    RenderSettings.ambientLight = ambientColor * skyLuminance;
    _atmosphereAttenuatedColor = AtmosphereColor * skyLuminance;
  }

  void UpdateClouds()
  {
    CloudOffset += CloudVelocity * Time.deltaTime;
  }

  void UpdateWater()
  {
    float relativeWaterCeiling = GetRelativeAltitude(WaterCeiling);
    float currentWaterCeiling = WaterSurfacePlane.transform.position.y;

    if (relativeWaterCeiling != currentWaterCeiling)
    {
      Vector3 waterPosition = new Vector3(0.0f, relativeWaterCeiling + transform.position.y, 0.0f);

      // Nothing is below the water surface, so it can move down immediately
      if (relativeWaterCeiling < currentWaterCeiling)
      {
        WaterSurfacePlane.transform.position = waterPosition;
      }
      // Objects above the water must be moved out of the way
      else
      {
        _waterSurfaceRigidbody.MovePosition(waterPosition);
      }
    }
  }

  public void StartTfWaves(int team)
  {
    Tf tf = GetTeamTf(team);
    Vector2 tfCenter = GetActualPosition(tf.transform.position).xz();
    Color teamColor = Common.GetTeamPrimaryColor(team);
    StartWaves(tfCenter, teamColor);
  }

  void StartWaves(Vector2 center, Color color)
  {
    _waveCenter = center;
    _waveColor = color;
    _waveDistance = float.Epsilon;
  }

  void UpdateWaves()
  {
    if (_waveDistance > 0.0f)
    {
      _waveDistance += Time.deltaTime * WaveSpeed;
    }

    if (_waveDistance > WaveDistanceMax)
    {
      _waveDistance = 0.0f;
    }
  }

  public void Goal(int team)
  {
    Tf tf = GetTeamTf(team);
    tf.Goal();

    if (team == 0)
    {
      _transitionSpeed = -TransitionSpeed;
    }
    else
    {
      _transitionSpeed = TransitionSpeed;
    }

    StartTfWaves(team);
  }

  float IncreaseValue(float value, float amount, float maximum)
  {
    value += amount * Time.deltaTime;
    value = Mathf.Min(value, maximum);
    return value;
  }

  float DecreaseValue(float value, float amount, float minimum)
  {
    value -= amount * Time.deltaTime;
    value = Mathf.Max(minimum, value);
    return value;
  }

  void Update()
  {
    UpdateLighting();
    UpdateClouds();
    UpdateWaves();

    bool previousWorld = Common.InputGetAxisButtonDown("Change World", true, ref _previousWorldPressed);
    bool nextWorld = Common.InputGetAxisButtonDown("Change World", false, ref _nextWorldPressed);

    if (previousWorld)
    {
      Seed--;
      Reset();
    }
    else if (nextWorld)
    {
      Seed++;
      Reset();
    }

    bool transformTerran = Common.InputGetAxisButtonDown("Transform", true, ref _transformTerranPressed);
    bool transformAlien = Common.InputGetAxisButtonDown("Transform", false, ref _transformAlienPressed);

    if (transformTerran)
    {
      Goal(0);
    }
    else if (transformAlien)
    {
      Goal(1);
    }

    if (_transitionSpeed != 0.0f)
    {
      _transitionProgress += _transitionSpeed * Time.deltaTime;

      if ((_transitionProgress < 0.0f) || (_transitionProgress > 1.0f))
      {
        _transitionSpeed = 0.0f;
        _transitionProgress = Mathf.Clamp01(_transitionProgress);
      }

      LerpUnclamped(_team1World, _team2World, _transitionProgress);
    }

    if (Input.GetButtonDown("Spawn"))
    {
      _rlSpawn = true;
    }

    if (_rlSpawn)
    {
      if ((Time.time - _rlSpawnLast) > 0.5f)
      {
        _rlSpawnLast = Time.time;

        RlController newRl = GetRlPool(_rlSpawnTeam).GetAvailableComponent();

        if (newRl != null)
        {
          Tf tf = GetTeamTf(_rlSpawnTeam);

          newRl.Reset(_rlSpawnTeam);

          newRl.transform.position = new Vector3(0.0f, 20000.0f, 0.0f) + tf.transform.position;

          _rlSpawnTeam = 1 - _rlSpawnTeam;
        }
      }
    }
  }

  void FixedUpdate()
  {
    UpdateWater();

    ExecutionPool.NextFrame();

    UpdateTerrainComputeShaderUniforms();
    PerformTerrainHeightSamples();
    PerformPointCollisions();
    PerformSphereCollisions();
  }
}
