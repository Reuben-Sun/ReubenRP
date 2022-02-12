#ifndef REUBEN_LIT_INPUT_INCLUDED
#define REUBEN_LIT_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"


// CBUFFER_START(UnityPerMaterial)
//     float4 _MainTex_ST;
//     float4 _BaseColor;
// CBUFFER_END

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
TEXTURE2D(_EmissionMap);

float2 TransformBaseUV(float2 baseUV)   //主帖图 UV转换
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBaseColor(float2 baseUV)      //返回 主帖图颜色 * baseColor
{
    float4 map = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    return map * color;
}

float GetCutoff(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
}

float GetSmoothness(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float3 GetEmission(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_MainTex, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
    return map.rgb * color.rgb;
}

float GetFresnel(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Fresnel);
}
#endif


