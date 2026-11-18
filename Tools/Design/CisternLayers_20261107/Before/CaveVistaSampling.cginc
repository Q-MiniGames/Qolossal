#ifndef QOLOSSAL_CAVE_VISTA_SAMPLING
#define QOLOSSAL_CAVE_VISTA_SAMPLING
float4 _CaveCamera;
float _CaveHalfHeight;

#define CAVE_SET(P) \
 sampler2D P##Vista,P##Left,P##Right,P##Vault,P##Extension,P##Continuous; \
 float4 P##VistaTile,P##LeftTile,P##RightTile,P##VaultTile,P##ExtensionTile; \
 float4 P##RoomAnchor,P##RoomRect,P##EdgeColor,P##ExtensionTint,P##Parallax,P##ParallaxPivot; \
 float4 P##ContinuousRect; \
 float P##CenterBelow,P##HasDepth,P##ContinuousEnabled;

#define SAMPLE_CAVE(P,W) CavePainting(W,P##Vista,P##Left,P##Right,P##Vault,P##Extension, \
 P##VistaTile.xy,P##LeftTile.xy,P##RightTile.xy,P##VaultTile.xy,P##ExtensionTile.xy, \
 P##RoomAnchor.xy,P##EdgeColor.rgb,P##ExtensionTint.rgb,P##CenterBelow,P##HasDepth,P##Parallax,P##ParallaxPivot.xy, \
 P##Continuous,P##ContinuousRect,P##ContinuousEnabled)

float2 CaveHash(float2 p)
{
 return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);
}
float3 CavePerimeter(sampler2D source,float2 uv,float3 tint)
{
 float2 cell=floor(uv), f=frac(uv);f=f*f*(3-2*f);
 float2 dx=ddx(uv),dy=ddy(uv);
 float3 a=tex2Dgrad(source,uv+CaveHash(cell),dx,dy).rgb;
 float3 b=tex2Dgrad(source,uv+CaveHash(cell+float2(1,0)),dx,dy).rgb;
 float3 c=tex2Dgrad(source,uv+CaveHash(cell+float2(0,1)),dx,dy).rgb;
 float3 d=tex2Dgrad(source,uv+CaveHash(cell+1),dx,dy).rgb;
 return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y)*tint;
}
float CaveWeight(float2 uv,float2 size)
{
 float2 edge=min(uv,1-uv)*size;
 float ripple=.10*sin(uv.x*19+uv.y*13)+.07*sin(uv.y*27-uv.x*7);
 return smoothstep(.04,.62,edge.x+ripple)*smoothstep(.04,.62,edge.y-ripple);
}
float3 CaveGrade(float3 rgb,float3 ambient)
{
 float gray=dot(rgb,float3(.299,.587,.114));
 return lerp(ambient*.82,lerp(gray.xxx,rgb,.76),.85)*.53;
}
float3 CavePainting(float2 world,sampler2D vista,sampler2D left,sampler2D right,sampler2D vault,sampler2D extension,
 float2 vistaSize,float2 leftSize,float2 rightSize,float2 vaultSize,float2 extensionSize,
 float2 anchor,float3 ambient,float3 tint,float below,float hasDepth,float4 parallax,float2 pivot,
 sampler2D continuous,float4 continuousRect,float continuousEnabled)
{
 // Explicit local art trial: one painting, one registration and one grade for
 // both rooms and passage. No texture-wall fade or room-to-room depth reset.
 if(continuousEnabled>.5)
 {
  float2 offset=parallax.z>.5?(_CaveCamera.xy-pivot)*parallax.xy:float2(0,0);
  // Keep landmarks registered to the cavern, including during a glide. Moving
  // the artwork to follow the viewport at its edges made flight feel stationary.
  float2 artCenter=continuousRect.xy+offset;
  float2 uv=(world-artCenter)/continuousRect.zw+.5;
  return CaveGrade(tex2Dgrad(continuous,saturate(uv),ddx(uv),ddy(uv)).rgb,float3(.1556,.2437,.2022));
 }
 // Stable room registration: 35% horizontal and 12% vertical terrain speed.
 // Tall climbing rooms retain a distant vista without pinning it to the screen.
 // The surrounding stone uses the same coordinate system.
 float2 center=anchor+(_CaveCamera.xy-anchor)*float2(.65,.88)-float2(0,below);
 // Opt-in comparison: preserve the composition at a shared reference camera.
 // Default z=0 leaves every existing room and passage exactly as authored.
 if(parallax.z>.5)
  center=anchor+(pivot-anchor)*float2(.65,.88)+(_CaveCamera.xy-pivot)*parallax.xy-float2(0,below);
 float2 p=world-center;
 float2 uv=p/vistaSize+.5;
 float2 leftUV=(p-float2(-5.65,1.4))/leftSize+.5;
 float2 rightUV=(p-float2(5.65,1.4))/rightSize+.5;
 float2 vaultUV=(p-float2(0,3.3))/vaultSize+.5;
 float wc=CaveWeight(uv,vistaSize),wl=CaveWeight(leftUV,leftSize)*hasDepth;
 float wr=CaveWeight(rightUV,rightSize)*hasDepth,wv=CaveWeight(vaultUV,vaultSize)*hasDepth;
 float3 a=tex2Dgrad(vista,saturate(uv),ddx(uv),ddy(uv)).rgb;
 float3 b=tex2Dgrad(left,saturate(leftUV),ddx(leftUV),ddy(leftUV)).rgb;
 float3 c=tex2Dgrad(right,saturate(rightUV),ddx(rightUV),ddy(rightUV)).rgb;
 float3 d=tex2Dgrad(vault,saturate(vaultUV),ddx(vaultUV),ddy(vaultUV)).rgb;
 float sum=wc+wl+wr+wv;
 float3 painting=(a*wc+b*wl+c*wr+d*wv)/max(.0001,sum);
 float3 rock=CavePerimeter(extension,p/extensionSize,tint);
 float halfHeight=_CaveHalfHeight>0?_CaveHalfHeight:5;
 float coverage=saturate(sum*2)*(1-smoothstep(10,24,halfHeight));
 return lerp(CaveGrade(rock,ambient)*.65,CaveGrade(painting,ambient),coverage);
}
#endif
