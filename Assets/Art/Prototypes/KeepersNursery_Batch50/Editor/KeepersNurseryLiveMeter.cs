using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading.Tasks;using UnityEngine;using UnityEditor;using Newtonsoft.Json;
[InitializeOnLoad] public static class KeepersNurseryLiveMeter {
 public class Line {public int id;public float[] a,b;}
 class Walk {public Line[] lines;}
 public class Reading {public double? median;public double worst;public int bands,frame,brightness;public float x,y;public double measuredMs;}
 public static Reading Last;public static double SavedAt=-999;
 static Task<Reading> pending;static double next,sampledAt;static int generation,pendingGeneration;static Line[] lines;
 static readonly float[] linear=Enumerable.Range(0,256).Select(i=>{float v=i/255f;return v<=.04045f?v/12.92f:(float)Math.Pow((v+.055)/1.055,2.4);}).ToArray();
 static KeepersNurseryLiveMeter(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=s=>{Invalidate();};}
 public static void Invalidate(){generation++;Last=null;next=0;RefreshLabel();}
 static void RefreshLabel(){string median=Last==null?"measuring...":Last.median.HasValue?(Last.median.Value*100).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"%":"N/A";string age=Last==null?"":$"  | frame {Last.frame}";KeepersNurseryGradeLabel.DisplayText=$"Brightness: {Math.Max(0,KeepersNurseryPreview.BrightnessPercent)}%   Walk-band median: {median}\nF9 -5   F10 +5   F11 save"+(EditorApplication.timeSinceStartup-SavedAt<4?"   Saved":"")+age;}
 static void Tick(){
  if(pending!=null&&pending.IsCompleted){if(pending.IsFaulted)Debug.LogException(pending.Exception);else if(pendingGeneration==generation)Last=pending.Result;pending=null;RefreshLabel();}
  if(!Application.isPlaying||!KeepersNurseryPreview.Enabled||!KeepersNurseryPreview.Root||KeepersNurseryPreview.SuspendLiveMeasurement||KeepersNurseryPresetSurvey.Pending>0||EditorApplication.isCompiling)return;
  RefreshLabel();if(pending!=null||EditorApplication.timeSinceStartup<next)return;var cam=Camera.main;if(!cam||!cam.orthographic)return;
  next=EditorApplication.timeSinceStartup+2;sampledAt=EditorApplication.timeSinceStartup;lines??=JsonConvert.DeserializeObject<Walk>(File.ReadAllText(KeepersNurseryPreview.Data+"walklines.json")).lines;
  int w=1920,h=Math.Max(1,Mathf.RoundToInt(w/cam.aspect)),frame=Time.frameCount,brightness=KeepersNurseryPreview.BrightnessPercent;float x=cam.transform.position.x,y=cam.transform.position.y,ppu=h/(2*cam.orthographicSize);
  var bg=Capture(cam,w,h,false);var terrain=Capture(cam,w,h,true);pendingGeneration=generation;pending=Task.Run(()=>Measure(bg,terrain,w,h,x,y,ppu,frame,brightness));
 }
 public static Reading Measure(Color32[] bg,Color32[] terrain,int w,int h,float cx,float cy,float ppu,int frame,int brightness){
  var watch=System.Diagnostics.Stopwatch.StartNew();var ratios=new List<double>();var ls=lines??JsonConvert.DeserializeObject<Walk>(File.ReadAllText(KeepersNurseryPreview.Data+"walklines.json")).lines;
  foreach(var l in ls){int left=Math.Max(0,(int)Math.Ceiling((l.a[0]-cx)*ppu+w/2f)),right=Math.Min(w,(int)Math.Floor((l.b[0]-cx)*ppu+w/2f));if(right-left<Math.Max(4,.25*ppu))continue;var ts=new List<float>();var bs=new List<float>();
   for(int col=left;col<right;col++){float wx=col/ppu+cx-w/2f/ppu;float top=l.a[1]+(wx-l.a[0])/(l.b[0]-l.a[0])*(l.b[1]-l.a[1]);int y0=Math.Max(0,(int)Math.Ceiling(h/2f-(top+3-cy)*ppu)),y1=Math.Min(h-1,(int)Math.Floor(h/2f-(top-1-cy)*ppu));
    for(int row=y0;row<=y1;row++){int idx=(h-1-row)*w+col;var t=terrain[idx];if(t.a>230)ts.Add(Lum(t));else bs.Add(Lum(bg[idx]));}}
   if(ts.Count<50||bs.Count<50)continue;float tmed=Median(ts);if(tmed>1e-6)ratios.Add(Median(bs)/tmed);
  }
  ratios.Sort();return new Reading{median=ratios.Count==0?(double?)null:ratios.Count%2==1?ratios[ratios.Count/2]:(ratios[ratios.Count/2-1]+ratios[ratios.Count/2])/2,worst=ratios.Count==0?0:ratios.Max(),bands=ratios.Count,frame=frame,brightness=brightness,x=cx,y=cy,measuredMs=watch.Elapsed.TotalMilliseconds};
 }
 static float Lum(Color32 c)=>linear[c.r]*.2126f+linear[c.g]*.7152f+linear[c.b]*.0722f;
 static float Median(List<float> a){a.Sort();return a.Count%2==1?a[a.Count/2]:(a[a.Count/2-1]+a[a.Count/2])/2;}
 public static Color32[] Capture(Camera source,int width,int height,bool terrainOnly){
  var all=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Concat(KeepersNurseryPreview.Root.GetComponentsInChildren<Renderer>()).Distinct().ToArray();var old=all.Select(r=>r.forceRenderingOff).ToArray();var active=RenderTexture.active;
  var go=new GameObject("Nursery live luminance measurement"){hideFlags=HideFlags.HideAndDontSave};var cam=go.AddComponent<Camera>();cam.CopyFrom(source);cam.enabled=false;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=terrainOnly?Color.clear:Color.black;cam.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
  var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);
  try{for(int i=0;i<all.Length;i++){var r=all[i];string parent=r.transform.parent?r.transform.parent.name:"";bool own=r.transform.IsChildOf(KeepersNurseryPreview.Root.transform);bool terrain=r.name.StartsWith("Rock_")||r.name.StartsWith("Rim_")||r.name.StartsWith("Natural_Rim_")||(r.name=="Art"&&new[]{"UGP deck ","UGP solid ","UGP crumble ","UGP mover "}.Any(s=>parent.StartsWith(s)));bool background=own||r.name.StartsWith("UGP backdrop")||r.name=="UGP passage backdrop"||parent=="Ambient life (generated)";r.forceRenderingOff=old[i]||!(terrainOnly?terrain:background);}
   CaveDepth.Apply(cam);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();return tex.GetPixels32();
  }finally{for(int i=0;i<all.Length;i++)if(all[i])all[i].forceRenderingOff=old[i];RenderTexture.active=active;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);CaveDepth.Apply(source);}
 }
}
