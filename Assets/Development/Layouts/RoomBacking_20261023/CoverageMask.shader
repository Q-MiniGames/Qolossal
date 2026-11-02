Shader "Qolossal/Review/BackingCoverageMask" {
 Properties { _MainTex("Alpha",2D)="white"{} _UseAlpha("Use painted alpha",Float)=0 }
 SubShader { Tags{"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half a:COLOR;};
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float _UseAlpha;
 V vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv;o.a=v.c.a;return o;}
 half4 frag(V i):SV_Target{return half4(1,1,1,i.a*lerp(1,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a,_UseAlpha));}
 ENDHLSL }
 }
}
