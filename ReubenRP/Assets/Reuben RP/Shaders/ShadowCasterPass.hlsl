#ifndef REUBEN_SHADOW_CASTER_PASS_INCLUDED
#define REUBEN_SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "LitInput.hlsl"

struct Attributes
{
    float4 vertex: POSITION;    //模型空间顶点坐标
    float3 normal: NORMAL;      //模型空间法向量
    float2 uv: TEXCOORD0;       //第一套 uv
    UNITY_VERTEX_INPUT_INSTANCE_ID      
};

struct Varyings
{
    float4 pos: SV_POSITION;        //齐次裁剪空间顶点坐标
    float2 uv: TEXCOORD0;           //纹理坐标
    float3 normalWS: TEXCOORD1;     //世界空间法线
    float3 posWS: TEXCOORD3;        //世界空间顶点位置
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings ShadowCasterPassVertex(Attributes v)
{
    
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(v);     //用于获取顶点数据在渲染对象中的索引，并存入全局变量中，与 GPU多例化有关
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.pos = TransformObjectToHClip(v.vertex.xyz);

    //unity使用了阴影平坠，在渲染阴影时近裁剪平面会尽可能向前，可以提高阴影贴图的精度，但这种做法可能会导致阴影出现镂空，可以通过调整近裁剪平面的偏移值来避免
    #if UNITY_REVERSED_Z
        o.pos.z = min(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
    #else
        o.pos.z = max(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE);
    #endif   
    
    o.uv = TransformBaseUV(v.uv);
    
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    return o;
}

half4 ShadowCasterPassFragment(Varyings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);

    ClipLOD(i.pos.xy, unity_LODFade.x);
    
    Surface surface;
    surface.posWS = i.posWS;
    surface.normal = normalize(i.normalWS);
    
    float4 color = GetBaseColor(i.uv);
    
    #if defined(_SHADOWS_CLIP)
        clip(color.a - GetCutoff(i.uv));     //透明度测试
    #elif defined(_SHADOWS_DITHER)
        float dither = InterleavedGradientNoise(i.pos.xy, 0);
        clip(color.a - dither);
    #endif


    return 0;
}




#endif
