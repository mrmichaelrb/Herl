Shader "Herl/Terrain Distance Hint"
{
  SubShader
  {
    Tags
    {
      "RenderPipeline" = "LightweightPipeline"
      "Queue" = "Geometry"
      "ForceNoShadowCasting" = "True"
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
      #pragma target 4.5
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
      #include "Terrain.hlsl"
      
      struct appdata
      {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        float distance : TANGENT;
      };

      v2f vert(appdata v)
      {
        v2f o;

        UNITY_SETUP_INSTANCE_ID(v);

        // Get the world position of each vertex in the mesh
        float4 vertexWorldPosition = mul(UNITY_MATRIX_M, v.vertex);

        // Calculate a height offset based on the rough distance
        float roughDistance = length(_WorldSpaceCameraPos.xz - vertexWorldPosition.xz);
        float heightOffset = (roughDistance * TerrainDistanceHintDistanceOffset) + TerrainDistanceHintHeightOffset;

        // Set the height of each vertex in the mesh according to its location on the terrain.
        // Apply a height offset to allow for surface attributes (like vegitation)
        // to protrude above the terrain, and avoid shimmering near peaks.
        vertexWorldPosition.y = TerrainHeight(vertexWorldPosition.xz) + heightOffset + WorldPosition.y;

        // Get the distance from the vertex to the camera
        float distance = length(_WorldSpaceCameraPos.xyz - vertexWorldPosition.xyz);
       
        // Pass the clip space position
        o.pos = mul(UNITY_MATRIX_VP, vertexWorldPosition);
        // Pass the distance to the camera
        o.distance = distance;

        return o;
      }

      float frag(v2f i) : SV_Target
      {
        return i.distance;
      }

      ENDHLSL
    }
  }

  FallBack Off
}
