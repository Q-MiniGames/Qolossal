if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var results=new System.Collections.Generic.List<object>();
try{
 foreach(var name in RegionalSceneCatalog.Surface){
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
  results.Add(new{scene=name,restored=WorldRevisionSurfaceCaves.RestoreCurrent(),corners=WorldRevisionSurfaceCaves.RoundUpperCornersCurrent(.18f)});
 }
 return results;
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
