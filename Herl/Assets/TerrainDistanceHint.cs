using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class TerrainDistanceHint : MonoBehaviour, IBeforeCameraRender
{
  const string CategoryName = "Terrain Distance Hint";

  static int s_propertyIdTerrainDistanceHintRendered;
  static int s_propertyIdTerrainDistanceHintTexture;
  static int s_propertyIdTerrainDistanceHintHeightOffset;
  static int s_propertyIdTerrainDistanceHintDistanceOffset;

  public Camera HintCamera;
  public Material HintMaterial;
  public Material MeshMaterial;
  public int TextureSizeBias;
  public float Distance;
  public float Length;
  public float NearWidth;
  public float FarWidth;
  public int Resolution;
  public float RecessionFactor;
  public int VerticesCount;
  public bool GenerateHints;
  public bool ShowMesh;

  World _world;
  Camera _viewCamera;
  LWRPAdditionalCameraData _hintCameraAdditionalData;
  RenderTexture _hintTexture;
  int _textureSizeBias;
  int _width;
  int _height;
  float _distance;
  float _length;
  float _nearWidth;
  float _farWidth;
  int _sizeX;
  float _recessionFactor;
  Mesh _terrainMesh;
  Vector3[] _terrainVertices;
  int[] _terrainIndices;
  bool _generateHintTexture;
  Matrix4x4 _terrainMatrix;
  CullResults _cullResults;

  void Init()
  {
    if (_world == null)
    {
      _world = FindObjectOfType<World>();
    }

    if (_viewCamera == null)
    {
      _viewCamera = GetComponent<Camera>();
    }

    if (_hintCameraAdditionalData == null)
    {
      _hintCameraAdditionalData = HintCamera.gameObject.GetComponent<LWRPAdditionalCameraData>();
    }

    if (_terrainMesh == null)
    {
      _terrainMesh = new Mesh();
    }

    s_propertyIdTerrainDistanceHintRendered = Shader.PropertyToID("TerrainDistanceHintRendered");
    s_propertyIdTerrainDistanceHintTexture = Shader.PropertyToID("TerrainDistanceHintTexture");
    s_propertyIdTerrainDistanceHintHeightOffset = Shader.PropertyToID("TerrainDistanceHintHeightOffset");
    s_propertyIdTerrainDistanceHintDistanceOffset = Shader.PropertyToID("TerrainDistanceHintDistanceOffset");

    _width = 0;
    _height = 0;
    _terrainVertices = null;
  }

  void Awake()
  {
    Init();
  }

  void OnEnable()
  {
    Init();
  }

  void Update()
  {
    if (Application.isEditor)
    {
      if (Input.GetKeyDown(KeyCode.B))
      {
        GenerateHints = !GenerateHints;
      }

      if (Input.GetKeyDown(KeyCode.N))
      {
        ShowMesh = !ShowMesh;
      }
    }

    if (
      (TextureSizeBias != _textureSizeBias) ||
      (_viewCamera.pixelWidth != _width) ||
      (_viewCamera.pixelHeight != _height))
    {
      _textureSizeBias = TextureSizeBias;
      _width = _viewCamera.pixelWidth;
      _height = _viewCamera.pixelHeight;

      int size = Common.GetSquareTextureSize(_width, _height, _textureSizeBias);

      HintCamera.targetTexture = null;

      if (_hintTexture != null)
      {
        _hintTexture.Release();
      }

      _hintTexture = new RenderTexture(size, size, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
      _hintTexture.name = CategoryName;
      _hintTexture.filterMode = FilterMode.Bilinear;
      Common.SetRenderTextureProperties(_hintTexture);

      Shader.SetGlobalTexture(s_propertyIdTerrainDistanceHintTexture, _hintTexture);
    }

    if (
      (Distance != _distance) ||
      (Length != _length) ||
      (NearWidth != _nearWidth) ||
      (FarWidth != _farWidth) ||
      (Resolution != _sizeX) ||
      (RecessionFactor != _recessionFactor) ||
      (_terrainVertices == null))
    {
      _distance = Distance;
      _length = Length;
      _nearWidth = NearWidth;
      _farWidth = FarWidth;
      _sizeX = Resolution;
      _recessionFactor = RecessionFactor;

      float initialStripLength = _nearWidth / (_sizeX - 1);

      int sizeY = 2;
      float stripLength = initialStripLength;
      float lengthLeft = _length - stripLength;
      float lengthTotal = stripLength;
      while (lengthLeft > 0)
      {
        sizeY++;
        stripLength *= _recessionFactor;
        lengthTotal += stripLength;
        lengthLeft -= stripLength;
      }

      VerticesCount = _sizeX * sizeY;
      _terrainVertices = new Vector3[_sizeX * sizeY];
      _terrainIndices = new int[(_sizeX - 1) * (sizeY - 1) * 6];

      int i = 0;
      int firstIndex = 0;

      for (int y = 0; y < sizeY - 1; y++)
      {
        for (int x = 0; x < _sizeX - 1; x++)
        {
          _terrainIndices[i] = firstIndex + x;
          i++;
          _terrainIndices[i] = firstIndex + x + _sizeX;
          i++;
          _terrainIndices[i] = firstIndex + x + 1;
          i++;

          _terrainIndices[i] = firstIndex + x + 1;
          i++;
          _terrainIndices[i] = firstIndex + x + _sizeX;
          i++;
          _terrainIndices[i] = firstIndex + x + _sizeX + 1;
          i++;
        }
        firstIndex += _sizeX;
      }

      float lengthProgress = 0.0f;
      float locationY = _distance;

      stripLength = initialStripLength;
      i = 0;

      for (int y = 0; y < sizeY; y++)
      {
        float lengthRatio = lengthProgress / lengthTotal;
        float stripWidth = Mathf.LerpUnclamped(_nearWidth, _farWidth, lengthRatio);
        float quadWidth = stripWidth / (_sizeX - 1);
        float locationX = stripWidth * -0.5f;

        for (int x = 0; x < _sizeX; x++)
        {
          _terrainVertices[i].Set(locationX, 0.0f, locationY);
          i++;

          locationX += quadWidth;
        }

        lengthProgress += stripLength;
        locationY += stripLength;
        stripLength *= _recessionFactor;
      }

      _terrainMesh.Clear(false);
      _terrainMesh.vertices = _terrainVertices;
      _terrainMesh.SetTriangles(_terrainIndices, 0, false);
      _terrainMesh.bounds = Common.NeverCullBounds;
    }

    Transform viewTransform = _viewCamera.transform;

    bool belowCloudFloor = (viewTransform.position.y < _world.CloudFloor);

    _generateHintTexture = (GenerateHints && belowCloudFloor);

    Shader.SetGlobalFloat(s_propertyIdTerrainDistanceHintRendered, _generateHintTexture ? 1.0f : 0.0f);

    bool generateHintMatrix = ((GenerateHints || ShowMesh) && (belowCloudFloor));

    if (generateHintMatrix)
    {
      Vector3 position = viewTransform.position;

      // Always in the direction of the camera
      Vector3 eulerAngles = viewTransform.rotation.eulerAngles;
      Quaternion rotation = Quaternion.Euler(0.0f, eulerAngles.y, 0.0f);

      _terrainMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

      if (ShowMesh)
      {
        Graphics.DrawMesh(_terrainMesh, _terrainMatrix, MeshMaterial, 0, _viewCamera);
      }
    }

    Shader.SetGlobalFloat(s_propertyIdTerrainDistanceHintHeightOffset, _world.TerrainDistanceHintHeightOffset);
    Shader.SetGlobalFloat(s_propertyIdTerrainDistanceHintDistanceOffset, _world.TerrainDistanceHintDistanceOffset);
  }

  void IBeforeCameraRender.ExecuteBeforeCameraRender(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera)
  {
    if (enabled && _generateHintTexture)
    {
      Profiler.BeginSample(CategoryName);

      HintCamera.CopyFrom(camera);
      HintCamera.targetTexture = _hintTexture;
      HintCamera.aspect = camera.aspect;
      HintCamera.cullingMask = Common.LayerTerrainDistanceHintMask;
      HintCamera.clearFlags = CameraClearFlags.Color;
      HintCamera.backgroundColor = Color.black;
      HintCamera.useOcclusionCulling = false;
      HintCamera.allowHDR = true;
      HintCamera.allowMSAA = false;
      HintCamera.depthTextureMode = DepthTextureMode.None;
      _hintCameraAdditionalData.requiresDepthTexture = false;
      _hintCameraAdditionalData.requiresColorTexture = false;
      _hintCameraAdditionalData.renderShadows = false;

      Graphics.DrawMesh(_terrainMesh, _terrainMatrix, HintMaterial, Common.LayerTerrainDistanceHint, HintCamera);

      LightweightRenderPipeline.RenderSingleCamera(pipelineInstance, context, HintCamera, ref _cullResults);

      Profiler.EndSample();
    }
  }
}
