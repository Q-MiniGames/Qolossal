if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Edit mode required");
string scenePath="Assets/Scenes/Game/Qolossal_Underground.unity",output="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/UndergroundRelocation_20261030/";
var data=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(output+"backing.json"));
string hash;using(var f=System.IO.File.OpenRead(scenePath))using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
if(hash!=(string)data["sourceHash"])throw new Exception("Scene differs from reviewed and backed-up source");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved editor scene: preserve before applying");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
try{
 var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
 var rooms=all.SelectMany(t=>t.GetComponents<UndergroundRoom>()).ToDictionary(r=>r.roomId);
 var moves=new[]{("MR06_C01",15,new Vector3(-205,-110,0)),("PC11",17,new Vector3(-91,-62.481f,0)),("PC12",3,new Vector3(-75,-36.304f,0))};
 var owned=new System.Collections.Generic.Dictionary<string,Transform[]>();
 foreach(var m in moves){
  var names=new[]{"Rim_"+m.Item2.ToString("000"),"Natural_Rim_"+m.Item2.ToString("000"),"Natural_Platforms_"+m.Item1};
  var extra=all.Where(t=>names.Contains(t.name)||t.name.StartsWith("Continuous_Collision_"+m.Item2+" | preserved segment ")).ToArray();
  if(extra.Length!=5)throw new Exception(m.Item1+" expected 3 visual meshes + 2 preserved edges, got "+extra.Length);
  owned.Add(m.Item1,extra);
 }
 var backing=all.Single(t=>t.name=="01 Shared local backing - all cave air subtracted");
 var sample=backing.GetComponentsInChildren<MeshRenderer>().First();
 string folder="Assets/Development/Layouts/UndergroundRelocation_20261030/Backing";System.IO.Directory.CreateDirectory(folder);UnityEditor.AssetDatabase.Refresh();
 var tiles=new System.Collections.Generic.List<object>();
 foreach(var token in (Newtonsoft.Json.Linq.JArray)data["chunks"]){
  string name=(string)token["name"];var existing=backing.Cast<Transform>().SingleOrDefault(t=>t.name==name);var vs=(Newtonsoft.Json.Linq.JArray)token["v"];
  if(vs.Count==0){if(existing!=null)existing.gameObject.SetActive(false);tiles.Add(new{name,vertices=0});continue;}
  var go=existing!=null?existing.gameObject:new GameObject(name);if(existing==null)go.transform.SetParent(backing,false);
  var mf=go.GetComponent<MeshFilter>();if(mf==null)mf=go.AddComponent<MeshFilter>();var mr=go.GetComponent<MeshRenderer>();if(mr==null)mr=go.AddComponent<MeshRenderer>();
  if(existing==null){UnityEditor.EditorUtility.CopySerialized(sample,mr);go.layer=sample.gameObject.layer;}
  var mesh=new Mesh{name=name+" relocated backing"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
  mesh.vertices=vs.Select(v=>new Vector3((float)v[0],(float)v[1],0)).ToArray();mesh.triangles=Enumerable.Range(0,vs.Count).ToArray();
  var weights=(Newtonsoft.Json.Linq.JArray)token["w"];mesh.SetUVs(0,weights.Select(w=>new Vector4((float)w[0],(float)w[1],(float)w[2],(float)w[3])).ToList());mesh.SetUVs(1,weights.Select(w=>new Vector4((float)w[4],(float)w[5],(float)w[6],(float)w[7])).ToList());mesh.colors=((Newtonsoft.Json.Linq.JArray)token["s"]).Select(v=>new Color((float)v,(float)v,(float)v,1)).ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();
  string asset=folder+"/"+name+".asset";var prior=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(asset);if(prior!=null){if(prior.vertexCount!=mesh.vertexCount)throw new Exception("Unexpected prior mesh");UnityEngine.Object.DestroyImmediate(mesh);mesh=prior;}else UnityEditor.AssetDatabase.CreateAsset(mesh,asset);mf.sharedMesh=mesh;tiles.Add(new{name,vertices=vs.Count});
 }
 var result=new System.Collections.Generic.List<object>();
 foreach(var m in moves){var room=rooms[m.Item1];var before=room.WorldBounds;var checks=room.GetComponentsInChildren<Checkpoint>(true).Select(c=>new{c.CheckpointId,position=c.transform.position}).ToArray();
  foreach(var p in room.GetComponentsInChildren<CaveThresholdPresentation>(true))p.mouth+=(Vector2)m.Item3;
  room.transform.position+=m.Item3;foreach(var t in owned[m.Item1])t.position+=m.Item3;
  var after=room.WorldBounds;result.Add(new{id=m.Item1,delta=new[]{m.Item3.x,m.Item3.y},oldBounds=new[]{before.min.x,before.min.y,before.size.x,before.size.y},newBounds=new[]{after.min.x,after.min.y,after.size.x,after.size.y},sharedObjects=owned[m.Item1].Select(t=>t.name).ToArray(),checkpoints=checks});
 }
 UnityEditor.AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
 System.IO.File.WriteAllText(output+"application.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{sourceHash=hash,moves=result,tiles},Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));return new{moved=result.Count,backingTiles=tiles.Count,saved=scenePath};
}finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}

