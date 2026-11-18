// A cave backdrop tiled from world position and shifted with the camera, so it reads as further away
// than the terrain while its box on screen stays where the room put it (CaveDepth).
Shader "Qolossal/Prototypes/KeepersNurseryLegacyDepth"
{
    Properties
    {
 _NurseryGrade("Preview brightness contrast pivot haze",Vector)=(.38,.8,.025,.035)
 _NurseryHaze("Preview cool haze",Vector)=(.018,.026,.032,1)
 _NurseryRegion("Temporary grade rectangle",Vector)=(1553,268,1671,327)
 _NurseryHighlight("Highlight knee ceiling",Vector)=(.045,.062,0,0)

        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Tile ("Tile size (u)", Vector) = (10, 10, 0, 0)
        _Rate ("Share of the camera's motion copied", Float) = 0.2
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 tile : TEXCOORD0;float2 world:TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Tile;
            float _Rate;
            float4 _CaveCamera;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color;
                o.world=mul(unity_ObjectToWorld,v.vertex).xy; o.tile = (mul(unity_ObjectToWorld, v.vertex).xy - _CaveCamera.xy * _Rate) / _Tile.xy;
                return o;
            }
            // frac wraps the tile; the gradients come from the unwrapped position, so no mip line shows at the join.
            fixed4 frag(v2f i) : SV_Target { fixed4 c=tex2Dgrad(_MainTex, frac(i.tile), ddx(i.tile), ddy(i.tile)) * i.color;c.rgb=NurseryBoundaryGrade(c.rgb,i.world);return c; }
            ENDCG
        }
    }
}
