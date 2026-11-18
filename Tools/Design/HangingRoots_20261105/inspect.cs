if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
var rows=new List<object>();
foreach(var name in RegionalSceneCatalog.Surface){var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(RegionalSceneCatalog.Folder+name+".unity");try{
foreach(var sr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Where(s=>s.gameObject.activeInHierarchy&&s.name.StartsWith("Hanging "))){
rows.Add(new{scene=name,sr.name,sprite=sr.sprite!=null?sr.sprite.name:null,asset=AssetDatabase.GetAssetPath(sr.sprite),parent=sr.transform.parent.name,pos=new[]{sr.transform.position.x,sr.transform.position.y},components=sr.GetComponentsInChildren<Component>(true).Select(c=>c.GetType().Name).ToArray()});
}}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}}
System.IO.File.WriteAllText("Tools/Design/HangingRoots_20261105/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
