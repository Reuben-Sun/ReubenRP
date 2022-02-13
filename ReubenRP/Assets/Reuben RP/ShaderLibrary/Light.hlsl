#ifndef REUBEN_LIGHT_INCLUDED
#define REUBEN_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

#include "Shadows.hlsl"
#include "Common.hlsl"

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};

CBUFFER_START(_ReubenLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT]; 
CBUFFER_END

int GetDirectionalLightCount()      //返回方向光数量
{
    return _DirectionalLightCount;
}

int GetOtherLightCount()            //返回其他光的数量
{
    return _OtherLightCount;
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

Light GetOtherLight(int index, Surface surface, ShadowData shadowData)
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 ray = _OtherLightPositions[index].xyz - surface.posWS;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.0001);     //距离的平方
    float rangeAttenuation = pow(saturate(1.0 - pow(distanceSqr * _OtherLightPositions[index].w, 2)), 2);
    float4 spotAngles = _OtherLightSpotAngles[index];
    float spotAttenuation = pow(saturate(dot(_OtherLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y), 2);   //聚光灯衰弱
    
    light.attenuation = spotAttenuation * rangeAttenuation / distanceSqr;      //加入平方反比衰减
    
    return light;
}

#endif