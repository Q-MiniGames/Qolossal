if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Unsaved scene");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
string file=RegionalSceneCatalog.Folder+"Qolossal_Underground.unity",output="Tools/Design/ObsoleteProps_20261105/";
System.IO.File.Copy(file,output+"Before/Scenes/Qolossal_Underground.unity.backup",false);
try{
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(file);
var roots=scene.GetRootGameObjects();var checkpoint=roots.SelectMany(r=>r.GetComponentsInChildren<Checkpoint>(true)).Single(c=>c.CheckpointId=="regional:HEART");
foreach(var sr in checkpoint.GetComponentsInChildren<SpriteRenderer>(true))sr.enabled=false;
var room=checkpoint.GetComponentInParent<UndergroundRoom>();
var ending=room.GetComponentInChildren<MraEnding>(true);
var quarters=room.GetComponentsInChildren<MraHeartQuarter>(true);
if(quarters.Length!=4||quarters.Any(q=>q.ending!=ending))throw new Exception("Heart controls not connected");
// The four current controls can still call Play() on this active object; retiring
// its autonomous Update and trigger removes the obsolete central knot shortcut.
ending.enabled=false;ending.GetComponent<BoxCollider2D>().enabled=false;
var art=room.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.sprite!=null&&s.sprite.name.StartsWith("Wakeknot_")).ToArray();
if(art.Length!=2)throw new Exception("Unexpected Heart knot art count");
foreach(var sr in art)sr.enabled=false;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(output+"heart_changes.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checkpointId=checkpoint.CheckpointId,checkpointActive=checkpoint.isActiveAndEnabled,hiddenKnotSprites=art.Select(s=>s.sprite.name).ToArray(),endingObjectActive=ending.gameObject.activeInHierarchy,centralShortcutDisabled=!ending.enabled,quarterControls=quarters.Length},Newtonsoft.Json.Formatting.Indented));
return "Heart props removed; safe respawn and four-control ending retained";
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
