Shader "Qolossal/WorldRefreshCaveVista"
{
 Properties
 {
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
   #pragma target 3.0
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 world:TEXCOORD0;};
   #include "CaveVistaSampling.cginc"
   CAVE_SET(_)
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.world=mul(unity_ObjectToWorld,v.vertex).xy;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    return fixed4(SAMPLE_CAVE(_,i.world),1)*i.color;
   }
   ENDCG
  }
 }
}

