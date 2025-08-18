using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class TreeLeaves : MonoBehaviour
{
    public GameObject player;
    private Wind windController;
    public Mesh leavesMesh;
    public Material leavesMaterial;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private const int subMeshIndex = 0;//子网格索引
    private List<Vector3> leafPositions = new List<Vector3>();

    int windStrengthId;
    public ComputeShader compute;
    public DepthTexture depthTexture;
    private Camera mainCamera;
    private ComputeBuffer cullResultBuffer;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;

    private int kernel;
    private int cullResultBufferId;
    private int vpMatrixId;
    private int positionBufferId;
    private int hizTextureId;
    private int cameraPositionId;
    // 添加UI文本组件引用
    //public Text leavesCountText;
    //private uint currentGrassCount = 0;
    void Start()
    {
        mainCamera = Camera.main;
        leavesMaterial.enableInstancing = true;
        windController = FindObjectOfType<Wind>();
        leavesMaterial.SetTexture("_WindTexture", windController.GetWindTexture());
        leavesMaterial.SetFloat("sizeInWorldSpace", windController.sizeInWorldSpace);
        if (leavesMesh != null)
        {
            args[0] = leavesMesh.GetIndexCount(subMeshIndex);
            args[2] = leavesMesh.GetIndexStart(subMeshIndex);
            args[3] = leavesMesh.GetBaseVertex(subMeshIndex);
        }
        windStrengthId = Shader.PropertyToID("windStrength");
        GetLeafPositions();
        InitializeBuffers();
        InitComputeShader();
    }

    void InitComputeShader()
    {
        kernel = compute.FindKernel("TreeLeavesCulling");

        compute.SetInt("leavesCount", leafPositions.Count);
        compute.SetInt("depthTextureSize", depthTexture.depthTextureSize);
        compute.SetBuffer(kernel, "treeLeavesMatrixBuffer", positionBuffer);
        // 设置包围盒
        compute.SetVector("boundMin", new Vector3(-0.6f, 0.0f, -0.6f));
        compute.SetVector("boundMax", new Vector3(0.6f, 1.5f, 0.6f));

        cullResultBufferId = Shader.PropertyToID("leavesCullResultBuffer");
        vpMatrixId = Shader.PropertyToID("vpMatrix");
        hizTextureId = Shader.PropertyToID("hizTexture");
        positionBufferId = Shader.PropertyToID("positionBuffer");
        cameraPositionId = Shader.PropertyToID("cameraPosition");

    }

    private void GetLeafPositions()
    {
        Transform leafMeshTransform = transform.Find("Mesh/C4D_CN_043_01");
        if (leafMeshTransform == null)
            return;
#if UNITY_ANDROID || UNITY_IOS
        const float SAMPLE_PERCENTAGE = 0.01f;
#else
        const float SAMPLE_PERCENTAGE = 0.02f;
#endif

        System.Random random = new System.Random();

        HashSet<Vector3> uniquePositions = new HashSet<Vector3>();
        //foreach (Transform child in leafMeshTransform)
        // {
        // if (child.name.StartsWith("sub"))//以sub开头的子物体
        // {
        MeshFilter meshFilter = leafMeshTransform.GetComponent<MeshFilter>();
        //if (meshFilter == null)
        //{
        //    Debug.LogError("No MeshFilter component found on sub039!");
        //    return;
        //}

        Mesh mesh = meshFilter.sharedMesh;
        int submeshCount = mesh.subMeshCount;
        int[] indices = mesh.GetIndices(submeshCount - 1);
        Vector3[] vertices = mesh.vertices;
        //子网格中提取顶点位置
        for (int i = 0; i < indices.Length; i++)
        {
            if (random.NextDouble() < SAMPLE_PERCENTAGE)
            {
                Vector3 worldPosition = leafMeshTransform.TransformPoint(vertices[indices[i]]);
                if (uniquePositions.Add(worldPosition))//防止一个点重复渲染树叶
                {
                    leafPositions.Add(worldPosition);
                }
            }
        }
        //Debug.Log("leafCount: " + leafPositions.Count);
        // }
        //}
    }

    private void InitializeBuffers()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        //为每个位置创建一个变换矩阵
        Matrix4x4[] matrices = new Matrix4x4[leafPositions.Count];
        for (int i = 0; i < leafPositions.Count; i++)
        {
            matrices[i] = Matrix4x4.TRS(
            leafPositions[i],
            Quaternion.identity,
            Vector3.one
            );
        }

        // 创建矩阵buffer
        positionBuffer = new ComputeBuffer(leafPositions.Count, 16 * sizeof(float));
        positionBuffer.SetData(matrices);
        leavesMaterial.SetBuffer("positionBuffer", positionBuffer);
        // 设置args buffer
        if (argsBuffer != null)
            argsBuffer.Release();

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        if (cullResultBuffer != null)

            cullResultBuffer.Release();

        cullResultBuffer = new ComputeBuffer(leafPositions.Count, sizeof(float) * 16, ComputeBufferType.Append);


    }

    private void Update()
    {
        leavesMaterial.SetFloat(windStrengthId, windController.windStrength);

        // 设置计算着色器参数
        compute.SetVector(cameraPositionId, mainCamera.transform.position);
        compute.SetTexture(kernel, hizTextureId, depthTexture.depthTexture);
        compute.SetMatrix(vpMatrixId, GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false) * mainCamera.worldToCameraMatrix);
        // 重置并设置剔除缓冲区
        cullResultBuffer.SetCounterValue(0);
        compute.SetBuffer(kernel, cullResultBufferId, cullResultBuffer);

        // 执行计算着色器
        compute.Dispatch(kernel, 1 + leafPositions.Count / 1024, 1, 1);

        // 设置渲染用的缓冲区
        leavesMaterial.SetBuffer(positionBufferId, cullResultBuffer);

        //实际要渲染的数量
        ComputeBuffer.CopyCount(cullResultBuffer, argsBuffer, sizeof(uint));

        leavesMaterial.SetFloat(windStrengthId, windController.windStrength);
        //获取实际要渲染的数量
        ComputeBuffer.CopyCount(cullResultBuffer, argsBuffer, sizeof(uint));
        //Debug.Log(debugArgs[1]);
        //currentGrassCount = debugArgs[1];
        //// 更新UI显示
        //if (leavesCountText != null)
        //{
        //    leavesCountText.text = $"Leaves Count: {currentGrassCount}";
        //}
        Graphics.DrawMeshInstancedIndirect(
            leavesMesh,
            subMeshIndex,
            leavesMaterial,
            new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)),
            argsBuffer
        );
    }
    private void OnDestroy()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        if (argsBuffer != null)
            argsBuffer.Release();
        if (cullResultBuffer != null)
            cullResultBuffer.Release();
    }
}