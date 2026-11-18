if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.Exception("Edit mode required");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("Save pending scene edits first");
string originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="C:/UnityProjects/Qolossal/Tools/Design/CradlePC01_20261104/Portals/";
// Focused regression for the two authorized surface-mouth moves.
int startIndex=0,take=1;
var pairs=new[]{new{id="PC01",scene="MRAtlas_MR01_Cradle"}};
System.IO.Directory.CreateDirectory(output);
string testSave=output+"traversal_save_"+System.Guid.NewGuid().ToString("N")+".json";
var hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",hidden);var dataField=typeof(GameSave).GetField("data",hidden);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath;object oldData=dataField.GetValue(null);
System.Func<string,string> hash=p=>{if(!System.IO.File.Exists(p))return null;using(var s=System.IO.File.OpenRead(p))using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(s));};
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,hash);
bool oldEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=UnityEditor.EditorSettings.enterPlayModeOptions;
var sceneSetup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var results=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(yes,message)=>{if(!yes)throw new System.Exception(message);};
System.Func<PlayerMovement> player=()=>UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
System.Func<string,CaveConnectionPoint> endpoint=id=>UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Single(p=>p.lowerRoomId==id);
System.Func<string,CaveThresholdPresentation> show=id=>endpoint(id).entrance.GetComponent<CaveThresholdPresentation>();
System.Action<string> grant=predicate=>{if(string.IsNullOrEmpty(predicate))return;if(predicate.StartsWith("relic:"))GameSave.AddRelic(predicate.Substring(6));else if(predicate.StartsWith("knot:"))GameSave.WakeKnot(predicate.Substring(5));else GameSave.SetFlag(predicate.StartsWith("flag:")?predicate.Substring(5):predicate);};
System.Action<string> unlock=id=>{var e=endpoint(id).entrance;if(e is MraChamberDoor d){if(!string.IsNullOrEmpty(d.prerequisite))GameSave.AddRelic(d.prerequisite);}if(e is MraUgPortal u){foreach(var s in u.requires)grant(s);if(!string.IsNullOrEmpty(u.unlockFlag))GameSave.SetFlag(u.unlockFlag);}if(e is MraExit x)foreach(var s in x.requires)grant(s);};
UnityEngine.InputSystem.Gamepad pad=null;bool steering=false, jumpHeld=false;float target=0;string travelTarget=null;
System.Action neutral=()=>{steering=false;jumpHeld=false;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());};
System.Action<float> steer=x=>{target=x;steering=true;};
System.Action<string> capture=name=>{var cam=Camera.main;var targetOld=cam.targetTexture;var activeOld=RenderTexture.active;foreach(int width in new[]{3840,1920}){var rt=new RenderTexture(width,width*9/16,24);var tex=new Texture2D(width,width*9/16,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,width*9/16),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+"walk_"+name+(width==3840?"_4K.png":"_1080p.png"),tex.EncodeToPNG());}finally{cam.targetTexture=targetOld;RenderTexture.active=activeOld;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);}}};
var steps=new System.Collections.Generic.List<(string label,string scene,double delay,System.Action action)>();
int dep=0,em=0,fall=0;
foreach(var pair in pairs){string id=pair.id,surface=pair.scene;
 steps.Add((id+" load surface",null,1.5,()=>{neutral();travelTarget=null;UnityEngine.SceneManagement.SceneManager.LoadScene(surface);}));
 steps.Add((id+" place on approach",surface,1,()=>{check(GameSave.FilePath==testSave,"Save isolation failed");if(pad==null)pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();unlock(id);dep=CaveThresholdPresentation.Departures;em=CaveThresholdPresentation.Emergences;fall=CaveThresholdPresentation.ArrivalFallbacks;var p=endpoint(id);player().PlaceAt(p.ArrivalPosition,show(id).inward,true);}));
 steps.Add((id+" walk to upper mouth",surface,4,()=>{var s=show(id);steer(s.mouth.x+s.inward*.15f);}));
 steps.Add((id+" settle upper mouth",surface,.4,()=>{neutral();var s=show(id);check(player().HasGroundContact&&Mathf.Abs(player().transform.position.x-(s.mouth.x+s.inward*.15f))<.45f,"Upper mouth blocked: "+id+" at "+player().transform.position);}));
 steps.Add((id+" capture upper and enter",surface,1,()=>{capture(id+"_upper");travelTarget=RegionalSceneCatalog.Underground;steer(endpoint(id).EntrancePosition.x+show(id).inward*.7f);}));
 steps.Add((id+" underground arrival",RegionalSceneCatalog.Underground,.7,()=>{neutral();travelTarget=null;check(Vector2.Distance(player().transform.position,endpoint(id).ArrivalPosition)<1.15f&&player().HasGroundContact,"Lower arrival mismatch: "+id+" actual="+player().transform.position+" expected="+endpoint(id).ArrivalPosition+" grounded="+player().HasGroundContact);check(CaveThresholdPresentation.ArrivalFallbacks==fall,"Lower emergence fallback: "+id);unlock(id);}));
 steps.Add((id+" walk to lower mouth",RegionalSceneCatalog.Underground,3,()=>{var s=show(id);steer((id=="MR01_C01"?35f:s.mouth.x)-s.inward*.10f);}));
 steps.Add((id+" settle lower mouth",RegionalSceneCatalog.Underground,.4,()=>{neutral();var s=show(id);check(player().HasGroundContact&&Mathf.Abs(player().transform.position.x-((id=="MR01_C01"?35f:s.mouth.x)-s.inward*.10f))<.45f,"Lower mouth blocked: "+id);}));
 steps.Add((id+" capture lower and return",RegionalSceneCatalog.Underground,1,()=>{capture(id+"_lower");travelTarget=surface;steer(endpoint(id).EntrancePosition.x+show(id).inward*.6f);}));
 steps.Add((id+" surface arrival",surface,1.1,()=>{neutral();travelTarget=null;check(Vector2.Distance(player().transform.position,endpoint(id).ArrivalPosition)<1.15f&&player().HasGroundContact,"Upper arrival mismatch: "+id+" actual="+player().transform.position+" expected="+endpoint(id).ArrivalPosition+" grounded="+player().HasGroundContact);check(CaveThresholdPresentation.ArrivalFallbacks==fall,"Upper emergence fallback: "+id);}));
 steps.Add((id+" stable control restored",surface,.1,()=>{check(CaveThresholdPresentation.Departures-dep==2&&CaveThresholdPresentation.Emergences-em==2,"Wrong presentation counts: "+id);check(player().TransitionWalkInput==null&&!AreaTransition.IsTransitioning,"Control not restored: "+id);}));
}
steps.Add(("PC01 place on outer approach","MRAtlas_MR01_Cradle",1,()=>{neutral();player().PlaceAt(new Vector2(58.6f,25.36f),1,true);}));
steps.Add(("PC01 jump onto rounded roof","MRAtlas_MR01_Cradle",1.6,()=>{jumpHeld=true;steer(61.8f);}));
steps.Add(("PC01 verify roof landing","MRAtlas_MR01_Cradle",.5,()=>{neutral();check(player().HasGroundContact&&player().transform.position.x>60.5f&&player().transform.position.y>27.6f,"Roof jump blocked: "+player().transform.position);capture("PC01_roof");}));
steps.Add(("PC01 walk onward over hill","MRAtlas_MR01_Cradle",7,()=>{steer(82f);}));
steps.Add(("PC01 verify onward route","MRAtlas_MR01_Cradle",.5,()=>{neutral();check(player().HasGroundContact&&Mathf.Abs(player().transform.position.x-82f)<.4f,"Onward slope blocked: "+player().transform.position);}));
// Verify durable checkpoint IDs resolve after a fresh scene load at the new positions.
foreach(var id in new[]{"PC01"}){
 steps.Add((id+" load checkpoint room",null,1,()=>{neutral();UnityEngine.SceneManagement.SceneManager.LoadScene(RegionalSceneCatalog.Underground);}));
 steps.Add((id+" save checkpoint ID",RegionalSceneCatalog.Underground,.5,()=>{var room=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).Single(r=>r.roomId==id);var cp=room.GetComponentsInChildren<Checkpoint>().First();GameSave.SetCheckpoint(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,cp.CheckpointId);dataField.SetValue(null,null);UnityEngine.SceneManagement.SceneManager.LoadScene(RegionalSceneCatalog.Underground);}));
 steps.Add((id+" restored checkpoint",RegionalSceneCatalog.Underground,1,()=>{var room=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).Single(r=>r.roomId==id);var cp=room.GetComponentsInChildren<Checkpoint>().First();check(Vector2.Distance(player().transform.position,cp.SpawnPosition)<1.2f&&player().HasGroundContact,"Checkpoint restore mismatch "+id+" actual="+player().transform.position+" expected="+cp.SpawnPosition);}));
}
fileField.SetValue(null,testSave);dataField.SetValue(null,null);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/"+pairs[0].scene+".unity");
int step=0,lastFrame=0;bool stopping=false,started=false;double beginning=UnityEditor.EditorApplication.timeSinceStartup,deadline=UnityEditor.EditorApplication.timeSinceStartup+60,ready=0;
UnityEngine.Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=log;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{try{double now=UnityEditor.EditorApplication.timeSinceStartup;
 if(stopping){if(UnityEditor.EditorApplication.isPlaying)return;UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=log;if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;UnityEditor.EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;bool preserved=hashes.All(h=>hash(h.Key)==h.Value);if(!preserved)errors.Add("Normal save changed");UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);System.IO.File.WriteAllText(output+"final_regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=step==steps.Count,passed=step==steps.Count&&errors.Count==0,startIndex,take,step,steps=results,errors,testSave,normalSavesPreserved=preserved,settingsRestored=true,enemyDamageSuppressedForGeometryTest=true,departures=CaveThresholdPresentation.Departures,emergences=CaveThresholdPresentation.Emergences,fallbacks=CaveThresholdPresentation.ArrivalFallbacks},Newtonsoft.Json.Formatting.Indented));return;}
 if(now-beginning>360)throw new System.Exception("Portal fixture total timeout");
 if(!UnityEditor.EditorApplication.isPlaying){if(started)throw new System.Exception("Play stopped before completion");if(now>deadline)throw new System.Exception("Play start timeout");return;}if(!started){started=true;ready=Time.timeAsDouble+2;deadline=now+60;}
 UnityEditor.EditorApplication.QueuePlayerLoopUpdate();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
 if(player()!=null)typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(player().GetComponent<PlayerHealth>(),float.PositiveInfinity); // Isolate passage geometry from enemy knockback in this test only.
 if(travelTarget!=null&&scene==travelTarget)neutral();
 if(steering&&player()!=null&&!AreaTransition.IsTransitioning){float dx=target-player().transform.position.x;var input=new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=new Vector2(Mathf.Abs(dx)<.10f?0:Mathf.Sign(dx)*(jumpHeld?.8f:(.23f+Mathf.Min(.45f,Mathf.Abs(dx)*.10f))),0)};if(jumpHeld)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,input);}
 if(Time.timeAsDouble<ready){if(now>deadline)throw new System.Exception("Simulation stalled during "+steps[step].label);return;}var next=steps[step];if(AreaTransition.IsTransitioning||Time.frameCount<=lastFrame||(next.scene!=null&&scene!=next.scene)){if(now>deadline)throw new System.Exception("Timeout at "+next.label+"; scene="+scene+" player="+(player()!=null?player().transform.position.ToString():"missing")+" grounded="+player().HasGroundContact+" velocity="+player().ObservedVelocity+" busy="+MraState.Busy+" portal="+Newtonsoft.Json.JsonConvert.SerializeObject(UnityEngine.Object.FindObjectsByType<MraUgPortal>(FindObjectsSortMode.None).Where(g=>Vector2.Distance(g.transform.position,player().transform.position)<4).Select(g=>new{g.name,g.Near,g.Open,g.Unlocked,g.LockedOut,g.enabled,g.walkThroughEntrance,g.walkThroughDirection})) );return;}
 lastFrame=Time.frameCount;next.action();results.Add(new{label=next.label,frame=lastFrame,passed=true});step++;System.IO.File.WriteAllText(output+"regression_progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{startIndex,take,step,total=steps.Count,label=next.label}));ready=Time.timeAsDouble+next.delay;deadline=UnityEditor.EditorApplication.timeSinceStartup+60;if(step==steps.Count){stopping=true;UnityEditor.EditorApplication.isPlaying=false;}
 }catch(System.Exception e){errors.Add(e.ToString());neutral();stopping=true;UnityEditor.EditorApplication.isPlaying=false;}};
UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;return new{started=true,pairs,steps=steps.Count,testSave};














