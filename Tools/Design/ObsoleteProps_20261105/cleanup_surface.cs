if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scenes");
string output="Tools/Design/ObsoleteProps_20261105/";
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var changes=new List<object>();
string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
try{foreach(var name in RegionalSceneCatalog.Surface){
string file=RegionalSceneCatalog.Folder+name+".unity";
System.IO.File.Copy(file,output+"Before/Scenes/"+name+".unity.backup",false);
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(file);
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
bool changed=false;
foreach(var t in all.Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Ledge ")&&t.parent!=null&&t.parent.name=="Cliff dressing")){
if(t.GetComponentsInChildren<Collider2D>(true).Length!=0||t.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("Not decor: "+PathOf(t));
changes.Add(new{scene=name,path=PathOf(t),action="Retired decorative ledge (no collision)"});t.gameObject.SetActive(false);changed=true;
}
foreach(var knot in all.Select(t=>t.GetComponent<MraKnot>()).Where(k=>k!=null&&k.gameObject.activeInHierarchy)){
changes.Add(new{scene=name,path=PathOf(knot.transform),action="Retired old wake knot and trigger"});knot.gameObject.SetActive(false);changed=true;
}
foreach(var gate in all.Select(t=>t.GetComponent<MraGate>()).Where(g=>g!=null&&g.requires.Contains("knot:grip"))){
changes.Add(new{scene=name,path=PathOf(gate.transform),action="Retired old knot gate; route always open"});gate.gameObject.SetActive(false);changed=true;
}
foreach(var variant in all.Select(t=>t.GetComponent<MraVariant>()).Where(v=>v!=null&&v.requires.Contains("knot:grip"))){
if(variant.invert)throw new Exception("Unexpected inverted crossing");
variant.requires=variant.requires.Where(p=>p!="knot:grip").ToArray();variant.Apply();
changes.Add(new{scene=name,path=PathOf(variant.transform),action="Existing crossing permanently present; no saved knot required"});changed=true;
}
if(changed){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
}}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText(output+"surface_changes.json",Newtonsoft.Json.JsonConvert.SerializeObject(changes,Newtonsoft.Json.Formatting.Indented));
return new{changes=changes.Count};
