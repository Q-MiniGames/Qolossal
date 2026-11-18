if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="Tools/Design/WorldRevision_20261104/Entrances/";System.IO.Directory.CreateDirectory(output);
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR04_Qvale.unity");
 var cave=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Single(c=>!c.undergroundSide);
 var rows=cave.GetComponentsInChildren<Transform>(true).Select(t=>new{name=t.name,id=GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),position=new[]{t.position.x,t.position.y},localPosition=new[]{t.localPosition.x,t.localPosition.y},active=t.gameObject.activeSelf,components=t.GetComponents<Component>().Select(c=>c==null?"missing":c.GetType().Name).ToArray()}).ToArray();
 System.IO.File.WriteAllText(output+"qvale_before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{path=cave.transform.parent.name,rows},Newtonsoft.Json.Formatting.Indented));
 var camera=Camera.main;camera.orthographicSize=5;camera.aspect=16f/9;
 foreach(var point in new[]{new{name="qvale_city_before",x=142f,y=11.5f},new{name="qvale_west_mountain_before",x=23f,y=24.1f}}){
  camera.transform.position=new Vector3(point.x,point.y,-10);
  foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
  foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
  foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+point.name+".png",tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 return new{objects=rows.Length};
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
