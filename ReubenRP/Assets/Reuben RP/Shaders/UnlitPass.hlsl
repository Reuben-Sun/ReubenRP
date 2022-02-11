#ifndef REUBEN_UNLIT_PASS_INCLUDED
#define REUBEN_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

struct Attributes
{
    float4 vertex: POSITION;    //模型空间顶点坐标
    float2 uv: TEXCOORD0;       //第一套 uv
    UNITY_VERTEX_INPUT_INSTANCE_ID      
};

struct Varyings
{
    float4 pos: SV_POSITION;        //齐次裁剪空间顶点坐标
    float2 uv: TEXCOORD0;           //纹理坐标
    float3 posWS: TEXCOORD3;    //世界空间顶点位置
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// CBUFFER_START(UnityPerMaterial)
//     float4 _MainTex_ST;
//     float4 _BaseColor;
// CBUFFER_END

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);

Varyings UnlitPassVertex(Attributes v)
{
    
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(v);     //用于获取顶点数据在渲染对象中的索引，并存入全局变量中，与 GPU多例化有关
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.pos = TransformObjectToHClip(v.vertex.xyz);
    float4 mainST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    o.uv = v.uv * mainST.xy + mainST.zw;
    return o;
}

half4 UnlitPassFragment(Varyings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * baseColor;
    
    #if defined(_CLIPPING)
        clip(color.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));     //透明度测试
    #endif
    
    return color;
}















#endif
