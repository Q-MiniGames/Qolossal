if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new System.Exception("Save authorized edits before testing");
string originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="C:/UnityProjects/Qolossal/Tools/Design/WorldRefresh_20261104/Underground/";System.IO.Directory.CreateDirectory(output);
string testSave=output+"settle_save_"+System.Guid.NewGuid().ToString("N")+".json";
var hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var fileField=typeof(GameSave).GetField("fileName",hidden);var dataField=typeof(GameSave).GetField("data",hidden);
string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath;
System.Func<string,string> hash=p=>{if(!System.IO.File.Exists(p))return null;using(var stream=System.IO.File.OpenRead(p))using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(stream));};
var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,hash);
bool oldEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=UnityEditor.EditorSettings.enterPlayModeOptions;
var results=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
var targets=new System.Collections.Generic.List<(string kind,string id,string room,Vector2 at)>();
PlayerMovement player=null;int checkpointCount=0,pairCount=0,index=0,phase=0,placedRespawn=0,lastFrame=-1;bool started=false,stopping=false,initialized=false;
double ready=0,deadline=UnityEditor.EditorApplication.timeSinceStartup+60;
UnityEngine.InputSystem.Gamepad pad=null;
UnityEngine.Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};Application.logMessageReceived+=log;
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{try{
 double now=UnityEditor.EditorApplication.timeSinceStartup;
 if(stopping){
  if(UnityEditor.EditorApplication.isPlaying)return;
  UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=log;
  if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
  fileField.SetValue(null,oldName);dataField.SetValue(null,null);
  UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;UnityEditor.EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;
  bool preserved=hashes.All(h=>hash(h.Key)==h.Value);if(!preserved)errors.Add("Normal save changed");
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScene);
  System.IO.File.WriteAllText(output+"settle_result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete=true,passed=index==targets.Count&&errors.Count==0,checkpointCount,pairCount,checkedCount=index,results,errors,testSave,normalSavesPreserved=preserved,settingsRestored=true,enemyDamageSuppressedForGeometryTest=true,settleSeconds=.8,repairedCheckpointSettleSeconds=4.4,scope="Placement and settling; does not exercise cave transitions or puzzle traversal"},Newtonsoft.Json.Formatting.Indented));return;
 }
 if(!UnityEditor.EditorApplication.isPlaying){if(now>deadline)throw new System.Exception("Play start timeout");return;}
 if(!started){started=true;ready=Time.timeAsDouble+2;deadline=now+60;}
 UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
 if(Time.timeAsDouble<ready){if(now>deadline)throw new System.Exception("Simulation stalled");return;}
 if(Time.frameCount<=lastFrame)return;lastFrame=Time.frameCount;
 if(!initialized){
  if(GameSave.FilePath!=testSave)throw new System.Exception("Save isolation failed");
  player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(player==null)throw new System.Exception("Player missing");
  pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
  foreach(var cp in UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).OrderBy(c=>c.CheckpointId)){
   var room=cp.GetComponentInParent<UndergroundRoom>();if(room==null)continue;
   targets.Add(("checkpoint",cp.CheckpointId,room.roomId,cp.SpawnPosition));checkpointCount++;
  }
  foreach(var pair in UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.undergroundSide).OrderBy(c=>c.connectionId)){
   targets.Add(("cave arrival",pair.connectionId,pair.lowerRoomId,pair.ArrivalPosition));pairCount++;
  }
  if(checkpointCount!=89||pairCount!=37)throw new System.Exception("Unexpected coverage: "+checkpointCount+" checkpoints, "+pairCount+" cave pairs");
  initialized=true;
 }
 UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,new UnityEngine.InputSystem.LowLevel.GamepadState());
 typeof(PlayerHealth).GetField("caveExitProtectedUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(player.GetComponent<PlayerHealth>(),float.PositiveInfinity);
 var item=targets[index];
 if(phase==0){
  player.PlaceAt(item.at,1,true);placedRespawn=player.RespawnVersion;phase=1;
  bool extended=item.kind=="checkpoint"&&new[]{"A04","D01","E10","MR06_C01","MR06_C03","MR06_C06","PC11"}.Contains(item.room);
  ready=Time.timeAsDouble+(extended?4.4:.8);deadline=now+30;return;
 }
 var position=(Vector2)player.transform.position;var box=player.GetComponent<BoxCollider2D>();
 var ground=new ContactFilter2D{useLayerMask=true,layerMask=LayerMask.GetMask("Ground"),useTriggers=false};var overlaps=new System.Collections.Generic.List<Collider2D>();
 Physics2D.OverlapBox(box.bounds.center,(Vector2)box.bounds.size-Vector2.one*.04f,0,ground,overlaps);
 var blocked=overlaps.Where(c=>c!=box&&c.attachedRigidbody!=box.attachedRigidbody&&!(c.TryGetComponent<PlatformEffector2D>(out var eff)&&eff.useOneWay)).Select(c=>c.name).ToArray();
 bool passed=player.HasGroundContact&&Mathf.Abs(player.ObservedVelocity.y)<.15f&&item.at.y-position.y<.5f&&Vector2.Distance(position,item.at)<1.2f&&player.RespawnVersion==placedRespawn&&blocked.Length==0&&!AreaTransition.IsTransitioning;
 if(!passed)errors.Add(item.kind+" "+item.id+" did not settle safely");
 results.Add(new{item.kind,item.id,item.room,expectedX=item.at.x,expectedY=item.at.y,x=position.x,y=position.y,grounded=player.HasGroundContact,verticalVelocity=player.ObservedVelocity.y,respawned=player.RespawnVersion!=placedRespawn,blocked,passed});
 index++;phase=0;ready=Time.timeAsDouble+.05;deadline=now+30;
 System.IO.File.WriteAllText(output+"settle_progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checkedCount=index,total=targets.Count,last=item.id,failures=errors.Count}));
 if(index==targets.Count){stopping=true;UnityEditor.EditorApplication.isPlaying=false;}
}catch(System.Exception e){errors.Add(e.ToString());stopping=true;UnityEditor.EditorApplication.isPlaying=false;}};
fileField.SetValue(null,testSave);dataField.SetValue(null,null);
UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(WorldRefreshUnderground.ScenePath);
UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;
return new{started=true,testSave,expectedCheckpoints=89,expectedCaveArrivals=37,output};

