#ifndef REUBEN_BRDF_INCLUDED
#define REUBEN_BRDF_INCLUDED

#include "Surface.hlsl"
#include "Common.hlsl"
//电解质的反射率平均设为 0.04，这其实是非金属 F0的典型值 
#define MIN_REFLECTIVITY 0.04


struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
    float perceptualRoughness;      //感知粗糙度
    float fresnel;      //菲涅尔
};

float OneMinusReflectivity(float metallic)      //这东西其实是Kd，感觉回头可以换成(1-F_Schlick)*(1-metallic)
{
    float range = 1-MIN_REFLECTIVITY;
    return range * (1-metallic);
}


BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)       //获得BRDF数据
{
    BRDF brdf = (BRDF)0;
    
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    
    brdf.diffuse = surface.albedo * oneMinusReflectivity;

    if(applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.albedo, surface.metallic);   //F0

    brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);   //光滑度——感知粗糙度
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);     //感知光滑度——实际粗糙度
    // brdf.roughness = (1-surface.smoothness) * (1-surface.smoothness);           //感知光滑度——实际粗糙度
    // brdf.roughness = 1-surface.smoothness;           //感知光滑度——实际粗糙度
    brdf.fresnel = saturate(surface.smoothness + 1 - oneMinusReflectivity);     //Schlick近似菲涅尔
    return brdf;
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)    //直接光 BRDF计算
{
    float3 L = normalize(light.direction);
    float3 H = SafeNormalize(L + surface.viewDir);    //半向量

    float VoH = max(0.001, saturate(dot(surface.viewDir, H)));
    float NoV = max(0.001, saturate(dot(surface.normal, surface.viewDir)));
    float NoL = max(0.001, saturate(dot(surface.normal, L)));
    float NoH = saturate(dot(surface.normal, H));
    
    //菲涅尔项 Schlick Fresnel
    float3 F_Schlick = brdf.specular + (1 - brdf.specular) * pow(1 - VoH, 5.0);     

    //法线分布项 D NDF GGX
    float a = brdf.roughness * brdf.roughness;
    float a2 = a * a;
    float d = (NoH * a2 - NoH) * NoH + 1;
    float D_GGX = a2 / (PI * d * d);
    
    //几何项 G
    float k = (brdf.roughness + 1) * (brdf.roughness + 1) / 8;
    float GV = NoV / (NoV * (1-k) + k);
    float GL = NoL / (NoL * (1-k) + k);
    float G_GGX = GV * GL;

    float3 specluarStrength = F_Schlick * D_GGX * G_GGX / (4 * NoV * NoL);
    
    return brdf.diffuse + specluarStrength;
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)    //间接光 BRDF
{
    //参数中的 diffuse、specular是从间接光中获得的
    float fresnelStrength = surface.fresnelStrength * Pow4(1 - saturate(dot(surface.normal, surface.viewDir)));
    float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1;
    return diffuse * brdf.diffuse + reflection;
}




#endif