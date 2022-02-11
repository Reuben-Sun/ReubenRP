using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadow";
    private const int maxShadowedDirectionalLightCount = 4;     //可投射阴影的方向光数量
    private const int maxCascades = 4;      //最大级联数量
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");   //阴影图集的着色器标识

    private static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");     //阴影转换矩阵的着色器标识
    private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];      //转换矩阵数组

    private static int cascadeCountId = Shader.PropertyToID("_CascadeCount");   //级联数量 ID
    private static int cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres");  //级联包围球 ID
    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];       //级联包围球数组

    // private static int shadowDistanceId = Shader.PropertyToID("_ShadowDistance");   //阴影最大距离 ID
    private static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");       //阴影过渡距离 ID

    private static int cascadeDataId = Shader.PropertyToID("_CascadeData");       //级联数据 ID
    private static Vector4[] cascadeData = new Vector4[maxCascades];

    private static int shadowAtlasSize = Shader.PropertyToID("_ShadowAtlasSize");   //阴影图集大小

    private static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };      //PCF滤波模式

    private static string[] cascadeBlendKeywords = {"_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER"};    //级联混合模式
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;   //可见光的索引
        public float slopScaleBias;       //斜度比例偏差（用于法线偏移）
        public float nearPlaneOffset;     //近裁剪平面偏移
    }

    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];     //可投射阴影的方向光 索引

    private int ShadowedDirectionalLightCount;  //当前可投射阴影的 可见的 方向光 数量
    
    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private ShadowSettings _shadowSettings;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        _context = context;
        _cullingResults = cullingResults;
        _shadowSettings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExcuteBuffer()
    {
        _context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)    //存储可见光的阴影数据
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None &&
            light.shadowStrength > 0 &&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            return new Vector3(light.shadowStrength, _shadowSettings.directional.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias);      //返回阴影强度、阴影图块偏移、灯光法线偏差
        }

        return Vector3.zero;
    }

    public void Render()    //创建阴影图集
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }

    void RenderDirectionalShadows()     //渲染阴影
    {
        int atlasSize = (int) _shadowSettings.directional.atlasSize;
        //生成一张 RT，变量意义分别是：RT ID、长、宽、深度缓冲位数、过滤模式（双线性过滤）、纹理类型（shadowmap）
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        //将渲染数据存储到 RT中，而不是帧缓冲中，于是 Store，我们不关心其初始状态，于是 DontCare
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);     //清空深度缓冲

        buffer.BeginSample(bufferName);
        ExcuteBuffer();

        //多个光源要分割图块，多个级联也要分割图块
        int tiles = ShadowedDirectionalLightCount * _shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;     //分割图块数量
        int tileSize = atlasSize / split;                           //分割图块大小
        
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)     //遍历所有方向光渲染阴影
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        
        buffer.SetGlobalInt(cascadeCountId, _shadowSettings.directional.cascadeCount);      //将级联数量发送到 GPU
        buffer.SetGlobalVectorArray(cascadeCullingSphereId, cascadeCullingSpheres);              //将级联包围球数据发送到 GPU
        
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);    //将级联数据发送到 GPU
        
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);    //将阴影转换矩阵发送到 GPU
        
        // buffer.SetGlobalFloat(shadowDistanceId, _shadowSettings.maxDistance);   //将阴影最大距离发送到 GPU
        float f = 1 - _shadowSettings.directional.cascadeFade;          //级联过渡
        buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / _shadowSettings.maxDistance, 1f / _shadowSettings.distanceFade, 1f / (1f - f * f)));

        SetKeywords(directionalFilterKeywords, (int)_shadowSettings.directional.filter -1);      //设置 PCF关键词
        SetKeywords(cascadeBlendKeywords, (int)_shadowSettings.directional.cascadeBlend -1);     //设置级联混合关键词
        buffer.SetGlobalVector(shadowAtlasSize, new Vector4(atlasSize, 1f / atlasSize));     //将阴影图集大小发送到 GPU
        buffer.EndSample(bufferName);
        
        ExcuteBuffer();
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)      //渲染单个光源阴影
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex);

        int cascadeCount = _shadowSettings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _shadowSettings.directional.CascadeRatios;

        float cullingFactor = Mathf.Max(0f, 0.8f - _shadowSettings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            //计算视图矩阵、投影矩阵、裁剪空间的立方体
            //1可见光索引、234阴影级联数据、5阴影贴图尺寸、6阴影近平面偏移、7视图矩阵、8投影矩阵、9剔除信息
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount,
                ratios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            if (index == 0)     //获得第一个光源的包围球数据（所有的光源使用相同的级联，所以只需拿到第一个方向的包围球数据）
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
                // Vector4 cullingSphere = splitData.cullingSphere;
                // cullingSphere.w *= cullingSphere.w;     //将半径平方
                // cascadeCullingSpheres[i] = cullingSphere;
            }

            splitData.shadowCascadeBlendCullingFactor = cullingFactor;     //剔除偏差
            shadowSettings.splitData = splitData;

            int tileIndex = tileOffset + i;     //图块索引 = 光源图块偏移 + 级联索引
            
        
            // SetTileViewport(index, split, tileSize);    //设置渲染视口

            // dirShadowMatrices[index] = projectionMatrix * viewMatrix;   // P矩阵*V矩阵 = 世界空间到光源空间
            dirShadowMatrices[tileIndex] =
                ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);     //设置视口投影矩阵
            
            buffer.SetGlobalDepthBias(0, light.slopScaleBias);      //设置深度偏差
            
            ExcuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            
            buffer.SetGlobalDepthBias(0f, 0f);          //还原深度偏差
        }
        
    }

    public void Cleanup()       //释放 RT
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExcuteBuffer();
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)      //调整渲染视口
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)      //输入拆分，输出合并
    {
        if (SystemInfo.usesReversedZBuffer)     //判断是否使用了反转 Z-Buffer（ DirectX启用深度反转，可以表达更高的精度）
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)   //设置级联数据
    {
        float texelSize = 2f * cullingSphere.w / tileSize;  //纹素大小 = 包围球直径 / 阴影图块大小

        float filterSize = texelSize * ((float) _shadowSettings.directional.filter + 1f);
        cullingSphere.w -= filterSize;      //包围盒半径 - 纹素偏移大小
        
        cullingSphere.w *= cullingSphere.w;     //将半径平方
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);     //1.4142136是根号2
    }

    void SetKeywords(string[] keywords, int enalbedIndex)      //设置关键词
    {
        // int enalbedIndex = (int) _shadowSettings.directional.filter - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enalbedIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
}
