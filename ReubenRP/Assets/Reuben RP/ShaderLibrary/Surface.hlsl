#ifndef REUBEN_SURFACE_INCLUDED
#define REUBEN_SURFACE_INCLUDED

struct Surface
{
    float3 normal;
    float3 albedo;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDir;
    float3 posWS;
    float depth;    //表面深度（基于视图空间）
    float dither;   //抖动，用于级联过渡抖动
};



#endif