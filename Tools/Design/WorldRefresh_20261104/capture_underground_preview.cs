if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save authorized edits before capture");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="Tools/Design/WorldRefresh_20261104/Underground/Captures/";System.IO.Directory.CreateDirectory(output);
var evidence=new System.Collections.Generic.List<object>();
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(WorldRefreshUnderground.ScenePath);
 var camera=Camera.main;camera.orthographic=true;camera.aspect=16f/9;
 var rooms=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None);
 void Shot(string label,Vector2 center,float half){
  camera.orthographicSize=half;camera.transform.position=new Vector3(center.x,center.y,-50);
  foreach(var vista in UnityEngine.Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None))vista.Refresh();
  foreach(var heart in UnityEngine.Object.FindObjectsByType<WorldRefreshHeartSurface>(FindObjectsSortMode.None))heart.Refresh();
  CaveDepth.Apply(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
  string file=output+label+"_4K.png";
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  evidence.Add(new{view=label,file,x=center.x,y=center.y,cameraSize=half,width=3840,height=2160,mode="Edit-mode saved-scene render; Heart state views are tint fixtures, not progression tests"});
 }
 foreach(string id in new[]{"A01"}){
  var room=rooms.First(r=>r.roomId==id);var b=room.WorldBounds;
  var cp=room.GetComponentsInChildren<Checkpoint>(true).FirstOrDefault(c=>c.gameObject.activeInHierarchy);
  var arrival=room.GetComponentsInChildren<MraArrival>(true).FirstOrDefault(a=>a.gameObject.activeInHierarchy);
  Vector2 at=cp!=null?cp.SpawnPosition:arrival!=null?(Vector2)arrival.transform.position:(Vector2)b.center;
  at+=new Vector2(1.9f,1f);
  if(b.size.x>18)at.x=Mathf.Clamp(at.x,b.min.x+9,b.max.x-9);

  Shot(id+"_play",at,5);
 }
 var hall=rooms.First(r=>r.roomId=="HEART");var quarters=hall.GetComponentsInChildren<WorldRefreshHeartSurface>(true).Select(h=>h.GetComponent<SpriteRenderer>()).ToArray();
 var original=quarters.Select(q=>q.color).ToArray();
 try{
  foreach(var q in quarters)q.color=new Color(.35f,.3f,.3f,.35f);
  Shot("HEART_overview_asleep",hall.WorldBounds.center,27);
  foreach(var q in quarters)q.color=new Color(1,.78f,.35f,.9f);
  Shot("HEART_overview_awake",hall.WorldBounds.center,27);
  if(quarters.Length>0)Shot("HEART_native_detail",quarters[0].bounds.center,5);
 }finally{for(int i=0;i<quarters.Length;i++)quarters[i].color=original[i];}
}finally{if(!string.IsNullOrEmpty(prior))UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
System.IO.File.WriteAllText(output+"capture_manifest.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
return new{captures=evidence.Count,output};

