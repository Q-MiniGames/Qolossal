Shader "Qolossal/Review/RegionalUndergroundRock"
{
 Properties{_T0("Cradle",2D)="white"{} _T1("Causeway",2D)="white"{} _T2("Mill",2D)="white"{} _T3("Cistern",2D)="white"{} _T4("Roots",2D)="white"{} _T5("Wind",2D)="white"{} _T6("Descent",2D)="white"{} _T7("Heart",2D)="white"{} _RepeatA("Native repeat A",Vector)=(16,4,4.8984375,4) _RepeatB("Native repeat B",Vector)=(4.8984375,4.8984375,4.8984375,8) }
 SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass{HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float4 w0:TEXCOORD0;float4 w1:TEXCOORD1;half4 c:COLOR;};
 struct V{float4 p:SV_POSITION;float2 world:TEXCOORD0;float4 w0:TEXCOORD1;float4 w1:TEXCOORD2;half4 c:COLOR;};
 TEXTURE2D(_T0);TEXTURE2D(_T1);TEXTURE2D(_T2);TEXTURE2D(_T3);TEXTURE2D(_T4);TEXTURE2D(_T5);TEXTURE2D(_T6);TEXTURE2D(_T7);SAMPLER(sampler_T0);float4 _RepeatA,_RepeatB;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 V vert(A a){V v;float3 w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(w);v.world=w.xy;v.w0=a.w0;v.w1=a.w1;v.c=a.c;return v;}
 half3 periodic(TEXTURE2D_PARAM(t,samp),float2 uv){float2 f=frac(uv);float2 w=smoothstep(0,.08,min(f,1-f));return SAMPLE_TEXTURE2D(t,samp,uv).rgb*w.x*w.y+SAMPLE_TEXTURE2D(t,samp,uv+float2(.5,0)).rgb*(1-w.x)*w.y+SAMPLE_TEXTURE2D(t,samp,uv+float2(0,.5)).rgb*w.x*(1-w.y)+SAMPLE_TEXTURE2D(t,samp,uv+.5).rgb*(1-w.x)*(1-w.y);}
 #define ROCK(T,U) periodic(TEXTURE2D_ARGS(T,sampler_T0),U)
 half4 frag(V v):SV_Target{float mix=smoothstep(.32,.68,noise(v.world*.193+17.3));float4 a=max(v.w0,0),b=max(v.w1,0);float total=dot(a,1)+dot(b,1);a/=max(total,.00001);b/=max(total,.00001);half3 c=0;
 if(a.x>.0001)c+=a.x*lerp(SAMPLE_TEXTURE2D(_T0,sampler_T0,v.world/16).rgb,SAMPLE_TEXTURE2D(_T0,sampler_T0,v.world/16+float2(.371,.619)).rgb,mix);if(a.y>.0001)c+=a.y*ROCK(_T1,v.world/_RepeatA.y);if(a.z>.0001)c+=a.z*ROCK(_T2,v.world/_RepeatA.z);if(a.w>.0001)c+=a.w*ROCK(_T3,v.world/_RepeatA.w);
 if(b.x>.0001)c+=b.x*ROCK(_T4,v.world/_RepeatB.x);if(b.y>.0001)c+=b.y*ROCK(_T5,v.world/_RepeatB.y);if(b.z>.0001)c+=b.z*ROCK(_T6,v.world/_RepeatB.z);if(b.w>.0001)c+=b.w*ROCK(_T7,v.world/_RepeatB.w);
 c*=v.c.rgb*lerp(.91,1.06,noise(v.world*.071-8.2));return half4(c,v.c.a);}
 ENDHLSL}
 }
}

