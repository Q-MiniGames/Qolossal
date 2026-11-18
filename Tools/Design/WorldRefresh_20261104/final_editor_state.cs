if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new Exception("Final checks require stable edit mode");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unexpected unsaved scene changes");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR06_Heights.unity");
var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
if(scenes.Length!=13)throw new Exception("Shipping scene catalog changed");
var receipt=new {playing=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,enabledBuildScenes=scenes,saveFile=GameSave.FilePath,runInBackground=Application.runInBackground,enterPlayModeOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled,enterPlayModeOptions=EditorSettings.enterPlayModeOptions.ToString()};
System.IO.File.WriteAllText("Tools/Design/WorldRefresh_20261104/final_editor_state.json",Newtonsoft.Json.JsonConvert.SerializeObject(receipt,Newtonsoft.Json.Formatting.Indented));
return receipt;
