Shader "Herl/Solid"
{
	HLSLINCLUDE

  #pragma prefer_hlslcc gles
  #pragma exclude_renderers d3d11_9x
  #pragma target 2.0
  #pragma multi_compile_instancing

  #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

  struct appdata
  {
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f
  {
    float4 pos : SV_POSITION;
  };

  v2f vert(appdata v)
  {
    v2f o;

    UNITY_SETUP_INSTANCE_ID(v);

    o.pos = TransformObjectToHClip(v.vertex.xyz);
    return o;
  }
     
  half4 frag() : SV_TARGET
  {
    return 0;
  }

  ENDHLSL

  SubShader
  {
    Pass
    {
      Name "Normal"

      Blend Off
      Cull Back
      ZTest LEqual
      ZWrite On

      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDHLSL
    }

    Pass
    {
      Name "Shadow Caster"

      Tags
      {
        "LightMode" = "ShadowCaster"
      }

      Blend Off
      Cull Back
      ZTest LEqual
      ZWrite On

      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0
      #pragma vertex ShadowPassVertex
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\LitInput.hlsl"
      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\ShadowCasterPass.hlsl"

      ENDHLSL
    }

    Pass
    {
      Name "Depth Only"

      Tags
      {
        "LightMode" = "DepthOnly"
      }

      Blend Off
      Cull Back
      ZTest LEqual
      ZWrite On
      ColorMask 0

      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0
      #pragma vertex DepthOnlyVertex
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\LitInput.hlsl"
      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\DepthOnlyPass.hlsl"

      ENDHLSL
    }


    Pass
    {
      Name "Depth Only Cull Off"

      Tags
      {
        "LightMode" = "DepthOnly"
      }

      Blend Off
      Cull Off
      ZTest LEqual
      ZWrite On
      ColorMask 0

      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0
      #pragma vertex DepthOnlyVertex
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\LitInput.hlsl"
      #include "Packages\com.unity.render-pipelines.lightweight\Shaders\DepthOnlyPass.hlsl"

      ENDHLSL
    }

    Pass
    {
      Name "Water Reflection Distance Only"

      Tags
      {
        "LightMode" = "WaterReflectionDistanceOnly"
      }

      Blend Off
      Cull Back
      ZTest LEqual
      ZWrite On

      HLSLPROGRAM

      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0
      #pragma vertex vertDistance
      #pragma fragment fragDistance
      #pragma multi_compile_instancing

      #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
      
      struct v2fDistance
      {
        float4 pos : SV_POSITION;
        float distance : TANGENT;
      };

      uniform float WaterCeiling;

      v2fDistance vertDistance(appdata v)
      {
        v2fDistance o;

        UNITY_SETUP_INSTANCE_ID(v);

        // Get the world position of each vertex in the mesh
        float4 world_pos = mul(UNITY_MATRIX_M, v.vertex);

        // Reflection position is reflected around water ceiling
        world_pos.y = (2.0 * WaterCeiling) - world_pos.y;

        // Get the distance from the vertex to the camera
        float distance = length(_WorldSpaceCameraPos.xyz - world_pos.xyz);
       
        // Pass the clip space position
        o.pos = TransformObjectToHClip(v.vertex.xyz);
        // Pass the distance to the camera
        o.distance = distance;

        return o;
      }

      float fragDistance(v2fDistance i) : SV_Target
      {
        return i.distance;
      }

      ENDHLSL
    }
  }

  FallBack Off
}
