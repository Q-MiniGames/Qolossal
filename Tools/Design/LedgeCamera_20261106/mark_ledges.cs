if(EditorApplication.isPlaying)throw new Exception("Stop play first");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scenes");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var requests=new[]{
 new{scene="MRAtlas_MR01_Cradle",names=new[]{"Sealed overhang","Fallen stone crossing"}},
 new{scene="MRAtlas_MR03_Terraces",names=new[]{"Lintel over the sluice"}},
 new{scene="MRAtlas_MR04_C05",names=new[]{"Sleeping loft (deck)","Loft ladder (rung)"}},
 new{scene="MRAtlas_MR04_C06",names=new[]{"Loft stair (collision on the painted walk lines)"}}
};
var marked=new System.Collections.Generic.List<string>();
try{foreach(var request in requests){
 string path="Assets/Scenes/Game/"+request.scene+".unity";
 string backup="Tools/Design/LedgeCamera_20261106/Before/"+request.scene+".unity";
 System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
 if(!System.IO.File.Exists(backup))System.IO.File.Copy(path,backup);
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
 foreach(var name in request.names){
  var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).Where(c=>c.name==name&&!c.isTrigger&&c.gameObject.layer==LayerMask.NameToLayer("Ground")).ToArray();
  if(colliders.Length==0)throw new Exception("Missing ledge: "+request.scene+"/"+name);
  foreach(var c in colliders){if(c.GetComponent<CameraPlatformSurface>()==null)c.gameObject.AddComponent<CameraPlatformSurface>();marked.Add(request.scene+"/"+name);}
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText("Tools/Design/LedgeCamera_20261106/marked.json",Newtonsoft.Json.JsonConvert.SerializeObject(marked));return marked;
