if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
string run="after",output="Tools/Design/QvaleJumpCamera_20261104/"+run+"/";System.IO.Directory.CreateDirectory(output);
var original=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath,testSave=System.IO.Path.GetFullPath(output+"isolated_"+Guid.NewGuid().ToString("N")+".json");object oldData=dataField.GetValue(null);
string Hash(string p){if(!System.IO.File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p)));}
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,Hash);
bool oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=EditorSettings.enterPlayModeOptions;
UnityEngine.InputSystem.Gamepad pad=null;bool jump=false,moving=false,monitor=false;float targetX=0;string label="";int sampled=-1;Vector3 renderedPosition=Vector3.zero;float renderedSize=5; int oldFrameRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;
var rows=new System.Collections.Generic.List<object>();var samples=new System.Collections.Generic.List<float[]>();var errors=new System.Collections.Generic.List<string>();
PlayerMovement Player()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
void Neutral(){jump=false;moving=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());}
void Place(float x){var p=Player();var hits=Physics2D.RaycastAll(new Vector2(x,35),Vector2.down,40,LayerMask.GetMask("Ground"));var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);if(hit.collider==null)throw new Exception("Missing floor "+x);p.PlaceAt(new Vector2(x,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();}
void Sample(Camera cam){if(!monitor||cam!=Camera.main||sampled==Time.frameCount||Player()==null)return;sampled=Time.frameCount;var b=Player().GetComponent<BoxCollider2D>().bounds;var lo=cam.WorldToViewportPoint(new Vector3(b.center.x,b.min.y));var hi=cam.WorldToViewportPoint(new Vector3(b.center.x,b.max.y));samples.Add(new[]{(float)Time.frameCount,lo.y,hi.y,cam.orthographicSize,cam.transform.position.y,Player().transform.position.x,Player().IsGrounded?1f:0f,VistaZone.CurrentLift,Time.time,Time.deltaTime,b.min.y,b.max.y});renderedPosition=cam.transform.position;renderedSize=cam.orthographicSize;}
UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=Rendered;
void Rendered(UnityEngine.Rendering.ScriptableRenderContext context,Camera cam)=>Sample(cam);
Camera.onPostRender+=Sample;
void Begin(string name){label=name;samples.Clear();sampled=-1;monitor=true;}
void Capture(){var cam=Camera.main;var oldPos=cam.transform.position;float oldSize=cam.orthographicSize;var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;monitor=false;try{cam.transform.position=renderedPosition;cam.orthographicSize=renderedSize;foreach(int w in new[]{1920,3840}){int h=w*9/16;var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+label+(w==3840?"_4K":"_1080p")+".png",tex.EncodeToPNG());}finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}}}finally{cam.transform.position=oldPos;cam.orthographicSize=oldSize;}}
void Finish(bool capture=false){monitor=false;float min=samples.Count>0?samples.Min(s=>s[1]):-1,max=samples.Count>0?samples.Max(s=>s[2]):2;float maxCameraStep=samples.Count>1?samples.Skip(1).Select((s,i)=>Mathf.Abs(s[4]-samples[i][4])).Max():0;
float cameraTravel=samples.Count>0?samples.Max(s=>s[4])-samples.Min(s=>s[4]):0;
float playerTravel=samples.Count>0?samples.Max(s=>s[10])-samples.Min(s=>s[10]):0;
int airborneFrames=samples.Count(s=>s[6]==0);
bool ordinaryJump=label.Contains("jump");
bool passed=samples.Count>=10&&min>=.075f&&max<=.85f&&(!ordinaryJump||(maxCameraStep<.1f&&cameraTravel<.3f&&playerTravel>1.5f&&airborneFrames>8));rows.Add(new{label,passed,frames=samples.Count,maxCameraStep,cameraTravel,playerTravel,airborneFrames,minFeet=min,maxHead=max,last=samples.LastOrDefault(),samples=samples.ToArray()});if(capture)Capture();Neutral();}
var steps=new System.Collections.Generic.List<(string name,double delay,Action action)>();
steps.Add(("initialize",2,()=>{pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();Place(129);Begin("street_129");}));
steps.Add(("street idle",.1,()=>Finish(true)));
if(run!="before"){
 foreach(float x in new[]{96f,163f,200f,230f}){float at=x;steps.Add(("place "+x,2.4,()=>{Place(at);Begin("street_"+at);}));steps.Add(("check "+x,.1,()=>Finish()));}
 steps.Add(("jump setup",1,()=>Place(129)));
 steps.Add(("jump",.45,()=>{Begin("street_jump");jump=true;}));steps.Add(("release jump",2,()=>jump=false));steps.Add(("check jump",.1,()=>Finish()));
 foreach(int fps in new[]{60,30}){
  int cap=fps;
  steps.Add(("jump cap setup",2,()=>{Place(129);Application.targetFrameRate=cap;}));
  steps.Add(("repeat jump",.45,()=>{Begin("repeat_jump_"+cap);jump=true;}));
  steps.Add(("release",2,()=>jump=false));steps.Add(("check repeat",.1,()=>Finish()));
 }
 steps.Add(("running jump setup",2,()=>{Place(175);Application.targetFrameRate=60;}));
 steps.Add(("run before jump",.8,()=>{moving=true;targetX=195;}));
 steps.Add(("running jump",.45,()=>{Begin("running_jump");jump=true;}));
 steps.Add(("release running jump",2,()=>jump=false));steps.Add(("check running jump",.1,()=>Finish()));
 steps.Add(("launch",3.8,()=>{Begin("street_high_launch");if(!Player().LaunchUp(18))throw new Exception("Launch refused");}));steps.Add(("check launch",.1,()=>Finish()));
 foreach(var edge in new[]{new[]{91f,97f},new[]{235f,230f},new[]{97f,91f},new[]{230f,235f}}){var pair=edge;steps.Add(("edge setup",2.5,()=>Place(pair[0])));steps.Add(("edge walk",4,()=>{Begin("walk_"+pair[0]+"_"+pair[1]);moving=true;targetX=pair[1];}));steps.Add(("edge verify",1.7,()=>{bool crossed=pair[1]>233?Player().transform.position.x>233.5f:Mathf.Abs(Player().transform.position.x-pair[1])<.5f;if(!crossed)errors.Add("Did not cross boundary "+label);Neutral();}));steps.Add(("edge finish",.1,()=>Finish()));}
 steps.Add(("normal view settle",2.5,()=>{Place(80);Begin("outside_vista");}));steps.Add(("normal view verify",.1,()=>{Finish();if(Mathf.Abs(Camera.main.orthographicSize-5)>.01f)errors.Add("Normal zoom not restored");}));
}
foreach(var row in Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/Design/QvaleVista_20261104/zones.json")).Where(z=>(string)z["scene"]!="MRAtlas_MR04_Qvale")){
 string scene=(string)row["scene"];float zoom=(float)row["size"];
 steps.Add((scene+" load",2,()=>{Neutral();monitor=false;UnityEngine.SceneManagement.SceneManager.LoadScene(scene);}));
 steps.Add((scene+" lookout",2.5,()=>{var zone=UnityEngine.Object.FindObjectsByType<VistaZone>(FindObjectsSortMode.None).First();var b=zone.GetComponent<Collider2D>().bounds;var hit=Physics2D.RaycastAll(new Vector2(b.center.x,b.max.y+.1f),Vector2.down,b.size.y+5,LayerMask.GetMask("Ground")).First(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);Player().PlaceAt(hit.point+Vector2.up*.66f,1,true);Physics2D.SyncTransforms();Begin(scene+"_vista");}));
 steps.Add((scene+" verify",.1,()=>{Finish();if(Mathf.Abs(Camera.main.orthographicSize-zoom)>.02f)errors.Add("Authored lookout zoom not reached: "+scene); }));
}
fileField.SetValue(null,testSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR04_Qvale.unity");
int step=0;double ready=0,deadline=EditorApplication.timeSinceStartup+60;bool started=false,stopping=false;
Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
EditorApplication.CallbackFunction tick=null;tick=()=>{try{
 if(stopping){if(EditorApplication.isPlaying)return;EditorApplication.update-=tick;Application.logMessageReceived-=log;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=Sample;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVSync;UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(original);bool saves=hashes.All(h=>Hash(h.Key)==h.Value);System.IO.File.WriteAllText(output+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,run,steps=step,total=steps.Count,rows,errors,normalSavesPreserved=saves,settingsRestored=true,measurement="Actual render-end callback after VistaZone.LateUpdate, deduplicated per frame"},Newtonsoft.Json.Formatting.Indented));return;}
 if(!EditorApplication.isPlaying){if(started||EditorApplication.timeSinceStartup>deadline)throw new Exception("Play stopped or failed to start");return;}if(!started){started=true;ready=Time.timeAsDouble+2;deadline=EditorApplication.timeSinceStartup+60;}
 EditorApplication.QueuePlayerLoopUpdate();var p=Player();if(p!=null)typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(p.GetComponent<PlayerHealth>(),float.PositiveInfinity);
 if(pad!=null){float dx=p!=null?targetX-p.transform.position.x:0;var input=new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(moving&&Mathf.Abs(dx)>.1f?Mathf.Sign(dx)*.55f:0,0)};if(jump)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,input);}
 if(Time.timeAsDouble<ready){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Simulation stalled");return;}
 var next=steps[step];next.action();step++;System.IO.File.WriteAllText(output+"progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,next.name,frame=Time.frameCount}));ready=Time.timeAsDouble+next.delay;deadline=EditorApplication.timeSinceStartup+60;if(step==steps.Count){stopping=true;monitor=false;EditorApplication.isPlaying=false;}
 }catch(Exception ex){errors.Add(ex.ToString());monitor=false;Neutral();stopping=true;EditorApplication.isPlaying=false;}};
EditorApplication.update+=tick;EditorApplication.isPlaying=true;return new{run,started=true,steps=steps.Count};


