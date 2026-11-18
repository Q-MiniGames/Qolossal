// Execute as an Editor C# snippet. No scene or asset is saved by this fixture.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Edit mode required");
for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
    if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.Exception("Save the current scene edits before running the smoke fixture");
string output=System.IO.Path.GetFullPath("Tools/Design/WorldRevision_20261104/Corners/");
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
var cases=new System.Collections.Generic.List<Newtonsoft.Json.Linq.JObject>();
foreach(string scene in RegionalSceneCatalog.Surface){
 string region=scene.Substring(8,4);
 foreach(string receiptPath in new[]{"Tools/Design/WorldRevision_20261104/Surface/"+region+"_rounded_corners.json","Tools/Design/WorldRevision_20261104/Surface/"+region+"_rounded_sloped_corners.json"}.Where(System.IO.File.Exists)){
 var receipt=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(receiptPath));
 foreach(var record in receipt["records"]){
  var before=record["before"].ToArray();var after=record["after"].ToArray();int found=0;
  for(int i=1;i<before.Length-1;i++){
   float x=(float)before[i][0],y=(float)before[i][1];
   if(after.Any(v=>Mathf.Abs((float)v[0]-x)<.0001f&&Mathf.Abs((float)v[1]-y)<.0001f))continue;
   float dx0=x-(float)before[i-1][0],dy0=y-(float)before[i-1][1],dx1=(float)before[i+1][0]-x,dy1=(float)before[i+1][1]-y;
   bool leftDrop=Mathf.Abs(dx0)<.0001f&&dy0>.5f&&dx1>.5f&&Mathf.Abs(dy1)<=dx1&&dx0*dy1-dy0*dx1<0;
   bool rightDrop=dx0>.5f&&Mathf.Abs(dy0)<=dx0&&Mathf.Abs(dx1)<.0001f&&dy1<-.5f&&dx0*dy1-dy0*dx1<0;
   if(!leftDrop&&!rightDrop)throw new System.Exception("Changed vertex is not an exposed upper corner: "+region+" "+x);
   float direction=rightDrop?1f:-1f;float inside=Mathf.Min(1f,(leftDrop?dx1:dx0)*.55f);
   cases.Add(new Newtonsoft.Json.Linq.JObject{{"region",region},{"scene",scene},{"terrain",(string)record["path"]},{"a",x},{"b",y},{"direction",direction},{"plannedDistance",inside+.8f},{"inside",inside}});found++;
  }
  if(found!=(int)record["corners"])throw new System.Exception("Corner receipt count mismatch "+region);
 }
 }
}
if(cases.Count==0)throw new System.Exception("No applied corner receipts to exercise");
var results=new System.Collections.Generic.List<object>();
var captures=new System.Collections.Generic.List<object>();
var errors=new System.Collections.Generic.List<string>();
var consoleErrors=new System.Collections.Generic.List<object>();
var samples=new System.Collections.Generic.List<object>();
System.Func<PlayerMovement> player=()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
System.Func<Transform,string> hierarchy=t=>{string s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;};
// Read the saved live terrain, never the planned y0/y1. World transform is honored.
System.Func<MraTerrain,float,float> floor=(t,x)=>{
 var p=t.surface;
 for(int i=1;i<p.Length;i++){
  Vector3 a=t.transform.TransformPoint(p[i-1]),b=t.transform.TransformPoint(p[i]);
  if(Mathf.Abs(b.x-a.x)>.00001f&&x>=Mathf.Min(a.x,b.x)-.001f&&x<=Mathf.Max(a.x,b.x)+.001f)
   return Mathf.LerpUnclamped(a.y,b.y,(x-a.x)/(b.x-a.x));
 }
 throw new System.Exception("No terrain segment beneath test x="+x+" in "+hierarchy(t.transform));
};
UnityEngine.InputSystem.Gamepad pad=null;
MraTerrain activeTerrain=null;
Newtonsoft.Json.Linq.JObject activeCase=null;
bool walking=false,monitoring=false,fell=false;
float targetX=0,startX=0,minCenterGap=float.PositiveInfinity,maxCenterGap=float.NegativeInfinity;
float lipX=0,progressAnchorX=0,minimumTrailingClearance=float.PositiveInfinity,maxTrailingClearance=float.NegativeInfinity;
bool startedGrounded=false,groundContactBeforeLip=false,clearedLip=false,sustainedSnag=false;
double firstClearanceTime=-1,stallWindowStart=-1,maxStallSeconds=0;
int drivenFrames=0;
int respawnStart=0,groundFrames=0,walkFrames=0;
double walkStart=0,nextSample=0;
System.Action neutral=()=>{walking=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());};
System.Action<string> capture=region=>{
 var cam=Camera.main;if(cam==null)throw new System.Exception("Missing gameplay camera");
 var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;float oldAspect=cam.aspect;
 var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
 string file=output+region+"_entry_play_4K.png";
 try{cam.aspect=16f/9f;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());
  var p=player();var position=p.transform.position;captures.Add(new{region,file,width=3840,height=2160,mode="Play-mode camera render, native render target; no resize",frame=Time.frameCount,cameraSize=cam.orthographicSize,playerPosition=new[]{position.x,position.y,position.z},grounded=p.HasGroundContact});
  System.IO.File.WriteAllText(output+"corner_playtest_captures.json",Newtonsoft.Json.JsonConvert.SerializeObject(captures,Newtonsoft.Json.Formatting.Indented));
 }finally{cam.targetTexture=oldTarget;cam.aspect=oldAspect;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}
};
var steps=new System.Collections.Generic.List<(string label,string scene,double delay,System.Action action)>();
foreach(string sceneName in RegionalSceneCatalog.Surface){
 string scene=sceneName,region=scene.Substring(8,4);
 steps.Add((region+" load",null,2,()=>{neutral();monitoring=false;activeTerrain=null;activeCase=null;UnityEngine.SceneManagement.SceneManager.LoadScene(scene);}));
 steps.Add((region+" entry capture",scene,.1,()=>{if(player()==null)throw new System.Exception("Missing player in "+scene);capture(region);}));
 foreach(var row in cases.Where(c=>(string)c["scene"]==scene)){
  var test=row;
  steps.Add((region+" place connector "+test["a"],scene,1,()=>{
   neutral();monitoring=false;
   if(GameSave.FilePath!=testSave)throw new System.Exception("Save isolation failed");
   activeCase=test;
   activeTerrain=UnityEngine.Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).Single(t=>hierarchy(t.transform)==(string)test["terrain"]);
   float direction=(float)test["direction"];
   Vector3 corner=activeTerrain.transform.TransformPoint(new Vector3((float)test["a"],(float)test["b"],0));
   if(Mathf.Abs(activeTerrain.transform.eulerAngles.z)>.001f||Mathf.Abs(activeTerrain.transform.lossyScale.x-1)>.001f)throw new System.Exception("Unsupported corner transform");
   // Start inside the high ledge (adapt to short treads), then walk 0.8u beyond its rounded lip.
   lipX=corner.x;startX=corner.x-direction*(float)test["inside"];targetX=corner.x+direction*.8f;
   var p=player();p.ForceWalk=true;p.PlaceAt(new Vector2(startX,floor(activeTerrain,startX)+.66f),direction,true);
   Physics2D.SyncTransforms();
  }));
  steps.Add((region+" walk connector "+test["a"],scene,.65,()=>{
   var p=player();startX=p.transform.position.x;respawnStart=p.RespawnVersion;
   minCenterGap=float.PositiveInfinity;maxCenterGap=float.NegativeInfinity;groundFrames=walkFrames=0;fell=false;
   startedGrounded=p.HasGroundContact;groundContactBeforeLip=false;clearedLip=false;sustainedSnag=false;
   firstClearanceTime=-1;stallWindowStart=-1;maxStallSeconds=0;drivenFrames=0;progressAnchorX=startX;
   minimumTrailingClearance=float.PositiveInfinity;maxTrailingClearance=float.NegativeInfinity;
   samples.Clear();walkStart=Time.timeAsDouble;nextSample=walkStart;monitoring=true;walking=true;
  }));
  steps.Add((region+" check connector "+test["a"],scene,.1,()=>{
   neutral();monitoring=false;var p=player();float endX=p.transform.position.x,endY=p.transform.position.y;
   float groundY=activeTerrain.transform.TransformPoint(new Vector3(0,(float)test["b"],0)).y,advance=(endX-startX)*(float)test["direction"],gap=endY-groundY;
   var collider=p.GetComponent<BoxCollider2D>();float trailing=(float)test["direction"]>0?collider.bounds.min.x:collider.bounds.max.x;
   float finalTrailingClearance=(trailing-lipX)*(float)test["direction"];
   var failureReasons=new System.Collections.Generic.List<string>();
   if(!startedGrounded)failureReasons.Add("not-grounded-at-start");
   if(!groundContactBeforeLip)failureReasons.Add("no-ground-contact-before-lip");
   if(!clearedLip||finalTrailingClearance<.05f)failureReasons.Add("collider-did-not-clear-lip");
   if(sustainedSnag)failureReasons.Add("sustained-snag-while-driven");
   if(p.RespawnVersion!=respawnStart)failureReasons.Add("respawn");
   if(drivenFrames==0)failureReasons.Add("no-driven-motion-observed");
   bool passed=failureReasons.Count==0;
   results.Add(new{region,scene,terrain=(string)test["terrain"],a=(float)test["a"],b=(float)test["b"],plannedDistance=(float)test["plannedDistance"],startX,targetX,lipX,endX,endY,floorY=groundY,centerAboveFloor=gap,advance,passed,failureReasons,startedGrounded,groundContactBeforeLip,clearedLip,firstClearanceTime,finalTrailingClearance,maxTrailingClearance,minimumTrailingClearance,sustainedSnag,maxStallSeconds,drivenFrames,colliderHalfWidth=collider.bounds.extents.x,fell,respawnDelta=p.RespawnVersion-respawnStart,grounded=p.HasGroundContact,groundFrames,walkFrames,minCenterGap,maxCenterGap,elapsedSimulationSeconds=Time.timeAsDouble-walkStart,samples=samples.ToArray()});
   // Durable evidence after every case; no Unity vectors in JSON (normalized recurses).
   System.IO.File.WriteAllText(output+"corner_playtest_cases.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=false,casesCompleted=results.Count,caseCount=cases.Count,cases=results},Newtonsoft.Json.Formatting.Indented));
  }));
 }
}
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
 bool casesPassed=results.Count==cases.Count&&results.All(r=>(bool)r.GetType().GetProperty("passed").GetValue(r));
 System.IO.File.WriteAllText(output+"corner_playtest_result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{criteriaVersion="2-collider-clearance-and-driven-stall",complete=step==steps.Count,passed=step==steps.Count&&casesPassed&&captures.Count==7&&errors.Count==0&&consoleErrors.Count==0,caseCount=cases.Count,cases=results,captures,errors,consoleErrors,stepsCompleted=step,stepsTotal=steps.Count,testSave,normalSavesPreserved=preserved,settingsRestored=true,scenesRestored=restored,enemyDamageSuppressedForGeometryTest=true,realInput="Virtual Gamepad left stick in each lip outward direction through normal PlayerMovement input actions; ForceWalk=true",scope="Walk off every rounded exposed upper lip in its outward direction. Falls after clearing a lip are expected. Requires grounded start, ground contact before the lip, actual BoxCollider2D trailing edge >=.05u past the original corner at completion, no respawn, and no sustained .15s driven stop (<.02u progress after .12s acceleration grace) before first full-body clearance. Endpoint braking precision is not a geometry requirement. It does not establish upward wall/ledge climbing, full traversal, controller hardware, performance or console certification."},Newtonsoft.Json.Formatting.Indented));
};
System.Action requestStop=()=>{
 // Set lifecycle flags before input cleanup, including when Play mode was stopped externally.
 stopping=true;monitoring=false;walking=false;
 try{neutral();}catch(System.Exception e){errors.Add("Input neutralization: "+e.Message);}
 if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)UnityEditor.EditorApplication.isPlaying=false;
 else cleanup();
};
tick=()=>{
 try{
  double now=UnityEditor.EditorApplication.timeSinceStartup;
  if(stopping){if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)cleanup();return;}
  if(now-beginning>900)throw new System.Exception("Overall fixture timeout (900 wall seconds)");
  if(!UnityEditor.EditorApplication.isPlaying){if(started)throw new System.Exception("Play mode stopped before fixture completion");if(now>deadline)throw new System.Exception("Play start timeout");return;}
  if(!started){started=true;ready=Time.timeAsDouble+2;deadline=now+60;pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();}
  UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
  var p=player();if(p!=null){var health=p.GetComponent<PlayerHealth>();if(health!=null)shieldField.SetValue(health,float.PositiveInfinity);}
  if(GameSave.FilePath!=testSave)throw new System.Exception("Save isolation changed while playing");
  string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  if(monitoring){
   if(p==null||activeTerrain==null||scene!=(string)activeCase["scene"])throw new System.Exception("Unexpected player, terrain or scene change during connector walk");
   if(Time.frameCount!=sampledFrame){
    sampledFrame=Time.frameCount;walkFrames++;if(p.HasGroundContact)groundFrames++;
    float px=p.transform.position.x,py=p.transform.position.y,fy=activeTerrain.transform.TransformPoint(new Vector3(0,(float)activeCase["b"],0)).y,gap=py-fy;
    minCenterGap=Mathf.Min(minCenterGap,gap);maxCenterGap=Mathf.Max(maxCenterGap,gap);
    if(p.RespawnVersion!=respawnStart)fell=true;
    float direction=(float)activeCase["direction"];var bounds=p.GetComponent<BoxCollider2D>().bounds;
    float trailing=direction>0?bounds.min.x:bounds.max.x,trailingClearance=(trailing-lipX)*direction;
    minimumTrailingClearance=Mathf.Min(minimumTrailingClearance,trailingClearance);maxTrailingClearance=Mathf.Max(maxTrailingClearance,trailingClearance);
    if(p.HasGroundContact&&(px-lipX)*direction<0)groundContactBeforeLip=true;
    if(trailingClearance>=.05f){if(!clearedLip)firstClearanceTime=Time.timeAsDouble-walkStart;clearedLip=true;}
    // Evaluate a sustained snag only under outward drive before whole-body
    // clearance. Ignore initial acceleration and neutral airborne braking.
    // .15s with <.02u progress is a stop, far below the normal ~5u/s walk.
    bool driven=Time.timeAsDouble-walkStart>=.12&&p.MoveInput*direction>.5f&&!clearedLip;
    if(driven){
     drivenFrames++;
     if(stallWindowStart<0){stallWindowStart=Time.timeAsDouble;progressAnchorX=px;}
     else if((px-progressAnchorX)*direction>=.02f){stallWindowStart=Time.timeAsDouble;progressAnchorX=px;}
     double stalled=Time.timeAsDouble-stallWindowStart;maxStallSeconds=Math.Max(maxStallSeconds,stalled);
     if(stalled>=.15)sustainedSnag=true;
    }else{stallWindowStart=-1;progressAnchorX=px;}
    if(Time.timeAsDouble>=nextSample){var velocity=p.ObservedVelocity;samples.Add(new{frame=Time.frameCount,t=Time.timeAsDouble-walkStart,x=px,y=py,floorY=fy,centerAboveFloor=gap,grounded=p.HasGroundContact,velocity=new[]{velocity.x,velocity.y},moveInput=p.MoveInput,trailingClearance,clearedLip,driven,stallSeconds=stallWindowStart<0?0:Time.timeAsDouble-stallWindowStart});nextSample=Time.timeAsDouble+.10;}
   }
  }
  if(walking&&p!=null){if(pad==null||!pad.added)throw new System.Exception("Fixture gamepad disconnected during walk");float direction=(float)activeCase["direction"];float dx=(targetX-p.transform.position.x)*direction;float input=dx<=.06f?0f:direction*(dx<.45f?.38f:1f);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(input,0)});}
  if(Time.timeAsDouble<ready||Time.frameCount<=lastFrame||AreaTransition.IsTransitioning||(steps[step].scene!=null&&scene!=steps[step].scene)){
   if(now>deadline)throw new System.Exception("Simulation or scene timeout at "+steps[step].label+" scene="+scene+" frame="+Time.frameCount);
   return;
  }
  var next=steps[step];lastFrame=Time.frameCount;next.action();step++;
  System.IO.File.WriteAllText(output+"corner_playtest_progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,label=next.label,casesCompleted=results.Count,frame=lastFrame,elapsedWallSeconds=now-beginning}));
  ready=Time.timeAsDouble+next.delay;deadline=UnityEditor.EditorApplication.timeSinceStartup+45;
  if(step==steps.Count)requestStop();
 }catch(System.Exception e){errors.Add(e.ToString());System.IO.File.WriteAllText(output+"corner_playtest_failure.txt",string.Join("\n\n",errors));requestStop();}
};
try{
 fileField.SetValue(null,testSave);dataField.SetValue(null,null);
 UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;
 Application.runInBackground=true;Time.timeScale=1;UnityEditor.EditorApplication.isPaused=false;
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Surface[0]+".unity");
 Application.logMessageReceived+=log;UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;
}catch(System.Exception e){errors.Add(e.ToString());cleanup();throw;}
return new{started=true,cases=cases.Count,steps=steps.Count,output,testSave,estimatedSimulationSeconds=cases.Count*1.8+20,maximumWallSeconds=900};
