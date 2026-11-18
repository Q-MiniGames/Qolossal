using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Visual-only end taper on two existing D01 overlapping floor paintings.
public static class WorldRevisionD01FloorJoin
{
    public static object Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scenes before join pass");
        var scene=EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Underground+".unity");
        var point=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CaveConnectionPoint>(true)).Single(p=>p.undergroundSide&&p.lowerRoomId=="D01");
        var group=point.transform.Find("Regional fitted passage");if(group==null)throw new Exception("D01 fitted passage missing");
        float inward=point.entrance.GetComponent<CaveThresholdPresentation>().inward;
        string folder="Assets/Development/Layouts/WorldRevision_20261104/D01FloorJoin/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        var records=new List<object>();
        foreach(string name in new[]{"Native painted walking edge","Registered regional floor"})
        {
            var filter=group.Find(name)?.GetComponent<MeshFilter>();if(filter==null)throw new Exception(name+" missing");
            string source="Assets/Development/Layouts/CaveRollout_20261027/Lower_D01/"+name+".asset";
            var original=AssetDatabase.LoadAssetAtPath<Mesh>(source);if(original==null)throw new Exception("Original D01 mesh missing");
            var copy=UnityEngine.Object.Instantiate(original);copy.hideFlags=HideFlags.None;copy.name=name+" native deep attachment fade";
            var vertices=copy.vertices;var colors=copy.colors;
            if(colors.Length!=vertices.Length)throw new Exception("Expected registered colour alpha");
            var depth=vertices.Select(v=>filter.transform.TransformPoint(v).x*inward).ToArray();float deep=depth.Max();int changed=0;
            for(int i=0;i<colors.Length;i++)
            {
                float factor=Mathf.SmoothStep(0,1,Mathf.Clamp01((deep-depth[i])/.25f));
                if(factor<1&&colors[i].a>0)changed++;var c=colors[i];c.a*=factor;colors[i]=c;
            }
            copy.colors=colors;
            if(!copy.vertices.SequenceEqual(original.vertices)||!copy.uv.SequenceEqual(original.uv)||!copy.triangles.SequenceEqual(original.triangles))throw new Exception("Unexpected geometry or UV edit");
            string target=folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(target);
            if(existing==null){AssetDatabase.CreateAsset(copy,target);existing=copy;}
            else{EditorUtility.CopySerialized(copy,existing);UnityEngine.Object.DestroyImmediate(copy);EditorUtility.SetDirty(existing);}
            filter.sharedMesh=existing;EditorUtility.SetDirty(filter);
            records.Add(new{name,source,target,taperWorldUnits=.25f,inward,deepWorldX=deep/inward,changedVertexAlphas=changed,geometryAndUVPreserved=true});
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var result=new{scene=scene.name,records,collisionUnchanged=true,paintedSourcesUnchanged=true};File.WriteAllText("Tools/Design/WorldRevision_20261104/RegionalEdge/D01_join_applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
