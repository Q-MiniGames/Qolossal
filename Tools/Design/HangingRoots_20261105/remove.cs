if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scenes");
string folder="Tools/Design/HangingRoots_20261105/",path=RegionalSceneCatalog.Folder+"MRAtlas_MR05_Ribwood.unity";
System.IO.Directory.CreateDirectory(folder+"Before");System.IO.File.Copy(path,folder+"Before/MRAtlas_MR05_Ribwood.unity.backup",false);
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
try{
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
var roots=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Where(s=>s.gameObject.activeInHierarchy&&s.sprite!=null&&s.sprite.name=="Decor_A2_hanging_roots").ToArray();
if(roots.Length!=19)throw new Exception("Unexpected root count "+roots.Length);
foreach(var sr in roots)if(sr.transform.parent.name!="Cliff dressing"||sr.GetComponentsInChildren<Component>(true).Any(c=>!(c is Transform)&&!(c is SpriteRenderer)))throw new Exception("Not purely decorative: "+sr.name);
foreach(var sr in roots)sr.gameObject.SetActive(false);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"removal.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{removed=roots.Length,scene=scene.name,sprite="Decor_A2_hanging_roots",collisionRemoved=false,positions=roots.Select(s=>new[]{s.transform.position.x,s.transform.position.y}).ToArray()},Newtonsoft.Json.Formatting.Indented));
return new{removed=roots.Length};
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
