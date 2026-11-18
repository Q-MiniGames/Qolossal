// STAGED ONLY. Import both staged shaders before applying in saved upper scenes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class WorldRevisionPaintedCaveSeams
{
    const string Output="Tools/Design/WorldRevision_20261104/Surface";
    const string Generated="Assets/Development/Layouts/WorldRevision_20261104/PaintedCaveSeams/";
    static IEnumerable<T> All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static T Save<T>(string path,T value)where T:Object {var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(value,path);return value;}EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;}
    static float FloorAt(List<Vector2> samples,float x)
    {
        if(x<=samples[0].x)return samples[0].y;if(x>=samples.Last().x)return samples.Last().y;
        for(int i=0;i<samples.Count-1;i++)if(x>=samples[i].x&&x<=samples[i+1].x)return Mathf.Lerp(samples[i].y,samples[i+1].y,Mathf.InverseLerp(samples[i].x,samples[i+1].x,x));
        throw new Exception("Floor interpolation failed");
    }
    public static string ApplyCurrent()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new Exception("Saved edit-mode scene required");
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var data=world["regions"].SingleOrDefault(r=>(string)r["scene"]==scene.name);if(data==null)throw new Exception("Upper scene required");string region=(string)data["id"];if(region=="MR01")return "Accepted Cradle assembly retained unchanged";
        var shader=Shader.Find("Qolossal/WorldRevision/Approved Inner Wall Floor Clip");if(shader==null)throw new Exception("Import staged ApprovedInnerFloorClip.shader first");
        string before=string.Join("\n",All<Collider2D>(scene).Select(c=>c.GetInstanceID()+EditorJsonUtility.ToJson(c)));
        Directory.CreateDirectory(Output+"/Before");string backup=Output+"/Before/"+scene.name+"_before_painted_seams.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);
        Directory.CreateDirectory(Generated+region);AssetDatabase.Refresh();var records=new List<object>();
        foreach(var point in All<CaveConnectionPoint>(scene).Where(p=>p.isActiveAndEnabled&&!p.undergroundSide))
        {
            var group=point.transform.Find("Regional fitted passage");if(group==null)continue;var inner=group.Find("Continuous themed inner wall")?.GetComponent<MeshFilter>();var walking=group.Find("Native walking surface")?.GetComponent<MeshFilter>();var rim=group.Find("Registered native outer rock")?.GetComponent<MeshRenderer>();
            if(inner==null||walking==null||rim==null)throw new Exception("Incomplete fitted assembly "+point.lowerRoomId);
            string folder=Generated+region+"/"+point.lowerRoomId+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var floorMesh=walking.sharedMesh;var floorVertices=floorMesh.vertices;var floorUV=floorMesh.uv;if(floorUV.Length!=floorVertices.Length)throw new Exception("Walking registration missing");
            var top=AssetDatabase.LoadAssetAtPath<Texture2D>(WorldRefreshSurface.Art+region+"/Top.png");var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(top));importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            float walkRow=region=="MR04"?333:region=="MR05"?327:region=="MR06"?324:region=="MR07"?321:309,vWalk=1-walkRow/height;
            var points=new List<Vector2>();for(int i=0;i<floorVertices.Length;i++){var p=walking.transform.TransformPoint(floorVertices[i]);points.Add(new Vector2(p.x,p.y+(vWalk-floorUV[i].y)*height/360f));}
            var samples=points.GroupBy(p=>Mathf.Round(p.x*10000)).Select(g=>new Vector2(g.Average(p=>p.x),g.Average(p=>p.y))).OrderBy(p=>p.x).ToList();
            var original=inner.sharedMesh;var copy=Object.Instantiate(original);copy.name=original.name+" exact floor clip";copy.hideFlags=HideFlags.None;var v=copy.vertices;var boundary=new List<Vector4>();copy.GetUVs(2,boundary);if(boundary.Count!=v.Length)throw new Exception("Approved terrain clipping channel missing");
            for(int i=0;i<v.Length;i++){var p=inner.transform.TransformPoint(v[i]);var value=boundary[i];value.y=p.y-FloorAt(samples,p.x);boundary[i]=value;}copy.SetUVs(2,boundary);
            if(!copy.vertices.SequenceEqual(original.vertices)||!copy.uv.SequenceEqual(original.uv))throw new Exception("Painted positions or RGB mapping changed");inner.sharedMesh=Save(folder+"Inner floor clip.asset",copy);
            var renderer=inner.GetComponent<MeshRenderer>();var material=new Material(renderer.sharedMaterial){name=renderer.sharedMaterial.name+" exact floor clip",shader=shader};renderer.sharedMaterial=Save(folder+"Inner floor clip.mat",material);EditorUtility.SetDirty(inner);EditorUtility.SetDirty(renderer);
            if(region=="MR07")
            {
                if(!rim.sharedMaterial.HasProperty("_OpaqueRoofStart"))throw new Exception("Import updated staged ApprovedSurfaceRim.shader before Summit");
                var rimMat=new Material(rim.sharedMaterial){name=rim.sharedMaterial.name+" approved alpha closure"};rimMat.SetFloat("_OpaqueRoofStart",1.01f);rim.sharedMaterial=Save(folder+"Summit approved alpha.mat",rimMat);EditorUtility.SetDirty(rim);
            }
            records.Add(new{id=point.lowerRoomId,belowFloorPaintingClipped=true,sourceVerticesAndUVPreserved=true,floorSamples=samples.Select(p=>new[]{p.x,p.y}).ToArray(),summitOpaqueRoofOverrideDisabled=region=="MR07",approvedRGBAndAlphaAssetsPreserved=true});
        }
        if(before!=string.Join("\n",All<Collider2D>(scene).Select(c=>c.GetInstanceID()+EditorJsonUtility.ToJson(c))))throw new Exception("Visual seam pass changed a collider");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string report=Output+"/"+region+"_painted_seams.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,records,collisionPreserved=true,note="Floor tails were the approved inner mesh's .10u underfloor overlap, exposed by optical strip joins. Summit pale ceiling band was a hardcoded 74%-height opaque Fill override masking a hole in its distinct approved rim alpha."},Formatting.Indented));return report;
    }
}
