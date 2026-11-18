// Water light playing on the walls above a pool (AmbientLife): added light, strongest at the waterline, gone by the top.
Shader "Qolossal/AmbientCaustic"
{
    Properties { _Tint ("Tint", Color) = (.55, .75, 1, .04) }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Tint;
            float _AmbientTime;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; };
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.world = mul(unity_ObjectToWorld, v.vertex).xy; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _AmbientTime;
                // Two warped wave fronts crossing at an angle: where they meet, a soft thread of light.
                float2 p = i.world + float2(sin(i.world.y * 1.3 + t * .4), sin(i.world.x * 1.1 - t * .3)) * .55;
                float a = sin(p.x * 1.9 + p.y * 1.2 + t * .7);
                float b = sin(p.x * 1.4 - p.y * 1.8 - t * .5 + 1.3);
                float web = pow(saturate(1 - abs(a + b) * .6), 2);
                float edges = smoothstep(0, .12, i.uv.x) * smoothstep(1, .88, i.uv.x);
                return fixed4(_Tint.rgb, _Tint.a * web * edges * (1 - i.uv.y) * (1 - i.uv.y));
            }
            ENDCG
        }
    }
}
