Shader "Qolossal/WorldRevision/Native Walking Join"
{
    Properties
    {
        _MainTex ("Native walking painting", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WorldBounds ("World X min/max, blend width", Vector) = (0,1,.3,0)
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
            sampler2D _MainTex; fixed4 _Color; float4 _WorldBounds;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float worldX:TEXCOORD1; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.worldX=mul(unity_ObjectToWorld,v.vertex).x;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color*_Color;
                float distanceToEnd=min(i.worldX-_WorldBounds.x,_WorldBounds.y-i.worldX);
                c.a*=smoothstep(0,max(.001,_WorldBounds.z),distanceToEnd);
                return c;
            }
            ENDCG
        }
    }
}
