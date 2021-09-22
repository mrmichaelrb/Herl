using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

[RequireComponent(typeof(Camera))]
public class WaterReflection : MonoBehaviour, IBeforeCameraRender
{
  const string TextureName = "Water Reflection";

  static int s_propertyIdWaterReflectionTexture;

  public Camera ReflectionCamera;
  public int TextureSizeBias;

  World _world;
  Camera _viewCamera;
  LWRPAdditionalCameraData _reflectionCameraAdditionalData;
  RenderTexture _colorTexture;
  int _textureSizeBias;
  int _width;
  int _height;
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

    if (_reflectionCameraAdditionalData == null)
    {
      _reflectionCameraAdditionalData = ReflectionCamera.gameObject.GetComponent<LWRPAdditionalCameraData>();
    }

    s_propertyIdWaterReflectionTexture = Shader.PropertyToID("WaterReflectionTexture");

    _width = 0;
    _height = 0;
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
    if (
      (TextureSizeBias != _textureSizeBias) || 
      (_viewCamera.pixelWidth != _width) || 
      (_viewCamera.pixelHeight != _height))
    {
      _textureSizeBias = TextureSizeBias;
      _width = _viewCamera.pixelWidth;
      _height = _viewCamera.pixelHeight;

      int size = Common.GetSquareTextureSize(_width, _height, _textureSizeBias);

      ReflectionCamera.targetTexture = null;

      if (_colorTexture != null)
      {
        _colorTexture.Release();
      }

      _colorTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
      _colorTexture.name = TextureName;
      _colorTexture.filterMode = FilterMode.Bilinear;
      Common.SetRenderTextureProperties(_colorTexture);

      Shader.SetGlobalTexture(s_propertyIdWaterReflectionTexture, _colorTexture);
    }
  }

  void IBeforeCameraRender.ExecuteBeforeCameraRender(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera)
  {
    if ((enabled) && (_colorTexture != null))
    {
      ReflectionCamera.CopyFrom(camera);
      // TODO: Unity will not yet render both eyes with an oblique reflection
      // plane for some reason, so use a single reflection for both eyes.
      // Hopefully this will be fixed in future versions.
      ReflectionCamera.stereoTargetEye = StereoTargetEyeMask.None;
      ReflectionCamera.targetTexture = _colorTexture;
      ReflectionCamera.aspect = camera.aspect;
      ReflectionCamera.cullingMask = Common.LayerEverythingExceptUIMask;
      ReflectionCamera.clearFlags = CameraClearFlags.Color;
      ReflectionCamera.backgroundColor = _world.AtmosphereColor;
      ReflectionCamera.allowHDR = true;
      ReflectionCamera.allowMSAA = false;
      ReflectionCamera.depthTextureMode = DepthTextureMode.None;
      _reflectionCameraAdditionalData.requiresDepthTexture = false;
      _reflectionCameraAdditionalData.requiresColorTexture = false;
      _reflectionCameraAdditionalData.renderShadows = false;

      Vector4 worldSpaceWaterPlane = new Vector4(0.0f, 1.0f, 0.0f, -_world.WaterCeiling - _world.transform.position.y);
      Matrix4x4 waterReflectionMatrix = Common.GetReflectionMatrix(worldSpaceWaterPlane);
      ReflectionCamera.worldToCameraMatrix *= waterReflectionMatrix;

      Vector4 cameraSpaceClipPlane = ReflectionCamera.WorldToCameraPlane(worldSpaceWaterPlane);
      Matrix4x4 projectionMatrix = ReflectionCamera.CalculateObliqueMatrix(cameraSpaceClipPlane);

      ReflectionCamera.projectionMatrix = projectionMatrix;
      ReflectionCamera.cullingMatrix = ReflectionCamera.projectionMatrix * ReflectionCamera.worldToCameraMatrix;

      Matrix4x4 cameraToWorldMatrix = ReflectionCamera.cameraToWorldMatrix;
      Vector3 reflectionPosition = cameraToWorldMatrix.GetPosition();
      Quaternion reflectionRotation = cameraToWorldMatrix.GetRotation();
      ReflectionCamera.transform.SetPositionAndRotation(reflectionPosition, reflectionRotation);

      bool oldInvertBackfaceCulling = GL.invertCulling;
      bool oldFog = RenderSettings.fog;
      int oldMaxLod = QualitySettings.maximumLODLevel;
      float oldLodBias = QualitySettings.lodBias;

      GL.invertCulling = !oldInvertBackfaceCulling;
      RenderSettings.fog = false;
      QualitySettings.maximumLODLevel = 1;
      QualitySettings.lodBias = oldLodBias * 0.5f;

      LightweightRenderPipeline.RenderSingleCamera(pipelineInstance, context, ReflectionCamera, ref _cullResults);

      GL.invertCulling = oldInvertBackfaceCulling;
      RenderSettings.fog = oldFog;
      QualitySettings.maximumLODLevel = oldMaxLod;
      QualitySettings.lodBias = oldLodBias;
    }
  }
}
