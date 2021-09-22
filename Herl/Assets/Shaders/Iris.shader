Shader "Herl/Iris"
{
  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "Queue" = "Background"
      "ForceNoShadowCasting" = "True"
      "IgnoreProjector" = "True"
    }

    Blend Off
    Cull Off
    ZTest Always
    ZWrite On

    Pass
    { 
      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      struct appdata
      {
        float4 vertex : POSITION;
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
      };
     
      v2f vert(appdata v)
      {
        v2f o;

        // No translation because the surface is always projected
        // flatly in front of camera
        o.pos = v.vertex;
        return o;
      }
     
      float4 frag(v2f i) : SV_Target
      {
        return float4(0.0, 0.0, 0.0, 1.0);
      }

      ENDHLSL
    }
  }

  Fallback Off
}
