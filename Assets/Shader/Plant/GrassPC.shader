Shader "MyShader/Grass/GrassPC"
{
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _AlphaTex("Alpha (A)", 2D) = "white" {}
        _Height("Height", float) = 1
        _Width("Width", range(0, 0.1)) = 0.03
        _Color("Color", Color) = (0.5, 1, 0.5, 1)
        _WindTexture ("Wind Texture", 2D) = "white" {}

        _DitherIntensity ("Dither Intensity", Range(0, 1)) = 0.5
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader{
        Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True"}
        Pass
        {
            Cull Off    // 如果不需要面剔除
            //Tags { "LightMode" = "ForwardBase" }
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
            float _Height; //草的高度
            float _Width; //草的宽度
            StructuredBuffer<float4x4> positionBuffer; //缓冲区绑定
            float windStrength;
            float sizeInWorldSpace;
            float4 PlayerPosition; // 玩家位置
            float _DitherIntensity;
            float _Cutoff;
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
                 float4x4 worldMatrix = positionBuffer[instanceID];  // 获取剔除后的草的位置矩阵
                 o.pos = mul(worldMatrix, v.vertex);  // 应用矩阵到顶点位置
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
            float dither(float2 uv)
            {
                float pattern = frac(sin(dot(uv, float2(10, 70))) * 50000);
                if(_DitherIntensity==0)
                return 0;
                else
                return step(pattern, _DitherIntensity);
            }
       
            [maxvertexcount(12)]
            void geom(point v2f points[1], inout TriangleStream<g2f> triStream)
            {   
                UNITY_SETUP_INSTANCE_ID(points[0]);
                float4 root = points[0].pos;
                const int vertexCount=12;
                float random = sin(UNITY_HALF_PI * frac(root.x) + UNITY_HALF_PI * frac(root.z));
                _Width = _Width + random / 30;
                _Height = _Height + random*random-0.5f;

                g2f v[12];
                [unroll]
                for (int j = 0; j < 12; j++) {
                    v[j] = createGSOut();
                }
                //处理纹理坐标
                float currentV = 0;
                float offsetV = 1.0f /((vertexCount / 2) - 1);
                //处理当前的高度
                float currentVertexHeight = 0;
                // 风的影响系数
                // float windCoEff = 0;
                //随机旋转角度
                float randomRotation = frac(sin(dot(root.xz, float2(12.9898, 78.233))) * 43758.5453) * UNITY_PI * 2.0;
                float2x2 rotationMatrix = float2x2(cos(randomRotation), -sin(randomRotation),
                                   sin(randomRotation), cos(randomRotation));
                //float bendStartRatio = 0.001f; // 草的弯曲起始高度比例
                float horizontalValue=1.2f;// 横向偏移
                float lowerValue=1.2f;//垂直压低

                 // 计算基础弯曲方向
                float baseSwayAngle = randomRotation + random * UNITY_PI; 
                float2 swayDir = float2(cos(baseSwayAngle), sin(baseSwayAngle));// 无风时的随机偏移
                 //添加玩家与草的交互
                float3 playerOffset = root.xyz - PlayerPosition.xyz;
                float distToPlayer = length(playerOffset);
                float PlayerInfluenceRadius=3.0f; // 玩家的风的影响范围
                float influenceStartHeight=0.005f;//玩家影响的高度起始比例
                root.y-=0.1f;
                for (int i = 0; i < vertexCount; i++)
                {
                    v[i].norm = float3(0, 0, 1);

                    if (fmod(i , 2) == 0)
                    { 
                        v[i].pos = float4(root.x - _Width , root.y+currentVertexHeight, root.z, 1);
                        v[i].uv = float2(0, currentV);
                    }
                    else
                    { 
                        v[i].pos = float4(root.x + _Width ,  root.y+currentVertexHeight, root.z, 1);
                        v[i].uv = float2(1, currentV);

                        currentV += offsetV;
                        currentVertexHeight = currentV * _Height;
                    }
                    float heightFactor = (v[i].pos.y - root.y) / _Height;// 计算草的顶点的高度比例

                    //应用旋转角度
                    float2 rotatedXZ = mul(rotationMatrix, float2(v[i].pos.x - root.x, v[i].pos.z - root.z));
                    v[i].pos.xz = rotatedXZ + root.xz;
    
                    // 使用二次曲线来创建弧形弯曲
                    float curve = heightFactor * heightFactor;
                    // 添加无风状态自然弯曲
                    float naturalBend = curve  * random*0.8f;
                    v[i].pos.xz += swayDir * naturalBend;
                    v[i].pos.y -= naturalBend; // 轻微下压


                    // 平滑衰减玩家的影响效果
                    float playerInfluence = pow(saturate(1.0f - distToPlayer / PlayerInfluenceRadius), 3.0f);
                    float heightInfluence = saturate((heightFactor - influenceStartHeight)/(1.0f-influenceStartHeight));
                    float curveHeightInfluence=heightInfluence*heightInfluence;
                    playerInfluence*=curveHeightInfluence;
                    float playerVerticalOffset = 0.0f;
                    float2 playerBend = float2(0.0f, 0.0f);

                    // 如果在玩家影响范围内，应用附加的弯曲和偏移
                    if (playerInfluence > 0.0f)
                    {    
                        // 玩家与草的交互弯曲方向
                        float2 playerDir = normalize(float2(playerOffset.x, playerOffset.z));

                        // 计算偏移和弯曲效果
                        playerBend = playerDir * playerInfluence*0.8f;
                        playerVerticalOffset = playerInfluence*1.3f;
                    }

                    // 风力应用部分
                    float2 worldUV = v[i].pos.xz / sizeInWorldSpace;//和像素对应上
                    float4 windSample = tex2Dlod(_WindTexture, float4(worldUV, 0, 0));
                    float windBaseStrength = windSample.r * windSample.g * windStrength*2.0f;// 计算基础风力
                    float windAngle = windSample.b * UNITY_TWO_PI; // 使用风场纹理的B通道来控制风向，将0-1的值映射到0-2π
                    float2 windDir = float2(cos(windAngle), sin(windAngle));
                    float bendIntensity = windSample.a * 3.0f; // 使用A通道控制弯曲程度
                    float horizontalCurve = curve * horizontalValue * bendIntensity;
                    float verticalCurve = curve * lowerValue * bendIntensity;
                    float2 windOffset = windDir * windBaseStrength * horizontalCurve;
                    v[i].pos.xz += windOffset; // 应用弧形偏移
                    float verticalOffset = windBaseStrength * verticalCurve;
                    // 使用正弦函数创造弧形，并加入A通道的影响
                    float archEffect = sin(heightFactor * UNITY_PI * 0.5) * bendIntensity;
                    v[i].pos.y -= (verticalOffset * archEffect + playerVerticalOffset);
                    // 添加小幅高频摆动
                    float microMove = sin(_Time.y  + root.x  + root.z  + heightFactor) * 0.02 * heightFactor;
                    v[i].pos.xz += (float2(microMove, microMove) * windBaseStrength+float2(microMove, microMove)*3.0f);
                    v[i].pos = UnityObjectToClipPos(v[i].pos);  // 转换到裁剪空间
                    // if (fmod(i, 2) == 1) {
                    //     windCoEff += offsetV;
                    // }
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
                fixed4 alpha = tex2D(_AlphaTex, IN.uv);//遮罩
                fixed4 grassColor = _Color;  // 通过 _Color 获取草的颜色       
                color*=grassColor;
                //Dithering
                float ditherFactor = dither(IN.uv);
                alpha.g *= ditherFactor;
                // 透明度剪切
                clip(alpha.g - _Cutoff);

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