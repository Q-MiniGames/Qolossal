#ifndef QOLOSSAL_CISTERN_NETWORK
#define QOLOSSAL_CISTERN_NETWORK
// One world registration across rooms AND passages. No room camera anchors,
// repetition of narrative paintings, viewport clamp, or reset at a doorway.
UNITY_DECLARE_TEX2DARRAY(_CisternFar);
UNITY_DECLARE_TEX2DARRAY(_CisternMiddle);
float4 _CisternNetworkFarRects[16], _CisternMiddleRects[4];
float4 _CisternNetworkDomains[16];
float4 _CisternEnclosures[16];
float _CisternEnclosureCount, _CisternLayered;
float4 _CisternPivot;

float CisternPanelWeight(float2 uv)
{
 float2 edge=min(uv,1-uv);
 float2 center=abs(uv-.5)*2;
 return smoothstep(0,.14,edge.x)*smoothstep(0,.14,edge.y)*pow(max(.01,1-max(center.x,center.y)),3);
}
float CisternSegmentDistance(float2 p,float4 segment)
{
 float2 d=segment.zw-segment.xy;
 return length(p-segment.xy-d*saturate(dot(p-segment.xy,d)/max(.001,dot(d,d))));
}
float3 CisternNetworkPainting(float2 world)
{
 float2 farPosition=world-(_CaveCamera.xy-_CisternPivot.xy)*float2(.55,.45);
 float3 sum=0;float weight=0;float coverage=0;
 [unroll] for(int i=0;i<8;i++)
 {
  float4 rect=_CisternNetworkFarRects[i];float2 uv=(farPosition-rect.xy)/rect.zw+.5;
  float4 domain=_CisternNetworkDomains[i];
  float distanceToDomain=length(max(max(domain.xy-world,world-domain.zw),0));
  float w=CisternPanelWeight(uv)*exp2(-distanceToDomain*.65);
  [branch] if(w>.00001) {sum+=UNITY_SAMPLE_TEX2DARRAY(_CisternFar,float3(saturate(uv),i)).rgb*w;weight+=w;coverage=max(coverage,smoothstep(0,.035,min(min(uv.x,1-uv.x),min(uv.y,1-uv.y))));}
 }
 // Above or below a composed opening, the vault closes into continuous stone.
 // Only this non-narrative mineral texture repeats; landmarks never repeat.
 float3 vault=UNITY_SAMPLE_TEX2DARRAY(_CisternFar,float3(frac(farPosition/5.6),8)).rgb*float3(.65,.85,.84);
 float3 color=lerp(vault,sum/max(weight,.00001),coverage);
 color=CaveGrade(color,float3(.1556,.2437,.2022));
 if(_CisternLayered>.5)
 {
  float2 middlePosition=world-(_CaveCamera.xy-_CisternPivot.xy)*float2(.12,.06);
  [unroll] for(int j=0;j<4;j++)
  {
   float4 rect=_CisternMiddleRects[j];float2 uv=(middlePosition-rect.xy)/rect.zw+.5;
   [branch] if(all(uv>0)&&all(uv<1))
   {
    float4 detail=UNITY_SAMPLE_TEX2DARRAY(_CisternMiddle,float3(uv,j));
    color=lerp(color,CaveGrade(detail.rgb,float3(.1556,.2437,.2022)),detail.a);
   }
  }
  // Enclosed connecting tunnels have an attached rear wall. The irregular
  // opening reveals the SAME world-registered scene on either side.
  float distance=10000;
  [loop] for(int k=0;k<(int)_CisternEnclosureCount;k++)
   distance=min(distance,CisternSegmentDistance(world,_CisternEnclosures[k]));
  float2 stoneUV=world/5.6;
  float3 stone=UNITY_SAMPLE_TEX2DARRAY(_CisternFar,float3(frac(stoneUV),8)).rgb;
  float rough=(stone.r-stone.b)*1.8+.22*sin(world.x*2.1+world.y*1.3);
  float wall=1-smoothstep(2.0,4.0,distance+rough);
  color=lerp(color,CaveGrade(stone,float3(.1556,.2437,.2022))*.82,wall);
 }
 return color;
}
#endif
