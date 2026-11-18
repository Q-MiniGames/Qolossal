var p=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
var state=new{wasPlaying=EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,position=p!=null?new[]{p.transform.position.x,p.transform.position.y}:null,savePath=GameSave.FilePath};
System.IO.File.WriteAllText("Tools/Design/DownhillPreview_20261104/session_before.json",Newtonsoft.Json.JsonConvert.SerializeObject(state,Newtonsoft.Json.Formatting.Indented));
EditorApplication.isPlaying=false;return state;



