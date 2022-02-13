#ifndef REUBEN_LIGHTING_INCLUDED
#define REUBEN_LIGHTING_INCLUDED

#include "Surface.hlsl"
#include "GlobalIllumination.hlsl"

float3 LambertLighting(Surface surface, Light light)    //兰伯特
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;    //话说这里除不除PI呢？
    // return float3(1,1,1) * light.attenuation;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    float3 directColor = LambertLighting(surface, light) * DirectBRDF(surface, brdf, light);  
    return directColor;
}

float3 GetLighting(Surface surface, BRDF brdf, GI gi)
{
    ShadowData shadowData = GetShadowData(surface);     //获得物体表面阴影数据
    
    // float3 color = gi.diffuse * brdf.diffuse;      //未接受光源前，物体的颜色来自之前烘焙的间接光
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for(int i = 0; i < GetDirectionalLightCount(); i++)     //遍历方向光
    {
        Light light = GetDirectionalLight(i, surface, shadowData);
        color += GetLighting(surface, brdf, light);
    }

    #if defined(_LIGHTS_PER_OBJECT)
        for(int j = 0; j < min(unity_LightData.y, 8); j++)       //遍历其他光
        {
            int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
            Light light = GetOtherLight(lightIndex, surface, shadowData);
            color += GetLighting(surface, brdf, light);
        }
    #else
        for(int j = 0; j < GetOtherLightCount(); j++)       //遍历其他光
        {
            Light light = GetOtherLight(j, surface, shadowData);
            color += GetLighting(surface, brdf, light);
        }
    #endif
    return color;
}
#endif