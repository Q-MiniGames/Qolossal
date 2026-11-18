if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
string run="preview",output="Tools/Design/DownhillPreview_20261104/Play/";System.IO.Directory.CreateDirectory(output);
var original=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath,testSave=System.IO.Path.GetFullPath(output+"isolated_"+Guid.NewGuid().ToString("N")+".json");object oldData=dataField.GetValue(null);
string Hash(string p){if(!System.IO.File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p)));}
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,Hash);
bool oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=EditorSettings.enterPlayModeOptions;
UnityEngine.InputSystem.Gamepad pad=null;UnityEngine.InputSystem.Keyboard keyboard=null;bool manualPad=false,manualDpad=false,manualKey=false,modal=false,aimHeld=false;bool jump=false,moving=false,monitor=false;float targetX=0;string label="";int sampled=-1;Vector3 renderedPosition=Vector3.zero;float renderedSize=5; int oldFrameRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;
var rows=new System.Collections.Generic.List<object>();var samples=new System.Collections.Generic.List<float[]>();var errors=new System.Collections.Generic.List<string>();
PlayerMovement Player()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
void Neutral(){jump=false;moving=false;manualPad=manualDpad=manualKey=aimHeld=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());}
void Place(float x,float facing=1){var p=Player();var hits=Physics2D.RaycastAll(new Vector2(x,250),Vector2.down,300,LayerMask.GetMask("Ground"));var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);if(hit.collider==null)throw new Exception("Missing floor "+x);p.PlaceAt(new Vector2(x,hit.point.y+.66f),facing,true);Physics2D.SyncTransforms();}
void Sample(Camera cam){if(!monitor||cam!=Camera.main||sampled==Time.frameCount||Player()==null)return;sampled=Time.frameCount;var b=Player().GetComponent<BoxCollider2D>().bounds;var lo=cam.WorldToViewportPoint(new Vector3(b.center.x,b.min.y));var hi=cam.WorldToViewportPoint(new Vector3(b.center.x,b.max.y));samples.Add(new[]{(float)Time.frameCount,lo.y,hi.y,cam.orthographicSize,cam.transform.position.y,Player().transform.position.x,Player().IsGrounded?1f:0f,VistaZone.CurrentLift,Time.time,Time.deltaTime,b.min.y,b.max.y,cam.GetComponent<CameraFollow>().DownwardPreview,cam.WorldToViewportPoint(new Vector3(181,91.714f,0)).y});renderedPosition=cam.transform.position;renderedSize=cam.orthographicSize;}
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
bool passed=samples.Count>=10&&min>=.075f&&max<=.91f&&(!ordinaryJump||(maxCameraStep<.1f&&cameraTravel<.3f&&playerTravel>1.5f&&airborneFrames>8));rows.Add(new{label,passed,frames=samples.Count,maxCameraStep,cameraTravel,playerTravel,airborneFrames,minFeet=min,maxHead=max,last=samples.LastOrDefault(),samples=samples.ToArray()});if(capture)Capture();Neutral();}
var steps=new System.Collections.Generic.List<(string name,double delay,Action action)>();
void CheckPreview(bool expected){float value=Camera.main.GetComponent<CameraFollow>().DownwardPreview;if(expected?value<3f:value>.06f)errors.Add(label+" preview="+value);}
steps.Add(("initialize",2,()=>{pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();Place(185,-1);Begin("automatic_ledge");}));
steps.Add(("check visible landing",.1,()=>{CheckPreview(true);if(samples.Last()[13]<.08f)errors.Add("Landing not visible before stepping off");Finish(true);}));
steps.Add(("walk down",3.5,()=>{Begin("walk_down");moving=true;targetX=178;}));
steps.Add(("check descent",.1,()=>{if(Player().transform.position.x>179)errors.Add("Did not cross ledge");Finish(true);}));
steps.Add(("running approach setup",2,()=>Place(192,1)));
steps.Add(("running approach",4,()=>{Begin("running_approach");moving=true;targetX=178;}));
steps.Add(("running approach check",.1,()=>{var beforeEdge=samples.Where(s=>s[6]==1&&s[5]>182&&s[5]<183).ToArray();if(beforeEdge.Length==0||beforeEdge.Max(s=>s[13])<.05f)errors.Add("Running approach did not reveal landing before edge: "+(beforeEdge.Length>0?beforeEdge.Max(s=>s[13]):-1));Finish();}));
steps.Add(("flat setup",2,()=>{Place(190,1);Begin("flat_idle");}));steps.Add(("flat check",.1,()=>{CheckPreview(false);Finish();}));
steps.Add(("keyboard peek",2,()=>{manualKey=true;Begin("keyboard_look_down");}));steps.Add(("keyboard check",.1,()=>{CheckPreview(true);Finish(true);}));
steps.Add(("release settle",2.2,()=>Begin("release_recovery")));steps.Add(("release check",.1,()=>{CheckPreview(false);Finish();}));
steps.Add(("right stick peek",2,()=>{manualPad=true;Begin("right_stick_look_down");}));steps.Add(("right stick check",.1,()=>{CheckPreview(true);Finish();}));
steps.Add(("dpad setup",2.2,()=>Place(190,1)));
steps.Add(("dpad peek",2,()=>{manualDpad=true;Begin("dpad_look_down");}));steps.Add(("dpad check",.1,()=>{CheckPreview(true);Finish();}));
steps.Add(("modal setup",2.2,()=>{Place(190,1);ModalUi.Open();modal=true;manualPad=true;Begin("modal_blocks_peek");}));steps.Add(("modal check",.1,()=>{CheckPreview(false);Finish();ModalUi.Close();modal=false;}));
steps.Add(("sling settle",.8,()=>Place(190,1)));
steps.Add(("sling aim setup",2,()=>{aimHeld=true;manualPad=true;Begin("sling_aim_blocks_peek");}));steps.Add(("sling check",.1,()=>{if(!UnityEngine.Object.FindAnyObjectByType<QoriArmory>().IsAiming)errors.Add("Aim input did not remain held");CheckPreview(false);Finish();UnityEngine.Object.FindAnyObjectByType<QoriArmory>().CancelAim();}));
steps.Add(("walk input setup",2.2,()=>Place(190,1)));
steps.Add(("manual before walk",2,()=>manualPad=true));
steps.Add(("walk cancels peek",2.5,()=>{Begin("walk_cancels_manual");moving=true;targetX=198;}));steps.Add(("walk cancel check",.1,()=>{CheckPreview(false);Finish();}));
steps.Add(("Qvale load",2,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MRAtlas_MR04_Qvale")));
steps.Add(("Qvale setup",2,()=>Place(129,1)));
steps.Add(("Qvale jump",.45,()=>{Begin("qvale_jump");jump=true;}));steps.Add(("Qvale release",2,()=>jump=false));steps.Add(("Qvale check",.1,()=>{CheckPreview(false);Finish();}));
fileField.SetValue(null,testSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR05_Ribwood.unity");
int step=0;double ready=0,deadline=EditorApplication.timeSinceStartup+60;bool started=false,stopping=false;
Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
EditorApplication.CallbackFunction tick=null;tick=()=>{try{
 if(stopping){if(EditorApplication.isPlaying)return;EditorApplication.update-=tick;Application.logMessageReceived-=log;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=Sample;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);if(keyboard!=null&&keyboard.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(modal){ModalUi.Close();modal=false;}fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVSync;UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(original);bool saves=hashes.All(h=>Hash(h.Key)==h.Value);System.IO.File.WriteAllText(output+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,run,steps=step,total=steps.Count,rows,errors,normalSavesPreserved=saves,settingsRestored=true,measurement="Actual render-end callback after VistaZone.LateUpdate, deduplicated per frame"},Newtonsoft.Json.Formatting.Indented));return;}
 if(!EditorApplication.isPlaying){if(started||EditorApplication.timeSinceStartup>deadline)throw new Exception("Play stopped or failed to start");return;}if(!started){started=true;ready=Time.timeAsDouble+2;deadline=EditorApplication.timeSinceStartup+60;}
 EditorApplication.QueuePlayerLoopUpdate();var p=Player();if(p!=null)typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(p.GetComponent<PlayerHealth>(),float.PositiveInfinity);
 if(pad!=null){float dx=p!=null?targetX-p.transform.position.x:0;var input=new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(moving&&Mathf.Abs(dx)>.1f?Mathf.Sign(dx):0,0)};input.rightStick=manualPad?Vector2.down:Vector2.zero;if(aimHeld)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.West);if(manualDpad)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.DpadDown);if(keyboard!=null)UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,manualKey?new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.S):new UnityEngine.InputSystem.LowLevel.KeyboardState());if(jump)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,input);}
 if(Time.timeAsDouble<ready){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Simulation stalled");return;}
 var next=steps[step];next.action();step++;System.IO.File.WriteAllText(output+"progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,next.name,frame=Time.frameCount}));ready=Time.timeAsDouble+next.delay;deadline=EditorApplication.timeSinceStartup+60;if(step==steps.Count){stopping=true;monitor=false;EditorApplication.isPlaying=false;}
 }catch(Exception ex){errors.Add(ex.ToString());monitor=false;Neutral();stopping=true;EditorApplication.isPlaying=false;}};
EditorApplication.update+=tick;EditorApplication.isPlaying=true;return new{run,started=true,steps=steps.Count};


