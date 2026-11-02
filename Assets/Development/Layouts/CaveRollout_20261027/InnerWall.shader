Shader "Qolossal/Prototypes/Cave Rollout Inner Wall"
{
    Properties
    {
        _MainTex ("Continuous wall painting", 2D) = "white" {}
        _EdgeMask ("Existing outer silhouette alpha", 2D) = "white" {}
        _WallTint ("Interior value and palette", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _EdgeMask;
            fixed4 _WallTint;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.edge=v.edge;o.boundary=v.boundary;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.boundary); fixed4 wall=tex2D(_MainTex,i.uv)*i.color*_WallTint;
                // Mask only the exposed outer contour. The whole inner wall stays opaque;
                // there is no second RGB painting and no vertical crossfade.
                fixed outline=i.edge.x<1 ? tex2D(_EdgeMask,saturate(i.edge)).a : 1;
                wall.a*=outline;
                return wall;
            }
            ENDCG
        }
    }
}

