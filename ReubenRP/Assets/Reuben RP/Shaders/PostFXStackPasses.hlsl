#ifndef REUBEN_POST_FX_PASSES_INCLUDED
#define REUBEN_POST_FX_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

struct Varyings
{
    float4 pos: SV_POSITION;
    float2 uv: TEXCOORD0;
};

TEXTURE2D(_PostFXSource);       SAMPLER(sampler_linear_clamp);
TEXTURE2D(_PostFXSource2);

float4 _ProjectionParams;           //用于判断图像API是否要翻转
float4 _PostFXSource_TexelSize;
bool _BloomBicubicUpsampling;       //用于判断是否使用双三次滤波采样（更高质量的Bloom）
float4 _BloomThreshold;             //阈值相关的四个参数
float _BloomIntensity;              //Bloom强度

Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
    Varyings o = (Varyings)0;
    //这里做了啥呢？原本的屏幕是由两个三角形拼成的长方形，现在变成一个巨大的三角形，三角形两个直角边与屏幕左下边重合，斜边与屏幕右上角相交
    //三角形左下角的uv是（-1，-1），左上角是（-1, 3），右下角是（3, -1）
    //屏幕中心uv是（0, 0）
    o.pos = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    o.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    if(_ProjectionParams.x < 0.0)
    {
        o.uv.y = 1 - o.uv.y;
    }
    return o;
}

float4 GetSource(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV)
{
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 CopyPassFragment(Varyings i) : SV_Target
{
    return GetSource(i.uv);
}

float4 GetSourceTexelSize()
{
    return _PostFXSource_TexelSize;
}

float4 GetSourceBicubic(float2 screenUV)    //双三次滤波，用来平滑光照
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV, _PostFXSource_TexelSize.zwxy, 1.0, 0.0);
}

float3 ApplyBloomThreshold(float3 color)    //应用阈值
{
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 BloomHorizontalPassFragment(Varyings i) : SV_Target
{
    float3 color = 0;
    float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
    float weights[] = {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703, 0.19459459, 0.12162162, 0.05405405, 0.01621622
    };      //这东西来自杨辉三角第13行，并裁剪其边沿：66 220 495 792 924 792 495 220 66，他们的总和为4070，于是各除4070就得到如上权重
    for(int index = 0; index < 9; index++)
    {
        float offset = offsets[index] * 2.0 * GetSourceTexelSize().x;       //因为同时进行下采样，所以每次偏移步长要x2
        color += GetSource(i.uv + float2(offset, 0.0)).rgb * weights[index];
    }
    return float4(color, 1.0);
}

float4 BloomVerticalPassFragment(Varyings i) : SV_Target
{
    float3 color = 0;
    float offsets[] = {-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923};
    float weights[] = {0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027};
    //在高斯采样点键适当偏移，来减少样本数量
    for(int index = 0; index < 5; index++)
    {
        float offset = offsets[index] * 1.0 * GetSourceTexelSize().y;       //在第一个Pass里已经此案有了，所以就不用x2了
        color += GetSource(i.uv + float2(0.0, offset)).rgb * weights[index];
    }
    return float4(color, 1.0);
}

float4 BloomCombinePassFragment(Varyings i) : SV_Target
{
    float3 lowRes;
    if(_BloomBicubicUpsampling)
    {
        lowRes = GetSourceBicubic(i.uv).rgb;
    }
    else
    {
        lowRes = GetSource(i.uv).rgb;
    }
    float3 highRes = GetSource2(i.uv).rgb;
    return float4(lowRes * _BloomIntensity + highRes, 1);
}

float4 BloomPrefilterPassFragment(Varyings i) : SV_Target
{
    float3 color = ApplyBloomThreshold(GetSource(i.uv).rgb);
    return float4(color, 1);
}
#endif