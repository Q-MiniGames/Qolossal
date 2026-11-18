using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;

[InitializeOnLoad]
public static class KeepersNurseryPreview {
 public const string Art="Assets/Art/Prototypes/KeepersNursery_Batch50/";
 public const string Delivery="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full/Batch50/50b-r5c/";
 public const string Data=Delivery+"sources/";
 const string Key="Qolossal.KeepersNursery50.";
 public static bool Enabled=>EditorPrefs.GetBool(Key+Application.dataPath,false);
 public static bool Near=true;
 public static GameObject Root=>root;
 static GameObject root;static bool f8Held,f9Held,f10Held,f11Held;static double next;
 static readonly List<Piece> pieces=new();static readonly List<UnityEngine.Object> owned=new();
 static readonly Dictionary<Renderer,bool> hidden=new();
 public class Placement {public string name,domain,layer;public float[] rect,clip,motion,anchor,uv;public bool near,flipX,baked;public float gain=1;public float feather=0;public int order;}
 public class LayerGrade {public float brightness=1,contrast=1,pivot=0,haze=0,highlightKnee=.045f,highlightCeiling=.062f;public float[] hazeColourSRGB;}
 public class GradeConfig {public int version=3,startBrightnessPercent=175,brightnessPercent=175;public bool selectionCommitted;public string savedAtUTC,formula,controls;public string[] targets;public float poolStrength=2.2f,legacyMultiplier=.35f,legacyHighlightKnee=.003f,legacyHighlightCeiling=.008f;public Dictionary<string,LayerGrade> baseLayers,layers;public LayerGrade legacyFar;}
 public static int BrightnessPercent {get;private set;}=-1;
 public static string SelectedPreset=>"B"+BrightnessPercent;
 public static string SurveyPath=>Data+"surveys/"+SelectedPreset+"/";
 public static bool SuspendLiveMeasurement=false;
 public static void SetPreset(string name)=>SetBrightness(int.Parse(name.Substring(1)));
 public static void SetBrightness(int percent){BrightnessPercent=Mathf.Clamp(percent,20,600);Build();KeepersNurseryLiveMeter.Invalidate();SceneView.RepaintAll();}
 public static void AdjustBrightness(int delta)=>SetBrightness(BrightnessPercent+delta);
 public static void SaveGrade(){
  if(grading==null)return;grading.brightnessPercent=BrightnessPercent;grading.selectionCommitted=true;grading.savedAtUTC=DateTime.UtcNow.ToString("O");
  string dest=Delivery+"grade.json",temp=dest+".tmp";File.WriteAllText(temp,JsonConvert.SerializeObject(grading,Formatting.Indented));File.Replace(temp,dest,null);KeepersNurseryLiveMeter.SavedAt=EditorApplication.timeSinceStartup;
 }
 static void DeriveGrade(){grading.layers=grading.baseLayers.ToDictionary(kv=>kv.Key,kv=>JsonConvert.DeserializeObject<LayerGrade>(JsonConvert.SerializeObject(kv.Value)));foreach(var g in grading.layers.Values)g.brightness*=BrightnessPercent/100f;grading.legacyFar=JsonConvert.DeserializeObject<LayerGrade>(JsonConvert.SerializeObject(grading.layers["FAR"]));grading.legacyFar.brightness*=grading.legacyMultiplier;grading.legacyFar.highlightKnee=grading.legacyHighlightKnee;grading.legacyFar.highlightCeiling=grading.legacyHighlightCeiling;grading.brightnessPercent=BrightnessPercent;}
 static GradeConfig grading;static readonly Dictionary<SpriteRenderer,Material> boundaryOriginal=new();
 class Piece {public Transform t;public Vector3 basis;public Vector2 motion,anchor;public Renderer renderer;public bool near;}
 static KeepersNurseryPreview(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Clear;Camera.onPreCull+=Apply;RenderPipelineManager.beginCameraRendering+=BeforeCamera;InputSystem.onEvent+=InputEvent;if(Enabled&&EditorApplication.isPlaying)RouteSave();}
 static void BeforeCamera(ScriptableRenderContext ctx,Camera cam)=>Apply(cam);
 static FieldInfo SaveField=>typeof(GameSave).GetField("fileName",BindingFlags.NonPublic|BindingFlags.Static);
 static void RouteSave(){SessionState.SetBool(GameSave.PlayHereKey,false);SaveField.SetValue(null,Data+"preview_save.json");GameSave.Reload();}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void RouteRuntimeSave(){if(Enabled)RouteSave();}
 static void State(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode||state==PlayModeStateChange.EnteredPlayMode)BrightnessPercent=-1;f8Held=f9Held=f10Held=f11Held=false;Clear();if(state==PlayModeStateChange.ExitingEditMode&&Enabled){SessionState.SetBool(Key+"saveActive",true);SessionState.SetBool(Key+"oldPlayHere",SessionState.GetBool(GameSave.PlayHereKey,false));SessionState.SetString(Key+"oldFile",(string)SaveField.GetValue(null)??"");SessionState.SetBool(Key+"oldBackground",Application.runInBackground);RouteSave();Application.runInBackground=true;}if(state==PlayModeStateChange.EnteredPlayMode&&Enabled)RouteSave();if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"saveActive",false)){SessionState.SetBool(GameSave.PlayHereKey,SessionState.GetBool(Key+"oldPlayHere",false));var old=SessionState.GetString(Key+"oldFile","");SaveField.SetValue(null,string.IsNullOrEmpty(old)?null:old);GameSave.Reload();Application.runInBackground=SessionState.GetBool(Key+"oldBackground",false);SessionState.SetBool(Key+"saveActive",false);}next=0;}
 [MenuItem("Qolossal/Art Review/Keepers' Nursery")]static void Toggle()=>SetEnabled(!Enabled);
 [MenuItem("Qolossal/Art Review/Keepers' Nursery",true)]static bool Validate(){Menu.SetChecked("Qolossal/Art Review/Keepers' Nursery",Enabled);return true;}
 public static void SetEnabled(bool value){if(value==Enabled)return;if(value)SessionState.SetBool(Key+"oldCistern",KeepersCisternPreview.Enabled);EditorPrefs.SetBool(Key+Application.dataPath,value);Clear();if(value)KeepersCisternPreview.SetEnabled(false);else KeepersCisternPreview.SetEnabled(SessionState.GetBool(Key+"oldCistern",false));next=0;Tick();SceneView.RepaintAll();}
 static void InputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr e,InputDevice device){
  if(!Enabled||!Application.isPlaying||!(device is Keyboard k)||!e.IsA<UnityEngine.InputSystem.LowLevel.StateEvent>())return;
  bool pressed=k.f8Key.ReadValueFromEvent(e)>.5f;if(pressed&&!f8Held)SetNear(!Near);f8Held=pressed;
  bool down=k.f9Key.ReadValueFromEvent(e)>.5f,up=k.f10Key.ReadValueFromEvent(e)>.5f,save=k.f11Key.ReadValueFromEvent(e)>.5f;
  if(down&&!f9Held)AdjustBrightness(-5);if(up&&!f10Held)AdjustBrightness(5);if(save&&!f11Held)SaveGrade();
  if(down||f9Held)k.f9Key.WriteValueIntoEvent(0f,e);if(up||f10Held)k.f10Key.WriteValueIntoEvent(0f,e);if(save||f11Held)k.f11Key.WriteValueIntoEvent(0f,e);f9Held=down;f10Held=up;f11Held=save;
 }
 public static void SetNear(bool value){Near=value;foreach(var p in pieces)if(p.renderer)p.renderer.enabled=!p.near||value;SceneView.RepaintAll();}
 static void Tick(){if(!Enabled||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode!=EditorApplication.isPlaying)return;if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;if(!root&&(!Application.isPlaying||Time.unscaledTime>1))Build();}
 public static void Clear(){foreach(var kv in boundaryOriginal)if(kv.Key)kv.Key.sharedMaterial=kv.Value;boundaryOriginal.Clear();foreach(var kv in hidden)if(kv.Key)kv.Key.forceRenderingOff=kv.Value;hidden.Clear();pieces.Clear();if(root)UnityEngine.Object.DestroyImmediate(root);root=null;foreach(var o in owned)if(o)UnityEngine.Object.DestroyImmediate(o);owned.Clear();}
 public static void Apply(Camera cam){if(!cam||!root)return;Vector2 c=cam.transform.position;foreach(var p in pieces){var d=c-p.anchor;p.t.position=p.basis+new Vector3(d.x*(1-p.motion.x),d.y*(1-p.motion.y),0);}}
 static string FullPath(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
 public static void Build(){
 Clear();var scene=SceneManager.GetSceneByPath("Assets/Scenes/Game/Qolossal_Underground.unity");if(!scene.isLoaded)return;
 var path=Data+"placements.json";if(!File.Exists(path))return;var data=JsonConvert.DeserializeObject<List<Placement>>(File.ReadAllText(path));
 if(data.Any(p=>!AssetDatabase.LoadAssetAtPath<Texture2D>(Art+p.name+".png")))return;
 root=new GameObject("Keepers' Nursery — temporary Editor preview"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);if(Application.isPlaying)root.AddComponent<KeepersNurseryGradeLabel>();
 foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)){
  var p=FullPath(r.transform);var asset=AssetDatabase.GetAssetPath(r.sprite);
  bool b04=p.Contains("[C04]");bool passage=p.Contains("C03_C04_main")||p.Contains("C04_C05_main");
  if((b04||passage)&&asset.Contains("Backdrop")){hidden[r]=r.forceRenderingOff;r.forceRenderingOff=true;}
 }
 grading=JsonConvert.DeserializeObject<GradeConfig>(File.ReadAllText(Delivery+"grade.json"));if(BrightnessPercent<0)BrightnessPercent=grading.selectionCommitted?grading.brightnessPercent:grading.startBrightnessPercent;DeriveGrade();foreach(var p in data)Quad(p);
 ApplyBoundaryGrade();Apply(Camera.main);
 }
 static void ApplyBoundaryGrade(){
  var region=new Vector4(1553,268,1671,327);var g=grading.legacyFar;
  foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)){
   if(r.forceRenderingOff||!r.name.Contains("backdrop")||!r.sharedMaterial)continue;
   var b=r.bounds;if(b.max.x<region.x-4||b.min.x>region.z+4||b.max.y<region.y-4||b.min.y>region.w+4)continue;
   string original=r.sharedMaterial.shader.name;string target=original=="Qolossal/WorldRefreshCaveVista"?"KeepersNurseryLegacyVista":original=="Qolossal/CaveDepthBackdrop"?"KeepersNurseryLegacyDepth":original=="Qolossal/CavePassageVista"?"KeepersNurseryLegacyPassage":null;
   if(target==null)continue;var mat=new Material(r.sharedMaterial){hideFlags=HideFlags.HideAndDontSave};mat.shader=Shader.Find("Qolossal/Prototypes/"+target);
   // Shader switches do not retain runtime-only uniforms absent from Properties.
   foreach(string prefix in new[]{"_","_B_"}){
    foreach(string key in new[]{"VistaTile","LeftTile","RightTile","VaultTile","ExtensionTile","RoomAnchor","RoomRect","EdgeColor","ExtensionTint","Parallax","ParallaxPivot","ContinuousRect","MiddleRect","MiddleParallax"})if(r.sharedMaterial.HasProperty(prefix+key))mat.SetVector(prefix+key,r.sharedMaterial.GetVector(prefix+key));
    foreach(string key in new[]{"CenterBelow","HasDepth","ContinuousEnabled","MiddleEnabled","NetworkEnabled"})if(r.sharedMaterial.HasProperty(prefix+key))mat.SetFloat(prefix+key,r.sharedMaterial.GetFloat(prefix+key));
   }

   mat.SetVector("_NurseryGrade",new Vector4(g.brightness,g.contrast,g.pivot,g.haze));mat.SetVector("_NurseryHaze",new Vector4(g.hazeColourSRGB[0],g.hazeColourSRGB[1],g.hazeColourSRGB[2],1));mat.SetVector("_NurseryRegion",region);mat.SetVector("_NurseryHighlight",new Vector4(g.highlightKnee,g.highlightCeiling,0,0));boundaryOriginal[r]=r.sharedMaterial;r.sharedMaterial=mat;owned.Add(mat);
  }
 }
 public static List<Texture2D> BeginCoverage(){var textures=new List<Texture2D>();if(!root)return textures;foreach(var r in root.GetComponentsInChildren<Renderer>()){var path=Data+"coverage/"+r.name+".png";if(!File.Exists(path))continue;var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true){hideFlags=HideFlags.HideAndDontSave};t.LoadImage(File.ReadAllBytes(path));t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;r.sharedMaterial.SetTexture("_BuiltMask",t);textures.Add(t);}return textures;}
 public static void EndCoverage(List<Texture2D> textures){if(root)foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterial.SetTexture("_BuiltMask",Texture2D.whiteTexture);foreach(var t in textures)UnityEngine.Object.DestroyImmediate(t);}
 static void Quad(Placement p){
 var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+p.name+".png");var r=p.rect;var go=new GameObject(p.name){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(r[0]+r[2]/2,r[1]+r[3]/2,0);
 var mesh=new Mesh{hideFlags=HideFlags.HideAndDontSave};float w=r[2]/2,h=r[3]/2;mesh.vertices=new[]{new Vector3(-w,-h),new Vector3(w,-h),new Vector3(w,h),new Vector3(-w,h)};float u=r[2]*216/tex.width,v=r[3]*216/tex.height;float ux=0,vy=0;if(p.uv!=null){ux=p.uv[0];vy=p.uv[1];u=p.uv[2];v=p.uv[3];}float left=p.flipX?ux+u:ux,right=p.flipX?ux:ux+u;mesh.uv=new[]{new Vector2(left,vy),new Vector2(right,vy),new Vector2(right,vy+v),new Vector2(left,vy+v)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
 var mat=new Material(Shader.Find("Qolossal/Prototypes/KeepersNursery50")){hideFlags=HideFlags.HideAndDontSave};mat.mainTexture=tex;mat.SetVector("_Clip",new Vector4(p.clip[0],p.clip[1],p.clip[0]+p.clip[2],p.clip[1]+p.clip[3]));
 bool far=p.name.StartsWith("FAR_");mat.SetFloat("_Lit",far||p.baked||p.name.StartsWith("FG_")?0:1);mat.SetFloat("_Wall",p.name.StartsWith("NK_Wall_Rock")?1:0);mat.SetFloat("_Emissive",p.name.StartsWith("NK_Niche_Lantern")?1:0);
 var gradeKey=p.name.StartsWith("SET_")?"SET":p.layer;var g=grading.layers.ContainsKey(gradeKey)?grading.layers[gradeKey]:grading.layers["rear"];mat.SetVector("_Grade",new Vector4(g.brightness,g.contrast,g.pivot,g.haze));mat.SetVector("_GradeHaze",new Vector4(g.hazeColourSRGB[0],g.hazeColourSRGB[1],g.hazeColourSRGB[2],1));mat.SetFloat("_PoolEnabled",far?0:1);mat.SetFloat("_PoolStrength",grading.poolStrength);mat.SetFloat("_GradeEnabled",1);mat.SetVector("_HighlightRoll",new Vector4(g.highlightKnee,g.highlightCeiling,0,0));
 mat.SetFloat("_FarAmbient",p.gain);mat.SetFloat("_Built",p.name.StartsWith("NK_Band")||p.name.StartsWith("NK_Niche")||p.name.StartsWith("NK_Trellis")||p.name.StartsWith("NK_Rib")||p.name.StartsWith("NK_Mouth")||p.name=="SET_B04_SeedArchive"||p.name.StartsWith("SET_B04_Rock_")?1:0);
 mat.SetVector("_PanelRect",new Vector4(r[0],r[1],r[2],r[3]));mat.SetFloat("_Feather",p.feather>0?p.feather:p.name.StartsWith("NK_Wall_Rock")?0f:p.name.StartsWith("NK_Band")?.45f:0f);
 mat.SetFloat("_JoinFeather",p.name.StartsWith("NK_Rock_Buttress")?.6f:0f);
 var domain=p.domain=="B05lower"?"P_B04_B05":p.domain;var lm=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"LM_"+domain+".png");mat.SetTexture("_RoomMap",lm);var lr=domain=="B04"?new Vector4(1564,270,64,36):domain=="P_B03_B04"?new Vector4(1594,298,68,30):new Vector4(1620,276,32,20);mat.SetVector("_RoomRect",lr);mat.SetTexture("_LilacMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"LM_B04_Lilac.png"));mat.SetFloat("_LilacEnabled",domain=="B04"?1:0);
 owned.Add(mat);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.sortingOrder=p.order;renderer.enabled=!p.near||Near;pieces.Add(new Piece{t=go.transform,basis=go.transform.position,motion=new Vector2(p.motion[0],p.motion[1]),anchor=new Vector2(p.anchor[0],p.anchor[1]),renderer=renderer,near=p.near});
 }
 [MenuItem("Qolossal/Art Review/Visit Nursery in Play")]
 public static void Visit(){if(!Enabled)SetEnabled(true);if(!Application.isPlaying)return;var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player)player.PlaceAt(new Vector2(1588.32f,270.66f),1,true);}
}

