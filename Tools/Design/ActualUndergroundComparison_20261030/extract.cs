if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Extraction requires Edit mode.");
var path="Assets/Scenes/Game/Qolossal_Underground.unity";
var output="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/ActualUndergroundComparison_20261030/scene_geometry.json";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
try{
 var roots=scene.GetRootGameObjects();
 System.Func<Transform,string> hierarchy=t=>{var names=new System.Collections.Generic.List<string>();while(t!=null){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);};
 System.Func<Vector3,float[]> xy=p=>new[]{p.x,p.y};
 var colliders=new System.Collections.Generic.List<object>();var unsupported=new System.Collections.Generic.List<string>();
 foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<Collider2D>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger)){
  var lines=new System.Collections.Generic.List<float[][]>();bool closed=true;
  if(c is PolygonCollider2D poly){for(int i=0;i<poly.pathCount;i++)lines.Add(poly.GetPath(i).Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else if(c is EdgeCollider2D edge){closed=false;lines.Add(edge.points.Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else if(c is BoxCollider2D box){var q=box.size*.5f;var pts=new System.Collections.Generic.List<Vector2>();var corners=new[]{new Vector2(q.x,q.y),new Vector2(-q.x,q.y),new Vector2(-q.x,-q.y),new Vector2(q.x,-q.y)};
   if(box.edgeRadius>0){for(int corner=0;corner<4;corner++)for(int j=0;j<=8;j++){float a=(corner*90+j*90f/8)*Mathf.Deg2Rad;pts.Add(corners[corner]+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*box.edgeRadius);}}
   else pts.AddRange(corners);lines.Add(pts.Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}
  else if(c is CompositeCollider2D composite){closed=composite.geometryType==CompositeCollider2D.GeometryType.Polygons;for(int i=0;i<composite.pathCount;i++){var pts=new Vector2[composite.GetPathPointCount(i)];composite.GetPath(i,pts);lines.Add(pts.Select(p=>xy(c.transform.TransformPoint(p+c.offset))).ToArray());}}
  else{unsupported.Add(LayerMask.LayerToName(c.gameObject.layer)+" / "+c.GetType().Name+" / "+hierarchy(c.transform));continue;}
  var room=c.GetComponentInParent<UndergroundRoom>();
  colliders.Add(new{path=hierarchy(c.transform),type=c.GetType().Name,layer=LayerMask.LayerToName(c.gameObject.layer),closed,room=room!=null?room.roomId:null,dynamic=c.attachedRigidbody!=null&&c.attachedRigidbody.bodyType!=RigidbodyType2D.Static,paths=lines});
 }
 var terrain=roots.SelectMany(r=>r.GetComponentsInChildren<MraTerrain>(true)).Where(t=>t.isActiveAndEnabled&&t.solid).Select(t=>new{path=hierarchy(t.transform),surface=t.surface.Select(p=>xy(t.transform.TransformPoint(p))).ToArray()}).ToArray();
 var rooms=roots.SelectMany(r=>r.GetComponentsInChildren<UndergroundRoom>(true)).Select(r=>new{id=r.roomId,name=r.roomName,r.region,bounds=new[]{r.WorldBounds.min.x,r.WorldBounds.min.y,r.WorldBounds.size.x,r.WorldBounds.size.y}}).ToArray();
 var caves=roots.SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Where(c=>c.isActiveAndEnabled&&c.undergroundSide).Select(c=>new{id=c.connectionId,room=c.lowerRoomId,area=c.upperAreaId,c.upperScene,entrance=xy(c.EntrancePosition),arrival=xy(c.ArrivalPosition),c.requirements}).ToArray();
 string hash;using(var f=System.IO.File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())hash=System.BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
 System.IO.File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.name,path,sha256=hash,unity=Application.unityVersion,rooms,caves,colliders,terrain,unsupported},Newtonsoft.Json.Formatting.Indented));
 return new{rooms=rooms.Length,caves=caves.Length,colliders=colliders.Count,terrain=terrain.Length,unsupported,file=output};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
