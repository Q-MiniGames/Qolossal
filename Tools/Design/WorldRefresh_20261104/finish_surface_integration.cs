if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string prior=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
var results=new System.Collections.Generic.List<object>();
try{foreach(string name in RegionalSceneCatalog.Surface){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var root=scene.GetRootGameObjects().First(g=>g.name=="World refresh | regional scenery 20261104");
 var sky=root.GetComponent<WorldRefreshSky>();
 if(sky!=null){var child=new GameObject("Sky gradient").AddComponent<WorldRefreshSky>();child.transform.SetParent(root.transform,false);EditorUtility.CopySerialized(sky,child);UnityEngine.Object.DestroyImmediate(sky);root.transform.position=Vector3.zero;child.Refresh();}
 var weather=root.GetComponent<WorldRefreshWeather>();
 if(weather!=null)for(int i=0;i<weather.clouds.Length;i++){var p=weather.clouds[i];p.climbProgression=true;p.verticalRate=.01f;p.sinkLimit=.6f;p.baseY=1.6f+(i%2)*1.5f;}
 foreach(var p in root.GetComponentsInChildren<ParallaxLayer>())p.Refresh(Camera.main);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 results.Add(WorldRefreshTrimDensity.ApplyCurrent());
}}
finally{UnityEditor.SceneManagement.EditorSceneManager.OpenScene(prior);}
System.IO.File.WriteAllText("Tools/Design/WorldRefresh_20261104/surface_finish_result.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
return results;
