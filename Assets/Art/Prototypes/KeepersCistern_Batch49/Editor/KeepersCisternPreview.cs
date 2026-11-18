using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

[InitializeOnLoad]
public static partial class KeepersCisternPreview {
 public const string Art="Assets/Art/Prototypes/KeepersCistern_Batch49/";
 public const string Delivery="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full/Batch49/";
 const string Key="Qolossal.KeepersCistern49.";
 public static bool Enabled=>EditorPrefs.GetBool(Key+Application.dataPath,false);
 public static bool Near=true;
 static GameObject root; static readonly List<Piece> pieces=new(); static readonly List<UnityEngine.Object> owned=new();static double next;static bool f8Held,f6Held,f7Held;
 class Piece {public Transform t;public Vector3 basis;public Vector2 anchor,motion;public Renderer r;public bool near;public Rect rect,clip;}
 static readonly Rect A01=new Rect(1480,170,36,22),A02=new Rect(1528,126,16,60),A03=new Rect(1490,102,48,12),A04=new Rect(1552,84,64,36),A05=new Rect(1628,92,16,48),A06=new Rect(1554,154,60,32),A07=new Rect(1628,170,42,14),A08=new Rect(1684,168,52,28),T1=new Rect(1538,106,14,4),T2=new Rect(1616,106,12,4);
 static KeepersCisternPreview(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Clear;Camera.onPreCull+=Apply;RenderPipelineManager.beginCameraRendering+=BeforeCamera;InputSystem.onEvent+=InputEvent;if(Enabled&&EditorApplication.isPlaying)RouteSave();}
 static void InputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr e,InputDevice device){if(!Enabled||!Application.isPlaying||!(device is Keyboard k)||!e.IsA<UnityEngine.InputSystem.LowLevel.StateEvent>())return;bool a=k.f8Key.ReadValueFromEvent(e)>.5f,b=k.f6Key.ReadValueFromEvent(e)>.5f,c=k.f7Key.ReadValueFromEvent(e)>.5f;if(a&&!f8Held)SetNear(!Near);if(b&&!f6Held)Visit();if(c&&!f7Held)VisitNext();f8Held=a;f6Held=b;f7Held=c;}
 static void BeforeCamera(ScriptableRenderContext c,Camera cam)=>Apply(cam);
 [MenuItem("Qolossal/Art Review/Keepers' Cistern")] static void Toggle()=>SetEnabled(!Enabled);
 [MenuItem("Qolossal/Art Review/Keepers' Cistern",true)]static bool Validate(){Menu.SetChecked("Qolossal/Art Review/Keepers' Cistern",Enabled);return true;}
 public static void SetEnabled(bool value){if(value&&!Enabled)SessionState.SetBool(Key+"oldPreview",CisternContinuousPreview.Enabled);EditorPrefs.SetBool(Key+Application.dataPath,value);Clear();if(value)CisternContinuousPreview.SetEnabled(false);else CisternContinuousPreview.SetEnabled(SessionState.GetBool(Key+"oldPreview",false));next=0;Tick();}
 static FieldInfo SaveField=>typeof(GameSave).GetField("fileName",BindingFlags.NonPublic|BindingFlags.Static);
 static void RouteSave(){SessionState.SetBool(GameSave.PlayHereKey,false);SaveField.SetValue(null,Delivery+"sources/49d-r1/preview_save.json");GameSave.Reload();}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
 static void RouteRuntimeSave(){if(Enabled)RouteSave();}
 static void State(PlayModeStateChange s){Clear();if(s==PlayModeStateChange.EnteredPlayMode&&Enabled)RouteSave();if(s==PlayModeStateChange.ExitingEditMode&&Enabled){SessionState.SetBool(Key+"saveActive",true);SessionState.SetBool(Key+"oldPlayHere",SessionState.GetBool(GameSave.PlayHereKey,false));SessionState.SetString(Key+"oldFile",(string)SaveField.GetValue(null)??"");SessionState.SetBool(Key+"oldBackground",Application.runInBackground);RouteSave();Application.runInBackground=true;}if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"saveActive",false)){SessionState.SetBool(GameSave.PlayHereKey,SessionState.GetBool(Key+"oldPlayHere",false));var old=SessionState.GetString(Key+"oldFile","");SaveField.SetValue(null,string.IsNullOrEmpty(old)?null:old);GameSave.Reload();Application.runInBackground=SessionState.GetBool(Key+"oldBackground",false);SessionState.SetBool(Key+"saveActive",false);}}
 public static void SetNear(bool value){Near=value;foreach(var p in pieces)if(p.r)p.r.enabled=!p.near||value;}
 static void Tick(){if(!Enabled||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying)return;if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;if(!root&&(!Application.isPlaying||Time.time>1))Build();}
 public static void Visit(){var p=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(!p)return;foreach(var h in Physics2D.RaycastAll(new Vector2(1610,109),Vector2.down,8,LayerMask.GetMask("Ground")))if(h.collider&&!h.collider.isTrigger&&h.normal.y>.6f){p.PlaceAt(new Vector2(1610,h.point.y+.66f),1,true);break;}}
 public static void Clear(){pieces.Clear();if(root)UnityEngine.Object.DestroyImmediate(root);root=null;foreach(var o in owned)if(o)UnityEngine.Object.DestroyImmediate(o);owned.Clear();}
 public static void Apply(Camera cam){if(!cam||!root)return;Vector2 c=cam.transform.position;foreach(var p in pieces){var d=c-p.anchor;p.t.position=p.basis+new Vector3(d.x*(1-p.motion.x),d.y*(1-p.motion.y),0);}}
 static Texture2D Paint(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49d-r1/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49d/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49c/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49b-r3/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49b-r2/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"49b-r1/"+name+".png")??AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+".png");
 static void Quad(string name,Rect rect,Rect clip,int order,bool near,Vector2 motion,Vector2 anchor,bool solid=false){
 var tex=solid?Texture2D.whiteTexture:Paint(name);if(!tex)throw new Exception("Missing Keepers paint "+name);
 var go=new GameObject(name){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(rect.center.x,rect.center.y,0);
 var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave};float w=rect.width/2,h=rect.height/2;mesh.vertices=new[]{new Vector3(-w,-h),new Vector3(w,-h),new Vector3(w,h),new Vector3(-w,h)};float u=solid?1:rect.width*216/tex.width,v=solid?1:rect.height*216/tex.height;mesh.uv=new[]{Vector2.zero,new Vector2(u,0),new Vector2(u,v),new Vector2(0,v)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
 var mat=new Material(Shader.Find("Qolossal/Prototypes/KeepersCistern49")){hideFlags=HideFlags.HideAndDontSave};mat.mainTexture=tex;mat.SetVector("_Clip",new Vector4(clip.xMin,clip.yMin,clip.xMax,clip.yMax));mat.SetFloat("_Solid",solid?1:0);mat.SetFloat("_Light",order>=-76&&order<=-55?1:0);bool stain=name.StartsWith("CK_Wall")||name.StartsWith("CK_Column");mat.SetFloat("_Tide",stain?1:0);if(stain)mat.SetTexture("_TideTex",Paint("CK_Band_Tide"));owned.Add(mat);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;r.enabled=!near||Near;pieces.Add(new Piece{t=go.transform,basis=go.transform.position,anchor=anchor,motion=motion,r=r,near=near,rect=rect,clip=clip});}
 static void Put(string n,float x,float y,Rect clip,int order=-65){var t=Paint(n);Quad(n,new Rect(x-t.width/432f,y-t.height/432f,t.width/216f,t.height/216f),clip,order,true,Vector2.one,clip.center);}
 static void Wall(Rect r,Rect clip,string n="CK_Wall_Masonry")=>Quad(n,r,clip,-76,true,Vector2.one,clip.center);
 static void Pier(float x,float top,Rect room,string name){
  int n=0;float end=top;
  while(end>room.yMin){bool first=n==0;float visible=first?12:10.6f;float bottom=Mathf.Max(room.yMin,end-visible);
   float centre=first?end-6:end-4.6f;
   var clip=Rect.MinMaxRect(room.xMin,bottom,room.xMax,end);
   Put(n%2==0?name:(name.EndsWith("01")?"CK_Column_02":"CK_Column_01"),x,centre,clip);
   end=bottom;n++;
  }
 }
 static void Tunnel(Rect r){Wall(r,r);int i=0;foreach(float offset in new[]{.5f,3.6f,6.3f,10.1f,13.3f})if(offset<r.width)Put("CK_Rib_Tunnel_"+(++i%3+1).ToString("00"),r.x+offset,108,r);Put("CK_Niche_Lantern_02",r.center.x+1.4f,108.7f,r);Quad("CK_Pipe_H",new Rect(r.x,r.yMax-.35f,r.width,1),r,-64,true,Vector2.one,r.center);}
 public static void Build(){Clear();missingPaint.Clear();var scene=SceneManager.GetSceneByPath("Assets/Scenes/Game/Qolossal_Underground.unity");if(!scene.isLoaded)return;if(!AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"FAR_A04_Colonnade_r1c2.png"))return;
 root=new GameObject("Keepers' Cistern — Editor art preview"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);
 foreach(var r in new[]{A03,A04,A05,T1,T2})Quad("Backing",r,r,-85,false,Vector2.one,r.center,true);
 for(int row=0;row<2;row++)for(int col=0;col<3;col++)Quad($"FAR_A04_Colonnade_r{row}c{col}",new Rect(1584-21.5f+col*43f/3,102-14+row*14,43f/3,14),A04,-80,false,new Vector2(.5f,.6f),A04.center);
 Wall(A03,A03); //49d: continuous masonry; recess penumbra is in the shared room map.
 // Native vault ends are embedded in rear masonry and the retained outer piers.
 foreach(var c in new[]{new Vector2(1496,112),new Vector2(1512,111.5f),new Vector2(1528,112)})Put("CK_Vault_Span",c.x,c.y,A03);
 foreach(float x in new[]{1490f,1504f,1536f})Pier(x,116,A03,"CK_Column_01"); //49d-r1: continuous masonry at the former x1520 strip; vault feet remain embedded in the rear wall.
 Put("CK_Window_Arch",1506,104.2f,A03);Put("CK_Niche_Lantern_01",1510.8f,107.8f,A03);
 Put("CK_Roots_Curtain_03",1517.8f,113.3f,A03);Put("CK_Roots_Curtain_02",1524.5f,112,A03);Put("CK_Roots_Curtain_01",1504,113,A03);
 Quad("CK_Pipe_H",new Rect(1490,110.6f,13,1),A03,-64,true,Vector2.one,A03.center);Put("CK_Pipe_Elbow",1503,110.6f,A03);Put("CK_Pipe_V",1503,105.6f,A03);
 Put("CK_Mound_02",1507,102,A03,-54);Quad("CK_Band_Silt",new Rect(1490,101,48,3),A03,-57,true,Vector2.one,A03.center);
 // Low west bay and uneven full-height reservoir piers frame the glide at 100%.
 Wall(new Rect(1552,84,4,36),A04);Wall(new Rect(1612,84,4,36),A04);Wall(new Rect(1552,116,64,4),A04);
 foreach(var b in new[]{new Vector2(1564,94),new Vector2(1583,113),new Vector2(1599,115.5f),new Vector2(1615,112)}){
  if(b.y+2<120)Wall(new Rect(b.x-8,b.y+2,16,120-b.y-2),A04);
  Put("CK_Vault_Span",b.x,b.y,A04);
 }
 foreach(float x in new[]{1556f,1575.8f,1607f,1612f})Pier(x,120,A04,"CK_Column_02");
 NicheSupport(1572,83,96,A04);NicheSupport(1590.4f,83,116,A04);
 Put("CK_Niche_Lantern_02",1572,90.8f,A04);Put("CK_Niche_Lantern_03",1590.4f,100.8f,A04);Put("CK_Niche_Lantern_01",1613.3f,108.8f,A04);
 Put("CK_Roots_Curtain_01",1575.6f,112.5f,A04);Put("CK_Roots_Curtain_02",1590f,115,A04);Put("CK_Roots_Curtain_03",1606.5f,116,A04);
 // Native shaft continuation; base stays submerged at y83, cap is at y101, separated from the deck at y105.
 Quad("MID_A04_GaugeTower",new Rect(1584.5f,89,5,13),Rect.MinMaxRect(1552,91.1f,1616,120),-74,false,new Vector2(.88f,.94f),A04.center);
 Quad("MID_A04_GaugeTower",new Rect(1584.5f,83,5,13),Rect.MinMaxRect(1552,84,1616,87),-75,false,new Vector2(.88f,.94f),A04.center);
 // Extend only plain native shaft sections: the gauge valve remains unique.
 for(float y=87;y<91.1f;y+=2.5f)Quad("MID_A04_GaugeTower",new Rect(1584.5f,y-8,5,13),Rect.MinMaxRect(1552,y,1616,Mathf.Min(91.1f,y+2.5f)),-75,false,new Vector2(.88f,.94f),A04.center);
 Put("CK_Mound_01",1587,84,A04,-54);Quad("CK_Band_Silt",new Rect(1552,82,64,3),A04,-53,true,Vector2.one,A04.center);
 Tunnel(T1);Tunnel(T2);
 // Mouths embedded in solid side walls, crown ends buried in the ceiling band.
 Put("CK_Mouth_Arch_01",1553.2f,108.3f,A04,-54);Put("CK_Mouth_Arch_02",1614.8f,108.3f,A04,-54);
 
 Wall(A05,A05);Recess(new Rect(1633.65f,92,4.7f,14),A05);Mat(pieces.Last()).SetFloat("_RecessArch",1);
 Put("CK_Recess_Arch",1636,104.5f,A05,-64);Mat(pieces.Last()).SetFloat("_Soffit",1);
 Put("CK_Vault_Span",1636,106,A05);Put("CK_Vault_Span",1636,116,A05);
 Pier(1628.2f,142,A05,"CK_Column_01");Pier(1643.5f,142,A05,"CK_Column_02");
 Pier(1633.65f,107.8f,A05,"CK_Column_01");Pier(1638.35f,107.8f,A05,"CK_Column_02");
 // Continuous squared caps hide the native shaft joins under the vault.
 foreach(float x in new[]{1633.65f,1638.35f}){Quad("CK_Basin_Coping",new Rect(x-.9f,105.75f,1.8f,.5f),A05,-63,true,Vector2.one,A05.center);pieces.Last().t.name="Capital_Coping";}
 Quad("MID_A05_WeirStair",new Rect(1627.5f,91,6,10),A05,-59,false,new Vector2(.88f,.94f),new Vector2(1636,104));
 // The weir ends in a painted coping, attached to the same middle motion.
 Quad("CK_Basin_Coping",new Rect(1627.5f,100.75f,1.55f,.5f),A05,-58,false,new Vector2(.88f,.94f),new Vector2(1636,104));pieces.Last().t.name="MID_A05_WeirCoping";
 Put("CK_Niche_Lantern_03",1640.6f,103.8f,A05);Put("CK_Roots_Curtain_03",1639,113,A05);
 Put("CK_Pipe_V",1642.5f,106,A05);Put("CK_Pipe_Valve",1642.5f,101,A05);Put("CK_Mound_02",1631,92,A05,-53);Put("CK_Water_Sheet_V",1630,106,A05,-62);
 foreach(var p in new[]{new Vector2(1501,116),new Vector2(1560,100),new Vector2(1575.8f,115.5f),new Vector2(1590.4f,116),new Vector2(1607,118),new Vector2(1614,117),new Vector2(1643,113)}){
  var r=p.x<1538?A03:p.x<1616?A04:A05;Quad("FG_Roots_01",new Rect(p.x-2,p.y-3,4,6),r,-25,true,new Vector2(1.1f,1.05f),r.center);
 }
 Quad("FG_Chain_Hook",new Rect(1580,110.2f,2,8),A04,-24,true,new Vector2(1.1f,1.05f),A04.center);
 BuildRestOfRoute();
 AddDepthPass();
 AddSharedLighting();
 Apply(Camera.main);
 }
}
