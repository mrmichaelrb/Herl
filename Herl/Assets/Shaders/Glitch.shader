Shader "Hidden/Custom/Glitch"
{
  HLSLINCLUDE

  #include "Common.hlsl"
  #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

  uniform float _BlocksX;
  uniform float _BlockSizeX;
  uniform float _BlocksY;
  uniform float _BlockSizeY;
  uniform float _Intensity;

  TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

  float4 frag(VaryingsDefault i) : SV_Target
  {
    float2 block;
    block.x = floor((i.texcoordStereo.x * _BlocksX) + 0.5) * _BlockSizeX;
    block.y = floor((i.texcoordStereo.y * _BlocksY) + 0.5) * _BlockSizeY;

    float noise = Float3Rand(float3(block.xy, TIME_SECONDS));
    float factor = (noise * -_Intensity) + _Intensity;
    
    float2 moveSize;
    moveSize.x = _BlockSizeX * factor;
    moveSize.y = _BlockSizeY * factor;

    float2 moveSizeReciprical = 1.0 / moveSize;
    float2 move = floor((i.texcoordStereo * moveSizeReciprical) + 0.5) * moveSize;
    float2 uv = ((block - move) * factor) + i.texcoordStereo;
    
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
  }

  ENDHLSL

  SubShader
  {
    Cull Off
    ZTest Always
    ZWrite Off

    Pass
    {
      HLSLPROGRAM

      #pragma vertex VertDefault
      #pragma fragment frag

      ENDHLSL
    }
  }
}
