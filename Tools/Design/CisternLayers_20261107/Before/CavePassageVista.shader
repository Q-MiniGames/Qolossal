Shader "Qolossal/CavePassageVista"
{
 Properties
 {
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
   #pragma target 3.0
   #include "UnityCG.cginc"
   #include "CaveVistaSampling.cginc"
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
    return fixed4(color,1)*i.color;
   }
   ENDCG
  }
 }
}
