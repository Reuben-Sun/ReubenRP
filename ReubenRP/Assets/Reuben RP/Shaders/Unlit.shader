Shader "Reuben RP/Unlit"
{
    Properties
    {
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _MainTex ("BaseMap", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("源混合因子", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("目标混合因子", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            //设置混合模式, color = 源Color * _SrcBlend + 缓冲Color * (1 - _DstBlend)
            Blend[_SrcBlend][_DstBlend]
            //是否开启深度写入
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma multi_compile_instancing    //允许 GPU Instancing（多例化）
            
            #pragma shader_feature _CLIPPING
            
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "ReubenShaderGUI"
}
