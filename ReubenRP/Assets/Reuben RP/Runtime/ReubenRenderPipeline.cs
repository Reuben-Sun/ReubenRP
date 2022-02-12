using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ReubenRenderPipeline : RenderPipeline
{
    private CameraRenderer renderer = new CameraRenderer();
    private ShadowSettings _shadowSettings;

    private bool _useDynamicBatching, _useGPUInstancing;      //批处理启用情况

    private PostFXSettings _postFXSettings;     //后处理
    //unity 每一帧都会调用这个方法进行渲染
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //遍历所有相机做独立渲染
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing, _shadowSettings, _postFXSettings);
        }
    }

    public ReubenRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings, PostFXSettings postFXSettings)
    {
        _shadowSettings = shadowSettings;       //阴影设置
        _postFXSettings = postFXSettings;       //后效设置
        //设置合批启用状态
        _useDynamicBatching = useDynamicBatching;   //动态合批，物体可以移动，但只适用于小型网格
        _useGPUInstancing = useGPUInstancing;       //GPU多例合批
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;    //SRP合批处理
        GraphicsSettings.lightsUseLinearIntensity = true;       //灯光使用 Linear
        

    }
}
