if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
string output="Tools/Design/CausewayGrass_20261105/Play/";System.IO.Directory.CreateDirectory(output);
var original=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath,testSave=System.IO.Path.GetFullPath(output+"isolated_"+Guid.NewGuid().ToString("N")+".json");object oldData=dataField.GetValue(null);
string Hash(string p){if(!System.IO.File.Exists(p))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p)));}
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,Hash);
bool oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=EditorSettings.enterPlayModeOptions;
int oldFrameRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;
var captures=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Tools/Design/SurfaceContrast_20261104/Before/captures.json"));
var rows=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
UnityEngine.InputSystem.Gamepad pad=null;float startX=0;
int renderedFrames=0,lastFrame=-1,firstFrame=0;float firstTime=0;Vector3 renderedPosition=Vector3.zero;float renderedSize=5;bool monitor=false;
PlayerMovement Player()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
void Sample(Camera cam){if(!monitor||cam!=Camera.main||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;renderedFrames++;renderedPosition=cam.transform.position;renderedSize=cam.orthographicSize;}
void Rendered(UnityEngine.Rendering.ScriptableRenderContext context,Camera cam)=>Sample(cam);
UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=Rendered;Camera.onPostRender+=Sample;
void Place(string name){var p=Player();string label=name.Contains("Ribwood")||name.Contains("Qvale")?"node6":"node0";var c=captures.First(r=>(string)r["scene"]==name&&(string)r["label"]==label);p.PlaceAt(new Vector2(92.24025f,39.60824f),1,true);Physics2D.SyncTransforms();startX=p.transform.position.x;pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();renderedFrames=0;firstFrame=Time.frameCount;firstTime=Time.time;monitor=true;}
void Capture(string name){monitor=false;var cam=Camera.main;var pos=cam.transform.position;float size=cam.orthographicSize;var target=cam.targetTexture;var active=RenderTexture.active;
 try{cam.transform.position=renderedPosition;cam.orthographicSize=renderedSize;foreach(int w in new[]{1920,3840}){int h=w*9/16;var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+name+(w==3840?"_4K":"_1080p")+".png",tex.EncodeToPNG());}finally{cam.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}}}
 finally{cam.transform.position=pos;cam.orthographicSize=size;}
 var p=Player();var b=p.GetComponent<BoxCollider2D>().bounds;var foot=cam.WorldToViewportPoint(new Vector3(b.center.x,b.min.y));
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();int expected=new[]{4,4,4,1,8,8,8}[Array.IndexOf(RegionalSceneCatalog.Surface,name)],graded=0;bool masks=true;
 foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()))if(r.name=="Grass walking edge (generated)"){
  graded++;var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);if(r.GetComponent<Collider2D>()!=null||r.transform.lossyScale!=Vector3.one)masks=false;
 }
 bool passed=renderedFrames>=10&&Time.frameCount-firstFrame>=10&&Time.time-firstTime>1&&graded>0&&masks&&p.transform.position.x-startX>3;
 rows.Add(new{scene=name,passed,renderedFrames,frameAdvance=Time.frameCount-firstFrame,simulationSeconds=Time.time-firstTime,feetViewport=foot.y,cameraSize=renderedSize,travel=p.transform.position.x-startX,gradedMeshes=graded,renderOnlyUnitScale=masks});
 if(!passed)errors.Add("Simulation not verified in "+name);
}
var steps=new System.Collections.Generic.List<(double delay,Action action)>();
foreach(var name in new[]{"MRAtlas_MR02_Causeway"}){string scene=name;if(name!=RegionalSceneCatalog.Surface[0])steps.Add((2,()=>UnityEngine.SceneManagement.SceneManager.LoadScene(scene)));steps.Add((2,()=>Place(scene)));steps.Add((.1,()=>Capture(scene)));}
fileField.SetValue(null,testSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+"MRAtlas_MR02_Causeway.unity");
int step=0;double ready=0,deadline=EditorApplication.timeSinceStartup+60;bool started=false,stopping=false;
Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
EditorApplication.CallbackFunction tick=null;tick=()=>{try{
 if(stopping){if(EditorApplication.isPlaying)return;EditorApplication.update-=tick;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);Application.logMessageReceived-=log;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;Camera.onPostRender-=Sample;fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVSync;UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(original);System.IO.File.WriteAllText(output+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,rows,errors,normalSavesPreserved=hashes.All(h=>Hash(h.Key)==h.Value),settingsRestored=true},Newtonsoft.Json.Formatting.Indented));return;}
 if(!EditorApplication.isPlaying){if(started||EditorApplication.timeSinceStartup>deadline)throw new Exception("Play failed or stopped");return;}if(!started){started=true;ready=Time.timeAsDouble+2;}
 EditorApplication.QueuePlayerLoopUpdate();if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=Vector2.right});var p=Player();if(p!=null)typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(p.GetComponent<PlayerHealth>(),float.PositiveInfinity);
 if(Time.timeAsDouble<ready){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Simulation stalled");return;}
 var next=steps[step];next.action();step++;ready=Time.timeAsDouble+next.delay;deadline=EditorApplication.timeSinceStartup+60;if(step==steps.Count){monitor=false;stopping=true;EditorApplication.isPlaying=false;}
 }catch(Exception ex){errors.Add(ex.ToString());monitor=false;stopping=true;EditorApplication.isPlaying=false;}};
EditorApplication.update+=tick;EditorApplication.isPlaying=true;return new{started=true,steps=steps.Count};



