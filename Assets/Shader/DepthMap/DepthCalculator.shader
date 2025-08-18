Shader "MyShader/Depth/DepthMipmapCalculator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off
            HLSLPROGRAM
            #pragma vertex vert//指定了顶点着色器（Vertex Shader）的入口函数名称为 vert
            #pragma fragment frag
            #pragma skip_variants SHADOWS_SCREEN DIRLIGHTMAP_COMBINED
            #pragma skip_variants LIGHTMAP_ON DYNAMICLIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);  // 使用point采样

            float4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            inline float CalculatorMipmapDepth(float2 uv)
            {
                float4 depth;
                float2 invSize = _MainTex_TexelSize.xy;
                
                // 在当前像素的四个角上采样
                float2 uv0 = uv + float2(-0.25f, -0.25f) * invSize;
                float2 uv1 = uv + float2(0.25f, -0.25f) * invSize;
                float2 uv2 = uv + float2(-0.25f, 0.25f) * invSize;
                float2 uv3 = uv + float2(0.25f, 0.25f) * invSize;

                depth.x = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv0).r;//.r表示只读取红色通道（在深度图中存储深度值）
                depth.y = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1).r;
                depth.z = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2).r;
                depth.w = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv3).r;

                #if defined(UNITY_REVERSED_Z)// 反向深度缓冲（Unity默认）
                    return min(min(depth.x, depth.y), min(depth.z, depth.w));
                #else
                    return max(max(depth.x, depth.y), max(depth.z, depth.w));
                #endif
                //DirectX：默认使用反向深度缓冲（1.0近 -> 0.0远）
                //OpenGL：默认使用正向深度缓冲（0.0近 -> 1.0远）
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
                float depth = CalculatorMipmapDepth(input.uv);
                return float4(depth, 0, 0, 1.0f);
            }
            ENDHLSL
        }
    }
}