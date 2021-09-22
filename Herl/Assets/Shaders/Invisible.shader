Shader "Herl/Invisible"
{
  SubShader
  {
    Blend Off
    Cull Off
    ZTest Always
    ZWrite Off

    Pass
    {
      ColorMask 0

      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      struct v2f
      {
        float4 pos : SV_POSITION;
      };
     
      v2f vert()
      {
        v2f o;
        o.pos = float4(0.0, 0.0, 0.0, 0.0);
        return o;
      }
 
      half4 frag(v2f i) : SV_Target
      {
        return half4(0.0, 0.0, 0.0, 0.0);
      }

     ENDHLSL
    }
  }

  Fallback Off
}
