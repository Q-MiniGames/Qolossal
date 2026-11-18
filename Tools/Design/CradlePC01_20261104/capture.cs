if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save pending scene edits first");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="Tools/Design/CradlePC01_20261104/Captures/";System.IO.Directory.CreateDirectory(output);
var world=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));
var records=new System.Collections.Generic.List<object>();
try{foreach(string name in new[]{"MRAtlas_MR01_Cradle"}){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var camera=Camera.main;camera.orthographicSize=5;camera.aspect=16f/9;
 var region=world["regions"].Single(r=>(string)r["scene"]==name);string rid=(string)region["id"];
 var points=new System.Collections.Generic.List<(string label,Vector2 floor)>();
 foreach(int index in new int[]{}){
  var n=region["nodes"][index];float x=(float)n["position"]["x"]+(index==0?5:0),y=(float)n["position"]["y"];
  var hits=Physics2D.RaycastAll(new Vector2(x,y+2),Vector2.down,6,LayerMask.GetMask("Ground"));
  var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.35f);
  points.Add((index==0?"entry":"mid",new Vector2(x,hit.collider!=null?hit.point.y:y)));
 }
 foreach(var cave in UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Where(p=>!p.undergroundSide && p.lowerRoomId=="PC01")){
  var threshold=cave.entrance.GetComponent<CaveThresholdPresentation>();
  Vector2 at=threshold!=null?threshold.mouth:(Vector2)cave.EntrancePosition;
  if(threshold!=null)at.x-=1.8f*threshold.inward;
  points.Add(("cave_"+cave.lowerRoomId,at));
 }
 foreach(var p in points){
  camera.transform.position=new Vector3(p.floor.x,p.floor.y+3.5f,-50);
  foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
  foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
  foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);string file=output+rid+"_"+p.label+"_4K.png";
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  records.Add(new{scene=name,region=rid,view=p.label,file,x=p.floor.x,y=p.floor.y+3.5f,cameraSize=5,width=3840,height=2160,mode="Saved-scene Editor render at revised grounded framing; separate Play-mode evidence verifies motion"});
 }
}}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
System.IO.File.WriteAllText(output+"surface_capture_manifest.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
return new{captures=records.Count,output};

