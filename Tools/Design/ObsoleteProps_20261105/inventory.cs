if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scenes");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
var rows=new List<object>();
try{
foreach(var file in System.IO.Directory.GetFiles("Assets/Scenes/Game","*.unity").OrderBy(s=>s)){
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(file);
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
var objects=all.Where(t=>t.name.IndexOf("knot",StringComparison.OrdinalIgnoreCase)>=0||t.name.StartsWith("Ledge ")||PathOf(t).Contains("Heart")&&(t.GetComponent<Checkpoint>()!=null||t.GetComponent<MraEnding>()!=null||t.GetComponent<SpriteRenderer>()!=null||t.GetComponent<MraHeartQuarter>()!=null)).Select(t=>new{path=PathOf(t),active=t.gameObject.activeInHierarchy,pos=new[]{t.position.x,t.position.y},components=t.GetComponents<Component>().Where(c=>c!=null).Select(c=>c.GetType().Name).ToArray(),sprite=t.GetComponent<SpriteRenderer>()!=null && t.GetComponent<SpriteRenderer>().sprite!=null?t.GetComponent<SpriteRenderer>().sprite.name:null,checkpoint=t.GetComponent<Checkpoint>()!=null?t.GetComponent<Checkpoint>().CheckpointId:null}).ToArray();
var dependencies=new List<object>();
foreach(var c in all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null)){
var so=new SerializedObject(c);var it=so.GetIterator();
while(it.Next(true))if(it.propertyType==SerializedPropertyType.String&&!string.IsNullOrEmpty(it.stringValue)&&(it.stringValue.StartsWith("knot:")||c is MraKnot||c is MraHeartQuarter||c is MraEnding))dependencies.Add(new{path=PathOf(c.transform),type=c.GetType().Name,active=c.gameObject.activeInHierarchy,property=it.propertyPath,value=it.stringValue});
}
rows.Add(new{scene=file,objects,dependencies});
}
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText("Tools/Design/ObsoleteProps_20261105/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
return new{scenes=rows.Count,report="Tools/Design/ObsoleteProps_20261105/inventory.json"};

