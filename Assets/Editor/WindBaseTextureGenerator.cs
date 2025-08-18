using UnityEngine;
using UnityEditor;
public class WindBaseTextureGenerator : EditorWindow
{
    private int textureSize = 512;
    private float baseNoiseScale = 5.0f;
    private float turbulenceNoiseScale = 8.0f;
    private float gustNoiseScale = 2.0f;
    private float pressureNoiseScale = 6.0f;
    private float vortexStrength = 5f;
    private Vector2 vortexCenter = Vector2.one * 0.5f;
    [MenuItem("Tools/Generate Wind Base Texture")]
    static void Init()
    {
        WindBaseTextureGenerator window = GetWindow<WindBaseTextureGenerator>();
        window.Show();
    }

    void OnGUI()
    {
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        baseNoiseScale = EditorGUILayout.FloatField("Base Noise Scale", baseNoiseScale);
        turbulenceNoiseScale = EditorGUILayout.FloatField("Turbulence Scale", turbulenceNoiseScale);
        gustNoiseScale = EditorGUILayout.FloatField("Gust Scale", gustNoiseScale);
        vortexStrength = EditorGUILayout.FloatField("Vortex Scale", vortexStrength);
        if (GUILayout.Button("Generate"))
        {
            GenerateWindBaseTexture();
        }
    }

    void GenerateWindBaseTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float u = (float)x / textureSize;
                float v = (float)y / textureSize;

                // R通道：基础风强度和方向
                float baseWind = Mathf.PerlinNoise(u * baseNoiseScale, v * baseNoiseScale);

                // G通道：湍流
                float turbulence = Mathf.PerlinNoise(u * turbulenceNoiseScale + 100,
                                                   v * turbulenceNoiseScale + 100);

                // B通道：压力场
                float pressure = Mathf.PerlinNoise(u * pressureNoiseScale + 200,
                                                 v * pressureNoiseScale + 200);


                Vector2 toCenter = new Vector2(u, v) - vortexCenter;
                float dist = toCenter.magnitude;
                float vortex = Mathf.Atan2(toCenter.y, toCenter.x) / (2f * Mathf.PI);
                vortex *= Mathf.Exp(-dist * 4f) * vortexStrength;
                baseWind = Mathf.Lerp(baseWind, vortex, 0.5f);


                // A通道：大规模阵风模式
                float gusts = Mathf.PerlinNoise(u * gustNoiseScale + 300,
                                              v * gustNoiseScale + 300);

                Color pixel = new Color(baseWind, turbulence, pressure, gusts);
                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();

        // 保存纹理
        byte[] bytes = tex.EncodeToPNG();
        string path = EditorUtility.SaveFilePanel(
            "Save Wind Base Texture",
            "Assets",
            "WindBaseTexture.png",
            "png");

        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }
    }
}