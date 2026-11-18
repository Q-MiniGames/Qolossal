Shader "Qolossal/Prototypes/KeepersNurseryLegacyPassage"
{
 Properties
 {_NurseryGrade("Preview brightness contrast pivot haze",Vector)=(.38,.8,.025,.035)
 _NurseryHaze("Preview cool haze",Vector)=(.018,.026,.032,1)
 _NurseryRegion("Temporary grade rectangle",Vector)=(1553,268,1671,327)
 _NurseryHighlight("Highlight knee ceiling",Vector)=(.045,.062,0,0)

  
 _NetworkEnabled ("Source connected Cistern review",Float)=0
 _B_NetworkEnabled ("Destination connected Cistern review",Float)=0
 _Middle ("Source middle-distance painting",2D)="black"{}
 _MiddleRect ("Source middle center and size",Vector)=(0,0,1,1)
 _MiddleParallax ("Source middle camera follow",Vector)=(0,0,0,0)
 _MiddleEnabled ("Source middle enabled",Float)=0
 _B_Middle ("Destination middle-distance painting",2D)="black"{}
 _B_MiddleRect ("Destination middle center and size",Vector)=(0,0,1,1)
 _B_MiddleParallax ("Destination middle camera follow",Vector)=(0,0,0,0)
 _B_MiddleEnabled ("Destination middle enabled",Float)=0
 [PerRendererData] _MainTex ("Sprite",2D)="white"{}
 _Vista ("_Vista",2D)="black"{}
 _Left ("_Left",2D)="black"{}
 _Right ("_Right",2D)="black"{}
 _Vault ("_Vault",2D)="black"{}
 _Extension ("_Extension",2D)="black"{}
 _B_Vista ("_B_Vista",2D)="black"{}
 _B_Left ("_B_Left",2D)="black"{}
 _B_Right ("_B_Right",2D)="black"{}
 _B_Vault ("_B_Vault",2D)="black"{}
 _B_Extension ("_B_Extension",2D)="black"{}
 _Continuous ("Optional source continuous painting",2D)="black"{}
 _ContinuousRect ("Source shared painting center and size",Vector)=(0,0,1,1)
 _ContinuousEnabled ("Source continuous review enabled",Float)=0
 _B_Continuous ("Optional destination continuous painting",2D)="black"{}
 _B_ContinuousRect ("Destination shared painting center and size",Vector)=(0,0,1,1)
 _B_ContinuousEnabled ("Destination continuous review enabled",Float)=0
  _EdgeColor ("_EdgeColor",Color)=(1,1,1,1)
  _ExtensionTint ("_ExtensionTint",Color)=(1,1,1,1)
  _B_EdgeColor ("_B_EdgeColor",Color)=(1,1,1,1)
  _B_ExtensionTint ("_B_ExtensionTint",Color)=(1,1,1,1)
  _Parallax ("Optional source room camera follow",Vector)=(0,0,0,0)
  _ParallaxPivot ("Source reference camera",Vector)=(0,0,0,0)
  _B_Parallax ("Optional destination room camera follow",Vector)=(0,0,0,0)
  _B_ParallaxPivot ("Destination reference camera",Vector)=(0,0,0,0)
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
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

   
   #include "Assets/Resources/FX/CaveVistaSampling.cginc"
   CAVE_SET(_)
   CAVE_SET(_B_)
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 world:TEXCOORD0;};
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
   float distanceToRoom(float2 p,float4 rect){return length(max(max(rect.xy-p,p-rect.zw),0));}
   fixed4 frag(v2f i):SV_Target
   {
    // Exactly the adjoining room's painting at either mouth. Smooth transition
    // through horizontal, vertical and bent passages, with no black separator.
    float a=distanceToRoom(i.world,_RoomRect),b=distanceToRoom(i.world,_B_RoomRect);
    float t=smoothstep(0,1,a/max(.0001,a+b));
    float3 color=lerp(SAMPLE_CAVE(_,i.world),SAMPLE_CAVE(_B_,i.world),t);
    fixed4 c=fixed4(color,1)*i.color;c.rgb=NurseryBoundaryGrade(c.rgb,i.world);return c;
   }
   ENDCG
  }
 }
}
