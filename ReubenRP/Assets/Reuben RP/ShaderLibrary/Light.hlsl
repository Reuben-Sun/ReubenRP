#ifndef REUBEN_LIGHT_INCLUDED
#define REUBEN_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

#include "Shadows.hlsl"

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

CBUFFER_START(_ReubenLight)
    // float3 _DirectionalLightColor;
    // float3 _DirectionalLightDirection;
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

int GetDirectionalLightCount()      //返回方向光数量
{
    return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int index, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[index].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[index].y + shadowData.cascadeIndex;    //光源索引+级联索引
    data.normalBias = _DirectionalLightShadowData[index].z;
    return data;
}

Light GetDirectionalLight(int index, Surface surface, ShadowData shadowData)    //返回指定索引的方向光数据
{
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surface);
    return light;
}



#endif