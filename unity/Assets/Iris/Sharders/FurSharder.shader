// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/FurSharder"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FurTex ("ファーテクスチャ(シェル)", 2D) = "black" {}
        _FurFinTex ("ファーテクスチャ(フィン)", 2D) = "black" {}
        _FinSubdivisionLevel ("ファーの分割レベル", Int) = 1
        _FinRandomness ("ファーのランダムさ", Range(0, 1)) = 0.1
        _ShellNum ("シェルの数", Int) = 1
        _FurHeight ("毛の長さ", float) = 0.1
        _FurShade ("ファーの影の強さ", float) = 0.2
    }

    SubShader
    {

        Pass
        {
            // オリジナル
            Tags
            {
                "RenderType"="Opaque"
                "LightMode"="ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FurShade;
            int _ShellNum;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                float3 worldNormal = UnityObjectToWorldNormal(i.normal);

                float3 ambient = ShadeSH9(half4(worldNormal, 1));
                float shadePower = _ShellNum > 0 ? 1 - _FurShade : 1;
                return fixed4(lerp(ambient, float3(1 , 1, 1), shadePower) * col, 1);
            }
            ENDCG
        }

        Pass
        {
            // シェル法
            Tags
            {
                "Queue"           = "AlphaTest"
                "RenderType"      = "TransparentCutout"
                "LightMode"       = "ForwardBase"
            }
            AlphaToMask ON
            Cull OFF

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float depth : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FurTex;
            float4 _FurTex_ST;
            int _ShellNum;
            float _FurHeight;
            float _FurShade;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            [maxvertexcount(24)] // シェルは最大8層まで ( 8 * 3 = 24 )
            void geom (triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {   
                g2f o;
                
                [loop]
                for(int i = 0; i < _ShellNum; i++)
                {
                    float depth = (float)(i + 1) / _ShellNum;
                    float currentFurHeight = _FurHeight * depth;
                    for(int k = 0; k < 3; k++)
                    {
                        float3 normal = IN[k].normal;
                        float4 vertex = IN[k].vertex;

                        o.vertex = UnityObjectToClipPos(float4(vertex.xyz + normal.xyz * currentFurHeight, vertex.w));
                        o.uv = IN[k].uv;
                        o.depth = depth;
                        o.normal = normal;
                        
                        triStream.Append(o);
                    }
                    triStream.RestartStrip();
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                float3 worldNormal = UnityObjectToWorldNormal(i.normal);
                float3 ambient = ShadeSH9(half4(worldNormal, 1));
                
                fixed furDepth = tex2D(_FurTex, i.uv);
                float shadePower = lerp(1 - _FurShade, 1, i.depth);
                col.rgb = lerp((float3)ambient, float3(1 , 1, 1), shadePower) * col.rgb;
                col.a = furDepth > i.depth ? 1 : 0;

                return col;
            }
            ENDCG
        }

        Pass
        {
            // フィン法
            Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "LightMode" = "ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha 
            // AlphaToMask ON
            ZWrite OFF
            Cull OFF

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float2 uv : TEXCOORD0;
                float2 finUv : TEXCOORD2;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _FurFinTex;
            int _FinSubdivisionLevel;
            float _FinRandomness;
            float _FurHeight;
            float _FurShade;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            inline float rand(float2 seed)
            {
                return frac(sin(dot(seed.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            inline float3 rand3(float2 seed)
            {
                return 2.0 * (float3(rand(seed * 1), rand(seed * 2), rand(seed * 3)) - 0.5);
            }

            void appendFin(inout TriangleStream<g2f> stream, v2g i0, v2g i1, v2g i2) {
                float randomness = _FurHeight * _FinRandomness;
                g2f o;
            
                float4 startVertex = i0.vertex;
                float3 startNormal = i0.normal;
                float2 startUv = i0.uv;
                
                float3 endNormal = (i1.normal + i2.normal) / 2;
                float4 endVertex = (i1.vertex + i2.vertex) / 2;
                float2 endUv = (i1.uv + i2.uv) / 2;

                float uvDistance = length(startUv - endUv) * 50;

                o.vertex = UnityObjectToClipPos(startVertex);
                o.normal = startNormal;
                o.uv = startUv;
                o.finUv = float2(0 , 0);
                stream.Append(o);

                o.vertex = UnityObjectToClipPos(endVertex);
                o.normal = endNormal;
                o.uv = endUv;
                o.finUv = float2(uvDistance , 0);
                stream.Append(o);

                o.vertex = UnityObjectToClipPos(startVertex + startNormal * _FurHeight + rand3(startUv) * randomness);
                o.normal = startNormal;
                o.uv = startUv;
                o.finUv = float2(0 , 1);
                stream.Append(o);

                o.vertex = UnityObjectToClipPos(endVertex + endNormal * _FurHeight  + rand3(startUv) * randomness);
                o.normal = endNormal;
                o.uv = endUv;
                o.finUv = float2(uvDistance , 1);
                stream.Append(o);

                stream.RestartStrip();
            }

            // aX は補完係数
            v2g calcSubdividedFinPos(v2g v0, v2g v1, v2g v2, int a0, int a1, int a2) {
                v2g v;
                int asum = a0 + a1 + a2;

                v.vertex = (v0.vertex * a0 + v1.vertex * a1 + v2.vertex * a2) / asum;
                v.uv = (v0.uv * a0 + v1.uv * a1 + v2.uv * a2) / asum;
                v.normal = (v0.normal * a0 + v1.normal * a1 + v2.normal * a2) / asum;

                return v;
            }

            [maxvertexcount(48)]
            // 4枚のフィンポリゴンを三角1つに付き3つ生成し、最大2分割(level: 4)
            // 4 * 3 * 4 = 48
            void geom (triangle v2g IN[3], inout TriangleStream<g2f> stream)
            {   
                float4 triWorldPos = mul(unity_ObjectToWorld, (IN[0].vertex + IN[1].vertex + IN[2].vertex) / 3);
                float3 triNormal = (IN[0].normal + IN[1].normal + IN[2].normal) / 3;
                float cameraDistance = length(_WorldSpaceCameraPos - triWorldPos);
                
                // 毛の長さが一定未満であれば描画しない
                if (_FurHeight / cameraDistance < 0.01) return;

                float3 worldNormal = normalize(UnityObjectToWorldNormal(triNormal));
                float3 viewDir = normalize(_WorldSpaceCameraPos - triWorldPos);
                float side = abs(dot(viewDir, worldNormal));

                // 角度が一定以上はシェル法で描画するため軽量化のために描画しない
                if (side > 0.9) return;

                for (int x = 0; x < _FinSubdivisionLevel; x++)
                {
                    for (int y = 0; y < _FinSubdivisionLevel - x; y++)
                    {
                        // v0の補完係数
                        int w = _FinSubdivisionLevel - x - y;

                        v2g i0 = calcSubdividedFinPos(IN[0], IN[1], IN[2], w, x, y);
                        v2g i1 = calcSubdividedFinPos(IN[0], IN[1], IN[2], w - 1, x + 1, y);
                        v2g i2 = calcSubdividedFinPos(IN[0], IN[1], IN[2], w - 1, x, y + 1);
                        appendFin(stream, i0, i1, i2);
                        appendFin(stream, i1, i2, i0);
                        appendFin(stream, i2, i0, i1);

                        if (w >= 2) {
                            v2g i3 = calcSubdividedFinPos(IN[0], IN[1], IN[2], w - 2, x + 1, y + 1);
                            appendFin(stream, i1, i2, i3);
                            appendFin(stream, i2, i3, i1);
                            appendFin(stream, i3, i1, i2);
                        }
                    }
                }
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed fur = tex2D(_FurFinTex, i.finUv);
                // fixed4 col = tex2D(_FurFinTex, i.uv);
                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);

                return fixed4(col.rgb, fur);
            }

            ENDCG
        }
    }
}
