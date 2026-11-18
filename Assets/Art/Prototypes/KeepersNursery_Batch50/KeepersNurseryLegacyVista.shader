Shader "Qolossal/Prototypes/KeepersNurseryLegacyVista"
{
 Properties
 {
 _NurseryGrade("Preview brightness contrast pivot haze",Vector)=(.38,.8,.025,.035)
 _NurseryHaze("Preview cool haze",Vector)=(.018,.026,.032,1)
 _NurseryRegion("Temporary grade rectangle",Vector)=(1553,268,1671,327)
 _NurseryHighlight("Highlight knee ceiling",Vector)=(.045,.062,0,0)

  _NetworkEnabled ("Connected Cistern review",Float)=0
  _Middle ("Optional middle-distance painting",2D)="black"{}
  _MiddleRect ("Shared middle painting center and size",Vector)=(0,0,1,1)
  _MiddleParallax ("Middle camera follow",Vector)=(0,0,0,0)
  _MiddleEnabled ("Middle-distance review enabled",Float)=0
  [PerRendererData] _MainTex ("Original native mineral detail",2D)="white"{}
  _Vista ("Central distant painting",2D)="black"{}
  _Left ("Unique left cavern",2D)="black"{}
  _Right ("Unique right cavern",2D)="black"{}
  _Vault ("Unique upper vault",2D)="black"{}
  _Extension ("Painted perimeter stone",2D)="black"{}
  _Continuous ("Optional continuous review painting",2D)="black"{}
  _ContinuousRect ("Shared painting center and size",Vector)=(0,0,1,1)
  _ContinuousEnabled ("Continuous review enabled",Float)=0
  _VistaTile ("Central native world size",Vector)=(8,4,0,0)
  _LeftTile ("Left native world size",Vector)=(4,7,0,0)
  _RightTile ("Right native world size",Vector)=(4,7,0,0)
  _VaultTile ("Vault native world size",Vector)=(8,4,0,0)
  _ExtensionTile ("Perimeter native world size",Vector)=(6,6,0,0)
  _BaseTile ("Original tile size",Vector)=(8,4,0,0)
  _EdgeColor ("Ambient stone color",Color)=(.2,.25,.2,1)
  _ExtensionTint ("Perimeter color match",Color)=(1,1,1,1)
  _RoomAnchor ("Room depth anchor",Vector)=(0,0,0,0)
  _Parallax ("Optional camera follow override",Vector)=(0,0,0,0)
  _ParallaxPivot ("Comparison reference camera",Vector)=(0,0,0,0)
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
   #pragma target 3.5
   #include "UnityCG.cginc"

 float4 _NurseryGrade,_NurseryHaze,_NurseryRegion,_NurseryHighlight;
 float3 NurseryBoundaryGrade(float3 original,float2 world){
  float2 outside=max(_NurseryRegion.xy-world,world-_NurseryRegion.zw);
  float weight=1-smoothstep(0,4,max(outside.x,outside.y));
  float3 display=LinearToGammaSpace(max(original,0));
  float3 compressed=max(0,display*_NurseryGrade.y+_NurseryGrade.z*(1-_NurseryGrade.y))*_NurseryGrade.x;
  float3 rgb=GammaToLinearSpace(lerp(compressed,_NurseryHaze.rgb,_NurseryGrade.w));
  float Y=dot(rgb,float3(.2126,.7152,.0722));float span=max(.0001,_NurseryHighlight.y-_NurseryHighlight.x);
  float bounded=_NurseryHighlight.x+span*(1-exp(-max(0,Y-_NurseryHighlight.x)/span));
  if(Y>_NurseryHighlight.x)rgb*=bounded/max(Y,.000001);
  return lerp(original,rgb,weight);
 }

   struct appdata {float4 vertex:POSITION;float4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 world:TEXCOORD0;};
   #include "Assets/Resources/FX/CaveVistaSampling.cginc"
   CAVE_SET(_)
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    fixed4 c=fixed4(SAMPLE_CAVE(_,i.world),1)*i.color;c.rgb=NurseryBoundaryGrade(c.rgb,i.world);return c;
   }
   ENDCG
  }
 }
}

