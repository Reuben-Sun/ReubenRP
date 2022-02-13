Shader "Reuben RP/Lit"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _MainTex("BaseMap", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Occlusion("Occlusion", Range(0,1)) = 1
        _Fresnel("Fresnel", Range(0,1)) = 1
        [NoScaleOffset] _MaskMap("Mask MODS", 2D) = "white"{}
        
        [Toggle(_NORMAL_MAP)] _NormalMapToggle("Normal Map", Float) = 0
        [NoScaleOffset] _NormalMap("NormalMap", 2D) = "bump"{}
        _NormalScale("Normal Scale", Range(0,1)) = 1
        
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "wgite" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,0)
        
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("源混合因子", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("目标混合因子", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        [Toggle(_CLIPPING)] _Clipping("开启透明度测试", Float) = 0
        _Cutoff("透明度测试值", Range(0.0, 1.0)) = 0.5
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("开启透明度预乘", Float) = 0
        
        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("阴影投影模式（透明物体）", Float) = 0
        [Toggle(_RECELIVE_SHADOWS)] _ReceiveShadows("接受投影", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags{"LightMode" = "ReubenLit"}
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma target 3.5      //级别越高，功能越新，默认是2.5
            #pragma multi_compile_instancing    //允许 GPU Instancing（多例化）
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7   //四个滤波模式，其中 _ 代表这默认的 _DIRECTIONAL_PCF2
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER       //级联混合模式
            #pragma multi_compile _ LIGHTMAP_ON     //开启光照贴图
            #pragma multi_compile _ LOD_FADE_CROSSFADE      //开启 LOD混合

            #pragma shader_feature _CLIPPING            //是否开启透明度测试
            #pragma shader_feature _PREMULTIPLY_ALPHA   //是否启用透明度预乘
            #pragma shader_feature _RECELIVE_SHADOWS    //是否接受投影
            #pragma shader_feature _NORMAL_MAP          //是否开启法线贴图
            
            #pragma vertex LitForawrdPassVertex
            #pragma fragment LitForwardPassFragment
            #include "LitForwardPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{"LightMode" = "ShadowCaster"}
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 3.5      
            #pragma multi_compile_instancing

            // #pragma shader_feature _CLIPPING            //是否开启透明度测试
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER      //投影模式
            #pragma multi_compile _ LOD_FADE_CROSSFADE      //开启 LOD混合

            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Tags{"LightMode" = "Meta"}
            // Meta用于烘焙 light probe间接光
            Cull Off
            
            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
            
    }
    CustomEditor "ReubenShaderGUI"
}
