if(EditorApplication.isPlaying)throw new Exception("The play test has not finished");
var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(System.IO.File.ReadAllText("Tools/Design/DownhillPreview_20261104/scene_setup.json"));
UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
return new{playing=EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};
