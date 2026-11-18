using UnityEngine;
using UnityEditor;
using System.Linq;
public static partial class KeepersCisternPreview {
 // Broad masonry support with a shaded return edge; a niche fits wholly on the front.
 static void NicheSupport(float x,float bottom,float top,Rect room){
  Quad("CK_Wall_Masonry",new Rect(x-1.7f,bottom,3.4f,top-bottom),room,-66,true,Vector2.one,room.center);
  var front=pieces.Last();front.t.name="Support_Limestone";Mat(front).SetFloat("_Support",1);Mat(front).SetVector("_NativeSize",new Vector4(3.4f,top-bottom,0,0));
 }
 static void Recess(Rect area,Rect room){Wall(area,room);var p=pieces.Last();p.t.name="Recess_Limestone";Mat(p).SetFloat("_Recess",1);p.r.sortingOrder=-75;}
 static void AddSharedLighting(){
  foreach(var p in pieces){
   var m=Mat(p);if(m.GetFloat("_Beam")>.5f||m.GetFloat("_Mode")>.5f||p.t.name.StartsWith("FAR")||p.t.name=="Backing")continue;
   Vector2 c=p.clip.center;Rect r=default;string name=null;
   // Select by actual room/connection domain, not x alone: upper and lower rooms overlap in x.
   for(int i=0;i<MainRooms.Length;i++)if(MainRooms[i].Contains(c)){r=MainRooms[i];name=RoomIds[i];break;}
   if(name==null)foreach(var passage in RoutePassages)if(passage.rect.Contains(c)){r=passage.rect;name=passage.name;break;}
   if(name==null)continue;
   var map=Paint("LM_"+name);if(!map){missingPaint.Add("LM_"+name);continue;}
   m.SetTexture("_RoomMap",map);m.SetVector("_RoomRect",new Vector4(r.x,r.y,r.width,r.height));m.SetFloat("_RoomLit",1);m.SetFloat("_Light",0);m.SetFloat("_RoomDay",name=="A01"?1:0);
   if(p.t.name.StartsWith("CK_Niche")||p.t.name.StartsWith("SET_"))m.SetFloat("_Emissive",1);
  }
 }
}
