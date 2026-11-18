Shader "Qolossal/WorldRefreshHeartSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original quarter reference",2D)="white"{}
        _Detail ("Native surface detail",2D)="white"{}
        _DetailTile ("Detail world size",Vector)=(4,4,0,0)
        _QuarterFlip ("Quarter orientation",Vector)=(0,0,0,0)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
            sampler2D _Detail;float4 _DetailTile,_QuarterFlip;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                // Analytical ellipse: no magnified bitmap silhouette. The existing quarter flips
                // retain the same four-sector heart and native SpriteRenderer color drives its pulse.
                // SpriteRenderer already supplies the mirrored quarter orientation in UVs.
                float2 quarterUV=i.uv;
                float d=length(quarterUV);float edge=max(fwidth(d),.0001);
                float alpha=1-smoothstep(1-edge,1+edge,d);
                float2 uv=i.world/_DetailTile.xy;float2 mirrored=1-abs(frac(uv*.5)*2-1);
                fixed3 detail=tex2Dgrad(_Detail,mirrored,ddx(uv),ddy(uv)).rgb;
                // A second, orthonormally rotated native sample breaks mirrored ornaments.
                // Rotation preserves the exact216PPU scale; neither sample is enlarged.
                float2 rotated=float2(.79863551*uv.x-.60181502*uv.y,.60181502*uv.x+.79863551*uv.y)+float2(.37,.19);
                float2 rotatedMirror=1-abs(frac(rotated*.5)*2-1);
                fixed3 alternate=tex2Dgrad(_Detail,rotatedMirror,ddx(rotated),ddy(rotated)).rgb;
                detail=lerp(detail,alternate,.45);
                float volume=.55+.45*sqrt(saturate(1-d*d));
                return fixed4(detail*volume*i.color.rgb,alpha*i.color.a);
            }
            ENDCG
        }
    }
}
