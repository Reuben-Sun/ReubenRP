#ifndef REUBEN_GI_INCLUDED
#define REUBEN_GI_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

TEXTURE2D(unity_Lightmap);      SAMPLER(samplerunity_Lightmap);
// TEXTURE3D_FLOAT(unity_ProVolumeSH);     SAMPLER(samplerunity_ProbeVolumeSH);
TEXTURECUBE(unity_SpecCube0);   SAMPLER(samplerunity_SpecCube0);    //间接光高光 Cube

#if defined(LIGHTMAP_ON)
    #define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
    #define GI_VARYINFS_DATA float2 lightMapUV : TEXCOORD4;
    #define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA 
    #define GI_VARYINFS_DATA 
    #define TRANSFER_GI_DATA(input, output) 
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

float3 SampleLightMap(float2 lightMapUV)
{
    #ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
    #else
    bool encodedLightmap = true;
    #endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);
    
    half4 transformCoords = half4(1, 1, 0, 0);
    
    #if defined(LIGHTMAP_ON)
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightMapUV, transformCoords, encodedLightmap, decodeInstructions);
    #else
    return half4(0.0, 0.0, 0.0, 0.0);
    #endif
}

float3 SampleLightProbe(Surface surface)
{
    #if defined(LIGHTMAP_ON)
        return 0;
    #else
        // if(unity_ProbeVolumeParams.x)
        // {
        //     return SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProVolumeSH, samplerunity_ProbeVolumeSH), surface.posWS, surface.normal,
        //         unity_ProbeVolumeWorldToObject, unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz,
        //         unity_ProbeVolumeSizeInv.xyz);
        // }
        // else
        // {
            float4 coefficients[7];
            coefficients[0] = unity_SHAr;
            coefficients[1] = unity_SHAg;
            coefficients[2] = unity_SHAb;
            coefficients[3] = unity_SHBr;
            coefficients[4] = unity_SHBg;
            coefficients[5] = unity_SHBb;
            coefficients[6] = unity_SHC;
            return max(0, SampleSH9(coefficients, surface.normal));
        // }
        
    #endif
   
}

float3 SampleEnvironment(Surface surface, BRDF brdf)   //采样 cube，用于间接光高光
{
    float3 uvw = reflect(-surface.viewDir, surface.normal);     //反射方向
    float mip = PerceptualRoughnessToMipmapLevel(brdf.perceptualRoughness);     //mipmap等级
    float4 environment = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, uvw, mip);     
    return DecodeHDREnvironment(environment, unity_SpecCube0_HDR);      //HDR解码
}

struct GI
{
    float3 diffuse;     //间接光漫反射颜色
    float3 specular;    //间接光高光颜色
};

GI GetGI(float2 lightMapUV, Surface surface, BRDF brdf)
{
    GI gi;
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surface);
    gi.specular = SampleEnvironment(surface, brdf);
    return gi;
}



#endif