#ifndef REUBEN_META_PASS_INCLUDED
#define REUBEN_META_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "LitInput.hlsl"

#define FIT_MIN 0.000001f
bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes
{
    float4 vertex: POSITION;    //模型空间顶点坐标
    float2 uv: TEXCOORD0;       //第一套 uv
    float2 lightMapUV: TEXCOORD1;
};

struct Varyings
{
    float4 pos: SV_POSITION;        //齐次裁剪空间顶点坐标
    float2 uv: TEXCOORD0;           //纹理坐标
};


Varyings MetaPassVertex(Attributes v)
{
    
    Varyings o = (Varyings)0;
    v.vertex.xy = v.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    v.vertex.z = v.vertex.z > 0.0 ? FIT_MIN : 0.0;

    o.pos = TransformWorldToHClip(v.vertex);
    o.uv = TransformBaseUV(v.uv);
    return o;
}

half4 MetaPassFragment(Varyings i) : SV_Target
{
    float4 color = GetBaseColor(i.uv);
    
    Surface surface = (Surface)0;
    ZERO_INITIALIZE(Surface, surface);
    surface.albedo = color.rgb;
    surface.metallic = GetMetallic(i.uv);
    surface.smoothness = GetSmoothness(i.uv);
    BRDF brdf = GetBRDF(surface);
    
    float4 meta = 0;
    if(unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse, 1);         //漫反射
        meta.rgb += brdf.specular * brdf.roughness * 0.5;       //漫反射 + 高光 * 粗糙度 * 0.5，高镜面但粗糙的材质也可以传递间接光照
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    else if(unity_MetaFragmentControl.y)
    {
        meta = float4(GetEmission(i.uv), 1);
    }
    
    return meta;
}




#endif
