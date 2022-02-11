using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Reuben Render Pipeline")]
public class ReubenRenderPipelineAsset : RenderPipelineAsset
{
    [Tooltip("启用动态合批")]
    [SerializeField] private bool useDynamicBatching = true;
    [Tooltip("启用GPU多例合批")]
    [SerializeField] private bool useGPUInstancing = true;
    [Tooltip("启用SRP合批")]
    [SerializeField] private bool useSRPBatcher = true;
    [Tooltip("阴影设置")]
    [SerializeField] private ShadowSettings shadows = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new ReubenRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
    }
   
    
}
