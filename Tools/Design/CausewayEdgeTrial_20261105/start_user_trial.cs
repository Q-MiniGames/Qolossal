if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Finish automated verification first");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits must be preserved");
string output="Tools/Design/CausewayEdgeTrial_20261105/";
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string originalName=(string)fileField.GetValue(null);object originalData=dataField.GetValue(null);
string trialSave=System.IO.Path.GetFullPath(output+"user_trial_"+Guid.NewGuid().ToString("N")+".json");
bool optionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,background=Application.runInBackground;var options=EditorSettings.enterPlayModeOptions;
fileField.SetValue(null,trialSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR02_Causeway.unity");
var entry=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(output+"Preview/captures.json")).First(x=>(string)x["label"]=="marked_slope");
float x=(float)entry["x"],floor=(float)entry["y"]-3.5f;
EditorApplication.CallbackFunction tick=null;Action<PlayModeStateChange> state=null;
int placedFrame=-1;double deadline=EditorApplication.timeSinceStartup+60;bool started=false;
state=s=>{if(s!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.update-=tick;EditorApplication.playModeStateChanged-=state;fileField.SetValue(null,originalName);dataField.SetValue(null,originalData);EditorSettings.enterPlayModeOptionsEnabled=optionsEnabled;EditorSettings.enterPlayModeOptions=options;Application.runInBackground=background;System.IO.File.WriteAllText(output+"user_trial_closed.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{normalSaveRoutingRestored=true,editorOptionsRestored=true}));};
tick=()=>{try{
 if(!EditorApplication.isPlaying){if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=tick;state(PlayModeStateChange.EnteredEditMode);}return;}
 started=true;EditorApplication.QueuePlayerLoopUpdate();var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
 if(player==null||Time.time<1)return;
 if(placedFrame<0){player.PlaceAt(new Vector2(x,floor+.66f),1,true);Physics2D.SyncTransforms();placedFrame=Time.frameCount;return;}
 if(Time.frameCount-placedFrame<30)return;
 var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type).Focus();
 System.IO.File.WriteAllText(output+"user_trial_ready.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{ready=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,frameAdvance=Time.frameCount-placedFrame,position=new[]{player.transform.position.x,player.transform.position.y},isolatedSave=trialSave,normalSaveUntouched=true},Newtonsoft.Json.Formatting.Indented));
 EditorApplication.update-=tick;
 }catch(Exception e){System.IO.File.WriteAllText(output+"user_trial_error.txt",e.ToString());EditorApplication.update-=tick;EditorApplication.isPlaying=false;}};
EditorApplication.playModeStateChanged+=state;EditorApplication.update+=tick;EditorApplication.isPlaying=true;
return new{starting=true,scene="Causeway",isolatedSave=trialSave};

