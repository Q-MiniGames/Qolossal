if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
try{return WorldRevisionRoofClosure.Apply();}
finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
