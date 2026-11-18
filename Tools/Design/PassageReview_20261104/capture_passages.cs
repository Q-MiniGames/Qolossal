if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string output="Tools/Design/PassageReview_20261104/PassageCaptures/";System.IO.Directory.CreateDirectory(output);
var cam=Camera.main;var priorPos=cam.transform.position;float priorSize=cam.orthographicSize,priorAspect=cam.aspect;
var bridge=CavePassagePresentation.Create();
var rows=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
try{
 var rooms=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None);
 foreach(string id in new[]{"A04_A05_main","A01_A02_main","A08_HEART_heart","D07_D08_main","MR01_1_2"}){
  var link=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Tools/Design/PassageReview_20261104/connections.json"))["connections"].First(c=>(string)c["id"]==id);var point=link["points"].First; Vector2 at=new Vector2((float)point["x"],(float)point["y"]+1.5f);
  cam.orthographicSize=5;cam.aspect=16f/9f;cam.transform.position=new Vector3(at.x,at.y,-50);CaveDepth.Apply(cam);
  var old=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1920,1080,24);var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes(output+id+".png",tex.EncodeToPNG());}finally{cam.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
  rows.Add(new{id,x=at.x,y=at.y});
 }
 foreach(string name in new[]{"Qolossal/WorldRefreshCaveVista","Qolossal/CavePassageVista"}){var shader=Shader.Find(name);foreach(var error in ShaderUtil.GetShaderMessages(shader))if(error.severity.ToString()=="Error")errors.Add(error.message);}
 System.IO.File.WriteAllText(output+"result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{connected=bridge.ConnectedCount,missing=bridge.MissingCount,rows,shaderErrors=errors},Newtonsoft.Json.Formatting.Indented));
 return new{connected=bridge.ConnectedCount,missing=bridge.MissingCount,shaderErrors=errors,captures=rows.Count};
}finally{UnityEngine.Object.DestroyImmediate(bridge.gameObject);cam.transform.position=priorPos;cam.orthographicSize=priorSize;cam.aspect=priorAspect;}
