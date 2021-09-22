Shader "Herl/HUD"
{
  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "RenderType" = "Overlay"
      "Queue" = "Geometry"
      "IgnoreProjector" = "True"
    }

    Blend Off
    Cull Back
    ZTest LEqual
    ZWrite On
   
    Pass
    {
      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      #define VertexColored_Intensity (2.0)
      #define VertexColored_Alpha (1.0)

      #include "VertexColored.hlsl"
     
      ENDHLSL
    }

    UsePass "Herl/Solid/Depth Only"
  }

  FallBack Off
}
