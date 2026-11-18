using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

[InitializeOnLoad]
public static class KeepersNurseryQA {
 public class Pose {public string name;public float x,y,qx,qy,time;}
 class Job {public Pose pose;public int width;public string kind;public bool near;}
 static readonly Queue<Job> jobs=new();static readonly List<object> records=new();static bool oldNear;static int total;
 public static int Pending=>jobs.Count;
 public static void StartQuick(){jobs.Clear();records.Clear();Directory.CreateDirectory(Output);oldNear=KeepersNurseryPreview.Near;foreach(var p in Poses().Take(4))foreach(var kind in new[]{"terrain","context","background","far","density"})jobs.Enqueue(new Job{pose=p,width=1920,kind=kind,near=true});total=jobs.Count;WriteProgress(null);}
 public static string Output=>KeepersNurseryPreview.SurveyPath+"captures/";
 static KeepersNurseryQA(){EditorApplication.update+=Tick;}
 public static Pose[] Poses(){var p=new List<Pose>{new(){name="B04_center",x=1596,y=278,qx=1602,qy=276},new(){name="B04_upper_left",x=1574,y=296,qx=1569,qy=292.5f},new(){name="B04_upper_right",x=1618,y=296,qx=0,qy=0},new(){name="B04_lower_left",x=1574,y=276,qx=1573,qy=273.7f},new(){name="B05_lower",x=1652,y=279,qx=1647,qy=274}};
 for(int i=0;i<5;i++){float t=i/4f;p.Add(new Pose{name="B04_glide_"+i,x=Mathf.Lerp(1582,1610,t),y=278+2*Mathf.Sin(t*Mathf.PI),qx=0,time=t*2});p.Add(new Pose{name="P_B04_B05_walk_"+i,x=Mathf.Lerp(1628,1644,t),y=286,qx=Mathf.Lerp(1629,1643,t),qy=284,time=t*2});}
 var xs=new[]{1652f,1652,1628,1604,1604};var ys=new[]{318f,314,314,314,306};for(int i=0;i<5;i++)p.Add(new Pose{name="P_B03_B04_descent_"+i,x=xs[i],y=ys[i],qx=0,time=i*.5f});return p.ToArray();}
 public static void Start(){if(Application.isPlaying)throw new Exception("Static capture suite requires Edit mode");jobs.Clear();records.Clear();Directory.CreateDirectory(Output);oldNear=KeepersNurseryPreview.Near;
 foreach(var p in Poses())foreach(int width in new[]{3840,1920}){
  jobs.Enqueue(new Job{pose=p,width=width,kind="terrain",near=true});
  foreach(bool near in new[]{true,false})foreach(var kind in new[]{"context","background"})jobs.Enqueue(new Job{pose=p,width=width,kind=kind,near=near});
 }
 foreach(var p in Poses().Where(p=>p.name.StartsWith("B04_")))foreach(int width in new[]{3840,1920})foreach(var kind in new[]{"far","density"})jobs.Enqueue(new Job{pose=p,width=width,kind=kind,near=true});
 total=jobs.Count;WriteProgress(null);
 }
 static void WriteProgress(string error){File.WriteAllText(Output+"progress.json",JsonConvert.SerializeObject(new{total,pending=jobs.Count,completed=records.Count,error,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty},Formatting.Indented));File.WriteAllText(Output+"capture_receipt.json",JsonConvert.SerializeObject(new{staticCameraSequence=true,orthographicHalfHeight=5,records},Formatting.Indented));}
 static void Tick(){if(jobs.Count==0||EditorApplication.isCompiling||EditorApplication.isUpdating)return;try{var j=jobs.Dequeue();records.Add(Capture(j.pose,j.width,j.kind,j.near));WriteProgress(null);if(jobs.Count==0)KeepersNurseryPreview.SetNear(oldNear);}catch(Exception e){jobs.Clear();KeepersNurseryPreview.SetNear(oldNear);WriteProgress(e.ToString());Debug.LogException(e);}}
 public static object Capture(Pose p,int width,string kind,bool near){
 KeepersNurseryPreview.SetNear(near);int height=width*9/16;var all=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Concat(KeepersNurseryPreview.Root?KeepersNurseryPreview.Root.GetComponentsInChildren<Renderer>():Array.Empty<Renderer>()).Distinct().ToArray();var old=all.Select(r=>r.forceRenderingOff).ToArray();var oldActive=RenderTexture.active;
 var go=new GameObject("Nursery QA camera"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=5;cam.aspect=16f/9;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=kind=="terrain"?Color.clear:Color.black;cam.transform.position=new Vector3(p.x,p.y,-50);
 var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);GameObject qg=null;var coverage=kind=="density"?KeepersNurseryPreview.BeginCoverage():new List<Texture2D>();
 try{
  if(kind=="context"&&p.qx>0){qg=new GameObject("Nursery QA visual Qori"){hideFlags=HideFlags.HideAndDontSave};var q=qg.AddComponent<SpriteRenderer>();q.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Characters/FrameAnimation/ArtLudoIdle/Qori_LudoIdle_01.png");q.sortingOrder=100;var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();var anim=player?player.GetComponentInChildren<QoriAnimator>():null;var body=anim?anim.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite&&(anim.weapon==null||!r.transform.IsChildOf(anim.weapon.transform))).ToArray():new SpriteRenderer[0];float worldHeight=1.3f;if(body.Length>0){var b=body[0].bounds;foreach(var r in body.Skip(1))b.Encapsulate(r.bounds);worldHeight=b.size.y;q.sharedMaterial=body[0].sharedMaterial;}qg.transform.localScale=Vector3.one*(worldHeight/465.3f*q.sprite.pixelsPerUnit);qg.transform.position=new Vector3(p.qx,p.qy,0);qg.transform.position+=new Vector3(0,p.qy-q.bounds.min.y-qg.transform.localScale.y*30f/q.sprite.pixelsPerUnit,0);}
  for(int i=0;i<all.Length;i++){
   var r=all[i];bool own=KeepersNurseryPreview.Root&&r.transform.IsChildOf(KeepersNurseryPreview.Root.transform);string parent=r.transform.parent?r.transform.parent.name:"";
   bool terrain=r.name.StartsWith("Rock_")||r.name.StartsWith("Rim_")||r.name.StartsWith("Natural_Rim_")||(r.name=="Art"&&new[]{"UGP deck ","UGP solid ","UGP crumble ","UGP mover "}.Any(s=>parent.StartsWith(s)));
   bool background=own||r.name.StartsWith("UGP backdrop")||r.name=="UGP passage backdrop"||parent=="Ambient life (generated)";bool syntheticContext=kind=="context"&&!p.name.StartsWith("PLAY_");
   r.forceRenderingOff=old[i]||(syntheticContext&&(r.GetComponentInParent<PlayerMovement>()!=null||(p.qx>0&&r.name=="Young Adventurer frames (test)")))||((kind=="background")&&!background)||((kind=="density"||kind=="rearCoverage")&&!own)||(kind=="terrain"&&!terrain)||(kind=="far"&&!((own&&r.name.StartsWith("FAR_"))||(!own&&r.name.Contains("backdrop")&&r.name!="UGP backdrop_near")));
   if(kind=="rearCoverage"&&own&&!r.name.StartsWith("SET_")&&!r.name.StartsWith("NK_"))r.forceRenderingOff=true;
   if(own)r.sharedMaterial.SetFloat("_QA",kind=="rearCoverage"?2:kind=="density"?1:0);
  }
  CaveDepth.Apply(cam);cam.targetTexture=rt;if(kind=="density")cam.RenderWithShader(Shader.Find("Qolossal/Prototypes/KeepersNurseryDensity"),"");else cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();string file=p.name+"_"+kind+"_"+(near?"on":"off")+"_"+width+".png";Directory.CreateDirectory(Output);File.WriteAllBytes(Output+file,tex.EncodeToPNG());
  var visible=new Bounds(new Vector3(p.x,p.y,0),new Vector3(160f/9,10,100));var colliders=UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Where(c=>c.enabled&&!c.isTrigger&&c.bounds.Intersects(visible)).Select(c=>new{name=c.name,type=c.GetType().Name,bounds=new[]{c.bounds.min.x,c.bounds.min.y,c.bounds.max.x,c.bounds.max.y}}).ToArray();
  return new{file,camera=new[]{p.x,p.y},nominalTime=p.time,kind,near,width,height,ppu=height/10f,visualQoriOnly=qg!=null,colliders,frame=Time.frameCount};
 }finally{KeepersNurseryPreview.EndCoverage(coverage);if(KeepersNurseryPreview.Root)foreach(var r in KeepersNurseryPreview.Root.GetComponentsInChildren<Renderer>())r.sharedMaterial.SetFloat("_QA",0);for(int i=0;i<all.Length;i++)if(all[i])all[i].forceRenderingOff=old[i];RenderTexture.active=oldActive;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);if(qg)UnityEngine.Object.DestroyImmediate(qg);UnityEngine.Object.DestroyImmediate(go);CaveDepth.Apply(Camera.main);}
 }
}


