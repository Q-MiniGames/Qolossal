Shader "Qolossal/WorldRevision/Approved Rim Native Attachment"
{
    Properties
    {
        _MainTex ("Native outer terrain", 2D) = "white" {}
        _EdgeMask ("Approved painted soffit and inward wall", 2D) = "white" {}
        _RimUV ("Approved source registration", Vector) = (1,1,0,0)
        _RimTint ("Approved inward palette", Color) = (1,1,1,1)
        _PixelsPerUnit ("Native outer pixels per world unit", Float) = 360
        _NativeTextureSize ("Native outer source dimensions", Vector) = (1254,1254,0,0)
        _AttachmentBlend ("Back attachment blend in edge coordinates", Float) = .085
        _OpaqueRoofStart ("Optional outer closure start; above1 preserves full approved alpha", Float) = .74
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
            sampler2D _MainTex, _EdgeMask;
            float4 _RimUV, _NativeTextureSize; fixed4 _RimTint;
            float _PixelsPerUnit, _AttachmentBlend, _OpaqueRoofStart;
            struct appdata { float4 vertex:POSITION; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 worldXY:TEXCOORD0; float2 edge:TEXCOORD1; float boundary:TEXCOORD2; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.worldXY=mul(unity_ObjectToWorld,v.vertex).xy;o.edge=v.edge;o.boundary=v.boundary;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                clip(i.boundary);
                float2 worldUV=i.worldXY*_PixelsPerUnit/max(_NativeTextureSize.xy,float2(1,1));
                fixed4 wall=tex2D(_MainTex,worldUV)*i.color;
                float2 rimUV=i.edge*_RimUV.xy+_RimUV.zw;
                fixed4 rim=tex2D(_EdgeMask,saturate(rimUV));
                fixed outline=max(rim.a,step(_OpaqueRoofStart,i.edge.y));
                // Preserve the approved October28 painted underside and inward-facing
                // facets. Only their solid outer attachment uses native terrain Fill.
                float reach=0;
                for(int k=1;k<=10;k++)
                {
                    float o=k*.022;
                    float hole=1-min(tex2D(_EdgeMask,saturate(rimUV+float2(o*_RimUV.x,0))).a,tex2D(_EdgeMask,saturate(rimUV-float2(0,o*_RimUV.y))).a);
                    reach=max(reach,hole*(1-smoothstep(.08,.22,o)));
                }
                wall.rgb=lerp(wall.rgb,rim.rgb*_RimTint.rgb,saturate(reach*1.35)*rim.a*(1-smoothstep(.80,.88,i.edge.y)));
                wall.a*=outline*(1-smoothstep(.98,1,i.edge.y));
                // The final .45 u lies beyond the carved doorway and blends into
                // existing terrain instead of terminating as a pasted rectangle.
                wall.a*=smoothstep(0,max(.0001,_AttachmentBlend),i.edge.x);
                return wall;
            }
            ENDCG
        }
    }
}

