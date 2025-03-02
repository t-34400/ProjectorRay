Shader "Custom/ProjectorRayRender"
{
    Properties
    {
        _ProjectionTex ("Projection Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct VertexData
            {
                float3 worldPos;
                float depth;
                uint valid;
            };

            StructuredBuffer<VertexData> _VertexBuffer;
            sampler2D _ProjectionTex;
            int _TexWidth;
            int _TexHeight;

            struct g2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2g
            {
                float2 cellCoord : TEXCOORD0;
            };

            v2g vert(appdata v)
            {
                v2g o;
                int cellCountX = _TexWidth - 1;
                int i = v.vertexID % cellCountX;
                int j = v.vertexID / cellCountX;
                o.cellCoord = float2(i, j);
                return o;
            }

            [maxvertexcount(6)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                int i = (int)input[0].cellCoord.x;
                int j = (int)input[0].cellCoord.y;

                int index00 = j * _TexWidth + i;
                int index10 = j * _TexWidth + (i + 1);
                int index01 = (j + 1) * _TexWidth + i;
                int index11 = (j + 1) * _TexWidth + (i + 1);

                VertexData v00 = _VertexBuffer[index00];
                VertexData v10 = _VertexBuffer[index10];
                VertexData v01 = _VertexBuffer[index01];
                VertexData v11 = _VertexBuffer[index11];

                if(v00.valid == 0 || v10.valid == 0 || v01.valid == 0 || v11.valid == 0)
                    return;

                float4 clip00 = UnityWorldToClipPos(v00.worldPos);
                float4 clip10 = UnityWorldToClipPos(v10.worldPos);
                float4 clip01 = UnityWorldToClipPos(v01.worldPos);
                float4 clip11 = UnityWorldToClipPos(v11.worldPos);

                float2 uv00 = float2(i, j) / float2(_TexWidth - 1, _TexHeight - 1);
                float2 uv10 = float2(i + 1, j) / float2(_TexWidth - 1, _TexHeight - 1);
                float2 uv01 = float2(i, j + 1) / float2(_TexWidth - 1, _TexHeight - 1);
                float2 uv11 = float2(i + 1, j + 1) / float2(_TexWidth - 1, _TexHeight - 1);

                g2f o;

                o.pos = clip00; o.uv = uv00; triStream.Append(o);
                o.pos = clip01; o.uv = uv01; triStream.Append(o);
                o.pos = clip10; o.uv = uv10; triStream.Append(o);

                o.pos = clip10; o.uv = uv10; triStream.Append(o);
                o.pos = clip11; o.uv = uv11; triStream.Append(o);
                o.pos = clip01; o.uv = uv01; triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_ProjectionTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
