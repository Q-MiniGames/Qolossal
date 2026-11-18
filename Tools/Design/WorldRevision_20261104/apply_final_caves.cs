if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
try{
 UnityEditor.SceneManagement.EditorSceneManager.OpenScene(WorldRefreshUnderground.ScenePath);
 UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/FX/WorldRefreshCaveVista.shader",UnityEditor.ImportAssetOptions.ForceUpdate|UnityEditor.ImportAssetOptions.ForceSynchronousImport);
 var applied=WorldRevisionCaveVistas.ApplyPaintedExtensions(true);
 var audited=WorldRevisionCaveVistas.AuditComplete();
 return new{applied,audited};
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
