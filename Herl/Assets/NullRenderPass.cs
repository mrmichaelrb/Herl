using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

class NullRenderPass : ScriptableRenderPass
{
  public readonly static NullRenderPass Instance = new NullRenderPass();

  public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
  {
  }
}
