if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
System.IO.File.WriteAllText("Tools/Design/PassageReview_20261104/scene_setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/Qolossal_Underground.unity");
var rooms=UnityEngine.Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None);
var arts=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(a=>a.name=="UGP passage backdrop").ToArray();
var result=new{rooms=rooms.Select(r=>new{r.roomId,bounds=new[]{r.WorldBounds.min.x,r.WorldBounds.min.y,r.WorldBounds.size.x,r.WorldBounds.size.y}}),passages=arts.Select(a=>new{owner=a.GetComponentInParent<UndergroundRoom>()?.roomId,a.sortingOrder,bounds=new[]{a.bounds.min.x,a.bounds.min.y,a.bounds.size.x,a.bounds.size.y},intersections=rooms.Where(r=>r.WorldBounds.Intersects(a.bounds)).Select(r=>r.roomId).ToArray()})};
System.IO.File.WriteAllText("Tools/Design/PassageReview_20261104/inventory.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{rooms=rooms.Length,passages=arts.Length};
