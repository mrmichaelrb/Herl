Shader "Herl/Vertex Colored Normal Lit"
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

      #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
      #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
      #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"
      #include "Common.hlsl"
      #include "Rendering.hlsl"

      #define VertexColored_Intensity (4.0)
      #define VertexColored_Alpha (v.color.a)

      struct appdata
      {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        half4 color : COLOR;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        half4 color : COLOR;
      };

      v2f vert(appdata v)
      {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);

        o.pos = TransformObjectToHClip(v.vertex.xyz);

        float3 surfaceNormal = TransformObjectToWorldNormal(v.normal);
        float sunlightProduct = saturate(-dot(SunlightDirection, surfaceNormal));
        half3 diffuseSunlight = SunlightColor * sunlightProduct + SunlightAmbientColor;
        o.color = half4(v.color.rgb * VertexColored_Intensity * diffuseSunlight, VertexColored_Alpha);

        return o;
      }

      half4 frag(v2f i) : SV_Target
      {
        return i.color;
      }
     
      ENDHLSL
    }

    UsePass "Herl/Solid/Shadow Caster"

    UsePass "Herl/Solid/Depth Only"

    UsePass "Herl/Solid/Water Reflection Distance Only"
}

  FallBack Off
}