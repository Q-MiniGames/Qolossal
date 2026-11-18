Shader "Qolossal/WorldRefreshCaveVista"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original rock field", 2D) = "white" {}
        _Vista ("Native scenic panel", 2D) = "black" {}
        _BaseTile ("Rock tile world size", Vector) = (8,4,0,0)
        _VistaTile ("Scenic tile world size", Vector) = (8,4,0,0)
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
            sampler2D _MainTex, _Vista;
            float4 _BaseTile, _VistaTile, _CaveCamera;
            float _Strength, _CenterBelow, _CaveHalfHeight;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; return o;
            }
            float2 mirrorUV(float2 uv) { return 1-abs(frac(uv*.5)*2-1); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 groundUV=(i.world-_CaveCamera.xy*.30)/_BaseTile.xy;
                fixed4 ground=tex2Dgrad(_MainTex,mirrorUV(groundUV),ddx(groundUV),ddy(groundUV));
                // A continuous painted cavern field fills the room. The first band treatment
                // read as a floating bright stripe; native-density tiling avoids that border.
                float2 uv=(i.world-_CaveCamera.xy*.30)/_VistaTile.xy;
                // Keep waterfalls and buildings upright. Uneven soft rock occlusion hides
                // the vertical repeat join without stretching or warping the native painting.
                float column=floor(uv.x);
                float phase=frac(sin(column*12.9898+19.13)*43758.5453)*.77;
                float row=floor(uv.y+phase);
                float2 sampleUV=float2(mirrorUV(uv).x,frac(uv.y+phase));
                fixed4 scene=tex2Dgrad(_Vista,sampleUV,ddx(uv),ddy(uv));
                float verticalOffset=(i.world.y-_CaveCamera.y)/5;
                float depth=.55+.12*(1-saturate(verticalOffset*verticalOffset));
                scene.rgb*=depth;
                float lower=.04+.12*(.5+.5*sin(uv.x*3.7+row*1.7));
                float upper=.84+.12*(.5+.5*cos(uv.x*4.3+row*2.8+1.2));
                float opening=smoothstep(lower,lower+.20,sampleUV.y)*(1-smoothstep(upper-.20,upper,sampleUV.y));
                float x=frac(uv.x);
                opening*=smoothstep(0,.12,x)*(1-smoothstep(.88,1,x));
                // Overview / Heart look views retain the quiet native rock field, avoiding
                // dozens of tiny repeated scenic windows competing with the story focal point.
                float halfHeight=_CaveHalfHeight>0?_CaveHalfHeight:5;
                float overview=1-smoothstep(6,12,halfHeight);
                return lerp(ground,scene,opening*_Strength*overview)*i.color;
            }
            ENDCG
        }
    }
}
