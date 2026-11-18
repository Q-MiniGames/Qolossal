// A cave backdrop tiled from world position and shifted with the camera, so it reads as further away
// than the terrain while its box on screen stays where the room put it (CaveDepth).
Shader "Qolossal/CaveDepthBackdrop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Tile ("Tile size (u)", Vector) = (10, 10, 0, 0)
        _Rate ("Share of the camera's motion copied", Float) = 0.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 tile : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _Tile;
            float _Rate;
            float4 _CaveCamera;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color;
                o.tile = (mul(unity_ObjectToWorld, v.vertex).xy - _CaveCamera.xy * _Rate) / _Tile.xy;
                return o;
            }
            // frac wraps the tile; the gradients come from the unwrapped position, so no mip line shows at the join.
            fixed4 frag(v2f i) : SV_Target { return tex2Dgrad(_MainTex, frac(i.tile), ddx(i.tile), ddy(i.tile)) * i.color; }
            ENDCG
        }
    }
}
