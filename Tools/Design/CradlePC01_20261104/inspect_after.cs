if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
string output="Tools/Design/CradlePC01_20261104/";
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity");
try{
 var roots=scene.GetRootGameObjects();
 string Hier(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
 float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
 var cave=roots.SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Single(p=>p.lowerRoomId=="PC01"&&!p.undergroundSide);
 var threshold=cave.entrance.GetComponent<CaveThresholdPresentation>();
 var terrains=roots.SelectMany(r=>r.GetComponentsInChildren<MraTerrain>(true)).Where(t=>t.gameObject.activeInHierarchy).Select(t=>new{path=Hier(t.transform),position=V(t.transform.position),surface=t.surface.Select(p=>V(t.transform.TransformPoint(p))).ToArray(),style=EditorJsonUtility.ToJson(t)}).ToArray();
 var renderers=cave.GetComponentsInChildren<Renderer>(true).Select(r=>new{path=Hier(r.transform),active=r.gameObject.activeInHierarchy,r.enabled,position=V(r.transform.position),bounds=new{center=V(r.bounds.center),size=V(r.bounds.size)},material=AssetDatabase.GetAssetPath(r.sharedMaterial),shader=r.sharedMaterial?.shader.name,mesh=r.GetComponent<MeshFilter>()!=null?AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh):null}).ToArray();
 var report=new{point=EditorJsonUtility.ToJson(cave),threshold=EditorJsonUtility.ToJson(threshold),entrance=V(cave.EntrancePosition),arrival=V(cave.ArrivalPosition),hierarchy=Hier(cave.transform),renderers,terrains,colliders=cave.GetComponentsInChildren<Collider2D>(true).Select(c=>new{path=Hier(c.transform),active=c.gameObject.activeInHierarchy,data=EditorJsonUtility.ToJson(c)}).ToArray()};
 System.IO.File.WriteAllText(output+"after.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 return new{cave=cave.connectionId,mouth=V(threshold.mouth),threshold.inward,renderers=renderers.Length,terrain=terrains.Length,output};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}

