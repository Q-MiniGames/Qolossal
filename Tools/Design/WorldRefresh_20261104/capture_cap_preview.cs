if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="Tools/Design/WorldRefresh_20261104/Captures/";System.IO.Directory.CreateDirectory(output);
var world=MraWorldBuilder.Prepare();var evidence=new System.Collections.Generic.List<object>();
try{foreach(string name in RegionalSceneCatalog.Surface.Skip(2).Take(2)){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var camera=Camera.main;camera.orthographicSize=5;camera.aspect=16f/9;
 var region=world.regions.First(r=>r.scene==name);var points=new System.Collections.Generic.Dictionary<string,Vector3>();
 var n=region.nodes[0];points["entry"]=new Vector3(n.position.x+5,n.position.y+2,-10);
 n=region.nodes[6];points["mid"]=new Vector3(n.position.x,n.position.y+2,-10);
 var cave=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>!c.undergroundSide).OrderBy(c=>c.EntrancePosition.x).FirstOrDefault();
 points["cave"]=cave!=null?(Vector3)cave.EntrancePosition+new Vector3(0,1.5f,-10):points["mid"];
 foreach(var pair in points){
  camera.transform.position=pair.Value;
  foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
  foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
  foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
  string file=output+name.Substring(8,4)+"_"+pair.Key+"_4K.png";
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  evidence.Add(new{scene=name,view=pair.Key,file,x=pair.Value.x,y=pair.Value.y,cameraSize=5,width=3840,height=2160,mode="Edit-mode saved scene render; not traversal evidence"});
 }
}}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
System.IO.File.WriteAllText(output+"surface_capture_manifest.json",Newtonsoft.Json.JsonConvert.SerializeObject(evidence,Newtonsoft.Json.Formatting.Indented));
return new{captures=evidence.Count};

