using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ReubenShaderGUI : ShaderGUI
{
    private MaterialEditor _editor;      //显示和编辑材质属性
    private Object[] _materilas;         //正在编辑的材质的引用对象
    private MaterialProperty[] _properties;      //可编辑的属性数组

    private bool showPresets;       //是否折叠预设

    enum ShadowMode
    {
        On, Clip, Dither, Off
    }

    ShadowMode Shadows
    {
        set
        {
            if (SetProperty("_Shadows", (float) value))
            {
                SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }
    

    #region 面板上的属性值

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    
    private bool PremultiplyAlpha
    {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float) value);
    }

    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float) value);
    }

    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1 : 0);
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in _materilas)
            {
                m.renderQueue = (int) value;
            }
        }
    }
    
    #endregion
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();
        
        base.OnGUI(materialEditor, properties);
        _editor = materialEditor;
        _materilas = materialEditor.targets;
        _properties = properties;
        
        BakedEmission();
        
        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "渲染模式预设", true);
        if (showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
        }
    }

    void BakedEmission()    //烘焙自发光
    {
        EditorGUI.BeginChangeCheck();
        _editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in _editor.targets)
            {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
    bool HasProperty(string name) => FindProperty(name, _properties, false) != null;    //判断有无这个材质信息
    
    private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");
    
    bool SetProperty(string name, float value)      //设置属性
    {
        MaterialProperty property = FindProperty(name, _properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;
        }

        return false;
    }

    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1 : 0))
        {
            SetKeyword(keyword, value);
        }

    }
    
    void SetKeyword(string keyword, bool enabled)   //设置关键字状态
    {
        if (enabled)
        {
            foreach (Material m in _materilas)  
            {
                m.EnableKeyword(keyword);
            }
        } 
        else
        {
            foreach (Material m in _materilas)  
            {
                m.DisableKeyword(keyword);
            }
        }
    }

    void SetShadowCasterPass()      //设置阴影投影是否启用
    {
        MaterialProperty shadows = FindProperty("_Shadows", _properties, false);
        if (shadows == null || shadows.hasMixedValue)
        {
            return;
        }

        bool enalbed = shadows.floatValue < (float) ShadowMode.Off;
        foreach (Material m in _materilas)
        {
            m.SetShaderPassEnabled("ShadowCaster", enalbed);
        }
    }

    bool PresetButton(string name)      
    {
        if (GUILayout.Button(name))
        {
            //属性重置
            _editor.RegisterPropertyChangeUndo(name);
            return true;
        }

        return false;
    }

    #region 渲染模式

    void OpaquePreset()     //不透明材质
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void ClipPreset()     //透明度测试
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    void FadePreset()       //标准透明渲染模式
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    void TransparentPreset()    //拥有正确照明的半透明表面
    {
        if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    #endregion
    
}
