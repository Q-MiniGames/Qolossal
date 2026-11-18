Shader "Qolossal/WorldRefreshCaveVista"
{
 Properties
 {
  [PerRendererData] _MainTex ("Original native mineral detail",2D)="white"{}
  _Vista ("Central distant painting",2D)="black"{}
  _Left ("Unique left cavern",2D)="black"{}
  _Right ("Unique right cavern",2D)="black"{}
  _Vault ("Unique upper vault",2D)="black"{}
  _Extension ("Painted perimeter stone",2D)="black"{}
  _VistaTile ("Central native world size",Vector)=(8,4,0,0)
  _LeftTile ("Left native world size",Vector)=(4,7,0,0)
  _RightTile ("Right native world size",Vector)=(4,7,0,0)
  _VaultTile ("Vault native world size",Vector)=(8,4,0,0)
  _ExtensionTile ("Perimeter native world size",Vector)=(6,6,0,0)
  _BaseTile ("Original tile size",Vector)=(8,4,0,0)
  _EdgeColor ("Ambient stone color",Color)=(.2,.25,.2,1)
  _ExtensionTint ("Perimeter color match",Color)=(1,1,1,1)
  _RoomAnchor ("Room depth anchor",Vector)=(0,0,0,0)
  _Strength ("Scenery contribution",Range(0,1))=.78
  _CenterBelow ("Composition below camera",Float)=.75
  _HasDepth ("Unique surrounding paintings present",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
  Cull Off Lighting Off ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 world:TEXCOORD0;};
   sampler2D _Vista,_Left,_Right,_Vault,_Extension;
   float4 _VistaTile,_LeftTile,_RightTile,_VaultTile,_ExtensionTile;
   float4 _CaveCamera,_RoomAnchor,_EdgeColor,_ExtensionTint;
   float _CenterBelow,_Strength,_HasDepth,_CaveHalfHeight;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
   float2 hash2(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
   float3 perimeter(float2 uv)
   {
    // Four independently shifted upright native samples: no triangle-wave mirror,
    // rotation symmetry, diamonds or repetitive narrative landmarks.
    float2 cell=floor(uv), f=frac(uv);f=f*f*(3-2*f);
    float2 dx=ddx(uv),dy=ddy(uv);
    float3 a=tex2Dgrad(_Extension,uv+hash2(cell),dx,dy).rgb;
    float3 b=tex2Dgrad(_Extension,uv+hash2(cell+float2(1,0)),dx,dy).rgb;
    float3 c=tex2Dgrad(_Extension,uv+hash2(cell+float2(0,1)),dx,dy).rgb;
    float3 d=tex2Dgrad(_Extension,uv+hash2(cell+1),dx,dy).rgb;
    return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y)*_ExtensionTint.rgb;
   }
   float weight(float2 uv,float2 size)
   {
    // Nonrectangular overlaps affect only blending, never distort native source UVs.
    float2 edge=min(uv,1-uv)*size;
    float ripple=.10*sin(uv.x*19+uv.y*13)+.07*sin(uv.y*27-uv.x*7);
    return smoothstep(.04,.62,edge.x+ripple)*smoothstep(.04,.62,edge.y-ripple);
   }
   float3 distantGrade(float3 rgb)
   {
    float gray=dot(rgb,float3(.299,.587,.114));
    return lerp(_EdgeColor.rgb*.82,lerp(gray.xxx,rgb,.76),.85)*.53;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float2 drift=clamp((_RoomAnchor.xy-_CaveCamera.xy)*.035,float2(-.35,-.25),float2(.35,.25));
    float2 center=_CaveCamera.xy+drift-float2(0,_CenterBelow);
    float2 p=i.world-center;
    float2 uv=p/_VistaTile.xy+.5;
    float2 leftUV=(p-float2(-5.65,1.4))/_LeftTile.xy+.5;
    float2 rightUV=(p-float2(5.65,1.4))/_RightTile.xy+.5;
    float2 vaultUV=(p-float2(0,3.3))/_VaultTile.xy+.5;
    float wc=weight(uv,_VistaTile.xy);
    float wl=weight(leftUV,_LeftTile.xy)*_HasDepth;
    float wr=weight(rightUV,_RightTile.xy)*_HasDepth;
    float wv=weight(vaultUV,_VaultTile.xy)*_HasDepth;
    float3 centerPaint=tex2Dgrad(_Vista,saturate(uv),ddx(uv),ddy(uv)).rgb;
    float3 leftPaint=tex2Dgrad(_Left,saturate(leftUV),ddx(leftUV),ddy(leftUV)).rgb;
    float3 rightPaint=tex2Dgrad(_Right,saturate(rightUV),ddx(rightUV),ddy(rightUV)).rgb;
    float3 vaultPaint=tex2Dgrad(_Vault,saturate(vaultUV),ddx(vaultUV),ddy(vaultUV)).rgb;
    float sum=wc+wl+wr+wv;
    float3 painting=(centerPaint*wc+leftPaint*wl+rightPaint*wr+vaultPaint*wv)/max(.0001,sum);
    float3 rock=perimeter((i.world-_CaveCamera.xy*.94)/_ExtensionTile.xy);
    float halfHeight=_CaveHalfHeight>0?_CaveHalfHeight:5;
    float coverage=saturate(sum*2)*(1-smoothstep(10,24,halfHeight));
    float3 result=lerp(distantGrade(rock)*.5,distantGrade(painting),coverage);
    return fixed4(result,1)*i.color;
   }
   ENDCG
  }
 }
}

