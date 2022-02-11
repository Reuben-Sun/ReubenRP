#ifndef REUBEN_LIT_FORWARD_PASS_INCLUDED
#define REUBEN_LIT_FORWARD_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/GlobalIllumination.hlsl"
#include "LitInput.hlsl"


struct Attributes
{
    float4 vertex: POSITION;    //模型空间顶点坐标
    float3 normal: NORMAL;      //模型空间法向量
    float2 uv: TEXCOORD0;       //第一套 uv
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID      
};

struct Varyings
{
    float4 pos: SV_POSITION;        //齐次裁剪空间顶点坐标
    float2 uv: TEXCOORD0;           //纹理坐标
    float3 normalWS: TEXCOORD1;     //世界空间法线
    float3 viewDirWS: TEXCOORD2;    //世界空间视线方向
    float3 posWS: TEXCOORD3;        //世界空间顶点位置
    GI_VARYINFS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitForawrdPassVertex(Attributes v)
{
    
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(v);     //用于获取顶点数据在渲染对象中的索引，并存入全局变量中，与 GPU多例化有关
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    TRANSFER_GI_DATA(v, o);
    
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.pos = TransformObjectToHClip(v.vertex.xyz);
    o.uv = TransformBaseUV(v.uv);
    
    o.normalWS = TransformObjectToWorldNormal(v.normal);
    
    return o;
}

half4 LitForwardPassFragment(Varyings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);

    ClipLOD(i.pos.xy, unity_LODFade.x);
    
    float4 color = GetBaseColor(i.uv);

    #if defined(_CLIPPING)
    clip(color.a - GetCutoff(i.uv));     //透明度测试
    #endif
    
    Surface surface = (Surface)0;
    surface.normal = normalize(i.normalWS);
    surface.albedo = color.rgb;
    surface.alpha = color.a;
    surface.metallic = GetMetallic(i.uv);
    surface.smoothness = GetSmoothness(i.uv);
    surface.viewDir = normalize(_WorldSpaceCameraPos - i.posWS);
    surface.posWS = i.posWS;
    surface.depth = -TransformWorldToView(i.posWS).z;
    surface.dither = InterleavedGradientNoise(i.pos.xy, 0);     //传入屏幕坐标，生成一个抖动值

    #if defined(_PREMULTIPLY_ALPHA)     //是否开启透明度预乘
        BRDF brdf = GetBRDF(surface, true);
    #else
        BRDF brdf = GetBRDF(surface);
    #endif

    GI gi = GetGI(GI_FRAGMENT_DATA(i), surface);
    
    float3 finalColor = GetLighting(surface, brdf, gi);
    finalColor += GetEmission(i.uv);

    return float4(finalColor, surface.alpha);
}




#endif
