Shader "MyShader/Wind/WindComposite"
{
    Properties
    {
        _WindBaseTexture ("Wind Base Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_WindBaseTexture);
            SAMPLER(sampler_WindBaseTexture);
            
            float4 windUVs;
            float4 windUVs1;
            float4 windUVs2;
            float4 windUVs3;
            float4 gust;
            float3 gustMixLayer;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target 
            {
                 // n1: 主风层
                half4 n1 = SAMPLE_TEXTURE2D(_WindBaseTexture, sampler_WindBaseTexture, input.uv + float2(windUVs.x, windUVs.y));
                 // n2: 第二层风
                half4 n2 = SAMPLE_TEXTURE2D(_WindBaseTexture, sampler_WindBaseTexture, input.uv + float2(windUVs1.x, windUVs1.y));
                 // n3: 第三层风
                half4 n3 = SAMPLE_TEXTURE2D(_WindBaseTexture, sampler_WindBaseTexture, input.uv + float2(windUVs2.x, windUVs2.y));
                 // n4: 阵风层（注意这里使用了gust.x作为缩放）
                half4 n4 = SAMPLE_TEXTURE2D(_WindBaseTexture, sampler_WindBaseTexture, input.uv * gust.x + float2(windUVs3.x, windUVs3.y));

                // 组合各层风场信息
                // r: 只使用主风层
                // g: 主风层 + 第二层
                // b: 主风层 + 第二层 + 第三层
                // a: 所有层的alpha通道叠加
                half4 sum = half4(n1.r, n1.g + n2.g, n1.b + n2.b + n3.b, n1.a + n2.a + n3.a + n4.a);
                // 各层的权重系数
                const half4 weights = half4(0.5000, 0.2500, 0.1250, 0.0625);
                
                half2 WindStrengthGustNoise;
                // 计算风强，使用权重对所有采样结果进行加权平均
                WindStrengthGustNoise.x = dot(sum, weights);
                // 计算阵风噪声，混合主阵风(n4.a)和三层风的alpha通道
                WindStrengthGustNoise.y = lerp(1.0h, (n4.a + dot(half3(n1.a, n2.a, n3.a), gustMixLayer)) * 0.85h, gust.y - 0.5h);
                //锐化风力,增加对比度
                WindStrengthGustNoise = (WindStrengthGustNoise - half2(0.5h, 0.5h)) * gust.y + half2(0.5h, 0.5h);
                // 计算A通道值：使用风强和阵风的组合
                half alphaValue = (WindStrengthGustNoise.x + WindStrengthGustNoise.y) * 0.5h;
                // 确保A通道值在合理范围内
                alphaValue = saturate(alphaValue);

                return half4(WindStrengthGustNoise.x, WindStrengthGustNoise.y, (n3.a + abs(WindStrengthGustNoise.y)) * 0.5h + n2.a * 0.0h, alphaValue);
            }
            ENDHLSL
        }
    }
}