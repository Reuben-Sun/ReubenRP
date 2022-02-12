using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private ScriptableRenderContext context;
    private Camera camera;
    private const string bufferName = "Reuben Render Camera";
    private CullingResults cullingResults;      //剔除后的结果数据
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");   //支持的着色器标志ID, 这个是Unlit
    private static ShaderTagId litShaderTagId = new ShaderTagId("ReubenLit");           //支持的着色器标志ID, 这个是Lit

    private Lighting lighting = new Lighting(); //光源信息


    
    //创建一个渲染缓冲区
    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    
    public void Render(ScriptableRenderContext _context, Camera _camera, bool _useDynamicBatching, bool _useGPUInstancing, ShadowSettings shadowSettings)
    {
        context = _context;
        camera = _camera;
        
        PrepareBuffer();    //设置命令缓冲区的名字
        PrepareForSceneWindow();

        if (!HadCull(shadowSettings.maxDistance))     //剔除检测
        {
            return;
        }
        
        buffer.BeginSample("_DirectionalShadowAtlas");      //开始阴影采样
        ExecuteBuffer();
        
        lighting.Setup(context, cullingResults, shadowSettings);    //传入光照信息
        
        buffer.EndSample("_DirectionalShadowAtlas");        //结束阴影采样
        
        Setup();    //初始化
        
        DrawVisibleGeometry(_useDynamicBatching, _useGPUInstancing);      //绘制可见物

        DrawUnsupportedShaders();   //绘制不支持的物体
        
        DrawGizmos();   //绘制辅助线
        
        lighting.Cleanup();     //释放贴图内存
        
        Submit();   //执行
    }

    void Setup()        //渲染初始化
    {
        context.SetupCameraProperties(camera);      //设置相机的属性与矩阵
        CameraClearFlags flags = camera.clearFlags;     //获取相机 clear flags 
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
            );      //清空渲染目标，三个变量依次是：是否清空深度信息、是否清空颜色信息、使用何种颜色进行清空
        buffer.BeginSample(bufferName);     //开始采样 命令缓冲区 一般放在渲染的开始
        ExecuteBuffer();

    }
    
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)      //绘制可见物
    {
        //绘制顺序：不透明物体——天空盒——透明物体
        var sortingSettings = new SortingSettings(camera)       //设置相机的透明排序模式，这里设置的是不透明对象
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            //批处理使用状态
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes
        };   //设置渲染的 Pass和排序方式

        drawingSettings.SetShaderPassName(1, litShaderTagId);   //渲染 Lit表示的 Pass块
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);    //设置那些类型的渲染队列可以被绘制，这里是不透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);      //绘制不透明物体
        
        context.DrawSkybox(camera);     //绘制天空盒

        sortingSettings.criteria = SortingCriteria.CommonTransparent;   //设置相机的透明排序模式，这里设置的是透明对象
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;      //设置那些类型的渲染队列可以被绘制，这里是透明物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);      //绘制不透明物体
    }

    void Submit()   //提交缓冲徐渲染命令
    {
        buffer.EndSample(bufferName);       //结束采样 命令缓冲区 一般放在渲染的结束
        ExecuteBuffer();
        context.Submit();   
    }

    void ExecuteBuffer()    
    {
        context.ExecuteCommandBuffer(buffer);   //执行缓冲区命令
        buffer.Clear();     //执行后清空 buffer，以便复用 buffer
    }

    bool HadCull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);   //阴影距离不能超过相机的远截面
            cullingResults = context.Cull(ref p);       //剔除
            return true;
        }

        return false;
    }
    
    
    void PrepareBuffer()    //设置命令缓冲区的名字
    {
        buffer.name = "Reuben " + camera.name;
    }
    
}
