// Execute as an Editor C# snippet. No scene or asset is saved by this fixture.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Edit mode required");
for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Save the current scene edits before running the smoke fixture");
string output=System.IO.Path.GetFullPath("Tools/Design/WorldRevision_20261104/CaveVista/Play/");
System.IO.Directory.CreateDirectory(output);
var originalScenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);
var dataField=typeof(GameSave).GetField("data",flags);
var shieldField=typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
if(fileField==null||dataField==null||shieldField==null)throw new System.Exception("Fixture isolation fields changed");
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath;
object oldData=dataField.GetValue(null);
string testSave=output+"isolated_save_"+System.Guid.NewGuid().ToString("N")+".json";
System.Func<string,string> hash=p=>{if(!System.IO.File.Exists(p))return null;using(var s=System.IO.File.OpenRead(p))using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(s));};
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,hash);
bool oldOptionsEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;
var oldOptions=UnityEditor.EditorSettings.enterPlayModeOptions;
bool oldBackground=Application.runInBackground,oldPaused=UnityEditor.EditorApplication.isPaused;
float oldTimeScale=Time.timeScale;
// Real controller jumps and normal Flower launch path; no camera reflection/tuning.
var results=new System.Collections.Generic.List<object>();
var captures=new System.Collections.Generic.List<object>();
var errors=new System.Collections.Generic.List<string>();
var consoleErrors=new System.Collections.Generic.List<object>();
var samples=new System.Collections.Generic.List<Newtonsoft.Json.Linq.JObject>();
var representative=new System.Collections.Generic.HashSet<string>{"A01","C01","D01","E05","MR01_C06","MR02_C06","MR03_C06","MR05_C01","MR06_C01","MR07_C01","PC01","HEART"};
var undergroundCases=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Tools/Design/WorldRefresh_20261104/Underground/settle_result.json"))["results"].Where(x=>(string)x["kind"]=="checkpoint").ToArray();
if(undergroundCases.Length!=89)throw new System.Exception("Expected 89 underground checkpoint identifiers");
System.Func<PlayerMovement> player=()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
UnityEngine.InputSystem.Gamepad pad=null;
string activeId="",activeKind="";
bool monitoring=false,jumpHeld=false,apexCaptured=false;
int respawnStart=0,launchStart=0;
float baselineCentre=0;
double sampleStart=0;
System.Action neutral=()=>{jumpHeld=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());};
System.Func<Newtonsoft.Json.Linq.JObject> measurement=()=>{
 var p=player();var cam=Camera.main;var b=p.GetComponent<BoxCollider2D>().bounds;
 var feet=cam.WorldToViewportPoint(new Vector3(b.center.x,b.min.y,0));
 var head=cam.WorldToViewportPoint(new Vector3(b.center.x,b.max.y,0));
 var centre=cam.WorldToViewportPoint(b.center);
 return new Newtonsoft.Json.Linq.JObject{{"frame",Time.frameCount},{"t",Time.timeAsDouble-sampleStart},{"feet",feet.y},{"head",head.y},{"centre",centre.y},{"cameraY",cam.transform.position.y},{"cameraSize",cam.orthographicSize},{"grounded",p.IsGrounded},{"x",p.transform.position.x},{"y",p.transform.position.y},{"velocityY",p.GetComponent<Rigidbody2D>().linearVelocity.y}};
};
System.Action<string> capture=id=>{
 var cam=Camera.main;var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;float oldAspect=cam.aspect;
 var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
 string file=output+id+"_4K.png";
 try{cam.aspect=16f/9f;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());captures.Add(new{id,file,width=3840,height=2160,mode="Native Play-mode camera render",frame=Time.frameCount});}
 finally{cam.targetTexture=oldTarget;cam.aspect=oldAspect;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}
};
System.Action<string,string> begin=(id,kind)=>{
 neutral();activeId=id;activeKind=kind;samples.Clear();sampleStart=Time.timeAsDouble;apexCaptured=false;
 var p=player();respawnStart=p.RespawnVersion;launchStart=p.LaunchVersion;baselineCentre=(float)measurement()["centre"];monitoring=true;
};
System.Action<bool> finish=takeCapture=>{
 monitoring=false;neutral();var p=player();var cam=Camera.main;var last=measurement();
 var airborne=samples.Where(x=>!(bool)x["grounded"]).ToArray();
 float minFeet=samples.Count==0?-1:samples.Min(x=>(float)x["feet"]),maxHead=samples.Count==0?2:samples.Max(x=>(float)x["head"]);
 float maxCentre=samples.Count==0?-1:samples.Max(x=>(float)x["centre"]),idleJitter=samples.Count==0?1:samples.Max(x=>(float)x["feet"])-minFeet;
 bool lowFoot=(float)last["feet"]>=.13f&&(float)last["feet"]<=.18f;
 bool passed=samples.Count>=6&&p.IsGrounded&&lowFoot&&p.RespawnVersion==respawnStart&&samples.All(x=>(float)x["cameraSize"]>=4.999f);
 if(activeKind=="idle")passed&=minFeet>=.13f&&samples.Max(x=>(float)x["feet"])<=.18f&&idleJitter<.008f;
 else passed&=airborne.Length>=6&&p.LaunchVersion>launchStart&&maxCentre>=baselineCentre+.08f&&minFeet>=.075f&&maxHead<=.79f;
 if(activeKind=="high launch")passed&=maxCentre>=.45f;
 // Overhead collision is diagnostic, not proof of visible ceiling art. A roof
 // below viewport .75 deserves capture review even when the foot metric passes.
 var b=p.GetComponent<BoxCollider2D>().bounds;var overhead=new System.Collections.Generic.List<object>();
 bool ceilingRisk=false;
 foreach(float dx in new[]{-2f,0f,2f}){
  var hits=Physics2D.RaycastAll(new Vector2(b.center.x+dx,b.max.y+.04f),Vector2.up,cam.orthographicSize*2f,LayerMask.GetMask("Ground"));
  var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y<-.5f);
  if(hit.collider!=null){float viewport=cam.WorldToViewportPoint(hit.point).y;ceilingRisk|=viewport<.75f;overhead.Add(new{dx,name=hit.collider.name,worldY=hit.point.y,viewportY=viewport});}
 }
 results.Add(new{id=activeId,kind=activeKind,passed,lowFoot,footViewport=(float)last["feet"],centreViewport=(float)last["centre"],baselineCentre,maxCentre,minFeet,maxHead,idleJitter,airborneFrames=airborne.Length,respawnDelta=p.RespawnVersion-respawnStart,launchDelta=p.LaunchVersion-launchStart,ceilingRisk,overhead,activeLock=CameraLockZone.Current!=null,samples=samples.ToArray()});
 if(!passed)errors.Add(activeId+" "+activeKind+" did not meet camera framing metrics");
 if(takeCapture||ceilingRisk||!passed)capture(activeId+"_"+activeKind.Replace(' ','_'));
 System.IO.File.WriteAllText(output+"camera_cases.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
};
var steps=new System.Collections.Generic.List<(string label,string scene,double delay,System.Action action)>();
foreach(string sceneName in System.Array.Empty<string>()){
 string scene=sceneName,region=scene.Substring(8,4);
 steps.Add((region+" load",null,2.2,()=>{neutral();monitoring=false;UnityEngine.SceneManagement.SceneManager.LoadScene(scene);}));
 steps.Add((region+" idle start",scene,.7,()=>begin(region,"idle")));
 steps.Add((region+" idle check",scene,.1,()=>finish(true)));
 steps.Add((region+" jump press",scene,.45,()=>{begin(region,"jump");jumpHeld=true;}));
 steps.Add((region+" jump release",scene,2.3,()=>neutral()));
 steps.Add((region+" jump check",scene,.1,()=>finish(false)));
 steps.Add((region+" high launch",scene,3.4,()=>{begin(region,"high launch");if(!player().LaunchUp(18f))throw new System.Exception("LaunchUp refused");}));
 steps.Add((region+" high launch check",scene,.1,()=>finish(false)));
}
steps.Add(("MR03 final decoration load",null,2.2,()=>{neutral();monitoring=false;UnityEngine.SceneManagement.SceneManager.LoadScene(RegionalSceneCatalog.Surface[2]);}));
steps.Add(("MR03 final idle start",RegionalSceneCatalog.Surface[2],.7,()=>begin("MR03","idle")));
steps.Add(("MR03 final idle check",RegionalSceneCatalog.Surface[2],.1,()=>finish(true)));
steps.Add(("Underground load",null,2.2,()=>{neutral();monitoring=false;UnityEngine.SceneManagement.SceneManager.LoadScene("Qolossal_Underground");}));
foreach(var row in undergroundCases){
 string id=(string)row["id"],room=(string)row["room"];
 steps.Add((room+" checkpoint place","Qolossal_Underground",1.5,()=>{
  neutral();monitoring=false;var cp=UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Single(c=>c.CheckpointId==id);
  player().PlaceAt(cp.SpawnPosition,1,true);Physics2D.SyncTransforms();
 }));
 steps.Add((room+" idle start","Qolossal_Underground",.45,()=>begin(room,"idle")));
 steps.Add((room+" idle check","Qolossal_Underground",.03,()=>finish(true)));
}
int expectedCases=undergroundCases.Length+1;
int step=0,lastFrame=-1,sampledFrame=-1;
bool stopping=false,started=false,cleaned=false;
double beginning=UnityEditor.EditorApplication.timeSinceStartup,deadline=beginning+60,ready=0;
UnityEngine.Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)consoleErrors.Add(new{type=type.ToString(),message,stack,frame=Time.frameCount,step});};
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action cleanup=()=>{
 if(cleaned)return;cleaned=true;
 UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=log;
 try{if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);}catch(System.Exception e){errors.Add("Input cleanup: "+e.Message);}
 fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);
 UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;UnityEditor.EditorSettings.enterPlayModeOptions=oldOptions;
 Application.runInBackground=oldBackground;Time.timeScale=oldTimeScale;UnityEditor.EditorApplication.isPaused=oldPaused;
 bool preserved=hashes.All(h=>hash(h.Key)==h.Value);if(!preserved)errors.Add("Normal save files changed");
 bool restored=false;try{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(originalScenes);restored=true;}catch(System.Exception e){errors.Add("Scene restore: "+e.Message);}
 int ceilingRiskCount=results.Count(r=>(bool)r.GetType().GetProperty("ceilingRisk").GetValue(r));
 System.IO.File.WriteAllText(output+"camera_result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,passed=step==steps.Count&&results.Count==expectedCases&&errors.Count==0&&consoleErrors.Count==0,caseCount=expectedCases,cases=results,captures,errors,consoleErrors,ceilingRiskCount,ceilingVisualReviewRequired=ceilingRiskCount>0,stepsCompleted=step,stepsTotal=steps.Count,testSave,normalSavesPreserved=preserved,settingsRestored=true,scenesRestored=restored,enemyDamageSuppressedForCameraTest=true,scope="Final cave visual pass: all 89 actual checkpoint idles captured in Play mode at native 3840x2160 after applying revised painted compositions. Overhead rays are review flags, not screenshot passes. Not full traversal or hardware/performance certification."},Newtonsoft.Json.Formatting.Indented));
};
System.Action requestStop=()=>{
 stopping=true;monitoring=false;
 try{neutral();}catch(System.Exception e){errors.Add("Input neutralization: "+e.Message);}
 if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)UnityEditor.EditorApplication.isPlaying=false;else cleanup();
};
tick=()=>{
 try{
  double now=UnityEditor.EditorApplication.timeSinceStartup;
  if(stopping){if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)cleanup();return;}
  if(now-beginning>900)throw new System.Exception("Overall camera fixture timeout");
  if(!UnityEditor.EditorApplication.isPlaying){if(started)throw new System.Exception("Play mode stopped early");if(now>deadline)throw new System.Exception("Play start timeout");return;}
  if(!started){started=true;ready=Time.timeAsDouble+2;deadline=now+60;pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();}
  UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
  var p=player();if(p!=null){var health=p.GetComponent<PlayerHealth>();if(health!=null)shieldField.SetValue(health,float.PositiveInfinity);}
  if(GameSave.FilePath!=testSave)throw new System.Exception("Save isolation changed");
  string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  if(monitoring&&Time.frameCount!=sampledFrame){
   if(p==null||Camera.main==null)throw new System.Exception("Camera/player missing");sampledFrame=Time.frameCount;var value=measurement();samples.Add(value);
   if(activeKind!="idle"&&!apexCaptured&&!(bool)value["grounded"]&&(float)value["velocityY"]<=0&&(float)value["centre"]>baselineCentre+.08f){apexCaptured=true;capture(activeId+"_"+activeKind.Replace(' ','_')+"_apex");}
  }
  if(pad==null||!pad.added)throw new System.Exception("Fixture gamepad disconnected");
  var input=new UnityEngine.InputSystem.LowLevel.GamepadState();
  if(jumpHeld)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
  UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,input);
  if(Time.timeAsDouble<ready||Time.frameCount<=lastFrame||AreaTransition.IsTransitioning||(steps[step].scene!=null&&scene!=steps[step].scene)){
   if(now>deadline)throw new System.Exception("Simulation/scene timeout at "+steps[step].label);return;
  }
  var next=steps[step];lastFrame=Time.frameCount;next.action();step++;
  System.IO.File.WriteAllText(output+"camera_progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,label=next.label,casesCompleted=results.Count,errors=errors.Count,frame=lastFrame,elapsedWallSeconds=now-beginning}));
  ready=Time.timeAsDouble+next.delay;deadline=UnityEditor.EditorApplication.timeSinceStartup+45;
  if(step==steps.Count)requestStop();
 }catch(System.Exception e){errors.Add(e.ToString());System.IO.File.WriteAllText(output+"camera_failure.txt",string.Join("\n\n",errors));requestStop();}
};
try{
 fileField.SetValue(null,testSave);dataField.SetValue(null,null);
 UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;
 Application.runInBackground=true;Time.timeScale=1;UnityEditor.EditorApplication.isPaused=false;
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Surface[0]+".unity");
 Application.logMessageReceived+=log;UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;
}catch(System.Exception e){errors.Add(e.ToString());cleanup();throw;}
return new{started=true,cases=expectedCases,steps=steps.Count,output,testSave,estimatedSimulationSeconds=240,maximumWallSeconds=900};

