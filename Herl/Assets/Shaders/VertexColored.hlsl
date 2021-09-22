#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

#ifndef VertexColored_Intensity
#define VertexColored_Intensity (1.0)
#endif

#ifndef VertexColored_Alpha
#define VertexColored_Alpha (1.0)
#endif

struct appdata
{
  float4 vertex : POSITION;
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
  o.color = half4(v.color.rgb * VertexColored_Intensity, VertexColored_Alpha);
  return o;
}
     
half4 frag(v2f i) : COLOR
{
  return i.color;
}
