#include <HLSLSupport.cginc>
#ifndef REUBEN_INPUT_INCLUDED
#define REUBEN_INPUT_INCLUDED

//转置矩阵
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;      //模型空间到世界空间
float4x4 unity_WorldToObject;      //世界空间到模型空间
float4 unity_LODFade;              //LOD渐出
half4 unity_WorldTransformParams;
float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

//光照探针数据
float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;

float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;

float4 unity_SHC;

// //光照探针代理体 LPPV
// float4 unity_ProbeVolumeParams;
// float4x4 unity_ProbeVolumeWorldToObject;
// float4 unity_ProbeVolumeSizeInv;
// float4 unity_ProbeVolumeMin;

float4 unity_SpecCube0_HDR;

float4 unity_LightData;
float4 unity_LightIndices[2];
CBUFFER_END

float4x4 unity_MatrixVP;           //世界空间到裁剪空间
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;

float3 _WorldSpaceCameraPos;        //世界空间相机位置


#endif
