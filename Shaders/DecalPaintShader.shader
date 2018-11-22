Shader "Hidden/MeshCanvas/DecalPaint"
{
    Properties
    {
        _PositionTex ("Position Texture", 2D) = "black" {}
        _MainTex("Brush Texture", 2D) = "black" {}
        _Color("Brush Color", Color) = (1, 1, 1, 1)
        _SmoothingMin("Smoothing Min (XYZ)", Vector) = (0.45, 0.45, 0.1, 0)
        _SmoothingMax("Smoothing Max (XYZ)", Vector) = (0.5, 0.5, 0.5, 0.5)
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
            half3 _SmoothingMin;
            half3 _SmoothingMax;

            half4 frag (v2f i) : SV_Target
            {
                half3 pos = tex2D(_PositionTex, i.uv);
                half3 decalPos = mul(_DecalMatrix, float4(pos, 1)).xyz;
                half3 absDecalPos = abs(decalPos);

                clip(_SmoothingMax - absDecalPos);

                half3 mixed = 1 - smoothstep(_SmoothingMin, _SmoothingMax, absDecalPos);
                half4 decalColor = tex2D(_MainTex, decalPos.xy + 0.5) * _Color;
                decalColor.a *= mixed.x * mixed.y * mixed.z;
                
                return decalColor;
            }
            ENDCG
        }
    }
}
