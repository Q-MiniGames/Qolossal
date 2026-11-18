Shader "Qolossal/WorldRefresh/Native Fitted Stone"
{
    Properties
    {
        _MainTex ("Native tiled stone", 2D) = "white" {}
        _WallTint ("Interior value", Color) = (1,1,1,1)
        _UseBoundary ("Retain authored terrain cut", Float) = 0
        _PixelsPerUnit ("Native source pixels per world unit", Float) = 360
        _NativeTextureSize ("Native source size (independent of platform import)", Vector) = (1254,1254,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _WallTint; float _UseBoundary, _PixelsPerUnit; float4 _NativeTextureSize;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 worldXY:TEXCOORD0; float boundary:TEXCOORD1; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.worldXY=mul(unity_ObjectToWorld,v.vertex).xy;o.boundary=v.boundary;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                if(_UseBoundary>.5)clip(i.boundary);
                // Derive UV after world-position interpolation. Tiny clipped triangles
                // no longer amplify independently rounded baked UV/position differences.
                float2 uv=i.worldXY*_PixelsPerUnit/max(_NativeTextureSize.xy,float2(1,1));
                return tex2D(_MainTex,uv)*i.color*_WallTint;
            }
            ENDCG
        }
    }
}
