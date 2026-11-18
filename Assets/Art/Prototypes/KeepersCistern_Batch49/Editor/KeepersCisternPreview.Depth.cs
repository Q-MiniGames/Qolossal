using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

public static partial class KeepersCisternPreview {
 static readonly Vector2[] DepthLights={new(1511.05f,107),new(1546.65f,108),new(1572.25f,90),new(1590.65f,100),new(1613.55f,108),new(1623.65f,108),new(1640.85f,103),new(1504,112),new(1517.8f,113.3f),new(1575.6f,112.5f),new(1590,115),new(1606.5f,116),new(1484.25f,173.2f),new(1512.65f,180.2f),new(1541.95f,174.2f),new(1541.95f,150.2f),new(1541.95f,134.2f),new(1640.95f,123.2f),new(1556.75f,162.2f),new(1573.45f,175.2f),new(1611.75f,177.2f),new(1630.25f,177.2f),new(1667.25f,176.2f),new(1703.15f,172.8f),new(1716.8f,173.5f),new(1703.6f,182.8f),new(1716.6f,183.8f),new(1640.66f,137.7f),new(1642.25f,134.77f),new(1567.87f,162.94f),new(1568f,160f),new(1599.44f,161.58f),new(1599.5f,157.35f),new(1605.03f,156.12f)};
 static Vector2 ClosestLight(Vector2 p)=>DepthLights.OrderBy(l=>(l-p).sqrMagnitude).First();
 static Material Mat(Piece p)=>p.r.sharedMaterial;
 static Piece Effect(Piece source,string label,Rect rect,int order,float mode){
  var go=new GameObject(label){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(rect.center.x,rect.center.y,0);
  var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave};float w=rect.width/2,h=rect.height/2;
  mesh.vertices=new[]{new Vector3(-w,-h),new Vector3(w,-h),new Vector3(w,h),new Vector3(-w,h)};
  float px=(rect.width-source.rect.width)/2/source.rect.width,py=(rect.height-source.rect.height)/2/source.rect.height;
  mesh.uv=new[]{new Vector2(-px,-py),new Vector2(1+px,-py),new Vector2(1+px,1+py),new Vector2(-px,1+py)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
  var m=new Material(Mat(source)){hideFlags=HideFlags.HideAndDontSave};m.SetFloat("_Mode",mode);m.SetFloat("_Light",0);m.SetFloat("_Tide",0);m.SetFloat("_Water",0);m.SetFloat("_Solid",0);m.SetVector("_NativeSize",new Vector4(source.rect.width,source.rect.height,0,0));owned.Add(m);
  var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=m;renderer.sortingOrder=order;
  var p=new Piece{t=go.transform,basis=go.transform.position,anchor=source.anchor,motion=source.motion,r=renderer,near=source.near,rect=rect,clip=source.clip};pieces.Add(p);return p;
 }
 static void AddDepthPass(){
  // The near reservoir lip has an actual painted top/return face and submerged courses.
  Quad("CK_Basin_Submerged",new Rect(1552,83,64,2),A04,-68,true,Vector2.one,A04.center);
  Quad("CK_Basin_Coping",new Rect(1552,85,64,.5f),A04,-67,true,Vector2.one,A04.center);
  Mat(pieces.Last()).SetFloat("_BasinWater",1);
  // Local soft gradient BETWEEN the far and the near; no full-frame tint.
  Quad("Backing",new Rect(1554,84,60,35),A04,-78,true,Vector2.one,A04.center,true);
  var mist=pieces.Last();mist.t.name="DepthMist";Mat(mist).SetFloat("_Mode",3);Mat(mist).SetFloat("_Solid",0);
  Quad("Backing",new Rect(1554,154,60,32),A06,-78,true,Vector2.one,A06.center,true);
  var engineMist=pieces.Last();engineMist.t.name="DepthMist_A06";Mat(engineMist).SetFloat("_Mode",3);Mat(engineMist).SetFloat("_Solid",0);Mat(engineMist).SetVector("_MistCentre",new Vector4(1584,165,24,0));Mat(engineMist).SetVector("_MistUpper",new Vector4(1584,171,14,0));
  foreach(var p in pieces.ToArray()){
   string n=p.t.name;
   if(n.StartsWith("MID"))Mat(p).SetFloat("_MidSoft",1);
   bool pier=n.StartsWith("CK_Column");
   bool support=n.StartsWith("Support_");bool stone=pier||support||n.StartsWith("CK_Mouth")||n.StartsWith("CK_Recess")||n.StartsWith("CK_Niche")||n.StartsWith("CK_Window")||n.StartsWith("CK_Vault");
   if(!stone)continue;
   Vector2 centre=p.rect.center;
   var light=ClosestLight(centre);var away=centre-light;
   bool ambient=away.magnitude>10;
   if(n.StartsWith("CK_Niche"))p.r.sortingOrder=-61;
   if(pier){float mirror=light.x>centre.x?1:0;Mat(p).SetFloat("_Mirror",mirror);Mat(p).SetFloat("_PierReturn",2);if(centre.x>1552&&centre.x<1616)Mat(p).SetFloat("_Water",1);}
   if(support)Mat(p).SetFloat("_Water",1);
   if(pier&&p.clip.yMin>=154&&p.clip.xMin>=1554&&p.clip.xMax<=1614){Mat(p).SetFloat("_Water",1);Mat(p).SetFloat("_WaterY",154.6f);Mat(p).SetFloat("_PierReturn",2);}
   if(pier&&p.clip.xMin>=1528&&p.clip.xMax<=1544){Mat(p).SetFloat("_PierReturn",2);Mat(p).SetFloat("_ShaftBlend",1);Mat(p).SetVector("_ShaftBounds",new Vector4(p.clip.yMin,p.clip.yMax,0,0));}
   // Shadow is a blurred sample of this piece's own alpha, never its rectangular canvas.
   Vector2 dir=ambient?Vector2.zero:new Vector2(Mathf.Sign(away.x),-.25f).normalized;
   Rect r=p.rect;r.position+=dir*.12f;r.xMin-=.6f;r.xMax+=.6f;r.yMin-=.6f;r.yMax+=.6f;
   var shadow=Effect(p,"Shadow_"+n,r,p.r.sortingOrder-1,1);Mat(shadow).SetFloat("_Strength",.30f);Mat(shadow).SetFloat("_Blur",.42f);
   // Cropped segment shafts retain their real alpha at water contact.
   if((pier||support)&&p.clip.yMin<=85&&p.clip.yMax>85&&centre.x>1552&&centre.x<1616){
    r=p.rect;r.center=new Vector2(r.center.x,170-r.center.y);
    var refl=Effect(p,"Reflection_"+n,r,-56,4);Mat(refl).SetVector("_Clip",new Vector4(1552,84,1616,85));Mat(refl).SetFloat("_Blur",.10f);Mat(refl).SetFloat("_Strength",.32f);
   }
  }
  AddTerrainContact();
 }
 static void AddTerrainContact(){
  var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();var proof=new List<object>();
  Rect bounds=new Rect(1479,67,276,132);
  foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None)){
   if(!c.enabled||c.isTrigger||c.gameObject.layer!=LayerMask.NameToLayer("Ground")||!c.gameObject.activeInHierarchy)continue;
   if(c.attachedRigidbody&&c.attachedRigidbody.bodyType!=RigidbodyType2D.Static)continue;
   if(!bounds.Overlaps(new Rect(c.bounds.min.x,c.bounds.min.y,c.bounds.size.x,c.bounds.size.y)))continue;
   var paths=new List<Vector2[]>();
   if(c is PolygonCollider2D poly)for(int i=0;i<poly.pathCount;i++)paths.Add(poly.GetPath(i));
   else if(c is BoxCollider2D box){var h=box.size*.5f;paths.Add(new[]{new Vector2(-h.x,-h.y),new Vector2(h.x,-h.y),new Vector2(h.x,h.y),new Vector2(-h.x,h.y)});}
   else if(c is EdgeCollider2D edge)paths.Add(edge.points);
   else if(c is CompositeCollider2D composite)for(int i=0;i<composite.pathCount;i++){var points=new Vector2[composite.GetPathPointCount(i)];composite.GetPath(i,points);paths.Add(points);}
   foreach(var path in paths){
    var world=path.Select(p=>(Vector2)c.transform.TransformPoint(p+c.offset)).ToArray();float area=0;for(int i=0;i<world.Length;i++){var a=world[i];var b=world[(i+1)%world.Length];area+=a.x*b.y-b.x*a.y;}
    int count=c is EdgeCollider2D?world.Length-1:world.Length;
    for(int i=0;i<count;i++){
     Vector2 a=world[i],b=world[(i+1)%world.Length],t=b-a;if(t.sqrMagnitude<.00001f)continue;
     var domains=MainRooms.Concat(RoutePassages.Select(p=>p.rect));var domain=domains.FirstOrDefault(r=>r.Contains((a+b)*.5f));if(domain.width==0)continue;
     Vector2 n=new Vector2(t.y,-t.x).normalized*(area>=0?1:-1);Vector2 mid=(a+b)*.5f;
     if(c.OverlapPoint(mid+n*.03f))n=-n;
     if(c is EdgeCollider2D){var rooms=MainRooms.Concat(RoutePassages.Select(p=>p.rect)).ToArray();bool pos=rooms.Any(r=>r.Contains(mid+n*.1f)),neg=rooms.Any(r=>r.Contains(mid-n*.1f));if(neg&&!pos)n=-n;}
     var test=mid+n*.05f;bool hidden=Physics2D.OverlapPointAll(test,LayerMask.GetMask("Ground")).Any(o=>o!=c&&!o.isTrigger);if(hidden)continue;
     Vector2 Bound(Vector2 point)=>new Vector2(Mathf.Clamp(point.x,domain.xMin,domain.xMax),Mathf.Clamp(point.y,domain.yMin,domain.yMax));
     int k=v.Count;v.Add(Bound(a));v.Add(Bound(b));v.Add(Bound(b+n*.6f));v.Add(Bound(a+n*.6f));uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));tris.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});proof.Add(new{collider=c.name,type=c.GetType().Name,a=new[]{a.x,a.y},b=new[]{b.x,b.y},normal=new[]{n.x,n.y}});
    }
   }
  }
  var go=new GameObject("TerrainContact_actualColliderContours"){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(root.transform,false);
  var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
  var mat=new Material(Shader.Find("Qolossal/Prototypes/KeepersCistern49")){hideFlags=HideFlags.HideAndDontSave};mat.SetFloat("_Mode",2);mat.SetFloat("_Strength",.45f);mat.SetVector("_Clip",new Vector4(1480,68,1754,196));owned.Add(mat);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=-21;
  pieces.Add(new Piece{t=go.transform,basis=Vector3.zero,anchor=Vector2.zero,motion=Vector2.one,r=r,near=true});
  System.IO.Directory.CreateDirectory(Delivery+"QA_49d-r1");System.IO.File.WriteAllText(Delivery+"QA_49d-r1/terrain_contact_edges.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{width=.6f,contactDarkening=.45f,source="Actual enabled static Ground collider paths; shared internal edges excluded",edges=proof},Newtonsoft.Json.Formatting.Indented));
 }
}
