using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class DepthTexture : MonoBehaviour
{
    public Shader depthTextureShader;//用来生成mipmap的shader

    RenderTexture m_depthTexture;//带 mipmap 的深度图
    public RenderTexture depthTexture => m_depthTexture;//表示的是一个只读属性，其值由 m_depthTexture 字段提供，保证不可修改
    //等价于：public RenderTexture depthTexture
    //{
    //  get { return m_depthTexture; }
    //}


    int m_depthTextureSize = 0;
    public int depthTextureSize
    {
        get
        {
            if (m_depthTextureSize == 0)
                m_depthTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));
            //Debug.Log(Screen.height);
            return m_depthTextureSize;
        }
    }

    Material m_depthTextureMaterial;
    const RenderTextureFormat m_depthTextureFormat = RenderTextureFormat.RHalf;//深度取值范围0-1，单通道即可。

    int m_depthTextureShaderID;

    void Start()
    {
        m_depthTextureMaterial = new Material(depthTextureShader);
        Camera.main.depthTextureMode |= DepthTextureMode.Depth;
        //获取Unity内置深度纹理的ID，用于后续快速访问
        m_depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");

        InitDepthTexture();
    }

    void InitDepthTexture()
    {   
        // 先释放旧的
        if (m_depthTexture != null)
        {
            m_depthTexture.Release();
        }
        m_depthTexture = new RenderTexture(depthTextureSize, depthTextureSize, 0, m_depthTextureFormat);
        m_depthTexture.autoGenerateMips = false;
        m_depthTexture.useMipMap = true;
        m_depthTexture.filterMode = FilterMode.Point;
        
        m_depthTexture.Create();
    }
    void OnEnable()
    {     
        // 订阅渲染事件
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        // 取消订阅渲染事件
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    //生成mipmap// OnPostRender在urp中不在使用，应切换为OnEndCameraRendering
    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != Camera.main) return; // 只处理主相机

        //var currentRenderTarget = RenderTexture.active;

        //// 直接从相机深度纹理复制到目标纹理
        //var depthTexture = Shader.GetGlobalTexture(m_depthTextureShaderID);
        //Graphics.Blit(depthTexture, m_depthTexture, m_depthTextureMaterial);

        //RenderTexture.active = currentRenderTarget;
        // 保存当前的RenderTarget，因为 Graphics.Blit会改变RenderTarget，导致了ugui的元素在运行时不显示
        //在Unity中通过RenderTexture.active来表示当前激活的渲染目标
        var currentRenderTarget = RenderTexture.active;
        int w = m_depthTexture.width;
        int mipmapLevel = 0;
        RenderTexture currentRenderTexture = null;
        RenderTexture preRenderTexture = null;

        while (w > 8)// mipmap 生成
        {
            currentRenderTexture = RenderTexture.GetTemporary(w, w, 0, m_depthTextureFormat);
            currentRenderTexture.filterMode = FilterMode.Point;

            if (preRenderTexture == null)
            {
                // 第一次迭代：从相机深度纹理开始//使用Graphics.Blit的原因是相机深度纹理是特殊的纹理
                var depthTexture = Shader.GetGlobalTexture(m_depthTextureShaderID);
                Graphics.Blit(depthTexture, currentRenderTexture);
            }
            else
            {
                // 后续迭代：使用上一级mipmap进行降采样//Graphics.Blit允许拷贝的过程中调用自定义shader而 Graphics.CopyTexture不允许
                Graphics.Blit(preRenderTexture, currentRenderTexture, m_depthTextureMaterial);
                RenderTexture.ReleaseTemporary(preRenderTexture);
            }
            // 将结果复制到最终深度图的对应mipmap级别
            Graphics.CopyTexture(currentRenderTexture, 0, 0, m_depthTexture, 0, mipmapLevel);
            preRenderTexture = currentRenderTexture;
            w /= 2;//降采样尺寸
            mipmapLevel++;//下一个mipmap级别
        }

        if (preRenderTexture != null)
        {
            RenderTexture.ReleaseTemporary(preRenderTexture);
        }
        // 恢复RenderTarget
        RenderTexture.active = currentRenderTarget;
    }
    void OnDestroy()
    {
        if (m_depthTextureMaterial != null)
        {
            Destroy(m_depthTextureMaterial);
        }

        if (m_depthTexture != null)
        {
            m_depthTexture.Release();
            Destroy(m_depthTexture);
        }
    }
}
