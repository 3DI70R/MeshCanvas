Shader "Hidden/MeshCanvas/DecalPaint"
{
    Properties
    {
        _PositionTex ("Position", 2D) = "black" {}
        _MainTex("Brush Tex", 2D) = "black" {}
        _Color("Brush Color", Color) = (1, 1, 1, 1)
        _Skew("Skew", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _PositionTex;
            sampler2D _MainTex;
            float4x4 _DecalMatrix;
            half4 _Color;
            half3 _Skew;

            half4 frag (v2f i) : SV_Target
            {
                float3 pos = tex2D(_PositionTex, i.uv);
                float3 decalPos = mul(_DecalMatrix, float4(pos, 1)).xyz;
                decalPos += _Skew * decalPos.z;
                
                clip (float3(0.5,0.5,0.5) - abs(decalPos));

                half4 decalColor = tex2D(_MainTex, decalPos.xy + 0.5) * _Color;
                decalColor.a *= 1 - abs(decalPos.z * 2);
                
                return decalColor;
            }
            ENDCG
        }
    }
}
