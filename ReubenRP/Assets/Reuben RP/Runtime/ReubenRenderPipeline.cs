using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

public partial class ReubenRenderPipeline : RenderPipeline
{
    private CameraRenderer renderer = new CameraRenderer();
    private ShadowSettings _shadowSettings;

    private bool _useDynamicBatching, _useGPUInstancing;      //批处理启用情况
    private bool _useLightsPerObject;        //是否使用逐对象光照

    private PostFXSettings _postFXSettings;     //后处理
    //unity 每一帧都会调用这个方法进行渲染
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        //遍历所有相机做独立渲染
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing, _useLightsPerObject,_shadowSettings, _postFXSettings);
        }
    }

    public ReubenRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSettings)
    {
        _shadowSettings = shadowSettings;       //阴影设置
        _postFXSettings = postFXSettings;       //后效设置
        //设置合批启用状态
        _useDynamicBatching = useDynamicBatching;   //动态合批，物体可以移动，但只适用于小型网格
        _useGPUInstancing = useGPUInstancing;       //GPU多例合批
        _useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;    //SRP合批处理
        GraphicsSettings.lightsUseLinearIntensity = true;       //灯光使用 Linear

        InitializeForEditor();
    }

    protected override void Dispose(bool disposing)     //当管线被清理掉时
    {
        base.Dispose(disposing);
        Lightmapping.ResetDelegate();       //重置委托
    }
}
