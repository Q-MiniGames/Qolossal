if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Finish verification first");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scenes");
string output="Tools/Design/CisternNetwork_20261107/";
CisternContinuousPreview.SetEnabled(true);var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",flags);var dataField=typeof(GameSave).GetField("data",flags);
string oldName=(string)fileField.GetValue(null);object oldData=dataField.GetValue(null);
string testSave=System.IO.Path.GetFullPath(output+"user_trial_"+Guid.NewGuid().ToString("N")+".json");
if(System.IO.File.Exists(output+"resume_save.json"))System.IO.File.Copy(output+"resume_save.json",testSave,false);
bool oldEnabled=EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=EditorSettings.enterPlayModeOptions;
fileField.SetValue(null,testSave);dataField.SetValue(null,null);EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/Qolossal_Underground.unity");
EditorApplication.CallbackFunction tick=null;Action<PlayModeStateChange> state=null;
CisternNetworkReview comparison=null;int placed=-1;double deadline=EditorApplication.timeSinceStartup+90;
state=s=>{if(s!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.update-=tick;EditorApplication.playModeStateChanged-=state;fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;System.IO.File.WriteAllText(output+"user_trial_closed.json","{\"normalSaveRoutingRestored\":true,\"settingsRestored\":true}");};
tick=()=>{try{
 if(!EditorApplication.isPlaying){if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=tick;state(PlayModeStateChange.EnteredEditMode);}return;}
 EditorApplication.QueuePlayerLoopUpdate();var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player==null||Time.time<1)return;
 if(placed<0){foreach(var gate in UnityEngine.Object.FindObjectsByType<MraGate>(FindObjectsSortMode.None)){var room=gate.GetComponentInParent<UndergroundRoom>();if(room!=null&&CisternNetworkReview.Includes(room.roomId))gate.SetOpen(true);}comparison=Resources.FindObjectsOfTypeAll<CisternNetworkReview>().FirstOrDefault(c=>c.gameObject.scene.IsValid()&&c.Healthy);if(comparison==null)return;comparison.ReturnToStart();foreach(var hit in Physics2D.RaycastAll(new Vector2(1610f,109),Vector2.down,8,LayerMask.GetMask("Ground")))if(hit.collider!=null&&!hit.collider.isTrigger&&hit.normal.y>.6f){player.PlaceAt(new Vector2(1610f,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();break;}placed=Time.frameCount;return;}
 if(Time.frameCount-placed<30)return;
 comparison.SetLayered(true);var type=typeof(Editor).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type).Focus();
 System.IO.File.WriteAllText(output+"user_trial_ready.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{ready=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,position=new[]{player.transform.position.x,player.transform.position.y},mode=comparison.Layered?"B":"A",frameAdvance=Time.frameCount-placed,bindings=comparison.BindingCount,isolatedSave=testSave,controls="F8 or left stick click: switch A/B; F6: return to start; F7: next room",scope="A01-A08 and matching passage ends"},Newtonsoft.Json.Formatting.Indented));EditorApplication.update-=tick;
 }catch(Exception e){System.IO.File.WriteAllText(output+"user_trial_error.txt",e.ToString());EditorApplication.update-=tick;EditorApplication.isPlaying=false;}};
EditorApplication.playModeStateChanged+=state;EditorApplication.update+=tick;EditorApplication.isPlaying=true;return new{starting=true,isolatedSave=testSave};




