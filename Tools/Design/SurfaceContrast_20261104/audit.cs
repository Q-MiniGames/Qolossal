if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
var rows=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();
try{foreach(var name in RegionalSceneCatalog.Surface.Concat(new[]{RegionalSceneCatalog.Underground,"MRAtlas_MR04_C02"})){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");
 var terrains=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MraTerrain>()).ToArray();
 int eligible=0,graded=0,excluded=0,vertices=0;
 foreach(var t in terrains){bool allowed=SurfaceTerrainContrast.IsUpperTerrain(t);if(allowed)eligible++;else excluded++;
  foreach(var r in t.GetComponentsInChildren<MeshRenderer>()){
   bool applied=r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Qolossal/Upper Terrain Contrast";
   if(applied&&!allowed)errors.Add(name+" unexpected graded renderer "+r.name);
   if(applied){graded++;var m=r.GetComponent<MeshFilter>().sharedMesh;vertices+=m.vertexCount;var uv=new System.Collections.Generic.List<Vector2>();m.GetUVs(2,uv);if(uv.Count!=m.vertexCount)errors.Add("Missing grade coordinates");}
  }
 }
 var foreign=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Qolossal/Upper Terrain Contrast"&&r.GetComponentInParent<MraTerrain>()==null).Count();
 if(foreign>0)errors.Add("Background or nonterrain renderer changed in "+name);
 if(eligible>0&&graded==0)errors.Add("Missing treatment in "+name);
 rows.Add(new{scene=name,eligibleTerrains=eligible,excludedTerrains=excluded,gradedMeshes=graded,vertices,extraVertexBytes=vertices*8,foreignGraded=foreign});
}}
finally{UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);}
var shader=Resources.Load<Shader>("FX/SurfaceTerrainContrast");var messages=ShaderUtil.GetShaderMessages(shader).Select(m=>m.message).ToArray();
System.IO.File.WriteAllText("Tools/Design/SurfaceContrast_20261104/scope_audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{rows,errors,shaderMessages=messages},Newtonsoft.Json.Formatting.Indented));
return new{scenes=rows.Count,errors,shaderMessages=messages};
