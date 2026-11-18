if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
string output="Tools/Design/CisternNetwork_20261107/";
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/Qolossal_Underground.unity");
var bridge=CavePassagePresentation.Create();foreach(var rock in UnityEngine.Object.FindObjectsByType<WorldRefreshCaveRockField>(FindObjectsSortMode.None))rock.Refresh();if(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Qolossal/CavePassageVista")!=63)throw new Exception("Passage ownership lost after rock refresh");
var vistas=UnityEngine.Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None).ToDictionary(v=>v.GetComponentInParent<UndergroundRoom>().roomId);
var passages=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("Continuous passage ")).ToDictionary(r=>r.sharedMaterial.name.Substring(19));
var links=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Assets/Resources/CavePassageConnections.json"))["connections"];
var compareObject=new GameObject("Network verifier");var comparison=compareObject.AddComponent<CisternNetworkReview>();comparison.Configure();
var go=new GameObject("Isolated seam probe");go.layer=31;var art=go.AddComponent<SpriteRenderer>();art.sprite=vistas.Values.First().GetComponent<SpriteRenderer>().sprite;art.transform.localScale=new Vector3(200/art.sprite.bounds.size.x,200/art.sprite.bounds.size.y,1);
var cg=new GameObject("Probe camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=16f/432f;cam.aspect=1;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.magenta;
var rt=new RenderTexture(16,16,24);cam.targetTexture=rt;var tex=new Texture2D(16,16,TextureFormat.RGB24,false);var active=RenderTexture.active;
var rows=new List<object>();var failures=new List<string>();int overrides=comparison.BindingCount;
Color Sample(Material material){art.sharedMaterial=material;CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,16,16),0,0);tex.Apply();return tex.GetPixel(8,8);}
try{
foreach(bool gentle in new[]{false,true}){
 comparison.SetLayered(gentle,false);
 foreach(var link in links)foreach(bool end in new[]{false,true}){
  string id=(string)link["id"],roomId=(string)link[end?"toRoom":"fromRoom"];var room=vistas[roomId];var b=room.RoomBounds;
  var point=end?link["points"].Last:link["points"].First;Vector2 at=new Vector2((float)point["x"],(float)point["y"]);
  at=Vector2.Max((Vector2)b.min+Vector2.one*.12f,Vector2.Min((Vector2)b.max-Vector2.one*.12f,at));
  cam.transform.position=new Vector3(at.x,at.y,-50);go.transform.position=new Vector3(at.x,at.y,0);
  Color a=Sample(room.GetComponent<SpriteRenderer>().sharedMaterial),c=Sample(passages[id].sharedMaterial);
  float error=Mathf.Max(Mathf.Abs(a.r-c.r),Mathf.Max(Mathf.Abs(a.g-c.g),Mathf.Abs(a.b-c.b)));
  if(error>1.01f/255)failures.Add(id+" "+roomId+" seam "+error);
  rows.Add(new{mode=gentle?"B":"A",id,roomId,error});
 }
}
cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
rt=new RenderTexture(1920,1080,24);tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);cam.targetTexture=rt;cam.orthographicSize=5;cam.aspect=16f/9;
art.sharedMaterial=vistas["A04"].GetComponent<SpriteRenderer>().sharedMaterial;go.transform.position=new Vector3(1618,108,0);
var far=Shader.GetGlobalTexture("_CisternFar");
var black=new RenderTexture(far.width,far.height,0,RenderTextureFormat.ARGB32){dimension=UnityEngine.Rendering.TextureDimension.Tex2DArray,volumeDepth=9};black.Create();
for(int i=0;i<9;i++){Graphics.SetRenderTarget(black,0,CubemapFace.Unknown,i);GL.Clear(false,true,Color.black);}
foreach(string layer in new[]{"far","middle"}){
 comparison.SetLayered(layer=="middle",false);Shader.SetGlobalTexture("_CisternFar",layer=="middle"?black:far);Shader.SetGlobalFloat("_CisternEnclosureCount",0);
 foreach(var offset in new[]{Vector2.zero,new Vector2(2,0),new Vector2(0,2)}){
 cam.transform.position=new Vector3(1618+offset.x,108+offset.y,-50);CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
 System.IO.File.WriteAllBytes(output+layer+"_"+offset.x+"_"+offset.y+".png",tex.EncodeToPNG());
 }}
Shader.SetGlobalTexture("_CisternFar",far);black.Release();UnityEngine.Object.DestroyImmediate(black);
bool outsideUnchanged=vistas.Where(v=>!CisternNetworkReview.Includes(v.Key)).All(v=>v.Value.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_NetworkEnabled")==0);
UnityEngine.Object.DestroyImmediate(compareObject);
bool restored=vistas.All(v=>v.Value.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_NetworkEnabled")==0)&&passages.Values.All(v=>v.sharedMaterial.GetFloat("_NetworkEnabled")==0&&v.sharedMaterial.GetFloat("_B_NetworkEnabled")==0);
var report=new{tested=rows.Count,overrides,outsideUnchanged,restored,connected=bridge.ConnectedCount,missing=bridge.MissingCount,failures,rows};
System.IO.File.WriteAllText(output+"render_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return new{tested=rows.Count,overrides,outsideUnchanged,restored,failures};
}finally{RenderTexture.active=active;cam.targetTexture=null;if(compareObject!=null)UnityEngine.Object.DestroyImmediate(compareObject);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(bridge.gameObject);UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}


