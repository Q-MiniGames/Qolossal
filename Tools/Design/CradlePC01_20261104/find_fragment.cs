if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity");
var list=new System.Collections.Generic.List<object>();
foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
 var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||!r.gameObject.activeInHierarchy||f.sharedMesh==null)continue;
 var b=r.bounds;if(b.min.x>60.2f||b.max.x<59.1f||b.min.y>27||b.max.y<26.3f)continue;
 var v=f.sharedMesh.vertices;var tr=f.sharedMesh.triangles;int hits=0;
 for(int i=0;i<tr.Length;i+=3){var a=f.transform.TransformPoint(v[tr[i]]);var c=f.transform.TransformPoint(v[tr[i+1]]);var d=f.transform.TransformPoint(v[tr[i+2]]);if(Mathf.Min(a.x,Mathf.Min(c.x,d.x))<60.2f&&Mathf.Max(a.x,Mathf.Max(c.x,d.x))>59.1f&&Mathf.Min(a.y,Mathf.Min(c.y,d.y))<27&&Mathf.Max(a.y,Mathf.Max(c.y,d.y))>26.3f)hits++;}
 if(hits>0)list.Add(new{f.name,parent=f.transform.parent?.name,r.sortingOrder,shader=r.sharedMaterial?.shader.name,hits});
}
return list;

