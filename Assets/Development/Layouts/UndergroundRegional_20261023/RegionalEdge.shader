Shader "Qolossal/Review/RegionalPaintedEdge" {
Properties{_E0("Cradle rim",2D)="white"{} _E1("Causeway rim",2D)="white"{} _E2("Mill rim",2D)="white"{} _E3("Cistern rim",2D)="white"{} _E4("Roots rim",2D)="white"{} _E5("Wind rim",2D)="white"{} _E6("Descent rim",2D)="white"{} _E7("Heart rim",2D)="white"{} _T0("Cradle cap",2D)="white"{} _T1("Causeway cap",2D)="white"{} _T2("Mill cap",2D)="white"{} _T3("Cistern cap",2D)="white"{} _T4("Roots cap",2D)="white"{} _T5("Wind cap",2D)="white"{} _T6("Descent cap",2D)="white"{} _T7("Heart cap",2D)="white"{} _RepeatA("Edge repeats A",Vector)=(8,4,8,4) _RepeatB("Edge repeats B",Vector)=(8,8,8,8)}
SubShader{Tags{"Queue"="Transparent+2" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
Pass{HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A{float4 p:POSITION;float4 a:TEXCOORD0;float4 b:TEXCOORD1;float3 edge:TEXCOORD2;half4 c:COLOR;};
struct V{float4 p:SV_POSITION;float4 a:TEXCOORD0;float4 b:TEXCOORD1;float3 edge:TEXCOORD2;float2 world:TEXCOORD3;half4 c:COLOR;};
TEXTURE2D(_E0);TEXTURE2D(_E1);TEXTURE2D(_E2);TEXTURE2D(_E3);TEXTURE2D(_E4);TEXTURE2D(_E5);TEXTURE2D(_E6);TEXTURE2D(_E7);TEXTURE2D(_T0);TEXTURE2D(_T1);TEXTURE2D(_T2);TEXTURE2D(_T3);TEXTURE2D(_T4);TEXTURE2D(_T5);TEXTURE2D(_T6);TEXTURE2D(_T7);SAMPLER(sampler_E0);SAMPLER(sampler_T0);float4 _RepeatA,_RepeatB;
V vert(A a){V v;float3 w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(w);v.world=w.xy;v.a=a.a;v.b=a.b;v.edge=a.edge;v.c=a.c;return v;}
half4 edgeSample(TEXTURE2D_PARAM(e,s),TEXTURE2D_PARAM(t,st),float repeat,float tile,V v){half4 c=SAMPLE_TEXTURE2D(e,s,float2(v.edge.x/repeat,v.edge.y));half3 cap=SAMPLE_TEXTURE2D(t,st,v.world/tile).rgb;c.rgb=lerp(c.rgb,cap,v.edge.z);return c;}
#define EDGE(E,T,R,S) edgeSample(TEXTURE2D_ARGS(E,sampler_E0),TEXTURE2D_ARGS(T,sampler_T0),R,S,v)
half4 frag(V v):SV_Target{half4 c=0;float4 a=max(v.a,0),b=max(v.b,0);float total=dot(a,1)+dot(b,1);a/=max(total,.00001);b/=max(total,.00001);
if(a.x>.0001)c+=a.x*EDGE(_E0,_T0,_RepeatA.x,16);if(a.y>.0001)c+=a.y*EDGE(_E1,_T1,_RepeatA.y,4);if(a.z>.0001)c+=a.z*EDGE(_E2,_T2,_RepeatA.z,4.8984375);if(a.w>.0001)c+=a.w*EDGE(_E3,_T3,_RepeatA.w,4);if(b.x>.0001)c+=b.x*EDGE(_E4,_T4,_RepeatB.x,4.8984375);if(b.y>.0001)c+=b.y*EDGE(_E5,_T5,_RepeatB.y,4.8984375);if(b.z>.0001)c+=b.z*EDGE(_E6,_T6,_RepeatB.z,4.8984375);if(b.w>.0001)c+=b.w*EDGE(_E7,_T7,_RepeatB.w,8);return c*v.c;}
ENDHLSL}}}

