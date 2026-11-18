Shader "Qolossal/Prototypes/KeepersNursery50" {
 Properties { _PoolStrength("Multiplicative pool exposure",Float)=2.2 _PoolEnabled("Local pools enabled",Float)=1  _HighlightRoll("Linear highlight knee and ceiling",Vector)=(.045,.062,0,0)  _GradeEnabled("R5 grade enabled",Float)=1 _Grade("Brightness Contrast Pivot Haze",Vector)=(1,1,0,0) _GradeHaze("Cool cave haze sRGB",Vector)=(.018,.026,.032,1) _PoolPreserve("Pool preservation",Float)=1  _BuiltMask("Measured built material mask",2D)="white"{} _PanelRect("Panel rectangle",Vector)=(0,0,1,1) _Feather("Native panel transition",Float)=0 _JoinFeather("Column overlap",Float)=0 _MainTex("Paint",2D)="white"{} _RoomMap("Irradiance",2D)="white"{} _LilacMap("Lilac irradiance",2D)="black"{} _LilacEnabled("Lilac enabled",Float)=0 _QA("QA mode",Float)=0 _Clip("Clip",Vector)=(0,0,0,0) _RoomRect("Light map domain",Vector)=(0,0,1,1) _Lit("Lit",Float)=1 _Emissive("Emissive",Float)=0 _Wall("Wall",Float)=0 _Built("Built coverage upper bound",Float)=0 _FarAmbient("Distant ambient",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _BuiltMask,_MainTex,_RoomMap,_LilacMap; float _GradeEnabled,_PoolPreserve,_PoolStrength,_PoolEnabled;float4 _Grade,_GradeHaze,_HighlightRoll;float _Feather,_JoinFeather; float4 _PanelRect; float4 _Clip,_RoomRect; float _Lit,_Emissive,_Wall,_LilacEnabled,_QA,_Built,_FarAmbient;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
 vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
 float4 frag(vary i):SV_Target {
 clip(i.world.x-_Clip.x);clip(i.world.y-_Clip.y);clip(_Clip.z-i.world.x);clip(_Clip.w-i.world.y);
 float4 c=tex2D(_MainTex,i.uv);if(_Feather>0){float2 edge=min(i.world-_PanelRect.xy,_PanelRect.xy+_PanelRect.zw-i.world);c.a*=smoothstep(0,_Feather,min(edge.x,edge.y));}if(_JoinFeather>0)c.a*=smoothstep(0,_JoinFeather,min(i.world.y-_PanelRect.y,_PanelRect.y+_PanelRect.w-i.world.y));if(_QA>1.5)return float4(1,1,1,c.a);if(_QA>.5)return float4(_Built*tex2D(_BuiltMask,i.uv).r,0,0,c.a);
 // Slow continuous world variation breaks material repetition without placement-sized bands.
 c.rgb*=1-_Wall*(.10+.09*sin(i.world.x*.39+i.world.y*.17)+.06*cos(i.world.x*.19-i.world.y*.31));
 if(_Lit>.5)c.rgb*=tex2D(_RoomMap,saturate((i.world-_RoomRect.xy)/_RoomRect.zw)).b;
 c.rgb*=_FarAmbient;if(_FarAmbient>4)c.rgb*=lerp(1,.79,smoothstep(283,291,i.world.y));if(_GradeEnabled>.5){float3 original=c.rgb;float3 display=LinearToGammaSpace(max(original,0));float3 compressed=max(0,display*_Grade.y+_Grade.z*(1-_Grade.y))*_Grade.x;compressed=lerp(compressed,_GradeHaze.rgb,_Grade.w);float2 tuv=saturate((i.world-_RoomRect.xy)/_RoomRect.zw);float4 pool=tex2D(_RoomMap,tuv);float3 weights=smoothstep(.015,.40,float3(pool.r,pool.g,_LilacEnabled*tex2D(_LilacMap,tuv).r));float3 exposure=weights.x*float3(1.2,.85,.35)+weights.y*float3(.35,1,.65)+weights.z*float3(.8,.45,1);c.rgb=GammaToLinearSpace(compressed)*exp2(_PoolStrength*_PoolEnabled*exposure);float Y=dot(c.rgb,float3(.2126,.7152,.0722));float headroom=1+2*_PoolEnabled*max(weights.x,max(weights.y,weights.z));float knee=_HighlightRoll.x*headroom;float span=max(.0001,(_HighlightRoll.y-_HighlightRoll.x)*headroom);float bounded=knee+span*(1-exp(-max(0,Y-knee)/span));if(Y>knee)c.rgb*=bounded/max(Y,.000001);}return c;
 }
 ENDHLSL }
 }
}

