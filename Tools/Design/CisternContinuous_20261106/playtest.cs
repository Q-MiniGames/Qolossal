if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
string run="fixed versus gentle",output="Tools/Design/CisternContinuous_20261106/Play/";System.IO.Directory.CreateDirectory(output);
var original=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath,testSave=System.IO.Path.GetFullPath(output+"isolated_"+Guid.NewGuid().ToString("N")+".json");object oldData=dataField.GetValue(null);
string Hash(string p){if(!System.IO.File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p)));}
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,Hash);
bool oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=EditorSettings.enterPlayModeOptions;
var oldInputBehavior=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;var oldInputBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior; UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus; CaveParallaxComparison comparison=null; UnityEngine.InputSystem.Keyboard keyboard=null; bool comparePress=false,keyboardPassed=false,padPassed=false; UnityEngine.InputSystem.Gamepad pad=null;bool jump=false,moving=false,monitor=false;float targetX=0;string label="";int sampled=-1;Vector3 renderedPosition=Vector3.zero;float renderedSize=5; int oldFrameRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;
var rows=new System.Collections.Generic.List<object>();var samples=new System.Collections.Generic.List<float[]>();var errors=new System.Collections.Generic.List<string>();
PlayerMovement Player()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
void Neutral(){jump=false;moving=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());}
void Place(float x){var p=Player();var hits=Physics2D.RaycastAll(new Vector2(x,109),Vector2.down,8,LayerMask.GetMask("Ground"));var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);if(hit.collider==null)throw new Exception("Missing floor "+x);p.PlaceAt(new Vector2(x,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();}
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
steps.Add(("initialize",2,()=>{pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();foreach(var gate in UnityEngine.Object.FindObjectsByType<MraGate>(FindObjectsSortMode.None)){var room=gate.GetComponentInParent<UndergroundRoom>();if(room!=null&&CaveParallaxComparison.Includes(room.roomId))gate.SetOpen(true);}comparison=new GameObject("Cave comparison test").AddComponent<CaveParallaxComparison>();comparison.Configure(new Vector2(1610,107.5f));if(comparison.BindingCount!=7)throw new Exception("Expected 7 live room/passage bindings, got "+comparison.BindingCount);comparison.SetContinuousPainting(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png"),new Vector4(1620,109.5f,48,16));Place(1610);}));
foreach(var mode in new[]{"A","B"}){
 string current=mode;
 if(mode=="B"){
  steps.Add(("press F8",.3,()=>{keyboard.MakeCurrent();System.IO.File.WriteAllText(output+"input_state.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{busy=MraState.Busy,blocked=GamePauseMenu.BlocksGameplayInput,enabled=comparison.enabled,active=comparison.gameObject.activeInHierarchy,count=UnityEngine.Object.FindObjectsByType<CaveParallaxComparison>(FindObjectsSortMode.None).Length,gentle=comparison.Gentle,keyboard=UnityEngine.InputSystem.Keyboard.current.name}));UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F8));}));
  steps.Add(("release F8",.3,()=>{UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());keyboardPassed=comparison.Gentle;if(!keyboardPassed)errors.Add("F8 did not switch to gentle mode");}));
 }
 steps.Add((current+" settle",1,()=>Place(1610)));
 steps.Add((current+" approach",1,()=>Begin(current+"_approach")));
 steps.Add((current+" capture approach",.1,()=>{Finish(true);var bridge=CavePassagePresentation.Instance;if(bridge==null||bridge.ConnectedCount!=63||bridge.MissingCount!=0)errors.Add("Runtime passage setup incomplete");}));
 steps.Add((current+" walk into passage",6,()=>{Begin(current+"_walk_into_passage");moving=true;targetX=1630;}));
 steps.Add((current+" check crossing",.1,()=>{if(Player().transform.position.x<1629.5f)errors.Add(current+" passage crossing failed");Finish(true);}));
 steps.Add((current+" walk back",6,()=>{Begin(current+"_walk_back");moving=true;targetX=1610;}));
 steps.Add((current+" check return",.1,()=>{if(Player().transform.position.x>1610.5f)errors.Add(current+" passage return failed");Finish(true);}));
}
steps.Add(("controller mode switch",.2,()=>comparePress=true));
steps.Add(("release controller",.3,()=>{comparePress=false;padPassed=!comparison.Gentle;if(!padPassed)errors.Add("Left stick click did not switch to fixed mode");}));
steps.Add(("verify input controls",.1,()=>{if(!keyboardPassed||!padPassed)errors.Add("Input comparison checks failed");}));
fileField.SetValue(null,testSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/Qolossal_Underground.unity");
int step=0;double ready=0,deadline=EditorApplication.timeSinceStartup+60;bool started=false,stopping=false;
Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
EditorApplication.CallbackFunction tick=null;tick=()=>{try{
 if(stopping){if(EditorApplication.isPlaying)return;EditorApplication.update-=tick;Application.logMessageReceived-=log;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=Sample;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);if(keyboard!=null&&keyboard.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=oldInputBehavior;UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=oldInputBackground;fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVSync;UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(original);bool saves=hashes.All(h=>Hash(h.Key)==h.Value);System.IO.File.WriteAllText(output+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,run,steps=step,total=steps.Count,rows,errors,normalSavesPreserved=saves,settingsRestored=true,keyboardPassed,padPassed,measurement="Actual render-end callback after VistaZone.LateUpdate, deduplicated per frame"},Newtonsoft.Json.Formatting.Indented));return;}
 if(!EditorApplication.isPlaying){if(started||EditorApplication.timeSinceStartup>deadline)throw new Exception("Play stopped or failed to start");return;}if(!started){started=true;ready=Time.timeAsDouble+2;deadline=EditorApplication.timeSinceStartup+60;}
 EditorApplication.QueuePlayerLoopUpdate();var p=Player();if(p!=null)typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(p.GetComponent<PlayerHealth>(),float.PositiveInfinity);
 if(pad!=null){float dx=p!=null?targetX-p.transform.position.x:0;var input=new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(moving&&Mathf.Abs(dx)>.1f?Mathf.Sign(dx)*.55f:0,0)};if(jump)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);if(comparePress)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.LeftStick);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,input);}
 if(Time.timeAsDouble<ready){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Simulation stalled");return;}
 var next=steps[step];next.action();step++;System.IO.File.WriteAllText(output+"progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,next.name,frame=Time.frameCount}));ready=Time.timeAsDouble+next.delay;deadline=EditorApplication.timeSinceStartup+60;if(step==steps.Count){stopping=true;monitor=false;EditorApplication.isPlaying=false;}
 }catch(Exception ex){errors.Add(ex.ToString());monitor=false;Neutral();stopping=true;EditorApplication.isPlaying=false;}};
EditorApplication.update+=tick;EditorApplication.isPlaying=true;return new{run,started=true,steps=steps.Count};






