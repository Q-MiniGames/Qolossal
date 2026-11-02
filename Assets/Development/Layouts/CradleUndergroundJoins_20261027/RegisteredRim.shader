Shader "Qolossal/Prototypes/Registered Cave Rim"
{
 Properties { _MainTex("Registered regional stone",2D)="white"{} _Rim("Painted inner silhouette",2D)="white"{} }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 rim:TEXCOORD1;half4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;float2 rim:TEXCOORD2;half4 color:COLOR;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);TEXTURE2D(_Rim);SAMPLER(sampler_Rim);
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   V vert(A a){V v;float3 w=TransformObjectToWorld(a.p.xyz);v.p=TransformWorldToHClip(w);v.world=w.xy;v.uv=a.uv;v.color=a.color;v.rim=a.rim;return v;}
   half4 frag(V v):SV_Target
   {
    // Translated samples retain painted pixel density; aperiodic weights break 16u repetition.
    half4 a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);
    half4 b=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv+float2(.371,.619));
    float mix=smoothstep(.32,.68,noise(v.world*.193+17.3));
    half4 c=lerp(a,b,mix)*v.color;c.rgb*=lerp(.91,1.06,noise(v.world*.071-8.2));half4 rim=SAMPLE_TEXTURE2D(_Rim,sampler_Rim,v.rim);
    // Local relief only along the painted aperture, never at the terrain attachment.
    half edge=1-min(SAMPLE_TEXTURE2D(_Rim,sampler_Rim,saturate(v.rim+float2(.035,0))).a,SAMPLE_TEXTURE2D(_Rim,sampler_Rim,saturate(v.rim-float2(0,.035))).a);
    c.rgb=lerp(c.rgb,rim.rgb*.72,edge*.65);c.a*=rim.a;return c;
   }
   ENDHLSL
  }
 }
}
