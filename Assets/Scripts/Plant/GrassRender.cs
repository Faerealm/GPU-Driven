using UnityEngine;
using Random = System.Random;
public class GrassRender : MonoBehaviour
{
    public GameObject player;
    public LayerMask groundLayer;  // 设置LayerMask，仅允许在指定层上生成草
    public Mesh grassMesh;
    private int subMeshIndex = 0;
    public Material grassMaterialPC;
    public Material grassMaterialPhone;
    private Material grassMaterial;
    private int grassPatchCount = 10;
#if UNITY_ANDROID || UNITY_IOS
    private int grassCount = 50000;
    private int terrainSize = 600;//草地大小
#else
    private int grassCount = 180000;
    private int terrainSize = 800;//草地大小
#endif

    private int grassStartPositionX = 100;
    private int grassStartPositionZ = 0;
    int m_grassCount;
    public DepthTexture depthTexture;
    public ComputeShader compute;
    Camera mainCamera;
    private Wind windController;
    ComputeBuffer argsBuffer;
    ComputeBuffer grassMatrixBuffer;
    ComputeBuffer cullResultBuffer;

    private Random random;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    private int kernel;
    private int cullResultBufferId;
    private int vpMatrixId;
    private int positionBufferId;
    private int hizTextureId;
    private int playerPositionId;
    private int windStrengthId;
    private int cameraPositionId;

    private int _DitherIntensityId;
    private float minDistance = 0.0f;
    private float maxDistance = 1.0f;

    //// 添加UI文本组件引用
    //public UnityEngine.UI.Text grassCountText;
   // private uint currentGrassCount = 0;
    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        grassMaterial = grassMaterialPhone;
#else
        grassMaterial = grassMaterialPC;
#endif
        // 启用GPU Instancing
        grassMaterial.enableInstancing = true;

        random = new Random();
        m_grassCount = grassPatchCount * grassPatchCount * grassCount;
        mainCamera = Camera.main;

