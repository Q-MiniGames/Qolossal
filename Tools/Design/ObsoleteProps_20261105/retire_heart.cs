if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
try{
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+"Qolossal_Underground.unity");
var roots=scene.GetRootGameObjects();var cp=roots.SelectMany(r=>r.GetComponentsInChildren<Checkpoint>(true)).Single(c=>c.CheckpointId=="regional:HEART");
var room=cp.GetComponentInParent<UndergroundRoom>();var ending=room.GetComponentInChildren<MraEnding>(true);var quarters=room.GetComponentsInChildren<MraHeartQuarter>(true);
var retired=new List<string>();void Retire(GameObject g){if(!g.activeSelf)return;retired.Add(g.name);g.SetActive(false);}
Retire(cp.gameObject);Retire(ending.gameObject);
foreach(var q in quarters){q.ending=null;Retire(q.gameObject);}
foreach(var t in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("UGP heart quarter ")||t.name=="After the ending (healed)"))Retire(t.gameObject);
foreach(var runtime in roots.SelectMany(r=>r.GetComponentsInChildren<RegionalUnderground>(true)))runtime.ending=null;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText("Tools/Design/ObsoleteProps_20261105/heart_retired.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{retired,legacyCheckpointFallback="regional:A08",allActivationMechanicsRetired=true},Newtonsoft.Json.Formatting.Indented));return retired;
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
