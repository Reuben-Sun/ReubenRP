using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    private const string bufferName = "Post FX";
    private int fxSourceId = Shader.PropertyToID("_PostFXSource");
    private int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
    private int bloomBicubicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
    private int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");  //用于预滤波
    private int bloomThresholdId = Shader.PropertyToID("_BloomThreshold"); //阈值
    private int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");  //Bloom强度
    private const int maxBloomPyramidLevels = 16;       //Bloom纹理金字塔最大数量
    private int bloomPyramidId;
    //一种快速 Bloom的方法是将原图像复制到一张长宽减半的图像中，各复制一份，然后在这四个像素间进行采样，然后使用双线性滤波
    //每向下一次，就模糊一点点，最后我们把这些模糊的图像累计起来，就是 Bloom效果
    
    private CommandBuffer _buffer = new CommandBuffer
    {
        name = bufferName
    };

    private ScriptableRenderContext _context;
    private Camera _camera;
    private PostFXSettings _settings;

    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomCombine,
        BloomPrefilter,
        Copy
    }
    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 1; i < maxBloomPyramidLevels * 2; i++)     //在每一个金字塔级别中多走一步
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }
    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
    {
        _context = context;
        _camera = camera;
        _settings = _camera.cameraType <= CameraType.SceneView ? settings : null;  //检查是否在渲染 Game、Scene场景，如果没有，停止处理后效
        ApplySceneViewState();
    }

    public bool IsActive => _settings != null;      //判断后处理栈是否为活跃状态

    public void Render(int sourceId)
    {
        // _buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        DoBloom(sourceId);
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        _buffer.SetGlobalTexture(fxSourceId, from);
        _buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //未使用的矩阵、用于后效的材质、指定的 Pass枚举，绘制的形状、有多少个顶点（三角形当然是三个顶点）
        _buffer.DrawProcedural(Matrix4x4.identity, _settings.material, (int)pass, MeshTopology.Triangles, 3);
    }

  

    void DoBloom(int sourceId)
    {
        _buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloomSettings = _settings.Bloom;
        
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;

        if (bloomSettings.maxIterations == 0 || bloomSettings.intensity <= 0f || height < bloomSettings.downscaleLimit * 2 ||
            width < bloomSettings.downscaleLimit * 2)
        {
            //如果迭代次数为 0，或者屏幕高宽小于 Bloom最小高宽，Bloom强度小于 0，直接退出
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            _buffer.EndSample("Bloom");
            return;
        }

        Vector4 threshold;      //Bloom阈值
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        _buffer.SetGlobalVector(bloomThresholdId, threshold);
        
        RenderTextureFormat format = RenderTextureFormat.Default;
        _buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
        Draw(sourceId, bloomPrefilterId, Pass.BloomPrefilter);
        
        int fromId = bloomPrefilterId;
        int toId = bloomPyramidId + 1;
        //每次做高斯模糊的时候，第一步采样+水平方向高斯滤波，第二步竖直方向高斯滤波
        int i;
        for (i = 0; i < bloomSettings.maxIterations; i++)     //循环遍历金字塔，直到找到宽高不能再减半的尺寸
        {
            if (height < bloomSettings.downscaleLimit || width < bloomSettings.downscaleLimit)
            {
                break;
            }

            int midId = toId - 1;
            _buffer.GetTemporaryRT(midId, width, height, 0, FilterMode.Bilinear, format);
            _buffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(fromId, toId, Pass.BloomVertical);
            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        _buffer.ReleaseTemporaryRT(bloomPrefilterId);
        //此时找到合适的最后一级图像，但不绘制出来，而是释放上一张纹理，并将目标设置为用于水平方向绘制的第一个级别的纹理
        // Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomHorizontal);
        _buffer.SetGlobalFloat(bloomBicubicUpsamplingId, bloomSettings.bicubicUpsampling ? 1f : 0f);    //是否开启双三次滤波
        _buffer.SetGlobalFloat(bloomIntensityId, 1f);   //混合图像时，Bloom强度为 1
        
        if (i > 1)      //至少要迭代两次才有效，一次的话就直接跳过采样
        {
            _buffer.ReleaseTemporaryRT(fromId-1);
            toId -= 5;
        
            for (i -= 1; i > 0; i--)
            {
                _buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                Draw(fromId, toId, Pass.BloomCombine);
                _buffer.ReleaseTemporaryRT(fromId);     //反向迭代，释放所有刚刚声明的渲染纹理
                _buffer.ReleaseTemporaryRT(fromId +1);
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            _buffer.ReleaseTemporaryRT(bloomPyramidId);
        }
        
        _buffer.SetGlobalFloat(bloomIntensityId, bloomSettings.intensity);      //只有当绘制屏幕时，才使用 Bloom强度
        _buffer.SetGlobalTexture(fxSource2Id, sourceId);
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        _buffer.ReleaseTemporaryRT(fromId);
        
        _buffer.EndSample("Bloom");
    }
}
