if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>r.gameObject.layer==31))throw new Exception("Probe layer already used");
var bridge=CavePassagePresentation.Create();
var vistas=UnityEngine.Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None).ToDictionary(v=>v.GetComponentInParent<UndergroundRoom>().roomId);
var passages=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("Continuous passage ")).ToDictionary(r=>r.sharedMaterial.name.Substring(19));
var links=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Assets/Resources/CavePassageConnections.json"))["connections"];
var go=new GameObject("Isolated seam probe");go.layer=31;var art=go.AddComponent<SpriteRenderer>();art.sprite=vistas.Values.First().GetComponent<SpriteRenderer>().sprite;art.transform.localScale=new Vector3(200/art.sprite.bounds.size.x,200/art.sprite.bounds.size.y,1);
var cg=new GameObject("Probe camera");var cam=cg.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=16f/432f;cam.aspect=1;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.magenta;
var rt=new RenderTexture(16,16,24);cam.targetTexture=rt;var tex=new Texture2D(16,16,TextureFormat.RGB24,false);var active=RenderTexture.active;
var rows=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();
Color Sample(Material material){art.sharedMaterial=material;CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,16,16),0,0);tex.Apply();return tex.GetPixel(8,8);}
try{
 foreach(var link in links){string id=(string)link["id"];foreach(bool end in new[]{false,true}){
  string roomId=(string)link[end?"toRoom":"fromRoom"];var room=vistas[roomId];var b=room.RoomBounds;
  var point=end?link["points"].Last:link["points"].First;Vector2 at=new Vector2((float)point["x"],(float)point["y"]);
  at=Vector2.Max((Vector2)b.min+Vector2.one*.12f,Vector2.Min((Vector2)b.max-Vector2.one*.12f,at));
  cam.transform.position=new Vector3(at.x,at.y,-50);go.transform.position=new Vector3(at.x,at.y,0);
  Color a=Sample(room.GetComponent<SpriteRenderer>().sharedMaterial),c=Sample(passages[id].sharedMaterial);
  float error=Mathf.Max(Mathf.Abs(a.r-c.r),Mathf.Max(Mathf.Abs(a.g-c.g),Mathf.Abs(a.b-c.b)));
  if(error>1.01f/255)failures.Add(id+" "+roomId+" color error "+error);
  rows.Add(new{id,roomId,error,expected=new[]{a.r,a.g,a.b},actual=new[]{c.r,c.g,c.b}});
 }}
 // Capture a single isolated chamber at two camera positions for image-based
 // parallax measurement, with 108 screen pixels per world unit.
 cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
 rt=new RenderTexture(1920,1080,24);tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);cam.targetTexture=rt;cam.orthographicSize=5;cam.aspect=16f/9f;
 var vista=vistas["A04"];var anchor=vista.GetComponent<SpriteRenderer>().sharedMaterial.GetVector("_RoomAnchor");
 for(int i=0;i<2;i++){cam.transform.position=new Vector3(anchor.x+i*2,anchor.y,-50);go.transform.position=new Vector3(anchor.x,anchor.y,0);art.sharedMaterial=vista.GetComponent<SpriteRenderer>().sharedMaterial;CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes("Tools/Design/PassageReview_20261104/parallax_"+i+".png",tex.EncodeToPNG());}
 var result=new{tested=rows.Count,connected=bridge.ConnectedCount,missing=bridge.MissingCount,failures,rows};
 System.IO.File.WriteAllText("Tools/Design/PassageReview_20261104/seams.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{tested=rows.Count,failures};
}finally{RenderTexture.active=active;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(bridge.gameObject);}
