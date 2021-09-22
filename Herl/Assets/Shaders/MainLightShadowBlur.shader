Shader "Herl/Main Light Shadow Blur"
{
	Properties
	{
		_MainTex ("", 2D) = "white" {}
	}

	HLSLINCLUDE

  #pragma prefer_hlslcc gles
  #pragma exclude_renderers d3d11_9x
  #pragma target 2.0

  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
  #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

  static const float Offset13Tap1 = 1.41176470;
  static const float Offset13Tap2 = 3.29411764;
  static const float Offset13Tap3 = 5.17647058;

  static const float Weight13Tap1 = 0.19648255;
	static const float Weight13Tap2 = 0.29690696;
	static const float Weight13Tap3 = Weight13Tap2;
	static const float Weight13Tap4 = 0.09447039;
	static const float Weight13Tap5 = Weight13Tap4;
	static const float Weight13Tap6 = 0.01038136;
	static const float Weight13Tap7 = Weight13Tap6;

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f_13tap
	{
		float4 pos : SV_POSITION;
		float2 texcoord : TEXCOORD0;
#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    float2 offset1 : TEXCOORD1;
    float2 offset2 : TEXCOORD2;
    float2 offset3 : TEXCOORD3;
#else
		float4 blurTexcoord[3] : TEXCOORD1;
#endif
	};

  Texture2D _MainTex;
	uniform float2 _MainTex_TexelSize;
  SamplerState _MainTex_linear_clamp_sampler;

//  Texture2D _MainLightShadowmapTexture;
  SamplerState _MainLightShadowmapTexture_point_clamp_sampler;

  uniform float MainLightShadowBlurRadius;

  float2 GetCascadeFilter(float2 uv)
  {
    return round(uv);
  }

  float GetCascadeBlurFactor(float2 filter)
  {
    // 0.0 < x < 0.5, 0.0 < y < 0.5 : 1
    // 0.5 < x < 1.0, 0.0 < y < 0.5 : 1/3
    // 0.0 < x < 0.5, 0.5 < y < 1.0 : 1/6
    // 0.5 < x < 1.0, 0.5 < y < 1.0 : 1/12
    float x = filter.x;
    float y = filter.y;
    return (1.0 / ((((2.0 * x) + 1.0) * (1.0 - y)) + (y * ((6.0 * x) + 6.0))));
  }

  float2 ClampWithinCascade(float2 filter, float2 uv)
  {
    float2 offset = filter * 0.5;
    return (saturate((uv - offset) * 2.0) * 0.5) + offset;
  }

  v2f vertFirst(appdata v)
	{
		v2f o;

		o.pos = TransformObjectToHClip(v.vertex.xyz);
		o.texcoord = v.uv;

		return o;
	}

  float fragFirst(v2f_13tap i) : SV_Target
	{
    float shadow = _MainLightShadowmapTexture.Sample(_MainLightShadowmapTexture_point_clamp_sampler, i.texcoord).r;
    shadow = (shadow != 0.0);
    shadow *= GetMainLightShadowStrength();

    return (1.0 - shadow);
	}

	v2f_13tap vert13Horizontal(appdata v)
	{
		v2f_13tap o;

    float2 uv = v.uv;
		o.pos = TransformObjectToHClip(v.vertex.xyz);
		o.texcoord = uv;

		float2 offset1 = float2(_MainTex_TexelSize.x * MainLightShadowBlurRadius * Offset13Tap1, 0.0); 
		float2 offset2 = float2(_MainTex_TexelSize.x * MainLightShadowBlurRadius * Offset13Tap2, 0.0);
		float2 offset3 = float2(_MainTex_TexelSize.x * MainLightShadowBlurRadius * Offset13Tap3, 0.0);

#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    o.offset1 = offset1;
    o.offset2 = offset2;
    o.offset3 = offset3;
#else
		o.blurTexcoord[0].xy = uv + offset1;
		o.blurTexcoord[0].zw = uv - offset1;
		o.blurTexcoord[1].xy = uv + offset2;
		o.blurTexcoord[1].zw = uv - offset2;
		o.blurTexcoord[2].xy = uv + offset3;
		o.blurTexcoord[2].zw = uv - offset3;
#endif

		return o;
	}

	v2f_13tap vert13Vertical(appdata v)
	{
		v2f_13tap o;

    float2 uv = v.uv;
		o.pos = TransformObjectToHClip(v.vertex.xyz);
		o.texcoord = uv;

		float2 offset1 = float2(0.0, _MainTex_TexelSize.y * MainLightShadowBlurRadius * Offset13Tap1); 
		float2 offset2 = float2(0.0, _MainTex_TexelSize.y * MainLightShadowBlurRadius * Offset13Tap2);
		float2 offset3 = float2(0.0, _MainTex_TexelSize.y * MainLightShadowBlurRadius * Offset13Tap3);

#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    o.offset1 = offset1;
    o.offset2 = offset2;
    o.offset3 = offset3;
#else
		o.blurTexcoord[0].xy = uv + offset1;
		o.blurTexcoord[0].zw = uv - offset1;
		o.blurTexcoord[1].xy = uv + offset2;
		o.blurTexcoord[1].zw = uv - offset2;
		o.blurTexcoord[2].xy = uv + offset3;
		o.blurTexcoord[2].zw = uv - offset3;
#endif

		return o;
	}

	float frag13Blur(v2f_13tap i) : SV_Target
	{
    float4 blurTexcoord[3];

#ifdef _MAIN_LIGHT_SHADOWS_CASCADE
    float2 uv = i.texcoord;
    float2 filter = GetCascadeFilter(uv);
    float factor = GetCascadeBlurFactor(filter);
		blurTexcoord[0].xy = ClampWithinCascade(filter, uv + (i.offset1 * factor));
		blurTexcoord[0].zw = ClampWithinCascade(filter, uv - (i.offset1 * factor));
		blurTexcoord[1].xy = ClampWithinCascade(filter, uv + (i.offset2 * factor));
		blurTexcoord[1].zw = ClampWithinCascade(filter, uv - (i.offset2 * factor));
		blurTexcoord[2].xy = ClampWithinCascade(filter, uv + (i.offset3 * factor));
		blurTexcoord[2].zw = ClampWithinCascade(filter, uv - (i.offset3 * factor));
#else
    blurTexcoord = i.blurTexcoord;
#endif

		float center = _MainTex.Sample(_MainTex_linear_clamp_sampler, i.texcoord).r;

    if (center < 1.0)
    {
      center = center * Weight13Tap1;

      float side1 = _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[0].xy).r * Weight13Tap2;
		  side1 += _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[1].xy).r * Weight13Tap4;
		  side1 += _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[2].xy).r * Weight13Tap6;

		  float side2 = _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[0].zw).r * Weight13Tap3;
		  side2 += _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[1].zw).r * Weight13Tap5;
		  side2 += _MainTex.Sample(_MainTex_linear_clamp_sampler, blurTexcoord[2].zw).r * Weight13Tap7;

      side1 = (2.0 * side1) + center;
      side2 = (2.0 * side2) + center;

      float brightSide = max(side1, side2);

      return saturate(max(brightSide, center));
    }
    else
    {
      return 1.0;
    }
	}

	ENDHLSL

	SubShader
	{
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "IgnoreProjector" = "True"
    }

    Cull Off
    ZWrite Off
		ZTest Always

		Pass
		{
      Name "First"

			HLSLPROGRAM
			#pragma vertex vertFirst
			#pragma fragment fragFirst
			ENDHLSL
		}

		Pass
		{
      Name "Horizontal"

			HLSLPROGRAM
			#pragma vertex vert13Horizontal
			#pragma fragment frag13Blur
			ENDHLSL
		}

		Pass
		{
      Name "Vertical"

			HLSLPROGRAM
			#pragma vertex vert13Vertical
			#pragma fragment frag13Blur
			ENDHLSL
		}
	}
}
