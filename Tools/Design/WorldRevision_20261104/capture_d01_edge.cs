if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save authorized edits before capture");
var priorSetup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
string output="Tools/Design/WorldRevision_20261104/RegionalEdge/Captures/";System.IO.Directory.CreateDirectory(output);
var evidence=new System.Collections.Generic.List<object>();
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(WorldRefreshUnderground.ScenePath);
 var camera=Camera.main;camera.orthographic=true;camera.aspect=16f/9;
 var rooms=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None);
 foreach(string id in new[]{"D01"}){
  var room=rooms.First(r=>r.roomId==id);var b=room.WorldBounds;
  var cp=room.GetComponentsInChildren<Checkpoint>(true).FirstOrDefault(c=>c.gameObject.activeInHierarchy);
  var arrival=room.GetComponentsInChildren<MraArrival>(true).FirstOrDefault(a=>a.gameObject.activeInHierarchy);
  Vector2 spawn=cp!=null?cp.SpawnPosition:arrival!=null?(Vector2)arrival.transform.position:(Vector2)b.center;
  // Existing player capsule feet are .66u below spawn. CameraFollow's .15 viewport
  // foot baseline gives 3.5u from feet to camera at half-height5 => spawn+2.84u.
  Vector2 at=spawn+new Vector2(1.9f,2.84f);
  if(b.size.x>18)at.x=Mathf.Clamp(at.x,b.min.x+9,b.max.x-9);
  at=new Vector2(2018.5f,441f); camera.orthographicSize=5;camera.transform.position=new Vector3(at.x,at.y,-50);
  CaveDepth.Apply(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
  string file=output+id+"_theme_4K.png";
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  evidence.Add(new{room=id,theme=UndergroundArtKit.SetOf(id),file,x=at.x,y=at.y,spawnX=spawn.x,spawnY=spawn.y,cameraSize=5,width=3840,height=2160,mode="Saved-scene Editor preview at grounded framing; no art mutations, no physics simulation"});
 }
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(priorSetup);}
System.IO.File.WriteAllText(output+"capture_manifest.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
return new{captures=evidence.Count,output};

