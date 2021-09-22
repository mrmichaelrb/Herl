Shader "Herl/Raymarcher"
{
  Properties
  {
    TestTexture("Test Texture", 2D) = "black" {}
    TestNormalMap("Test Normal Map", 2D) = "black" {}
  }
  
  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "Queue" = "AlphaTest-1"
      "ForceNoShadowCasting" = "True"
      "IgnoreProjector" = "True"
    }

    Blend SrcAlpha OneMinusSrcAlpha
    Cull Off
    ZTest Always
    ZWrite On

    Pass
    {
      HLSLPROGRAM

//#pragma enable_d3d11_debug_symbols
//#define SHOW_DISCARD
//#define SHOW_TERRAIN_DISTANCE_HINT
//#define SHOW_WATER_REFLECTION
//#define SHOW_WATER_REFLECTION_DISTANCE

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 4.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing

#define _MAIN_LIGHT_SHADOWS
#define _MAIN_LIGHT_SHADOWS_CASCADE
#define _SHADOWS_SOFT

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
      TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
      TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

      SAMPLER(sampler_CameraDepthTexture);

#include "Scene.hlsl"

      struct appdata
      {
        // Viewport position
        float4 vertex : POSITION;

        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        // Viewport position
        float4 pos : SV_POSITION;
        // Screen position
        float2 uv : NORMAL;
        // World direction
        float3 worldDir : TEXCOORD0;
      };

      struct frag_out
      {
        half4 color : SV_Target;
        float depth : SV_Depth;
      };

      // Copied from UnityCG.cginc because ComputeScreenPos in SRP is broken
      float4 ComputeNonStereoScreenPosOld(float4 pos)
      {
        float4 o = pos * 0.5;
        o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
        o.zw = pos.zw;
        return o;
      }

      float4 ComputeScreenPosOld(float4 pos)
      {
        float4 o = ComputeNonStereoScreenPosOld(pos);
#if defined(UNITY_SINGLE_PASS_STEREO)
        o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
        return o;
      }

      v2f vert(appdata v)
      {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);

        // No translation because the surface is always projected
        // flatly in front of camera
        o.pos = v.vertex;
        o.uv = ComputeScreenPosOld(v.vertex).xy;

        float3 worldDir = mul(unity_CameraInvProjection, v.vertex).xyz;
        worldDir.x *= -1.0;
        worldDir = worldDir / worldDir.z;

        o.worldDir = mul(unity_CameraToWorld, float4(worldDir.xyz, 0.0)).xyz;

        return o;
      }

      frag_out frag(v2f i)
      {
        frag_out o;

        float2 screenUv = i.uv.xy;

        float hitDistance;
        float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenUv);

        // If the depth texture has not been written at this location, then
        // no vector object has been rendered
        if (depthSample == UNITY_RAW_FAR_CLIP_VALUE)
        {
          hitDistance = 1.#INF;
        }
        // Otherwise, get the line of sight distance to the object rendered
        // in the depth texture
        else
        {
          float depth = LinearEyeDepth(depthSample, _ZBufferParams);
          hitDistance = depth * length(i.worldDir);
        }

        if (hitDistance <= RenderDistanceMin)
        {
#ifdef SHOW_DISCARD
          o.color = half4(1.0, 1.0, 1.0, 1.0);
          return o;
#else
          // TODO: Determine if discard is faster than returning transparent pixel
          discard;
#endif
        }

        half4 fragColor = NoColor;
        float3 rayStart = _WorldSpaceCameraPos;
        float3 rayDirection = normalize(i.worldDir);
        RenderState r = {screenUv, rayStart, rayDirection, NoColor, NoColor};

        RenderScene(r, hitDistance, fragColor);

#ifdef SHOW_TERRAIN_DISTANCE_HINT
        float hint = TerrainDistanceHint(r);
        if (hint == 0.0)
        {
          fragColor.b = 1.0;
        }
        else if (hint <= 1.0)
        {
          fragColor.r = 1.0;
        }
        else if (hint < 1.#INF)
        {
          fragColor.g = 1.0;
        }
#endif

#ifdef SHOW_WATER_REFLECTION
        half4 reflectionColor = SampleTexture(WaterReflectionTexture, WaterReflectionUv(r));
        RenderColor(reflectionColor.rgb, 0.5, fragColor);
#endif

#ifdef SHOW_WATER_REFLECTION_DISTANCE
        float reflectionDistance = SampleTexture(WaterReflectionDistanceTexture, WaterReflectionUv(r)).r;
        if (reflectionDistance == 1.0)
        {
          fragColor.b = 1.0;
        }
        else if (reflectionDistance <= 1.0)
        {
          fragColor.r = 1.0;
        }
        else if (reflectionDistance < 1.#INF)
        {
          fragColor.g = 1.0;
        }
#endif

        if (fragColor.a == 0.0)
        {
          discard;
        }

        o.depth = LinearEyeDepth(hitDistance, _ZBufferParams);
        o.color = fragColor;
        return o;
      }

      ENDHLSL
    }
  }

  FallBack "Hidden/InternalErrorShader"
}
