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
    private const int maxDirLightCount = 4;     //最大可见平行光数量

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    // private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    // private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    //存储光源信息
    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

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

    void SetupVisibleLights()       //返回所有可见光信息
    {
        NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;

        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)    //如果是方向光，那么存储信息
            {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);
                if (dirLightCount >= maxDirLightCount)      //如果灯光数量大于最大灯光数量
                {
                    break;
                }
            }
        }
        
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }

    public void Cleanup()       //内存释放
    {
        _shadows.Cleanup();     //释放阴影贴图
    }
}
