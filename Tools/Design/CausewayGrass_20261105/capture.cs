if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
string output="Tools/Design/CausewayGrass_20261105/Preview/";System.IO.Directory.CreateDirectory(output);
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var world=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));
var rows=new System.Collections.Generic.List<object>();
try{foreach(string name in new[]{"MRAtlas_MR02_Causeway"}){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var camera=Camera.main;camera.orthographicSize=5;camera.aspect=16f/9;
 var region=world["regions"].Single(r=>(string)r["scene"]==name);string rid=(string)region["id"];
 var points=new System.Collections.Generic.List<(string label,Vector2 floor)>();
 foreach(int index in new[]{0,6,10}){
  var n=region["nodes"][index];float x=(float)n["position"]["x"]+(index==0?5:0),y=(float)n["position"]["y"];
  if(rid=="MR05"&&index==6){x=185;y=98;}if(rid=="MR04"&&index==6){x=129;y=30;}
  var hits=Physics2D.RaycastAll(new Vector2(x,y+12),Vector2.down,35,LayerMask.GetMask("Ground"));
  var hit=hits.FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.35f);
  points.Add(("node"+index,new Vector2(x,hit.collider!=null?hit.point.y:y)));
 }
 foreach(var cave in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CaveConnectionPoint>()).Where(c=>!c.undergroundSide)){
  var t=cave.entrance.GetComponent<CaveThresholdPresentation>();Vector2 p=t!=null?t.mouth:(Vector2)cave.EntrancePosition;
  points.Add(("cave_"+cave.lowerRoomId,p));
 }
 points.Add(("marked_slope",new Vector2(92.24025f,38.94824f)));
 foreach(var p in points){
  camera.transform.position=new Vector3(p.floor.x,p.floor.y+3.5f,-50);
  foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
  foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
  foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
  int w=p.label.StartsWith("cave")?960:p.label=="marked_slope"?1920:3840,h=w*9/16;
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);string file=output+rid+"_"+p.label+".png";
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  rows.Add(new{scene=name,file,label=p.label,x=p.floor.x,y=p.floor.y+3.5f,w,h});
 }
}}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText(output+"captures.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
return new{captures=rows.Count};



