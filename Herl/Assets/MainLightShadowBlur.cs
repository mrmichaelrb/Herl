using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class MainLightShadowBlur : MonoBehaviour, IAfterMainLightShadowCasterPass
{
  const string ShadowBlurTextureName = "Main Light Shadow Blur";
  const string ShadowTextureName = "Main Light Shadow";

  static int s_propertyIdMainLightShadowRendered;
  static int s_propertyIdMainLightShadowDistance;
  static int s_propertyIdMainLightShadowTexture;
  static int s_propertyIdMainLightShadowBlurRadius;

  public Material Blur;
  public int TextureSizeBias;

  [Range(0, 8)]
  public int Iterations;

  Camera _camera;
  MainLightShadowBlurRenderPass _pass;
  int _size;
  RenderTexture _blurBuffer;
  RenderTexture _shadowTexture;

  void Init()
  {
    if (_camera == null)
    {
      _camera = GetComponent<Camera>();
    }

    if (_pass == null)
    {
      _pass = new MainLightShadowBlurRenderPass();
    }

    s_propertyIdMainLightShadowRendered = Shader.PropertyToID("MainLightShadowRendered");
    s_propertyIdMainLightShadowDistance = Shader.PropertyToID("MainLightShadowDistance");
    s_propertyIdMainLightShadowTexture = Shader.PropertyToID("MainLightShadowTexture");
    s_propertyIdMainLightShadowBlurRadius = Shader.PropertyToID("MainLightShadowBlurRadius");
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
    Shader.SetGlobalFloat(s_propertyIdMainLightShadowRendered, 0.0f);

    LightweightRenderPipelineAsset pipeline = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;

    if (pipeline != null)
    {
      Shader.SetGlobalFloat(s_propertyIdMainLightShadowDistance, pipeline.shadowDistance);

      int size = pipeline.mainLightShadowmapResolution;
      size = Common.ApplyTextureSizeBias(size, TextureSizeBias);

      if (_size != size)
      {
        if (_blurBuffer != null)
        {
          _blurBuffer.Release();
        }

        if (_shadowTexture != null)
        {
          _shadowTexture.Release();
        }

        _blurBuffer = new RenderTexture(size, size, 0, RenderTextureFormat.R8);
        _blurBuffer.name = ShadowBlurTextureName;
        _blurBuffer.filterMode = FilterMode.Bilinear;
        Common.SetRenderTextureProperties(_blurBuffer);

        _shadowTexture = new RenderTexture(size, size, 0, RenderTextureFormat.R8);
        _shadowTexture.name = ShadowTextureName;
        _shadowTexture.filterMode = FilterMode.Bilinear;
        Common.SetRenderTextureProperties(_shadowTexture);

        Shader.SetGlobalTexture(s_propertyIdMainLightShadowTexture, _shadowTexture);

        _size = size;
      }
    }
  }

  ScriptableRenderPass IAfterMainLightShadowCasterPass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle mainShadowmapHandle)
  {
    if ((enabled) && (_size > 0))
    {
      _pass.BlurShader = Blur;
      _pass.Iterations = Iterations;
      _pass.BlurBuffer = _blurBuffer;
      _pass.ShadowTexture = _shadowTexture;
      _pass.PropertyIdShadowBlurRadius = s_propertyIdMainLightShadowBlurRadius;

      Shader.SetGlobalFloat(s_propertyIdMainLightShadowRendered, 1.0f);

      return _pass;
    }
    else
    {
      return NullRenderPass.Instance;
    }
  }
}

public class MainLightShadowBlurRenderPass : ScriptableRenderPass
{
  const string PassName = "Main Light Shadow Blur Pass";

  const int PassFirst = 0;
  const int PassHorizontal = 1;
  const int PassVertical = 2;

  public Material BlurShader;
  public int Iterations;
  public RenderTexture BlurBuffer;
  public RenderTexture ShadowTexture;
  public int PropertyIdShadowBlurRadius;

  public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
  {
    CommandBuffer commandBuffer = CommandBufferPool.Get(PassName);

    using (new ProfilingSample(commandBuffer, PassName))
    {
      commandBuffer.Blit(ShadowTexture, ShadowTexture, BlurShader, PassFirst);

      for (int i = 1; i <= Iterations; i++)
      {
        commandBuffer.SetGlobalFloat(PropertyIdShadowBlurRadius, i);
        commandBuffer.Blit(ShadowTexture, BlurBuffer, BlurShader, PassHorizontal);
        commandBuffer.Blit(BlurBuffer, ShadowTexture, BlurShader, PassVertical);
      }
    }

    context.ExecuteCommandBuffer(commandBuffer);
    CommandBufferPool.Release(commandBuffer);
  }
}