        windController = FindObjectOfType<Wind>();
        // 设置wind texture到草的材质
        grassMaterial.SetTexture("_WindTexture", windController.GetWindTexture());
        grassMaterial.SetFloat("sizeInWorldSpace", windController.sizeInWorldSpace);
        if (grassMesh != null)
        {
            args[0] = grassMesh.GetIndexCount(subMeshIndex);
            args[2] = grassMesh.GetIndexStart(subMeshIndex);
            args[3] = grassMesh.GetBaseVertex(subMeshIndex);
        }
        InitComputeBuffer();
        InitGrassPosition();
        InitComputeShader();
    }

    void InitComputeShader()
    {
        kernel = compute.FindKernel("GrassCulling");
        compute.SetInt("grassCount", m_grassCount);
        // 设置深度纹理大小
        compute.SetInt("depthTextureSize", depthTexture.depthTextureSize);
        //将存储草矩阵的缓冲区绑定到compute shader
        compute.SetBuffer(kernel, "grassMatrixBuffer", grassMatrixBuffer);
        compute.SetVector("boundMin", new Vector3(-0.15f, 0.0f, -0.15f));
        compute.SetVector("boundMax", new Vector3(0.15f, 0.8f, 0.15f));

        cullResultBufferId = Shader.PropertyToID("grassCullResultBuffer");
        vpMatrixId = Shader.PropertyToID("vpMatrix");
        hizTextureId = Shader.PropertyToID("hizTexture");
        positionBufferId = Shader.PropertyToID("positionBuffer");
        playerPositionId = Shader.PropertyToID("PlayerPosition");
        windStrengthId = Shader.PropertyToID("windStrength");
        cameraPositionId = Shader.PropertyToID("cameraPosition");
        _DitherIntensityId = Shader.PropertyToID("_DitherIntensity");
    }

    void InitComputeBuffer()
    {
        if (grassMatrixBuffer != null) return;
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        grassMatrixBuffer = new ComputeBuffer(m_grassCount, sizeof(float) * 16);
        cullResultBuffer = new ComputeBuffer(m_grassCount, sizeof(float) * 16, ComputeBufferType.Append);

    }

    void Update()
    {
        grassMaterial.SetVector(playerPositionId, player.transform.position);
        grassMaterial.SetFloat(windStrengthId, windController.windStrength);
        compute.SetVector(cameraPositionId, mainCamera.transform.position);
        compute.SetTexture(kernel, hizTextureId, depthTexture.depthTexture);
        compute.SetMatrix(vpMatrixId, GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix);
        cullResultBuffer.SetCounterValue(0);
        // 设置剔除结果缓冲区
        compute.SetBuffer(kernel, cullResultBufferId, cullResultBuffer);
        compute.Dispatch(kernel, 1 + m_grassCount / 1024, 1, 1);
        //将剔除后的草位置传递到草的渲染shader里
        grassMaterial.SetBuffer(positionBufferId, cullResultBuffer);

        //获取实际要渲染的数量
        ComputeBuffer.CopyCount(cullResultBuffer, argsBuffer, sizeof(uint));
        //更新UI显示
        //if (grassCountText != null)
        //{
        //    uint[] debugArgs = new uint[5];
        //    argsBuffer.GetData(debugArgs);
        //    //Debug.Log(debugArgs[1]);
        //    currentGrassCount = debugArgs[1];
        //    grassCountText.text = $"Total Grass: {m_grassCount}\n" +
        //                  $"Rendered Grass: {debugArgs[1]}\n" +
        //                  $"Device: {SystemInfo.graphicsDeviceName}\n" +
        //                  $"API: {SystemInfo.graphicsDeviceType}\n" +
        //                  $"CPU: {SystemInfo.processorType}\n" +
        //                  $"Memory: {SystemInfo.systemMemorySize} MB";
        //}
        Graphics.DrawMeshInstancedIndirect(grassMesh, subMeshIndex, grassMaterial, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);
        Dithering();
    }
    private void Dithering()
    {
        // 根据距离计算 Dither 强度
        float ditherIntensity = Mathf.InverseLerp(minDistance, maxDistance, CameratoGround());
        // 设置 Dither 强度
        grassMaterial.SetFloat(_DitherIntensityId, ditherIntensity);
    }
    private float CameratoGround()
    {
        float CameraX = mainCamera.transform.position.x;
        float CameraZ = mainCamera.transform.position.z;
        float verticalDistance = maxDistance;
        if (CameraX > grassStartPositionX + 5.0f && CameraX < grassStartPositionX + terrainSize - 5.0f && CameraZ > grassStartPositionZ + 5.0f && CameraZ < grassStartPositionZ + terrainSize - 5.0f)
        {
            RaycastHit hit;
            Vector3 rayStart = mainCamera.transform.position;

#if UNITY_EDITOR
            Debug.DrawRay(rayStart, Vector3.down * 5, Color.red);
#endif
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 5, groundLayer))
            {
                verticalDistance = mainCamera.transform.position.y - hit.point.y;
            }
            return verticalDistance;
        }
        return verticalDistance;
    }
    private void InitGrassPosition()
    {
        Vector3 startPosition = new Vector3(grassStartPositionX, 0, grassStartPositionZ);
        Vector3 patchSize = new Vector3(terrainSize / grassPatchCount, 0, terrainSize / grassPatchCount);
        Matrix4x4[] grassMatrixs = new Matrix4x4[m_grassCount];
        for (int x = 0; x < grassPatchCount; x++)
        {
            for (int y = 0; y < grassPatchCount; y++)
            {
                for (var i = 0; i < grassCount; i++)
                {
                    var randomizedZDistance = (float)random.NextDouble() * patchSize.z - (float)random.NextDouble() * patchSize.z / 2;
                    var randomizedXDistance = (float)random.NextDouble() * patchSize.x - (float)random.NextDouble() * patchSize.x / 2;

                    var xz = new Vector2(startPosition.x + randomizedXDistance, startPosition.z + randomizedZDistance);
                    float groundHeight = GetGroundHeight(xz);
                    if (groundHeight == 0)
                    {
                        continue;
                    }
                    var currentPosition = new Vector3(xz.x, groundHeight, xz.y);

                    //verts.Add(currentPosition);
                    grassMatrixs[(x * grassPatchCount + y) * grassCount + i] = Matrix4x4.TRS(currentPosition, Quaternion.identity, Vector3.one);
                }
                startPosition.x += patchSize.x;
            }
            startPosition.x = grassStartPositionX;
            startPosition.z += patchSize.z;
        }
        grassMatrixBuffer.SetData(grassMatrixs);
    }

    float GetGroundHeight(Vector2 xz)
    {
        RaycastHit hit;
        Vector3 rayStart = new Vector3(xz.x, 200, xz.y);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 250, groundLayer))
        {
            return 200 - hit.distance;
        }

        return 0;
    }

    void OnDisable()
    {

        grassMatrixBuffer?.Release();//释放创建的缓冲区，以免内存泄漏
        grassMatrixBuffer = null;

        cullResultBuffer?.Release();
        cullResultBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }
}
