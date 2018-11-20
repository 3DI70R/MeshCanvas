Shader "Hidden/MeshCanvas/PositionBake"
{
    Properties { }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
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
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
                o.vertex = float4(v.uv.x * 2 - 1, 1 - v.uv.y * 2, 0, 1);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.worldPos.xyz, 1);
            }
            ENDCG
        }
    }
}
