if(EditorApplication.isPlaying)throw new Exception("Wait for the isolated play test to finish");
var fields=UnityEngine.Object.FindObjectsByType<WorldRefreshCaveVista>(FindObjectsSortMode.None);var rows=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
foreach(var field in fields){foreach(var texture in new[]{field.vista,field.leftDepth,field.rightDepth,field.upperVault,field.extension}){
 string path=AssetDatabase.GetAssetPath(texture);var importer=(TextureImporter)AssetImporter.GetAtPath(path);int w,h;importer.GetSourceTextureWidthAndHeight(out w,out h);
 float worldWidth=texture.width/field.pixelsPerUnit,worldHeight=texture.height/field.pixelsPerUnit;
 int requiredWidth=Mathf.CeilToInt(worldWidth*216-.001f),requiredHeight=Mathf.CeilToInt(worldHeight*216-.001f);
 bool passed=texture.width>=requiredWidth&&texture.height>=requiredHeight&&w>=requiredWidth&&h>=requiredHeight;
 if(!passed)errors.Add(path);rows.Add(new{path,nativeWidth=w,nativeHeight=h,importedWidth=texture.width,importedHeight=texture.height,worldWidth,worldHeight,requiredWidth,requiredHeight,passed});
}}
System.IO.File.WriteAllText("Tools/Design/PassageReview_20261104/density.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rooms=fields.Length,screenPixelsPerWorldUnit=216,rows,errors},Newtonsoft.Json.Formatting.Indented));
var setup=Newtonsoft.Json.JsonConvert.DeserializeObject<UnityEditor.SceneManagement.SceneSetup[]>(System.IO.File.ReadAllText("Tools/Design/PassageReview_20261104/scene_setup.json"));
UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
return new{rooms=fields.Length,errors,restoredScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name};
