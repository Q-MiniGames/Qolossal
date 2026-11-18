if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;var results=new System.Collections.Generic.List<object>();
try{
 foreach(string name in RegionalSceneCatalog.Surface.Skip(1)){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
  results.Add(new{scene=name,report=WorldRevisionPaintedCaveSeams.ApplyCurrent()});
 }
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Underground+".unity");
 results.Add(new{scene=RegionalSceneCatalog.Underground,report=WorldRevisionCausewayBorder.ApplyCurrent()});
 return results;
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
