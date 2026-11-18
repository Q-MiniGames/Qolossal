if(EditorApplication.isPlaying)throw new Exception("Stop play first");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var rows=new System.Collections.Generic.List<object>();
string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
try {
foreach(var path in System.IO.Directory.GetFiles("Assets/Scenes/Game","*.unity")) {
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
 foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).Where(c=>!c.isTrigger&&c.gameObject.layer==LayerMask.NameToLayer("Ground")&&c.enabled&&c.gameObject.activeInHierarchy)) {
 var t=c.GetComponentInParent<MraTerrain>(); var r=c.GetComponentInParent<UndergroundRoom>();
 rows.Add(new{scene=scene.name,path=PathOf(c.transform),type=c.GetType().Name,pos=new[]{c.bounds.center.x,c.bounds.center.y},size=new[]{c.bounds.size.x,c.bounds.size.y},components=c.GetComponents<Component>().Select(x=>x.GetType().Name).ToArray(),room=r?.roomId,roomBottom=r?.WorldBounds.min.y,under=t!=null&&t.under!=null,surface=t?.surface.Select(v=>new[]{v.x,v.y}).ToArray(),bottom=t?.bottom,platform=CameraPlatformSurface.IsPlatform(c)});
 }
}
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText("Tools/Design/LedgeCamera_20261106/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));return new{count=rows.Count};
