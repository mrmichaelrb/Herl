#ifndef COMPRESSION_HLSL
#define COMPRESSION_HLSL

float3 UnkpackNormal(
  float4 packedNormal)
{
#if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(packedNormal, 1.0);
#else
  return UnpackNormalmapRGorAG(packedNormal, 1.0);
#endif
}

#endif
