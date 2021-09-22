using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class WaterReflectionDistance : MonoBehaviour, IAfterOpaquePass
{
  const string TextureName = "Water Reflection Distance";

  static int s_propertyIdWaterReflectionDistanceTexture;

  public int TextureSizeBias;

  Camera _camera;
  WaterReflectionDistanceOnlyRenderPass _pass;
  RenderTexture _distanceTexture;
  int _textureSizeBias;
  int _width;
  int _height;

  ScriptableRenderPass IAfterOpaquePass.GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorAttachmentHandle, RenderTargetHandle depthAttachmentHandle)
  {
    if (_camera == null)
    {
      _camera = GetComponent<Camera>();
    }

    if (_pass == null)
    {
      _pass = new WaterReflectionDistanceOnlyRenderPass();
    }

    if (s_propertyIdWaterReflectionDistanceTexture == 0)
    {
      s_propertyIdWaterReflectionDistanceTexture = Shader.PropertyToID("WaterReflectionDistanceTexture");
    }

    if (
      (TextureSizeBias != _textureSizeBias) ||
      (_camera.pixelWidth != _width) ||
      (_camera.pixelHeight != _height))
    {
      _textureSizeBias = TextureSizeBias;
      _width = _camera.pixelWidth;
      _height = _camera.pixelHeight;

      int size = Common.GetSquareTextureSize(_width, _height, _textureSizeBias);

      if (_distanceTexture != null)
      {
        _distanceTexture.Release();
      }

      _distanceTexture = new RenderTexture(size, size, 16, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
      _distanceTexture.name = TextureName;
      _distanceTexture.filterMode = FilterMode.Point;
      Common.SetRenderTextureProperties(_distanceTexture);

      Shader.SetGlobalTexture(s_propertyIdWaterReflectionDistanceTexture, _distanceTexture);

      _pass.DistanceTexture = _distanceTexture;
    }

    if ((enabled) && (_distanceTexture != null))
    {
      return _pass;
    }
    else
    {
      return NullRenderPass.Instance;
    }
  }
}

public class WaterReflectionDistanceOnlyRenderPass : ScriptableRenderPass
{
  const string PassName = "Water Reflection Distance Pass";

  public RenderTexture DistanceTexture;

  FilterRenderersSettings _opaqueFilterSettings;

  public WaterReflectionDistanceOnlyRenderPass()
  {
    RegisterShaderPassName("WaterReflectionDistanceOnly");

    _opaqueFilterSettings = new FilterRenderersSettings(true);
    _opaqueFilterSettings.renderQueueRange = RenderQueueRange.opaque;
  }

  public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
  {
    if (renderer != null)
    {
      CommandBuffer commandBuffer = CommandBufferPool.Get(PassName);

      using (new ProfilingSample(commandBuffer, PassName))
      {
        SetRenderTarget(
          commandBuffer,
          DistanceTexture,
          RenderBufferLoadAction.DontCare,
          RenderBufferStoreAction.Store,
          ClearFlag.All,
          Common.InfinityColor,
          DistanceTexture.dimension);

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();

        SortFlags sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

        DrawRendererSettings drawSettings = CreateDrawRendererSettings(
          renderingData.cameraData.camera, 
          sortFlags, 
          RendererConfiguration.None, 
          renderingData.supportsDynamicBatching);

        if (renderingData.cameraData.isStereoEnabled)
        {
          Camera camera = renderingData.cameraData.camera;
          context.StartMultiEye(camera);
          context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, _opaqueFilterSettings);
          context.StopMultiEye(camera);
        }
        else
        {
          context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, _opaqueFilterSettings);
        }
      }

      context.ExecuteCommandBuffer(commandBuffer);

      CommandBufferPool.Release(commandBuffer);
    }
  }
}
