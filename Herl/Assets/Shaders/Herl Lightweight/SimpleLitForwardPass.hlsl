#ifndef LIGHTWEIGHT_SIMPLE_LIT_PASS_INCLUDED
#define LIGHTWEIGHT_SIMPLE_LIT_PASS_INCLUDED

#include "../Rendering.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS    : POSITION;
    float3 normalOS      : NORMAL;
    float4 tangentOS     : TANGENT;
    float2 texcoord      : TEXCOORD0;
    float2 lightmapUV    : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID

#if defined(_VERTEXCOLOR) || defined(_VERTEXCOLOR_LERP)
    half4 color : COLOR;
#endif
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float4 posWSShininess           : TEXCOORD2;    // xyz: posWS, w: Shininess * 128

#if defined(_NORMALMAP) || defined(_NORMALNOISEGRID)
    half4 normal                    : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangent                   : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 bitangent                  : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
#else
    half3  normal                   : TEXCOORD3;
    half3 viewDir                   : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord              : TEXCOORD7;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO

#if defined(_VERTEXCOLOR) || defined(_VERTEXCOLOR_LERP)
    half4 color : COLOR;
#endif
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = input.posWSShininess.xyz;

#if defined(_NORMALMAP) || defined(_NORMALNOISEGRID)
    half3 viewDirWS = half3(input.normal.w, input.tangent.w, input.bitangent.w);
    inputData.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangent.xyz, input.bitangent.xyz, input.normal.xyz));
#else
    half3 viewDirWS = input.viewDir;
    inputData.normalWS = input.normal;
#endif

#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    inputData.viewDirectionWS = viewDirWS;
#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    inputData.shadowCoord = input.shadowCoord;
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Simple Lighting) shader
Varyings LitPassVertexSimple(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

    output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
    output.posWSShininess.xyz = vertexInput.positionWS;
    output.posWSShininess.w = _Shininess * 128.0;
    output.positionCS = vertexInput.positionCS;

#if defined(_NORMALMAP) || defined(_NORMALNOISEGRID)
    output.normal = half4(normalInput.normalWS, viewDirWS.x);
    output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normal = normalInput.normalWS;
    output.viewDir = viewDirWS;
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normal.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

#if defined(_VERTEXCOLOR)
    output.color.rgb = input.color.rgb * _IntensityVC;
    output.color.a = input.color.a;
#endif
#if defined(_VERTEXCOLOR_LERP)
    output.color = input.color;
#endif

    return output;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.uv;
    half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex));
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

    half alpha = diffuseAlpha.a * _Color.a;
    AlphaDiscard(alpha, _Cutoff);
#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

#if _VERTEXCOLOR
    if (all(input.color.rgb <= 1.0f))
    {
      diffuse.rgb *= input.color.rgb;
    }
    else
    {
      return half4(input.color.rgb, alpha);
    }
#endif
#if _VERTEXCOLOR_LERP
    diffuse.rgb *= lerp(half3(1,1,1), input.color.rgb, _IntensityVC);
#endif

    half3 normalTS = SampleNormal(uv, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
    half3 emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));
    half4 specularGloss = SampleSpecularGloss(uv, diffuseAlpha.a, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap));
    half shininess = input.posWSShininess.w;

#if _NORMALNOISEGRID
    normalTS = AddMapNormalNoiseGrid(normalTS, uv, _NormalNoiseGridResolution, _NormalNoiseGridWidth, _NormalNoiseGridIntensity);
    // TODO: Necessary?
    normalTS = normalize(normalTS);
#endif

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    half4 color = LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
};

#endif
