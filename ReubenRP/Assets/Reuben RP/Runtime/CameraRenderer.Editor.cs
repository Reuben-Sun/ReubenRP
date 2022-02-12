using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    partial void DrawUnsupportedShaders();
    
#if UNITY_EDITOR    //只有在编辑模式下才会绘制不支持的材质
    private static Material errorMaterial;      //错误的材质
    
    private static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBass"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };      //不支持的着色器标志ID，这些物体都会被绘制成品红色
    

    partial void DrawUnsupportedShaders()       //绘制不支持的着色器类型
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
#endif

    // partial void DrawGizmos();
    partial void DrawGizmosBeforeFX();  //后效前的辅助线
    partial void DrawGizmosAfterFX();   //后效后的辅助线
#if UNITY_EDITOR
    // partial void DrawGizmos()       //绘制辅助线框
    // {
    //     if (Handles.ShouldRenderGizmos())
    //     {
    //         context.DrawGizmos(camera, GizmoSubset.PreImageEffects);    //后处理前绘制辅助线
    //         context.DrawGizmos(camera, GizmoSubset.PostImageEffects);   //后处理后绘制辅助线
    //     }
    // }
    partial void DrawGizmosBeforeFX()       //绘制辅助线框
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);    //后处理前绘制辅助线
        }
    }
    partial void DrawGizmosAfterFX()       //绘制辅助线框
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);   //后处理后绘制辅助线
        }
    }
#endif

    partial void PrepareForSceneWindow();
#if UNITY_EDITOR
    partial void PrepareForSceneWindow()    //在 Scene绘制 UI
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

#endif
}
