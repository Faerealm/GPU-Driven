using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TriangleMeshCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Triangle Mesh")]
    static void CreateTriangleMesh()
    {
        // 创建网格
        Mesh mesh = new Mesh();

        // 设置顶点
        Vector3[] vertices = new Vector3[3]
        {
            new Vector3(-0.5f, 0f, 0f),    // 左下
            new Vector3(0.5f, 0f, 0f),     // 右下
            new Vector3(0f, 1f, 0f)        // 顶部
        };

        // 设置三角形
        int[] triangles = new int[3]
        {
            0, 1, 2
        };

        // 设置UV坐标
        Vector2[] uv = new Vector2[3]
        {
            new Vector2(0f, 0f),           // 左下
            new Vector2(1f, 0f),           // 右下
            new Vector2(0.5f, 1f)          // 顶部
        };

        // 设置法线
        Vector3[] normals = new Vector3[3]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        // 应用设置到mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;

        // 重新计算边界
        mesh.RecalculateBounds();

        // 保存mesh资源
        string path = "Assets/TriangleMesh.asset";
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Triangle mesh created at: " + path);
    }
#endif
}