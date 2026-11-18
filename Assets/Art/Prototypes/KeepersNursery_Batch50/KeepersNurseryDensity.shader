Shader "Qolossal/Prototypes/KeepersNurseryDensity" {
 Properties { _BuiltMask("Measured built material mask",2D)="white"{} _PanelRect("Panel rectangle",Vector)=(0,0,1,1) _Feather("Native panel transition",Float)=0 _JoinFeather("Column overlap",Float)=0 _MainTex("Paint",2D)="white"{} _Clip("Clip",Vector)=(0,0,0,0) _Wall("Wall",Float)=0 _Built("Built coverage upper bound",Float)=0 _FarAmbient("Distant ambient",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _BuiltMask,_MainTex; float _Feather,_JoinFeather; float4 _PanelRect; float4 _Clip; float _Wall,_Built;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
 vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
 float4 frag(vary i):SV_Target {clip(i.world.x-_Clip.x);clip(i.world.y-_Clip.y);clip(_Clip.z-i.world.x);clip(_Clip.w-i.world.y);return float4(_Built*tex2D(_BuiltMask,i.uv).r,0,0,tex2D(_MainTex,i.uv).a);}
 ENDHLSL }
 }
}
