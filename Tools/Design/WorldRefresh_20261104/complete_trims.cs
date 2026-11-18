if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
var results=new System.Collections.Generic.List<object>();
foreach(string name in RegionalSceneCatalog.Surface){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var mask=WorldRefreshTrimSilhouette.ApplyCurrent();
 var decor=WorldRefreshDecorGrounding.ApplyCurrent();
 object cradle=null;if(name==RegionalSceneCatalog.Surface[0])cradle=WorldRefreshSurface.RetireWeakCradleOverlaysCurrent();
 var validation=WorldRefreshSurface.ValidateCurrent();
 results.Add(new{scene=name,mask,decor,cradle,validation});
 System.IO.File.WriteAllText("Tools/Design/WorldRefresh_20261104/trim_application.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
}
return new{regions=results.Count};
