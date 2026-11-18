Shader "Qolossal/Prototypes/KeepersCistern49" {
 Properties { _ShaftBlend("Local shaft feather",Float)=0 _ShaftBounds("Shaft segment bounds",Vector)=(0,0,0,0)  _SrcBlend("Source blend",Float)=5 _DstBlend("Destination blend",Float)=10 _RoomDay("Daylight enabled",Float)=0 _Beam("Daylight beam",Float)=0 _Soffit("Soffit",Float)=0 _WaterY("Water surface",Float)=85 _MistCentre("Mist centre",Vector)=(1583,99,21,0) _MistUpper("Mist upper",Vector)=(1588,106,12,0) _Mode("Mode",Float)=0 _Mirror("Mirror",Float)=0 _Water("Water",Float)=0 _Strength("Strength",Float)=0 _Blur("Blur",Float)=0 _MidSoft("MidSoft",Float)=0 _BasinWater("BasinWater",Float)=0 _PierReturn("PierReturn",Float)=0 _RoomLit("RoomLit",Float)=0 _Emissive("Emissive",Float)=0 _Recess("Recess",Float)=0 _Support("Support",Float)=0 _RecessArch("RecessArch",Float)=0 _RoomRect("Room coordinates",Vector)=(0,0,1,1) _NativeSize("Native size",Vector)=(1,1,0,0) _RoomMap("Shared room irradiance",2D)="white"{} _MainTex("Paint",2D)="white"{} _TideTex("High water stain",2D)="black"{} _Tide("Receive stain",Float)=0 _Clip("World clip",Vector)=(0,0,0,0) _Solid("Solid backing",Float)=0 _Light("Wall lighting",Float)=1 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off Blend [_SrcBlend] [_DstBlend]
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_TideTex,_RoomMap; float4 _Clip,_NativeSize,_MainTex_TexelSize,_RoomRect,_MistCentre,_MistUpper,_ShaftBounds; float _ShaftBlend,_Solid,_Light,_Tide,_Mode,_Mirror,_Water,_Strength,_Blur,_MidSoft,_BasinWater,_PierReturn,_RoomLit,_Emissive,_Recess,_Support,_RecessArch,_RoomDay,_Beam,_Soffit,_WaterY;
 struct app { float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct vary {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;};
 vary vert(app v){vary o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
 float pool(float2 p,float2 c,float r){float t=saturate(1-distance(p,c)/r);return t*t*(3-2*t);}
 float4 silhouette(float2 uv){float inside=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);uv.x=lerp(uv.x,1-uv.x,_Mirror);return tex2D(_MainTex,uv)*inside;}
 float4 softSample(float2 uv,float radius){float2 b=radius/max(_NativeSize.xy,float2(.01,.01));float4 c=silhouette(uv)*.20;c+=silhouette(uv+float2(b.x,0))*.12;c+=silhouette(uv-float2(b.x,0))*.12;c+=silhouette(uv+float2(0,b.y))*.12;c+=silhouette(uv-float2(0,b.y))*.12;c+=silhouette(uv+b)*.08;c+=silhouette(uv-b)*.08;c+=silhouette(uv+float2(b.x,-b.y))*.08;c+=silhouette(uv+float2(-b.x,b.y))*.08;return c;}
 fixed4 frag(vary i):SV_Target {
 clip(i.world.x-_Clip.x);clip(i.world.y-_Clip.y);clip(_Clip.z-i.world.x);clip(_Clip.w-i.world.y);
 if(_RecessArch>.5)clip(103+2.8*sqrt(saturate(1-pow((i.world.x-1636)/2.6,2)))-i.world.y);
 if(_Mode>3.5){float depth=_WaterY-i.world.y;float2 uv=float2(i.uv.x+.045*sin(depth*36+i.world.x*.7),1-i.uv.y);float4 r=softSample(uv,.10);float broken=.38+.62*smoothstep(-.35,.5,sin(depth*48+sin(i.world.x*3)));return float4(r.rgb*.60,r.a*min(.4,_Strength)*exp(-depth*1.8)*broken);}
 if(_Mode>2.5){float f=pool(i.world,_MistCentre.xy,_MistCentre.z)*(.4+.6*pool(i.world,_MistUpper.xy,_MistUpper.z));return float4(.10,.17,.18,.085*f);}
 if(_Mode>1.5){float f=1-smoothstep(0,1,i.uv.y);return float4(0,0,0,_Strength*f);}
 if(_Mode>.5)return float4(.012,.020,.021,softSample(i.uv,_Blur).a*_Strength);
 float2 uv=i.uv;uv.x=lerp(uv.x,1-uv.x,_Mirror);fixed4 c=tex2D(_MainTex,uv);if(_Solid>.5)return fixed4(.014,.027,.031,1);
 if(_MidSoft>.5){float2 d=_MainTex_TexelSize.xy*2;c=(c*4+tex2D(_MainTex,uv+float2(d.x,0))+tex2D(_MainTex,uv-float2(d.x,0))+tex2D(_MainTex,uv+float2(0,d.y))+tex2D(_MainTex,uv-float2(0,d.y)))/8;}
 float tideY=i.world.y>150?179:109;
 if(_Tide>.5&&abs(i.world.y-tideY)<=.75){fixed4 t=tex2D(_TideTex,float2((i.world.x-1490)/10,(i.world.y-tideY+.75)/1.5));c.rgb=lerp(c.rgb,t.rgb,t.a);}
 float shadow=0;
 if(_Tide>.5&&_RoomLit<.5){
  if(i.world.x>=1490&&i.world.x<=1538)shadow=.34*smoothstep(105,114,i.world.y)+.20*(1-saturate(min(i.world.x-1490,1538-i.world.x)/2));
  if(i.world.x>=1628&&i.world.x<=1644)shadow=.28*smoothstep(107,116,i.world.y)+.20*(1-saturate(min(i.world.x-1628,1644-i.world.x)/2));
  if(i.world.x>=1552&&i.world.x<=1616)shadow=.18*smoothstep(113,120,i.world.y);
 }
 c.rgb*=1-saturate(shadow);
 float warm=pool(i.world,float2(1511,107),2)+pool(i.world,float2(1546.4,108),1.8)+pool(i.world,float2(1571.9,90),2.5)+pool(i.world,float2(1590.4,100),2.5)+pool(i.world,float2(1613,108),2.2)+pool(i.world,float2(1623.4,108),1.8)+pool(i.world,float2(1640,103),2);
 float mint=pool(i.world,float2(1525,108),2)+pool(i.world,float2(1570,92),2)+pool(i.world,float2(1599,104),2)+pool(i.world,float2(1639,110),2);
 c.rgb+=_Light*(warm*float3(.055,.027,.006)+mint*float3(.005,.024,.016));
 // Local occlusion on the painted return face; follows the light-facing mirror.
 if(_PierReturn>.5){float3 front=tex2D(_MainTex,float2(.48,uv.y)).rgb;float returnBlend=smoothstep(.62,.86,uv.x);c.rgb=lerp(c.rgb,max(c.rgb,front*.78),returnBlend);if(_PierReturn>1.5){c.rgb=lerp(c.rgb,max(c.rgb,front*.82),smoothstep(.65,.73,uv.x));c.a*=1-smoothstep(.72,.85,uv.x);}}
 if(_ShaftBlend>.5){float bend=.022*sin(i.world.y*2.4)+.012*sin(i.world.y*5.7);float side=smoothstep(.035+bend,.20+bend,uv.x)*(1-smoothstep(.66+bend,.85+bend,uv.x));float join=smoothstep(0,.32,min(i.world.y-_ShaftBounds.x,_ShaftBounds.y-i.world.y));c.a*=side*join;}
 if(_Support>.5){float edge=smoothstep(3.12,3.4,uv.x*10);c.rgb*=1-.28*edge;}
 if(_Recess>.5)c.rgb*=float3(.22,.29,.32);
 if(_Soffit>.5){float ellipse=sqrt(pow((uv.x-.5)/.33,2)+pow((1-uv.y)/.46,2));float rim=1-smoothstep(.025,.09,abs(ellipse-1));c.rgb+=rim*float3(.006,.012,.013);}
 if(_RoomLit>.5){
  float3 base=c.rgb;float4 lm=tex2D(_RoomMap,saturate((i.world-_RoomRect.xy)/_RoomRect.zw));
  float diffuse=saturate(dot(base,float3(.2126,.7152,.0722))*6+.12);
  c.rgb=base*lm.b+diffuse*(lm.r*float3(.12,.064,.018)+lm.g*float3(.018,.072,.050));
  c.rgb+=_RoomDay*lm.a*diffuse*float3(.035,.052,.058);
  float flame=_Emissive*smoothstep(.25,.55,base.r)*saturate((base.r-base.g)*8);c.rgb=lerp(c.rgb,base,flame);
 }
 if(_Water>.5){float wet=1-smoothstep(_WaterY+.08,_WaterY+.27,i.world.y);c.rgb*=1-.43*wet;float meniscus=(1-smoothstep(.012,.035,abs(i.world.y-_WaterY)))*(.4+.6*pow(saturate(sin(i.world.x*17)),2));c.rgb+=float3(.025,.045,.042)*meniscus;if(i.world.y<_WaterY)c.a*=lerp(.2,.68,saturate((i.world.y-_WaterY+1)));}
 if(_BasinWater>.5)c.rgb*=1-.4*(1-smoothstep(_WaterY+.02,_WaterY+.12,i.world.y));
 if(_Beam>.5){c.rgb*=.6;c.a*=.18;}
 return c;
 }
 ENDHLSL }
 }
}
