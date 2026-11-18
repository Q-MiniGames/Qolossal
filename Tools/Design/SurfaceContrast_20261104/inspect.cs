if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var rows=new System.Collections.Generic.List<object>();
string Hier(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
try{foreach(var name in RegionalSceneCatalog.Surface){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 rows.Add(new{scene=name,terrains=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MraTerrain>(true)).Select(t=>new{path=Hier(t.transform),active=t.gameObject.activeInHierarchy,points=t.surface.Length,fill=t.fill!=null?t.fill.name:null,shade=t.shadeFloor,depth=t.shadeDepth,holes=t.caveOpenings,backdrop=t.chamberBackdrop!=null}).ToArray(),caves=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Select(c=>new{path=Hier(c.transform),position=c.EntrancePosition}).ToArray()});
}}
finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
System.IO.File.WriteAllText("Tools/Design/SurfaceContrast_20261104/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));
return new{regions=rows.Count};
