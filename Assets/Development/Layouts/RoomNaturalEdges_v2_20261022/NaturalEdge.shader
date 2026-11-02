Shader "Qolossal/Review/NaturalEdgeCaps"
{
 Properties { _MainTex("Painted edge",2D)="white"{} _CapTex("Unbent capstones",2D)="white"{} }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float2 cap:TEXCOORD1;float2 mix:TEXCOORD2;half4 color:COLOR;};
 struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 cap:TEXCOORD1;float mix:TEXCOORD2;half4 color:COLOR;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);TEXTURE2D(_CapTex);SAMPLER(sampler_CapTex);
 V vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.cap=a.cap;v.mix=a.mix.x;v.color=a.color;return v;}
 half4 frag(V v):SV_Target {half4 edge=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv);half3 cap=SAMPLE_TEXTURE2D(_CapTex,sampler_CapTex,v.cap).rgb;edge.rgb=lerp(edge.rgb,cap,saturate(v.mix));return edge*v.color;}
 ENDHLSL }
 }
}
