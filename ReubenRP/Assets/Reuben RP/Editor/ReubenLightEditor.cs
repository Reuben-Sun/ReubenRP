using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(ReubenRenderPipelineAsset))]
public class ReubenLightEditor : LightEditor
{

    public override void OnInspectorGUI()       //这东西是用于替换Inspector的
    {
        base.OnInspectorGUI();
        if (!settings.lightType.hasMultipleDifferentValues &&
            (LightType) settings.lightType.enumValueIndex == LightType.Spot)    //只修改聚光灯的面板
        {
            settings.DrawInnerAndOuterSpotAngle();      //绘制调节内外聚光角度滑块
            settings.ApplyModifiedProperties();         //应用修改
        }
    }
}
