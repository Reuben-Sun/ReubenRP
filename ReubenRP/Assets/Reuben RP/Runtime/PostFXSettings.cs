using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rendering/Reuben Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [Tooltip("后效使用的shader")]
    [SerializeField] private Shader _shader = default;
    
    [Tooltip("Bloom")]
    [SerializeField] private BloomSettings bloom = default;
    public BloomSettings Bloom => bloom;

    [System.NonSerialized] private Material _material;

    public Material material
    {
        get
        {
            if (_material == null && _shader != null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            return _material;
        }
    }
    [System.Serializable]
    public struct BloomSettings
    {
        [Tooltip("Bloom最大模糊迭代次数")]
        [Range(0f, 16f)] public int maxIterations;
        [Tooltip("纹理尺寸下限")]
        [Min(1f)] public int downscaleLimit;
        [Tooltip("是否使用双三次滤波（更好但更贵的Bloom）")]
        public bool bicubicUpsampling;
        [Tooltip("阈值")]
        [Min(0f)] public float threshold;
        [Tooltip("阈值拐点")]
        [Range(0f, 1f)] public float thresholdKnee;
        [Tooltip("Bloom强度")]
        [Min(0f)] public float intensity;
    }


}
