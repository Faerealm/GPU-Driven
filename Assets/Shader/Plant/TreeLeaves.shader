Shader "Tree/TreeLeaves"
{
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _AlphaTex("Alpha (A)", 2D) = "white" {}
        _Height("Height", float) = 0.5
        _Width("Width", range(0, 5)) = 1.0
        _Color("Color", Color) = (0.5, 1, 0.5, 1)
        _WindTexture ("Wind Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5 
    }

    SubShader{
        Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True"}
        Pass
        {
            Cull Off 
            Tags { "LightMode" = "UniversalForward" }
            AlphaToMask On

            HLSLPROGRAM
            #include "UnityCG.cginc" 
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma multi_compile_instancing
            #include "UnityLightingCommon.cginc" 
            uniform float4 _Color;
            sampler2D _MainTex;
            sampler2D _AlphaTex;
            sampler2D _WindTexture;
            float _Height; //树叶的高度
            float _Width; //树叶的宽度
            float _Cutoff;
            StructuredBuffer<float4x4> positionBuffer; //缓冲区绑定
            float windStrength;
            float sizeInWorldSpace;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID  // 接收来自GPU实例ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 norm : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID  // 在顶点着色器和几何着色器之间传递实例ID
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 norm : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID  // 将实例ID传递到片段着色器，允许在片段着色器中访问实例化属性
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                 v2f o;
                 UNITY_SETUP_INSTANCE_ID(v);
                 UNITY_TRANSFER_INSTANCE_ID(v, o);
                 float4x4 worldMatrix = positionBuffer[instanceID];  // 获取位置矩阵
                 //float4 adjustedVertex = v.vertex+float4(0.495f, 0.54f, 0.05f, 0);
                 o.pos = mul(worldMatrix,v.vertex);
                 o.norm = v.normal;
                 o.uv = v.texcoord;
                 return o;
            }

            g2f createGSOut() {
                g2f output;
                UNITY_INITIALIZE_OUTPUT(g2f, output);
                output.pos = float4(0, 0, 0, 0);
                output.norm = float3(0, 0, 0);
                output.uv = float2(0, 0);
                return output;
            }

            [maxvertexcount(6)]
            void geom(point v2f points[1], inout TriangleStream<g2f> triStream)
            {   
                UNITY_SETUP_INSTANCE_ID(points[0]);
                float4 root = points[0].pos;
                const int vertexCount = 6;
                float random = sin(UNITY_HALF_PI * frac(root.x) + UNITY_HALF_PI * frac(root.z));
                _Width = _Width + random / 16.0f;
                _Height = _Height + random/8.0f;

                g2f v[6];
                [unroll]
                for (int j = 0; j < 6; j++) {
                    v[j] = createGSOut();
                }
                //处理纹理坐标
                float currentV = 0;
                float offsetV = 1.f /((vertexCount / 2) - 1);
                //处理当前的高度
                float currentVertexHeight = 0;
                //风的影响系数
                float windCoEff = 0;
                float horizontalValue=1.2f;// 横向偏移
                float lowerValue=0.9f;//弯曲
                //随机旋转角度
                float randomRotation = frac(sin(dot(root.xz, float2(12.9898, 78.233))) * 43758.5453) * UNITY_PI * 2.0;
                float2x2 rotationMatrix = float2x2(cos(randomRotation), -sin(randomRotation),
                                   sin(randomRotation), cos(randomRotation));
                //叶子生长方向
                float flag=step(0.6f,frac(random*365.327))*2.0f-1.0f;
                for (int i = 0; i < vertexCount; i++)
                {
                    v[i].norm = float3(0, 0, 1);

                    if (fmod(i , 2) == 0)
                    { 
                        v[i].pos = float4(root.x - _Width , root.y + flag*currentVertexHeight, root.z, 1);
                        v[i].uv = float2(0, currentV);
                        
                    }
                    else
                    { 
                         v[i].pos = float4(root.x + _Width , root.y + flag*currentVertexHeight, root.z, 1);
                         v[i].uv = float2(1, currentV);

                         currentV += offsetV;
                         currentVertexHeight = currentV * _Height;
                    }
                    //应用旋转角度
                    float2 rotatedXZ = mul(rotationMatrix, float2(v[i].pos.x - root.x, v[i].pos.z - root.z));
                    v[i].pos.xz = rotatedXZ + root.xz;
                    float heightFactor = abs((v[i].pos.y - root.y) / (_Height * flag));// 计算高度比例
                     
                    // 使用二次曲线来创建弧形弯曲
                    float curve = heightFactor * heightFactor;
                    // 风力应用部分
                    float2 worldUV = v[i].pos.xz / sizeInWorldSpace;
                    float4 windSample = tex2Dlod(_WindTexture, float4(worldUV, 0, 0));

                    // 计算基础风力
                    float windBaseStrength = windSample.r * windSample.g * windStrength*2.0f;

                    // 使用风场纹理的B通道来控制风向
                    float windAngle = windSample.b * UNITY_TWO_PI;
                    float2 windDir = float2(cos(windAngle), sin(windAngle));
                    // 使用A通道控制弯曲程度
                    float bendIntensity = windSample.a*3.0f;
                    float horizontalCurve = curve * horizontalValue * bendIntensity;
                    float verticalCurve = curve * lowerValue * bendIntensity;

                    // 应用风力效果，考虑生长方向
                    float2 windOffset = windDir * windBaseStrength * horizontalCurve;
                    v[i].pos.xz += windOffset;

                    // 垂直方向的弯曲，根据生长方向调整
                    float verticalOffset = windBaseStrength * verticalCurve;
                    float archEffect = sin(heightFactor * UNITY_PI * 0.5);
                    v[i].pos.y -= (verticalOffset * archEffect * flag); // 使用flag来决定弯曲方向


                    // 添加小幅高频摆动
                    float microMove = sin(_Time.y  + root.x  + root.z  + heightFactor) * 0.02 * heightFactor;
                    v[i].pos.xz += (float2(microMove, microMove) * windBaseStrength+float2(microMove, microMove)*3.0f);

                    // 转换到裁剪空间
                    v[i].pos = UnityObjectToClipPos(v[i].pos);
                    if (fmod(i, 2) == 1) 
                    {
                        windCoEff += offsetV;
                    }
                }
                [unroll]
                for (int p = 0; p < vertexCount; p++) {
                    triStream.Append(v[p]);
                }
            }

            half4 frag(g2f IN) : COLOR
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                fixed4 color = tex2D(_MainTex, IN.uv);
                fixed4 alpha = tex2D(_AlphaTex, IN.uv);
                fixed4 grassColor = _Color;  // 通过 _Color 获取树叶的颜色
                clip(alpha.g - _Cutoff);
                color*=grassColor;

                half3 worldNormal = UnityObjectToWorldNormal(IN.norm);             
                fixed3 light;

                fixed3 ambient = ShadeSH9(half4(worldNormal, 1));

                fixed3 diffuseLight = saturate(dot(worldNormal, UnityWorldSpaceLightDir(IN.pos))) * _LightColor0;
 
                fixed3 halfVector = normalize(UnityWorldSpaceLightDir(IN.pos) + WorldSpaceViewDir(IN.pos));
                fixed3 specularLight = pow(saturate(dot(worldNormal, halfVector)), 15) * _LightColor0;
                light = ambient + diffuseLight + specularLight;
 
                return float4(color.rgb * light, alpha.g);
            }
            ENDHLSL
        }
    }
}
