Shader "MyShader/OrderedDithering/DitheredTransparency"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _DitherIntensity ("Dither Intensity", Range(0, 1)) = 0.5
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            //Tags {"LightMode" = "ForwardBase" }
            //Deferred,半透明物体不能直接在 deferred 中完成绘制
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _DitherIntensity;
            float _Cutoff;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = _Color;
                o.uv = v.uv;
                return o;
            }

            float dither(float2 uv)
            {
                float pattern = frac(sin(dot(uv, float2(10, 70))) * 50000);
                //float pattern = uv;达不到粒子效果
                if(_DitherIntensity==0)
                    return 0;
                else
                    return step(pattern, _DitherIntensity);//step(x, edge) 的作用是：若 x 小于 edge，返回 1，x 大于或等于 edge，返回 0
            }
            half4 frag(v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv) * i.color;
                // 使用 Dithering 控制透明度
                float ditherFactor = dither(i.uv);
                col.a *= ditherFactor;
                // 应用透明度剪切,col.a - _Cutoff小于0则不渲染大于等于0则继续渲染
               clip(col.a - _Cutoff);
               return col;
            }
            ENDHLSL
        }
    }
}