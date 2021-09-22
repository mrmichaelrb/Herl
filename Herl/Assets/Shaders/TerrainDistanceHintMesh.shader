Shader "Herl/Terrain Distance Hint Mesh"
{
  Properties
  {
    _Color("Color", Color) = (1.0, 1.0, 1.0, 0.5)
    _WireColor("WireColor", Color) = (0.0, 1.0, 0.0, 0.01)
  }

  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "Queue" = "Transparent"
      "ForceNoShadowCasting" = "True"
      "IgnoreProjector" = "True"
    }

    Blend SrcAlpha OneMinusSrcAlpha
    Cull Back
    ZTest Always
    ZWrite Off

    Pass
    {
      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 4.5
      #pragma require geometry
      #pragma vertex vert
      #pragma geometry geom
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
      #include "Common.hlsl"
      #include "Terrain.hlsl"
      
      uniform half4 _Color;
      uniform half4 _WireColor;

      struct appdata
      {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2g
      {
        float4 pos : SV_POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct g2f
      {
        float4 pos : SV_POSITION;
        float3 dist : TEXCOORD0;
      };

      v2g vert(appdata v)
      {
        v2g o;

        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        float4 vertexWorldPosition = mul(UNITY_MATRIX_M, v.vertex);

        float roughDistance = length(_WorldSpaceCameraPos.xz - vertexWorldPosition.xz);
        float heightOffset = (roughDistance * TerrainDistanceHintDistanceOffset) + TerrainDistanceHintHeightOffset;

        vertexWorldPosition.y = TerrainHeight(vertexWorldPosition.xz) + heightOffset + WorldPosition.y;

        o.pos = mul(UNITY_MATRIX_VP, vertexWorldPosition);

        return o;
      }

      [maxvertexcount(3)]
      void geom(triangle v2g i[3], inout TriangleStream<g2f> triStream)
      {
        float2 scale = float2(_ScreenParams.x * 0.5, _ScreenParams.y * 0.5);

        // Fragment position
        float2 p0 = scale * i[0].pos.xy / i[0].pos.w;
        float2 p1 = scale * i[1].pos.xy / i[1].pos.w;
        float2 p2 = scale * i[2].pos.xy / i[2].pos.w;

        // Barycentric position
        float2 v0 = p2 - p1;
        float2 v1 = p2 - p0;
        float2 v2 = p1 - p0;

        // Area of triangle
        float area = abs((v1.x * v2.y) - (v1.y * v2.x));

        g2f o;

        o.pos = i[0].pos;
        o.dist = float3(area / length(v0), 0.0, 0.0);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        triStream.Append(o);

        o.pos = i[1].pos;
        o.dist = float3(0.0, area / length(v1), 0.0);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        triStream.Append(o);

        o.pos = i[2].pos;
        o.dist = float3(0.0, 0.0, area / length(v2));
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        triStream.Append(o);
      }

      half4 frag(g2f i) : SV_Target
      {
        // Distance from triangle center
        float d = min(i.dist.x, min(i.dist.y, i.dist.z));

        float fade = exp2(-4.0 * d * d);

        return lerp(_Color, _WireColor, fade);
      }

      ENDHLSL
    }
  }

  FallBack Off
}
