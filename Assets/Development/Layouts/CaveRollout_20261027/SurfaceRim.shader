Shader "Qolossal/Prototypes/Cave Rollout Surface Rim"
{
    Properties
    {
        _MainTex ("Continuous wall painting", 2D) = "white" {}
        _EdgeMask ("Existing outer silhouette alpha", 2D) = "white" {}
        _RimUV ("Painted depth source registration", Vector) = (1,1,0,0)
        _RimTint ("Painted depth palette", Color) = (1,1,1,1)
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
            float4 _RimUV; fixed4 _RimTint;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.edge=v.edge;o.boundary=v.boundary;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.boundary); fixed4 wall=tex2D(_MainTex,i.uv)*i.color;
                // The recessed wall is a separate opaque renderer. This pass draws
                // its foreground rock rim and the solid attachment to the terrain.
                float2 rimUV=i.edge*_RimUV.xy+_RimUV.zw;
                fixed4 rim=tex2D(_EdgeMask,saturate(rimUV));
                fixed outline=rim.a;
                // Continue native terrain above the roof; only the aperture is cut out.
                // The source has transparent space above its exterior silhouette.
                // That space is solid mountain here, not a slit into the back wall.
                outline=max(outline,step(.74,i.edge.y));
                // Keep the actual painted soffit and return face. Only their solid outer
                // attachment blends into the world-registered terrain texture.
                float reach=0;
                for(int k=1;k<=10;k++){float o=k*.022;float hole=1-min(tex2D(_EdgeMask,saturate(rimUV+float2(o*_RimUV.x,0))).a,tex2D(_EdgeMask,saturate(rimUV-float2(0,o*_RimUV.y))).a);reach=max(reach,hole*(1-smoothstep(.08,.22,o)));}
                wall.rgb=lerp(wall.rgb,rim.rgb*_RimTint.rgb,saturate(reach*1.35)*rim.a*(1-smoothstep(.80,.88,i.edge.y)));
                wall.a*=outline*(1-smoothstep(.98,1,i.edge.y));
                return wall;
            }
            ENDCG
        }
    }
}







