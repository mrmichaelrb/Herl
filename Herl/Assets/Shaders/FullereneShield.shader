Shader "Herl/Fullerene Shield"
{
  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "RenderType" = "Opaque"
      "Queue" = "Geometry"
      "IgnoreProjector" = "True"
    }

    // Blend with uncleared last frame to get burn effect
    Blend SrcAlpha OneMinusSrcAlpha
    // Draw both sides of shield
    Cull Off
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
      #define VertexColored_Alpha (v.color.a)

      #include "VertexColored.hlsl"
     
      ENDHLSL
    }

    UsePass "Herl/Solid/Depth Only Cull Off"

    UsePass "Herl/Solid/Water Reflection Distance Only"
  }

  FallBack Off
}
