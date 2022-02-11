#ifndef REUBEN_SHADOWS_INCLUDED
#define REUBEN_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);  //阴影图集
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_ReubenShadows)
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];  //阴影转换矩阵

    int _CascadeCount;  //级联数量
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];   //级联包围球

    // float _ShadowDistance;
    float4 _ShadowDistanceFade;     //阴影过渡距离

    float4 _CascadeData[MAX_CASCADE_COUNT];     //级联数据

    float4 _ShadowAtlasSize;
CBUFFER_END

struct DirectionalShadowData    //阴影信息
{
    float strength;
    int tileIndex;      //在图集中的索引
    float normalBias;   //法线偏差
};

struct ShadowData       //阴影数据
{
    int cascadeIndex;   //级联索引
    float strength;     //如果超出了最后一个级联的范围，就没有有效的阴影数据了，此时不需要采样，将 strength设为 0
    float cascadeBlend; //混合级联
};

float SampleDirectionalShadowAtlas(float3 positionSTS)      //采集阴影图集
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

// PCF滤波样本
#if defined(_DIRECTIONAL_PCF3)
#define DIRECTIONAL_FILTER_SAMPLES 4
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
#define DIRECTIONAL_FILTER_SAMPLES 9
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
#define DIRECTIONAL_FILTER_SAMPLES 16
#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];       //样本权重
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];   //样本位置
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;

    for(int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++)
    {
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
    #else
    return SampleDirectionalShadowAtlas(positionSTS);
    #endif
    
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data,ShadowData global,  Surface surface)
{
    #if !defined(_RECELIVE_SHADOWS)
        return 1;           //  如果不接受阴影，阴影衰减为1
    #endif
    if(data.strength <= 0)
    {
        return 1;       //当灯光阴影强度小于0时，阴影衰减不受阴影影响
    }
    float3 normalBias = surface.normal * _CascadeData[global.cascadeIndex].y;       //法线偏差
    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surface.posWS + normalBias, 1)).xyz;     //获得图块空间位置
    float shadow = FilterDirectionalShadow(positionSTS);       //对图集进行采样

    if(global.cascadeBlend < 1.0)   //如果级联混合小于 1，表示在级联过渡区域内，采集下一个级联并插值
    {
        normalBias = surface.normal * (data.normalBias * _CascadeData[global.cascadeIndex + 1].y);      //下一个级联
        positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex + 1], float4(surface.posWS + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    
    return lerp(1.0, shadow, data.strength);
}

float FadeShadowStrength(float distance, float scale, float fade)   //计算阴影过渡时的强度
{
    return saturate((1 - distance * scale) * fade);
}


ShadowData GetShadowData(Surface surface)
{
    ShadowData data;
    data.cascadeBlend = 1.0;
    // data.strength = surface.depth < _ShadowDistance ? 1 : 0;
    data.strength = FadeShadowStrength(surface.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);    //线性过渡的阴影强度
    int i;
    for(i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSir = DistanceSquared(surface.posWS, sphere.xyz);
        if(distanceSir < sphere.w)
        {
            float fade = FadeShadowStrength(distanceSir, _CascadeData[i].x, _ShadowDistanceFade.z);     //级联过渡强度
            if(i == _CascadeCount -1)   //如果绘制的对象在最后一个级联中，阴影强度 = 级联过渡阴影强度 * 距离过渡阴影强度
            {
                // data.strength *= FadeShadowStrength(distanceSir, _CascadeData[i].x, _ShadowDistanceFade.z);
                data.strength *= fade;
            }
            else
            {
                data.cascadeBlend = fade;
            }
            break;      //包围球越来越大，直到包围球包裹了物体
        }
    }
    if(i == _CascadeCount)  //i 最大到 _CascadeCount-1，如果等于 _CascadeCount，说明超了，没找到
    {
        data.strength = 0;
    }

#if defined(_CASCADE_BLEND_DITHER)
    else if(data.cascadeBlend < surface.dither)
    {
        i += 1;
    }
#endif
    
#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1;
#endif
    
    data.cascadeIndex = i;
    return data;
}



#endif