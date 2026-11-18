Shader "Qolossal/WorldRefreshCaveVista"
{
    Properties
    {
        [PerRendererData] _MainTex ("Native mineral detail", 2D) = "white" {}
        _Vista ("Single distant painting", 2D) = "black" {}
        _Extension ("Painted cavern extension", 2D) = "black" {}
        _ExtensionTile ("Native extension world size", Vector) = (6,6,0,0)
        _ExtensionTint ("Extension color match", Color) = (1,1,1,1)
        _BaseTile ("Mineral tile world size", Vector) = (8,4,0,0)
        _VistaTile ("Native painting world size", Vector) = (8,4,0,0)
        _EdgeColor ("Painted perimeter palette", Color) = (.2,.25,.2,1)
        _RoomAnchor ("Room depth anchor", Vector) = (0,0,0,0)
        _Strength ("Scenery contribution", Range(0,1)) = .78
        _CenterBelow ("Center below camera", Float) = .75
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 world:TEXCOORD0; };
            sampler2D _MainTex, _Vista, _Extension;
            float4 _ExtensionTile, _ExtensionTint;
            float4 _BaseTile, _VistaTile, _CaveCamera, _RoomAnchor, _EdgeColor;
            float _Strength, _CenterBelow, _CaveHalfHeight;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; return o;
            }
            float2 mirrorUV(float2 uv) { return 1-abs(frac(uv*.5)*2-1); }
            fixed4 frag(v2f i):SV_Target
            {
                // A single distant view drifts slightly against the room. Bounded parallax
                // keeps that one landmark visible on long routes without spawning copies.
                float2 drift=clamp((_RoomAnchor.xy-_CaveCamera.xy)*.075,float2(-1.1,-.45),float2(1.1,.45));
                float2 center=_CaveCamera.xy+drift-float2(0,_CenterBelow);
                float2 p=i.world-center;
                float2 uv=p/_VistaTile.xy+.5;

                // Continuous mineral atmosphere outside the original painting. Both detail
                // samples have native216PPU density; the second transform is orthonormal.
                // It breaks mirror symmetry in the rock, never in the narrative painting.
                float2 w=i.world-_CaveCamera.xy*.92;
                float2 rockUV=w/_ExtensionTile.xy;
                float2 rotated=float2(w.x*.7986355-w.y*.6018150,w.x*.6018150+w.y*.7986355);
                float2 rockUV2=(rotated+float2(3.17,7.39))/_ExtensionTile.xy;
                float3 a=tex2Dgrad(_Extension,mirrorUV(rockUV),ddx(rockUV),ddy(rockUV)).rgb;
                float3 b=tex2Dgrad(_Extension,mirrorUV(rockUV2),ddx(rockUV2),ddy(rockUV2)).rgb;
                float mix=.5+.35*sin(w.x*.31+w.y*.27+.7*sin(w.x*.57-w.y*.21));
                float3 field=lerp(a,b,mix)*_ExtensionTint.rgb*.68;

                // Sample the entire native painting ONCE, upright, without frac/mirror or
                // coordinate warping. Its softly blended stone perimeter joins the cavern.
                float3 scene=tex2Dgrad(_Vista,saturate(uv),ddx(uv),ddy(uv)).rgb*.68;
                float2 edge=min(uv,1-uv);
                float blend=smoothstep(0,.18,edge.x)*smoothstep(0,.21,edge.y);
                float halfHeight=_CaveHalfHeight>0?_CaveHalfHeight:5;
                float overview=1-smoothstep(8,18,halfHeight);
                float3 result=lerp(field,scene,blend*saturate(_Strength+.15)*overview);
                return fixed4(result,1)*i.color;
            }
            ENDCG
        }
    }
}
