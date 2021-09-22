Shader "Herl/Simple Lit"
{
  Properties
  {
    _Color("Color", Color) = (0.5, 0.5, 0.5, 1)
    _MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}

    _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

    _Shininess("Shininess", Range(0.01, 1.0)) = 0.5
    _GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0

    _Glossiness("Glossiness", Range(0.0, 1.0)) = 0.5
    [Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

    [HideInInspector] _SpecSource("Specular Color Source", Float) = 0.0
    _SpecColor("Specular", Color) = (0.5, 0.5, 0.5)
    _SpecGlossMap("Specular", 2D) = "white" {}
    [HideInInspector] _GlossinessSource("Glossiness Source", Float) = 0.0
    [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
    [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

    [HideInInspector] _BumpScale("Scale", Float) = 1.0
    [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

    _EmissionColor("Emission Color", Color) = (0,0,0)
    _EmissionMap("Emission", 2D) = "white" {}

    // Blending state
    [HideInInspector] _Surface("__surface", Float) = 0.0
    [HideInInspector] _Blend("__blend", Float) = 0.0
    [HideInInspector] _AlphaClip("__clip", Float) = 0.0
    [HideInInspector] _SrcBlend("__src", Float) = 1.0
    [HideInInspector] _DstBlend("__dst", Float) = 0.0
    [HideInInspector] _ZWrite("__zw", Float) = 1.0
    [HideInInspector] _Cull("__cull", Float) = 2.0

    [ToogleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0

    _IntensityVC("Vertex Color Intensity", Float) = 0.0

    _NormalNoiseGridIntensity("Normal Noise Grid Intensity", Float) = 0.0
    _NormalNoiseGridWidth("Normal Noise Grid Width", Float) = 0.0
    _NormalNoiseGridResolution("Normal Noise Grid Resolution", Float) = 0.0
  }

  SubShader
  {
    Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
    LOD 300

    Pass
    {
      Name "ForwardLit"
      Tags { "LightMode" = "LightweightForward" }

      // Use same blending / depth states as Standard shader
      Blend[_SrcBlend][_DstBlend]
      ZWrite[_ZWrite]
      Cull[_Cull]

      HLSLPROGRAM
      // Required to compile gles 2.0 with standard srp library
      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0

      // -------------------------------------
      // Material Keywords
      #pragma shader_feature _ALPHATEST_ON
      #pragma shader_feature _ALPHAPREMULTIPLY_ON
      #pragma shader_feature _ _SPECGLOSSMAP _SPECULAR_COLOR
      #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA
      #pragma shader_feature _NORMALMAP
      #pragma shader_feature _EMISSION
      #pragma shader_feature _RECEIVE_SHADOWS_OFF

      #pragma shader_feature _VERTEXCOLOR
      #pragma shader_feature _VERTEXCOLOR_LERP

      #pragma shader_feature _NORMALNOISEGRID

      // -------------------------------------
      // Lightweight Pipeline keywords
      //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
      //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
      //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
      //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
      //#pragma multi_compile _ _SHADOWS_SOFT
      //#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
      #define _MAIN_LIGHT_SHADOWS
      #define _MAIN_LIGHT_SHADOWS_CASCADE
      #define _SHADOWS_SOFT

      // -------------------------------------
      // Unity defined keywords
      //#pragma multi_compile _ DIRLIGHTMAP_COMBINED
      //#pragma multi_compile _ LIGHTMAP_ON
      //#pragma multi_compile_fog

      //--------------------------------------
      // GPU Instancing
      #pragma multi_compile_instancing

      #pragma vertex LitPassVertexSimple
      #pragma fragment LitPassFragmentSimple
      #define BUMP_SCALE_NOT_SUPPORTED 1

      #include "SimpleLitInput.hlsl"
      #include "SimpleLitForwardPass.hlsl"
      ENDHLSL
    }

    UsePass "Herl/Solid/Shadow Caster"

    UsePass "Herl/Solid/Depth Only"

    UsePass "Herl/Solid/Water Reflection Distance Only"
  }

  CustomEditor "UnityEditor.Herl.SimpleLitShaderGUI"
}
