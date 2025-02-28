Shader "Custom/ParticleBillboard"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Size("Particle Size", Range(0.01, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Size;

            // Vertex Shader: Simply Passes Data to Geometry Shader
            appdata_t vert(appdata_t v)
            {
                return v;
            }

            // Geometry Shader: Expands Each Point into a Quad
            [maxvertexcount(4)]
            void geom(point appdata_t inputPoint[1], inout TriangleStream<v2f> triStream)
            {
                v2f o;

                // Particle world position
                float3 worldPos = mul(unity_ObjectToWorld, inputPoint[0].vertex).xyz;
                
                // Get camera-facing billboard axes
                float3 right = UNITY_MATRIX_V[0].xyz; // Camera right
                float3 up = UNITY_MATRIX_V[1].xyz;    // Camera up

                // Quad vertex offsets & UVs
                float2 offsets[4] = { float2(-0.5, -0.5), float2(0.5, -0.5), float2(-0.5, 0.5), float2(0.5, 0.5) };
                float2 uvs[4] = { float2(0,0), float2(1,0), float2(0,1), float2(1,1) };

                for (int i = 0; i < 4; i++)
                {
                    float3 quadVertexPos = worldPos + right * offsets[i].x * _Size + up * offsets[i].y * _Size;
                    o.pos = UnityWorldToClipPos(float4(quadVertexPos, 1.0));
                    o.uv = uvs[i];
                    o.color = inputPoint[0].color;
                    triStream.Append(o);
                }
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}