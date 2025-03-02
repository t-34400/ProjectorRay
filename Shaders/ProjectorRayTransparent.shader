Shader "Custom/ProjectorRayRenderTransparent"
{
    Properties
    {
        _ProjectionTex ("Projection Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ProjectionTex;
            float _DepthFactor;

            struct g2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 shaderParams : TEXCOORD1;
            };

            struct v2g
            {
                float4 clipPos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
                bool valid : TEXCOORD2;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.clipPos = UnityObjectToClipPos(v.vertex);

                o.depth = v.shaderParams.x;
                o.valid = (v.shaderParams.y > 0.5);

                o.uv = v.uv;
                
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                if (!input[0].valid || !input[1].valid || !input[2].valid)
                    return;

                g2f o;
                o.pos = input[0].clipPos;
                o.uv = input[0].uv;
                o.depth = input[0].depth;
                triStream.Append(o);

                o.pos = input[1].clipPos;
                o.uv = input[1].uv;
                o.depth = input[1].depth;
                triStream.Append(o);

                o.pos = input[2].clipPos;
                o.uv = input[2].uv;
                o.depth = input[2].depth;
                triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_ProjectionTex, i.uv);
                col.a *= 1.0 - ((1 - i.depth) * (1 - i.depth) * _DepthFactor);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
