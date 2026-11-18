if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
string output="Tools/Design/WorldRevision_20261104/Surface/Captures/";System.IO.Directory.CreateDirectory(output);
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR02_Causeway.unity");
 var camera=Camera.main;camera.orthographicSize=5;camera.aspect=16f/9;
 var entries=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Where(p=>!p.undergroundSide).ToArray();
 foreach(var cave in entries){
  var threshold=cave.GetComponentInChildren<CaveThresholdPresentation>(true);
  Vector2 mouth=threshold!=null?threshold.mouth:cave.EntrancePosition;
  camera.transform.position=new Vector3(mouth.x-1.8f*(threshold!=null?threshold.inward:1),mouth.y+3.5f,-50);
  foreach(var sky in UnityEngine.Object.FindObjectsByType<WorldRefreshSky>(FindObjectsSortMode.None))sky.Refresh();
  foreach(var weather in UnityEngine.Object.FindObjectsByType<WorldRefreshWeather>(FindObjectsSortMode.None))weather.Refresh(0);
  foreach(var layer in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))layer.Refresh(camera);
  var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(3840,2160,24);var tex=new Texture2D(3840,2160,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,3840,2160),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+cave.lowerRoomId+"_restored_4K.png",tex.EncodeToPNG());}
  finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
 return new{captures=entries.Length,output};
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
