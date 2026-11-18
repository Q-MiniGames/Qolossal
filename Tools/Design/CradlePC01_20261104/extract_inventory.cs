// Read-only saved-scene extraction. Run after saving the complete revision.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode required");
var areas=new System.Collections.Generic.List<object>();var rooms=new System.Collections.Generic.List<object>();var caves=new System.Collections.Generic.List<object>();
var sceneHashes=new System.Collections.Generic.Dictionary<string,string>();
var presentations=new System.Collections.Generic.List<object>();
System.Func<Vector3,float[]> xy=p=>new[]{p.x,p.y};
foreach(string path in RegionalSceneCatalog.BuildPaths){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
 try{
  var roots=scene.GetRootGameObjects();
  foreach(var a in roots.SelectMany(r=>r.GetComponentsInChildren<SurfaceArea>(true))){var b=a.WorldBounds;areas.Add(new{id=a.areaId,name=a.areaName,scene=scene.name,bounds=new[]{b.min.x,b.min.y,b.size.x,b.size.y}});}
  foreach(var r in roots.SelectMany(t=>t.GetComponentsInChildren<UndergroundRoom>(true))){var b=r.WorldBounds;rooms.Add(new{id=r.roomId,name=r.roomName,scene=scene.name,bounds=new[]{b.min.x,b.min.y,b.size.x,b.size.y}});}
  foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Where(c=>c.isActiveAndEnabled)){
   caves.Add(new{c.connectionId,c.upperScene,c.upperRegion,c.upperAreaId,c.upperAreaName,c.lowerRoomId,c.lowerRoomName,c.requirements,c.activation,c.undergroundSide,scene=scene.name,entrancePosition=xy(c.EntrancePosition),arrivalPosition=xy(c.ArrivalPosition)});
   var show=c.entrance!=null?c.entrance.GetComponent<CaveThresholdPresentation>():null;
   presentations.Add(new{c.connectionId,c.undergroundSide,scene=scene.name,mouth=show!=null?new[]{show.mouth.x,show.mouth.y}:null,inward=show!=null?show.inward:0f,entrance=xy(c.EntrancePosition),arrival=xy(c.ArrivalPosition),arrivalId=c.ArrivalId,presentation=show!=null});
  }
  using(var f=System.IO.File.OpenRead(path))using(var sha=System.Security.Cryptography.SHA256.Create())sceneHashes[scene.name]=System.BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
 }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
}
if(areas.Count!=84||rooms.Count!=89||caves.Count!=74||sceneHashes.Count!=13)throw new System.Exception("Unexpected catalog/area/room/pair coverage");
string folder="Tools/Design/CradlePC01_20261104/Map/";System.IO.Directory.CreateDirectory(folder);
System.IO.File.WriteAllText(folder+"inventory_fresh.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{areas,rooms,caves,sceneHashes},Newtonsoft.Json.Formatting.Indented));
System.IO.File.WriteAllText(folder+"presentations_fresh.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{sceneHashes,presentations},Newtonsoft.Json.Formatting.Indented));
return new{areas=areas.Count,rooms=rooms.Count,caves=caves.Count,scenes=sceneHashes.Count,folder,savedScenes=false};

