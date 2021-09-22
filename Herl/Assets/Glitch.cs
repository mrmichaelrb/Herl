using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using MinAttribute = UnityEngine.Rendering.PostProcessing.MinAttribute;

[Serializable]
[PostProcess(typeof(GlitchRenderer), PostProcessEvent.AfterStack, "Herl/Glitch")]
public sealed class Glitch : PostProcessEffectSettings
{
  [Min(1.0f), Tooltip("Number of vertical and horizontal blocks.")]
  public FloatParameter Blocks = new FloatParameter { value = 64.0f };

  [Range(0.0f, 1.0f), Tooltip("Glitch effect intensity.")]
  public FloatParameter Intensity = new FloatParameter { value = 0.0f };

  public override bool IsEnabledAndSupported(PostProcessRenderContext context)
  {
    return ((enabled.value) && (Intensity.value > 0.0f));
  }
}

public sealed class GlitchRenderer : PostProcessEffectRenderer<Glitch>
{
  public override void Render(PostProcessRenderContext context)
  {
    PropertySheet sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Glitch"));
    sheet.properties.SetFloat("_BlocksY", settings.Blocks);
    sheet.properties.SetFloat("_BlockSizeY", (1.0f / settings.Blocks));
    sheet.properties.SetFloat("_Intensity", settings.Intensity);

    bool singlePassDoubleWide = (
      (context.stereoActive) &&
      (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) &&
      (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));

    if (singlePassDoubleWide)
    {
      sheet.properties.SetFloat("_BlocksX", settings.Blocks * 2.0f);
      sheet.properties.SetFloat("_BlockSizeX", (0.5f / settings.Blocks));
    }
    else
    {
      sheet.properties.SetFloat("_BlocksX", settings.Blocks);
      sheet.properties.SetFloat("_BlockSizeX", (1.0f / settings.Blocks));
    }

    context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
  }
}
