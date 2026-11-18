if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene edits");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();System.IO.File.WriteAllText("Tools/Design/DownhillPreview_20261104/scene_setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(setup));
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR05_Ribwood.unity");Physics2D.SyncTransforms();
var rows=new System.Collections.Generic.List<object>();
for(float x=150;x<=310;x+=.5f){var h=Physics2D.RaycastAll(new Vector2(x,300),Vector2.down,350,LayerMask.GetMask("Ground")).FirstOrDefault(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);if(h.collider!=null)rows.Add(new{x,y=h.point.y,nx=h.normal.x,ny=h.normal.y,name=h.collider.name});}
System.IO.File.WriteAllText("Tools/Design/DownhillPreview_20261104/terrain_scan.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return new{samples=rows.Count};
