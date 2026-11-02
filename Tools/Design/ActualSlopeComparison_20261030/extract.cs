if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Read-only extraction requires Edit mode.");
var output="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/ActualSlopeComparison_20261030/";
var paths=UnityEditor.EditorBuildSettings.scenes.Where(s=>s.enabled&&s.path.Contains("MRAtlas_MR")).Select(s=>s.path).ToArray();
var result=new System.Collections.Generic.List<object>();
System.Func<Transform,string> hierarchy=t=>{var names=new System.Collections.Generic.List<string>();while(t!=null){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);};
System.Func<Vector3,float[]> xy=p=>new[]{p.x,p.y};
foreach(var path in paths){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
 try{
 var roots=scene.GetRootGameObjects();var terrains=roots.SelectMany(r=>r.GetComponentsInChildren<MraTerrain>(true)).ToArray();
 var terrainData=terrains.Select(t=>new{path=hierarchy(t.transform),active=t.isActiveAndEnabled,solid=t.solid,surface=t.surface.Select(p=>xy(t.transform.TransformPoint(p))).ToArray(),bottom=t.bottom,position=xy(t.transform.position),scale=xy(t.transform.lossyScale),colliderEnabled=(t.GetComponent<PolygonCollider2D>()!=null&&t.GetComponent<PolygonCollider2D>().enabled)}).ToArray();
 var colliderData=new System.Collections.Generic.List<object>();
 foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<Collider2D>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&c.gameObject.layer==LayerMask.NameToLayer("Ground"))){
  var lines=new System.Collections.Generic.List<float[][]>();bool closed=true;
  if(c is PolygonCollider2D poly){for(int i=0;i<poly.pathCount;i++)lines.Add(poly.GetPath(i).Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else if(c is EdgeCollider2D edge){closed=false;lines.Add(edge.points.Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else if(c is BoxCollider2D box){var q=box.size*.5f;lines.Add(new[]{new Vector2(-q.x,-q.y),new Vector2(q.x,-q.y),new Vector2(q.x,q.y),new Vector2(-q.x,q.y)}.Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else continue;
  colliderData.Add(new{path=hierarchy(c.transform),type=c.GetType().Name,closed,terrainOwner=c.GetComponent<MraTerrain>()!=null,dynamic=(c.attachedRigidbody!=null&&c.attachedRigidbody.bodyType!=RigidbodyType2D.Static)||c.GetComponentInParent<MovingPlatform>()!=null,paths=lines});
 }
 var areas=roots.SelectMany(r=>r.GetComponentsInChildren<SurfaceArea>(true)).Select(a=>new{id=a.areaId,name=a.areaName,bounds=new[]{a.WorldBounds.min.x,a.WorldBounds.min.y,a.WorldBounds.size.x,a.WorldBounds.size.y}}).ToArray();
 var caves=roots.SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Where(c=>c.isActiveAndEnabled&&!c.undergroundSide).Select(c=>new{id=c.connectionId,room=c.lowerRoomId,area=c.upperAreaId,entrance=xy(c.EntrancePosition),arrival=xy(c.ArrivalPosition),requirements=c.requirements}).ToArray();
 var exits=roots.SelectMany(r=>r.GetComponentsInChildren<MraExit>(true)).Where(e=>e.isActiveAndEnabled).Select(e=>new{path=hierarchy(e.transform),e.targetScene,e.arrivalId,e.direction,position=xy(e.transform.position),ground=xy(e.transform.position-Vector3.up*e.groundOffset)}).ToArray();
 var arrivals=roots.SelectMany(r=>r.GetComponentsInChildren<MraArrival>(true)).Select(a=>new{a.arrivalId,position=xy(a.transform.position)}).ToArray();
 using(var file=System.IO.File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())result.Add(new{scene=scene.name,path,sha256=System.BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").ToLowerInvariant(),terrains=terrainData,colliders=colliderData,areas,caves,exits,arrivals});
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
System.IO.File.WriteAllText(output+"scene_geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source="Saved scenes opened as read-only Unity preview scenes; TransformPoint world coordinates; no scenes saved",unity=Application.unityVersion,scenes=result},Newtonsoft.Json.Formatting.Indented));return new{scenes=result.Count,file=output+"scene_geometry.json",playing=UnityEditor.EditorApplication.isPlaying};
