Shader "Qolossal/WorldRevision/Native Regional Painted Edge" {
Properties{_E0("Cradle rim",2D)="white"{} _E1("Causeway rim",2D)="white"{} _E2("Mill rim",2D)="white"{} _E3("Cistern rim",2D)="white"{} _E4("Roots rim",2D)="white"{} _E5("Wind rim",2D)="white"{} _E6("Descent rim",2D)="white"{} _E7("Heart rim",2D)="white"{} _T0("Cradle cap",2D)="white"{} _T1("Causeway cap",2D)="white"{} _T2("Mill cap",2D)="white"{} _T3("Cistern cap",2D)="white"{} _T4("Roots cap",2D)="white"{} _T5("Wind cap",2D)="white"{} _T6("Descent cap",2D)="white"{} _T7("Heart cap",2D)="white"{} _NativeEdgeScaleA("Native edge scale A",Vector)=(1,1,1,1) _NativeEdgeScaleB("Native edge scale B",Vector)=(1,1,1,1) _RepeatA("Edge repeats A",Vector)=(8,4,8,4) _RepeatB("Edge repeats B",Vector)=(8,8,8,8)}
SubShader{Tags{"Queue"="Transparent+2" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
Pass{HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 p:POSITION;float4 a:TEXCOORD0;float4 b:TEXCOORD1;float3 edge:TEXCOORD2;half4 c:COLOR;};
struct V{float4 p:SV_POSITION;float4 a:TEXCOORD0;float4 b:TEXCOORD1;float3 edge:TEXCOORD2;float2 world:TEXCOORD3;half4 c:COLOR;};
TEXTURE2D(_E0);TEXTURE2D(_E1);TEXTURE2D(_E2);TEXTURE2D(_E3);TEXTURE2D(_E4);TEXTURE2D(_E5);TEXTURE2D(_E6);TEXTURE2D(_E7);TEXTURE2D(_T0);TEXTURE2D(_T1);TEXTURE2D(_T2);TEXTURE2D(_T3);TEXTURE2D(_T4);TEXTURE2D(_T5);TEXTURE2D(_T6);TEXTURE2D(_T7);SAMPLER(sampler_E0);SAMPLER(sampler_T0);float4 _RepeatA,_RepeatB,_NativeEdgeScaleA,_NativeEdgeScaleB;
V vert(A a){V v;float3 w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(w);v.world=w.xy;v.a=a.a;v.b=a.b;v.edge=a.edge;v.c=a.c;return v;}
// Match the surrounding RegionalUndergroundRock cap exactly, including
// periodic seam blending and low-frequency world lighting.
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
half3 periodic(TEXTURE2D_PARAM(t,samp),float2 uv){float2 f=frac(uv);float2 w=smoothstep(0,.08,min(f,1-f));return SAMPLE_TEXTURE2D(t,samp,uv).rgb*w.x*w.y+SAMPLE_TEXTURE2D(t,samp,uv+float2(.5,0)).rgb*(1-w.x)*w.y+SAMPLE_TEXTURE2D(t,samp,uv+float2(0,.5)).rgb*w.x*(1-w.y)+SAMPLE_TEXTURE2D(t,samp,uv+.5).rgb*(1-w.x)*(1-w.y);}
// Keep the original outer edge at source y=1. Scale source coordinates,
// not geometry; all joined meshes share the same per-family registration.
half4 edgeSample(TEXTURE2D_PARAM(e,s),TEXTURE2D_PARAM(t,st),float repeat,float tile,float nativeScale,V v){
 float2 uv=float2(v.edge.x/repeat*nativeScale,1+(v.edge.y-1)*nativeScale);
 half4 c=SAMPLE_TEXTURE2D(e,s,uv);
 half3 cap;
 if(tile>15.9)cap=lerp(SAMPLE_TEXTURE2D(t,st,v.world/16).rgb,SAMPLE_TEXTURE2D(t,st,v.world/16+float2(.371,.619)).rgb,smoothstep(.32,.68,noise(v.world*.193+17.3)));
 else cap=periodic(TEXTURE2D_ARGS(t,st),v.world/tile);
 cap*=lerp(.91,1.06,noise(v.world*.071-8.2));
 // The deeper strip is actual native rock, never a stretched clamped edge row.
 float inner=1-smoothstep(0,.08,uv.y);
 float capMix=max(saturate(v.edge.z),inner);
 c.rgb=lerp(c.rgb,cap,capMix);c.a=lerp(c.a,1,inner);
 return c;
}
#define EDGE(E,T,R,S,N) edgeSample(TEXTURE2D_ARGS(E,sampler_E0),TEXTURE2D_ARGS(T,sampler_T0),R,S,N,v)
half4 frag(V v):SV_Target{half4 c=0;float4 a=max(v.a,0),b=max(v.b,0);float total=dot(a,1)+dot(b,1);a/=max(total,.00001);b/=max(total,.00001);
if(a.x>.0001)c+=a.x*EDGE(_E0,_T0,_RepeatA.x,16,_NativeEdgeScaleA.x);if(a.y>.0001)c+=a.y*EDGE(_E1,_T1,_RepeatA.y,4,_NativeEdgeScaleA.y);if(a.z>.0001)c+=a.z*EDGE(_E2,_T2,_RepeatA.z,4.8984375,_NativeEdgeScaleA.z);if(a.w>.0001)c+=a.w*EDGE(_E3,_T3,_RepeatA.w,4,_NativeEdgeScaleA.w);if(b.x>.0001)c+=b.x*EDGE(_E4,_T4,_RepeatB.x,4.8984375,_NativeEdgeScaleB.x);if(b.y>.0001)c+=b.y*EDGE(_E5,_T5,_RepeatB.y,4.8984375,_NativeEdgeScaleB.y);if(b.z>.0001)c+=b.z*EDGE(_E6,_T6,_RepeatB.z,4.8984375,_NativeEdgeScaleB.z);if(b.w>.0001)c+=b.w*EDGE(_E7,_T7,_RepeatB.w,8,_NativeEdgeScaleB.w);return c*v.c;}
ENDHLSL}}}

