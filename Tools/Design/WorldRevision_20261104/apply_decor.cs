if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Surface[2]+".unity");
 return WorldRevisionTerracesDecor.ApplyCurrent();
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
