using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    //这个文件用于向 CPU传递光源信息
    private const string bufferName = "Lighting";
    private CullingResults _cullingResults;      //存储剔除后的结果
    private const int maxDirLightCount = 4;      //最大可见平行光数量
    private const int maxOtherLightCount = 64;   //最大其他光源数量（点光、spot光）

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    //存储方向光ID
    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    
    //存储方向光信息
    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    
    //存储其他光ID
    private static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    private static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    private static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    private static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");   //其他光方向，主要是给聚光灯用的
    private static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");   //聚光灯角度
    
    //存储其他光信息
    private static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
    private static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];

    private Shadows _shadows = new Shadows();   //阴影

    private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    private static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];        //存储阴影数据
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        _cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        _shadows.Setup(context, cullingResults, shadowSettings);        //传递阴影信息
        SetupVisibleLights();
        _shadows.Render();      //阴影绘制
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)    //将可见光属性存入数组
    {
        // Light light = RenderSettings.sun;
        // buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        // buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] =_shadows.ReserveDirectionalShadows(visibleLight.light, index);      //存储阴影数据
         
    }

    void SetupPointLight(int index, ref VisibleLight visibleLight)
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);        //位置信息
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);     //光照范围的平方的倒数
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);  //为了确保点光不受聚光灯角度衰减影响
    }
    
    void SetupSpotLight(int index, ref VisibleLight visibleLight)       //聚光灯
    {
        otherLightColors[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);        //位置信息
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);     //光照范围的平方的倒数
        otherLightPositions[index] = position;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);    //内角
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);  //外角
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
    }

    void SetupVisibleLights()       //返回所有可见光信息
    {
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

        int dirLightCount = 0;
        int otherLightCount = 0;
        
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupPointLight(otherLightCount++, ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                    {
                        SetupSpotLight(otherLightCount++, ref visibleLight);
                    }
                    break;
                default:
                    break;
            }
           
        }
        
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData); 
        }
        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
        }
    }

    public void Cleanup()       //内存释放
    {
        _shadows.Cleanup();     //释放阴影贴图
    }
}
