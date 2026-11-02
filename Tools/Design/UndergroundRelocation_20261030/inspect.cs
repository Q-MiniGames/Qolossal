if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Edit mode required");
var s=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game/Qolossal_Underground.unity");
try{
 var roots=s.GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
 var rooms=all.SelectMany(t=>t.GetComponents<UndergroundRoom>()).Where(r=>new[]{"MR06_C01","PC11","PC12"}.Contains(r.roomId)).ToArray();
 Func<Transform,string> hp=t=>{var n=new System.Collections.Generic.List<string>();while(t!=null){n.Add(t.name);t=t.parent;}n.Reverse();return string.Join("/",n);};
 var list=new System.Collections.Generic.List<object>();
 foreach(var room in rooms){var b=room.WorldBounds;b.Expand(new Vector3(10,6,100));
 var renderers=all.SelectMany(t=>t.GetComponents<Renderer>()).Where(r=>r.bounds.Intersects(b)).Select(r=>new{path=hp(r.transform),type=r.GetType().Name,child=r.transform.IsChildOf(room.transform),bounds=new[]{r.bounds.min.x,r.bounds.min.y,r.bounds.max.x,r.bounds.max.y},mesh=r.GetComponent<MeshFilter>()!=null?UnityEditor.AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh):null}).ToArray();
 var colliders=all.SelectMany(t=>t.GetComponents<Collider2D>()).Where(c=>c.bounds.Intersects(b)).Select(c=>new{path=hp(c.transform),type=c.GetType().Name,child=c.transform.IsChildOf(room.transform),enabled=c.enabled,points=c is EdgeCollider2D e?e.pointCount:c is PolygonCollider2D p?p.GetTotalPointCount():0}).ToArray();
 list.Add(new{id=room.roomId,path=hp(room.transform),position=room.transform.position,children=room.transform.Cast<Transform>().Select(t=>t.name).ToArray(),renderers,colliders});}
 System.IO.File.WriteAllText("C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/UndergroundRelocation_20261030/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(list,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));return new{rooms=rooms.Length};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(s);}
