using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ShadowSettings
{
    [Min(0f)] public float maxDistance = 100f;      //阴影最大距离

    [Range(0.001f, 1f)] public float distanceFade = 0.1f;       //阴影过渡距离
    public enum TextureSize     //阴影贴图大小
    {
        _256 = 256, _512 = 512, _1024 = 1024,
        _2048 = 2048, _4096 = 4096, _8192 = 8192
    }
    

    
    [System.Serializable]
    public struct Directional       //方向光阴影配置
    {
        [Tooltip("阴影图集大小")]
        public TextureSize atlasSize;

        [Tooltip("PCF滤波模式")]
        public FilterMode filter;  
        
        [Tooltip("级联混合模式")]
        public CascadeBlendMode cascadeBlend;   

        [Tooltip("级联数量")]
        [Range(1, 4)] public int cascadeCount;      
        
        [Tooltip("级联比例")]
        [Range(0f, 1f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;      

        [Tooltip("级联过渡速度")]
        [Range(0.001f, 1f)] public float cascadeFade;    
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);   
    }

    public Directional directional = new Directional        //默认大小为1024
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeBlend = CascadeBlendMode.Hard,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f
    };
    
    //PCF滤波模式：由于阴影存储的是深度而不是颜色，如果对周围做均值，会得到错误的结果，于是引入PCF滤波模式
    //采集周围的深度跟自己比，>自己就表示不在阴影中，取0，<=自己就取1
    //把这些值加起来去平均值，作为自己最后的深度
    
    public enum FilterMode      //PCF滤波模式
    {
        PCF2x2,PCF3x3,PCF5x5,PCF7x7
    }
    
    public enum CascadeBlendMode    //级联混合模式
    {
        Hard, Soft, Dither  
    }

}


