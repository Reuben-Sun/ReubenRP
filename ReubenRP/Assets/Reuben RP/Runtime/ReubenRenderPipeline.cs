using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ReubenRenderPipeline : RenderPipeline
{
    private CameraRenderer renderer = new CameraRenderer();
    private ShadowSettings _shadowSettings;

    private bool useDynamicBatching, useGPUInstancing;      //批处理启用情况
    //unity 每一帧都会调用这个方法进行渲染
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //遍历所有相机做独立渲染
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, _shadowSettings);
        }
    }

    public ReubenRenderPipeline(bool _useDynamicBatching, bool _useGPUInstancing, bool _useSRPBatcher, ShadowSettings shadowSettings)
    {
        _shadowSettings = shadowSettings;       //阴影设置
        //设置合批启用状态
        useDynamicBatching = _useDynamicBatching;   //动态合批，物体可以移动，但只适用于小型网格
        useGPUInstancing = _useGPUInstancing;       //GPU多例合批
        GraphicsSettings.useScriptableRenderPipelineBatching = _useSRPBatcher;    //SRP合批处理
        GraphicsSettings.lightsUseLinearIntensity = true;       //灯光使用 Linear
        

    }
}
