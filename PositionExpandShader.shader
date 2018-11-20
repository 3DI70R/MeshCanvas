Shader "Hidden/MeshCanvas/PositionExpand"
{
    Properties
    {
        _MainTex ("World Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            #define sampleCount 12
            static float2 texelPositions[sampleCount] = 
            {
                float2(-2, -2),
                float2(0, -2),
                float2(2, -2),
                float2(-1, -1),
                float2(1, -1),
                float2(-2, 0),
                float2(2, 0),
                float2(-1, 1),
                float2(1, 1),
                float2(-2, 2),
                float2(0, 2),
                float2(2, 2)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                if(col.a > 0.5) 
                {
                    return col;
                }
                
                fixed count = 0;
                fixed3 resultColor = fixed3(0, 0, 0);
                
                for (int t = 0; t < sampleCount; t++)
                {
                    fixed4 blend = tex2D(_MainTex, i.uv.xy + _MainTex_TexelSize.xy * texelPositions[t]);
                    
                    if(blend.a > 0.5) 
                    {
                        count++;
                        resultColor += blend.rgb;
                    }
                }

                return fixed4(resultColor.rgb / count, step(0, count));
            }
            ENDCG
        }
    }
}
