#ifndef REUBEN_LIT_INPUT_INCLUDED
#define REUBEN_LIT_INPUT_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// struct InputConfig
// {
//     float2 baseUV;
//     float2 detailUV;    //目前本项目不支持细节纹理
// };
//
// InputConfig GetInputConfig(float2 baseUV, float2 detailUV = 0.0)
// {
//     InputConfig config;
//     config.baseUV = baseUV;
//     config.detailUV = detailUV;
//     return config;
// }

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
UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
TEXTURE2D(_EmissionMap);
TEXTURE2D(_MaskMap);    //遮罩贴图
TEXTURE2D(_NormalMap);  //法线贴图


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

float4 GetMask(float2 baseUV)
{
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_MainTex, baseUV);
}

float GetMetallic(float2 baseUV)
{
    float metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    metallic *= GetMask(baseUV).r;
    return metallic;
}

float GetSmoothness(float2 baseUV)
{
    float smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    smoothness *= GetMask(baseUV).a;
    return smoothness;
}

float GetOcclusion(float2 baseUV)
{
    float strength = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Occlusion);
    float occlusion = GetMask(baseUV).g;
    occlusion = lerp(occlusion, 1.0, strength);
    return occlusion;
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

float3 GetNormalTS(float2 baseUV)       //获得切线空间法线
{
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_MainTex, baseUV);
    float scale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalScale);
    float3 normal = DecodeNormal(map, scale);
    return normal;
}
#endif


