using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

// Preserve the native terrain's painted roof trim above the new fitted passage.
public static class CaveRolloutTerrainJoin
{
    const string Output="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CaveRollout_20261027/";
    const string Root="Assets/Development/Layouts/CaveRollout_20261027/";
    struct V { public Vector3 p; public Vector2 uv; public Color c; public static V Mix(V a,V b,float t)=>new V{p=Vector3.LerpUnclamped(a.p,b.p,t),uv=Vector2.LerpUnclamped(a.uv,b.uv,t),c=Color.LerpUnclamped(a.c,b.c,t)}; }
    static List<V> Half(List<V> input,int axis,float limit,bool greater){var result=new List<V>();if(input.Count==0)return result;var a=input[input.Count-1];float da=(axis==0?a.p.x:a.p.y)-limit;foreach(var b in input){float db=(axis==0?b.p.x:b.p.y)-limit;bool ia=greater?da>=0:da<=0,ib=greater?db>=0:db<=0;if(ia!=ib)result.Add(V.Mix(a,b,da/(da-db)));if(ib)result.Add(b);a=b;da=db;}return result;}
    static List<V> Inside(List<V> p,Rect r){p=Half(p,0,r.xMin,true);p=Half(p,0,r.xMax,false);p=Half(p,1,r.yMin,true);return Half(p,1,r.yMax,false);}
    static IEnumerable<List<V>> Outside(List<V> p,Rect r){var rem=p;var a=Half(rem,0,r.xMin,false);if(a.Count>=3)yield return a;rem=Half(rem,0,r.xMin,true);a=Half(rem,0,r.xMax,true);if(a.Count>=3)yield return a;rem=Half(rem,0,r.xMax,false);a=Half(rem,1,r.yMin,false);if(a.Count>=3)yield return a;rem=Half(rem,1,r.yMin,true);a=Half(rem,1,r.yMax,true);if(a.Count>=3)yield return a;}
    static Mesh Clip(MeshFilter f,Rect r,bool inside){var mesh=f.sharedMesh;var v=mesh.vertices;var uv=mesh.uv;var c=mesh.colors;var tr=mesh.triangles;var result=new List<V>();var indices=new List<int>();
        V Read(int i)=>new V{p=f.transform.TransformPoint(v[i]),uv=uv.Length==v.Length?uv[i]:Vector2.zero,c=c.Length==v.Length?c[i]:Color.white};
        void Add(List<V> p){int n=result.Count;result.AddRange(p);for(int i=1;i<p.Count-1;i++)indices.AddRange(new[]{n,n+i,n+i+1});}
        for(int i=0;i<tr.Length;i+=3){var p=new List<V>{Read(tr[i]),Read(tr[i+1]),Read(tr[i+2])};if(inside){var xs=new[]{r.xMin,r.xMin+.3f,r.xMax-.3f,r.xMax};var ys=new[]{r.yMin,r.yMin+.45f,r.yMax};for(int xx=0;xx<3;xx++)for(int yy=0;yy<2;yy++){var poly=Inside(p,Rect.MinMaxRect(xs[xx],ys[yy],xs[xx+1],ys[yy+1]));for(int j=0;j<poly.Count;j++){var vtx=poly[j];vtx.c.a*=Mathf.SmoothStep(0,1,Mathf.Clamp01((vtx.p.y-r.yMin)/.45f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((vtx.p.x-r.xMin)/.3f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((r.xMax-vtx.p.x)/.3f));poly[j]=vtx;}Add(poly);}}else foreach(var q in Outside(p,r))Add(q);}
        var m=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(result.Select(x=>x.p).ToList());m.SetUVs(0,result.Select(x=>x.uv).ToList());m.SetColors(result.Select(x=>x.c).ToList());m.SetTriangles(indices,0);m.RecalculateBounds();return m;
    }
    static Mesh Save(string path,Mesh m){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(m,path);return m;}old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;old.uv=m.uv;old.colors=m.colors;old.triangles=m.triangles;old.RecalculateBounds();old.UploadMeshData(false);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(m);return old;}
    public static object Build(int region){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");EditorSceneManager.OpenScene(RegionalSceneCatalog.BuildPaths[region]);var plans=JArray.Parse(System.IO.File.ReadAllText(Output+"upper_result_"+region+".json"));var points=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None);var records=new List<object>();
        foreach(var q in plans){string id=(string)q["id"];var p=points.Single(p=>p.lowerRoomId==id);var group=p.transform.Find("Regional fitted passage");var existing=group.Find("Native roof trim");if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);var parent=new GameObject("Native roof trim").transform;parent.SetParent(group,true);
            float mouth=(float)q["mouth"],floor=(float)q["floor"],dir=(float)q["dir"],depth=(float)q["depth"];var terrain=UnityEngine.Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).Single(t=>t.name==(string)q["terrain"]);var opening=terrain.caveOpenings.FirstOrDefault(c=>c.roomId==id);if(opening==null)continue;float roof=opening.outline.Max(v=>v.y);float x0=Mathf.Min(mouth,mouth+dir*(depth+.8f))-.6f,x1=Mathf.Max(mouth,mouth+dir*(depth+.8f))+.6f;var regionRect=Rect.MinMaxRect(x0,roof+.01f,x1,roof+10);
            var sources=terrain.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()!=null&&f.GetComponent<MeshRenderer>().enabled&&!f.name.StartsWith("Fill")&&!f.transform.IsChildOf(parent)).ToArray();int count=0;
            foreach(var f in sources){var r=f.GetComponent<MeshRenderer>();if(r.bounds.max.x<x0||r.bounds.min.x>x1||r.bounds.max.y<roof||r.bounds.min.y>roof+10)continue;var m=Clip(f,regionRect,true);if(m.vertexCount==0){UnityEngine.Object.DestroyImmediate(m);continue;}string safe="Native roof "+count;var go=new GameObject(safe);go.transform.SetParent(parent,true);go.AddComponent<MeshFilter>().sharedMesh=Save(Root+"Upper_"+id+"/"+safe+".asset",m);var rr=go.AddComponent<MeshRenderer>();var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);string matPath=Root+"Upper_"+id+"/"+safe+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(r.sharedMaterial){hideFlags=HideFlags.None};AssetDatabase.CreateAsset(material,matPath);}else material.CopyPropertiesFromMaterial(r.sharedMaterial);var texture=block.GetTexture(Shader.PropertyToID("_MainTex"));if(texture!=null)material.mainTexture=texture;EditorUtility.SetDirty(material);rr.sharedMaterial=material;rr.sortingOrder=146;count++;}
            // Old riser fringe can hang across the new mouth even though its collision was carved.
            var cut=Rect.MinMaxRect(mouth-.6f,floor-.05f,mouth+.3f,floor+2.72f);if(dir<0)cut=Rect.MinMaxRect(mouth-.3f,floor-.05f,mouth+.6f,floor+2.72f);
            int trimmed=0;foreach(var f in sources.Where(f=>f.name.Contains("Riser")||f.name.Contains("wall")||f.name.Contains("outer")||f.name.Contains("inner"))){var r=f.GetComponent<Renderer>();if(r.bounds.max.x<cut.xMin||r.bounds.min.x>cut.xMax||r.bounds.max.y<cut.yMin||r.bounds.min.y>cut.yMax)continue;var m=Clip(f,cut,false);var vv=m.vertices;for(int i=0;i<vv.Length;i++)vv[i]=f.transform.InverseTransformPoint(vv[i]);m.vertices=vv;m.RecalculateBounds();f.sharedMesh=Save(Root+"Upper_"+id+"/Trimmed native fringe "+trimmed+".asset",m);EditorUtility.SetDirty(f);trimmed++;}
            var outline=new[]{new Vector2(cut.xMin,cut.yMin),new Vector2(cut.xMax,cut.yMin),new Vector2(cut.xMax,cut.yMax),new Vector2(cut.xMin,cut.yMax)}.Select(v=>(Vector2)terrain.transform.InverseTransformPoint(v)).ToArray();
            terrain.caveFringeTrims=(terrain.caveFringeTrims??Array.Empty<MraTerrain.CaveOpening>()).Where(c=>c.roomId!=id).Concat(new[]{new MraTerrain.CaveOpening{roomId=id,outline=outline}}).ToArray();EditorUtility.SetDirty(terrain);
            foreach(var lip in p.GetComponentsInChildren<MraTerrain>(true).Where(t=>t.name=="Limestone floor lip")){lip.replacedPaintedParts=(lip.replacedPaintedParts??Array.Empty<string>()).Concat(new[]{"Top"}).Distinct().ToArray();lip.Rebuild();EditorUtility.SetDirty(lip);}
            records.Add(new{id,nativeRoofLayers=count,trimmedFringes=trimmed});
        }
        foreach(var t in UnityEngine.Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).Where(t=>t.caveFringeTrims!=null&&t.caveFringeTrims.Length>0))t.Rebuild();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();System.IO.File.WriteAllText(Output+"roof_joins_"+region+".json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return records;
    }
}


