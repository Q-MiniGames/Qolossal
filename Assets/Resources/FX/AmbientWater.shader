// Moving water (AmbientLife): the painted sheet is sampled through a slow wobble and scrolled along its flow, so a
// still strip or fall runs without new frames, plus glints. The art's bytes and the hazard are unchanged.
Shader "Qolossal/AmbientWater"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Flow ("Tiles per second", Vector) = (0.03, 0, 0, 0)
        _Wobble ("Wobble (share of a tile)", Float) = 0.012
        _Glint ("Glints", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnitySprites.cginc"
            float4 _Flow;
            float _Wobble, _Glint, _AmbientTime;
            struct wave { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; };
            wave vert(appdata_t v)
            {
                wave o;
                float4 world = mul(unity_ObjectToWorld, UnityFlipSprite(v.vertex.xyz, _Flip));
                o.vertex = mul(UNITY_MATRIX_VP, world);
                o.uv = v.texcoord; o.world = world.xy;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }
            fixed4 frag(wave i) : SV_Target
            {
                float t = _AmbientTime;
                float2 uv = i.uv + _Flow.xy * t;
                uv.x += _Wobble * (sin(i.world.y * 3.1 + t * 1.7) + sin(i.world.x * 1.3 + t * 1.1));
                uv.y += _Wobble * sin(i.world.x * 2.3 + t * 1.9);
                // frac wraps the tile; gradients come from the unwrapped uv, so no mip line shows at the join.
                uv = float2(frac(uv.x), _Flow.y != 0 ? frac(uv.y) : saturate(uv.y));
                fixed4 c = tex2Dgrad(_MainTex, uv, ddx(i.uv), ddy(i.uv)) * i.color;
                float glint = sin(i.world.x * 7 + t * 2.3) * sin(i.world.x * 3.1 - t * 1.7 + i.world.y * 5);
                float patch = saturate(sin(i.world.x * .83 - t * .37) * sin(i.world.x * .31 + t * .23 + 1.7) * 2);
                c.rgb += saturate(glint - .8) * 3 * patch * _Glint * float3(.75, .85, .95);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
