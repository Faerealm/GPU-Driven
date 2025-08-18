using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class Wind : MonoBehaviour
{
    [Header("Render Texture Settings")]
    public int resolution = 512;
    public RenderTextureFormat format = RenderTextureFormat.ARGB32;
    public Texture2D windBaseTexture;
    public Shader windCompositeShader;
    [Header("Wind Speed and Size")]
    public float sizeInWorldSpace = 50f;//在RenderGrass中使用，用于控制风场纹理的"重复"尺度，float2 worldUV = worldPosition.xz / _SizeInWorldSpace;
    //值较大纹理会被过度拉伸导致细节丢失产生更连续的大尺度风向
    [HideInInspector]
    public float windStrength = 1f;
    public float grassGustTiling = 1f;   
    private string layerToMixWith = "Layer_1";

    [Header("Wind Speed Controls")]
    public float baseWindSpeed = 1f;    // 基础风速倍率
    public float turbulenceSpeed = 1f;   // 湍流速度倍率
    public float detailSpeed = 1f;       // 细节层速度倍率
    public float grassGustSpeed = 0.1f;

    private Material windCompositeMat;
    private RenderTexture windRT;
    private WindZone windZone;
    private Vector4 windUVs;
    private Vector4 windUVs1;
    private Vector4 windUVs2;
    private Vector4 windUVs3;
    private float time;

    private string textureFolder = "MyAssets/NoiseTexture"; // 噪声图所在的 Resources子文件夹
    private string[] textureNames;// 存储噪声图的名称
    private float minInterval = 20.0f; 
    private float maxInterval = 25.0f; 
    private float windPauseDuration = 2.0f;

    private float currentWindScale = 0.0f; 
    private float targetWindScale = 0.0f;  
    private float transitionSpeed=0.05f;
    private bool isTransitioning = false;
    Vector3 cameraPosition;
    void OnEnable()
    {
        windZone = GetComponent<WindZone>(); 
        SetupWindTexture();
    }
    void Start()
    {
        cameraPosition = Camera.main.transform.position;
        LoadTextureNames();
       
        StartCoroutine(RandomLoadTextures());// 启动随机加载协程
    }

    void LoadTextureNames()
    {
        // 从 Resources 文件夹加载所有纹理的名字
        Texture2D[] textures = Resources.LoadAll<Texture2D>(textureFolder);
        textureNames = new string[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            textureNames[i] = textures[i].name; 
        }
    }
    void SetupWindTexture()
    {
        if (windRT == null || windRT.width != resolution)
        {
            if (windRT != null)
                windRT.Release();

            windRT = new RenderTexture(resolution, resolution, 0, format);
            windRT.wrapMode = TextureWrapMode.Repeat;//无缝重复：
            //当UV坐标超出[0, 1]范围时，纹理会无缝重复
        }

        if (windCompositeMat == null)
        {
            windCompositeMat = new Material(windCompositeShader);
            windCompositeMat.SetTexture("_WindBaseTexture", windBaseTexture);
        }
    }
    IEnumerator RandomLoadTextures()
    {
        while (true)
        {
            // 风最大状态持续时间
            float waitTime = Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(waitTime);
            // 开始过渡到无风状态
            isTransitioning = true;
            targetWindScale = 0.0f;
            yield return new WaitUntil(() => !isTransitioning);

            // 风停时间
            yield return new WaitForSeconds(windPauseDuration);
                    
            if (textureNames.Length > 0)
            {
                string randomTextureName = textureNames[Random.Range(0, textureNames.Length)];
                //Debug.Log(randomTextureName);
                windBaseTexture = Resources.Load<Texture2D>($"{textureFolder}/{randomTextureName}");//加载噪声图
                // 更新 windBaseTexture 到 windCompositeMat
                windCompositeMat.SetTexture("_WindBaseTexture", windBaseTexture);
            }
            // 开始过渡到有风状态
            isTransitioning = true;
            targetWindScale = 1f;
            yield return new WaitUntil(() => !isTransitioning);       
        }
    }
    void Update()
    {
        if (!windZone || !windCompositeMat || !windRT)
            return;
        if (isTransitioning)
        {   
            currentWindScale = Mathf.MoveTowards(currentWindScale, targetWindScale, Time.deltaTime * transitionSpeed);
            // 检查是否完成过渡
            if (currentWindScale == targetWindScale)
            {
                isTransitioning = false;
            }
        }
        windStrength = windZone.windMain * currentWindScale;  // 风力缩放
        time += Time.deltaTime;
        //计算各层风的UV偏移
        float speedMultiplier = windStrength*0.1f;
        float strengthMultiplier = 0.1f;
        switch (windZone.mode)
        {
            case WindZoneMode.Directional:
                // 方向性风，保持稳定的风速
                strengthMultiplier = windStrength;
                break;

            case WindZoneMode.Spherical:
                // 球形风，风力随距离衰减
                float distance = Vector3.Distance(transform.position, cameraPosition);
                strengthMultiplier = Mathf.Lerp(windStrength, 0f, distance / windZone.radius);
                break;

            default:
                strengthMultiplier = 1f;
                break;
        }
        // 应用风区影响
        speedMultiplier *= strengthMultiplier;
        windUVs.x = time * speedMultiplier * baseWindSpeed;
        windUVs1.x = time * speedMultiplier * turbulenceSpeed;
        windUVs2.x = time * speedMultiplier * detailSpeed;
        //计算阵风的UV偏移
        float gustSpeed = time * grassGustSpeed * strengthMultiplier;
        windUVs3.Set(gustSpeed, gustSpeed, 0, 0);
        //设置shader中的风场UV变量
        windCompositeMat.SetVector("windUVs", windUVs);
        windCompositeMat.SetVector("windUVs1", windUVs1);
        windCompositeMat.SetVector("windUVs2", windUVs2);
        windCompositeMat.SetVector("windUVs3", windUVs3);
        //设置阵风和湍流参数
        float turbulence = Mathf.Lerp(1.0f, 2.0f, windZone.windTurbulence * strengthMultiplier);
        windCompositeMat.SetVector("gust", new Vector4(grassGustTiling, turbulence, 0, 0));
        //设置阵风层混合参数
        //根据layerToMixWith选择要混合的层，将选中的层设为1，其他层设为0
        Vector3 gustMixLayer = new Vector3(
            layerToMixWith == "Layer_0" ? 1 : 0,
            layerToMixWith == "Layer_1" ? 1 : 0,
            layerToMixWith == "Layer_2" ? 1 : 0
        );
        windCompositeMat.SetVector("gustMixLayer", gustMixLayer);
        //将风场效果渲染到目标纹理
        Graphics.Blit(null, windRT, windCompositeMat);
    }
    // 获取生成的WindTexture
    public RenderTexture GetWindTexture()
    {
        return windRT;
    }
    void OnDisable()
    {
        if (windRT != null)
        {
            windRT.Release();
            windRT = null;
        }
        if (windCompositeMat != null)
        {
            if (Application.isPlaying)
                Destroy(windCompositeMat);
            else
                DestroyImmediate(windCompositeMat);
        }
    }


   
// //实现保存windTexture功能，并显示保存按钮
//#if UNITY_EDITOR
//    public void SaveWindTexture()
//    {
//        if (windRT == null)
//        {
//            Debug.LogError("No wind texture to save!");
//            return;
//        }

//        // 创建一个临时的RenderTexture来读取像素
//        RenderTexture tempRT = RenderTexture.GetTemporary(
//            windRT.width,
//            windRT.height,
//            0,
//            RenderTextureFormat.ARGB32
//        );

//        // 复制当前的wind texture到临时texture
//        Graphics.Blit(windRT, tempRT);

//        // 保存之前的RenderTexture
//        RenderTexture prev = RenderTexture.active;
//        RenderTexture.active = tempRT;

//        // 创建一个Texture2D并读取像素
//        Texture2D tex = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGBA32, false);
//        tex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
//        tex.Apply();

//        // 恢复之前的RenderTexture
//        RenderTexture.active = prev;
//        RenderTexture.ReleaseTemporary(tempRT);

//        // 保存为PNG
//        byte[] bytes = tex.EncodeToPNG();
//        string path = EditorUtility.SaveFilePanel(
//            "Save Wind Texture",
//            Application.dataPath,
//            "WindTexture",
//            "png"
//        );

//        if (!string.IsNullOrEmpty(path))
//        {
//            System.IO.File.WriteAllBytes(path, bytes);
//            Debug.Log("Wind texture saved to: " + path);

//            // 如果保存在Assets文件夹内，刷新AssetDatabase
//            if (path.StartsWith(Application.dataPath))
//            {
//                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
//                AssetDatabase.Refresh();

//                // 设置导入设置
//                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
//                if (importer != null)
//                {
//                    importer.sRGBTexture = false; // 禁用sRGB
//                    importer.textureCompression = TextureImporterCompression.Uncompressed;
//                    importer.wrapMode = TextureWrapMode.Repeat;
//                    importer.filterMode = FilterMode.Bilinear;
//                    importer.SaveAndReimport();
//                }
//            }
//        }

//        // 清理
//        DestroyImmediate(tex);
//    }

//    [CustomEditor(typeof(Wind))]
//    public class WindEditor : Editor
//    {
//        public override void OnInspectorGUI()
//        {
//            DrawDefaultInspector();

//            Wind wind = (Wind)target;
//            if (GUILayout.Button("Save Wind Texture"))
//            {
//                wind.SaveWindTexture();
//            }
//        }
//    }
//#endif
}