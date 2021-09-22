namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
  public interface IAfterMainLightShadowCasterPass
  {
    ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle mainShadowmapHandle);
  }
}
