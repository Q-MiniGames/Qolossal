if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
try{
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR02_Causeway.unity");
 var restored=WorldRevisionSurfaceCaves.RestoreCurrent();
 var corners=WorldRevisionSurfaceCaves.RoundUpperCornersCurrent(.18f);
 return new{restored,corners};
}finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
