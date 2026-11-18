using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

// Staged helper. Root copies this and the shader into Assets after Play mode.
// Sources, colliders and meshes are never changed. All materials share one
// family scale so the original arc-coordinate registration stays continuous.
public static class WorldRevisionRegionalEdgeDensity
{
    const string Output="Tools/Design/WorldRevision_20261104/RegionalEdge/";
    const string Folder="Assets/Art/Prototypes/WorldRevision_20261104/RegionalEdge";
    const string ShaderName="Qolossal/WorldRevision/Native Regional Painted Edge";
    public static object Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scenes before edge revision");
        var shader=Shader.Find(ShaderName);if(shader==null)throw new Exception("Import staged NativeRegionalPaintedEdge shader first");
        var rows=JObject.Parse(File.ReadAllText("Tools/Design/WorldRevision_20261104/density_instances.json"))["rows"];
        var scales=Enumerable.Repeat(1f,8).ToArray();var minimum=Enumerable.Repeat(double.PositiveInfinity,8).ToArray();
        foreach(var row in rows.Where(r=>(string)r["kind"]=="regional-painted-edge-uv2"))
        {
            int family=int.Parse(((string)row["textureProperty"]).Substring(2));
            // On rerun the recorded density already includes the existing scale.
            double applied=(double?)row["samplingScale"]??1;
            scales[family]=Math.Max(scales[family],(float)applied);
            double density=(double)row["nativeMinimumPixelsPerUnit"]/applied;
            double importRatio=Math.Min((double)row["importedWidth"]/(double)row["sourceWidth"],(double)row["importedHeight"]/(double)row["sourceHeight"]);
            minimum[family]=Math.Min(minimum[family],density*Math.Min(1,importRatio));
        }
        for(int i=0;i<8;i++)
        {
            if(double.IsInfinity(minimum[i])||minimum[i]<=0)throw new Exception("No measurable family "+i);
            scales[i]=(float)Math.Max(scales[i],Math.Ceiling(232/minimum[i]*1000)/1000);
        }
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Underground+".unity");
        var replacements=new Dictionary<Material,Material>();var changed=new List<object>();
        foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)))
        {
            var old=renderer.sharedMaterial;if(old==null||!(old.shader.name=="Qolossal/Review/RegionalPaintedEdge"||old.shader.name==ShaderName))continue;
            if(!replacements.TryGetValue(old,out var replacement))
            {
                string oldPath=AssetDatabase.GetAssetPath(old);
                string path=old.shader.name==ShaderName?oldPath:Folder+"/NativeEdge_"+AssetDatabase.AssetPathToGUID(oldPath)+".mat";
                replacement=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(replacement==null){replacement=new Material(old);AssetDatabase.CreateAsset(replacement,path);}
                else replacement.CopyPropertiesFromMaterial(old);
                replacement.shader=shader;
                replacement.SetVector("_NativeEdgeScaleA",new Vector4(scales[0],scales[1],scales[2],scales[3]));
                replacement.SetVector("_NativeEdgeScaleB",new Vector4(scales[4],scales[5],scales[6],scales[7]));
                EditorUtility.SetDirty(replacement);replacements.Add(old,replacement);
                changed.Add(new{before=oldPath,after=path,sourceTexturesUnchanged=true,capMappingUnchanged=true});
            }
            renderer.sharedMaterial=replacement;EditorUtility.SetDirty(renderer);
        }
        if(replacements.Count==0)throw new Exception("No regional edge materials found");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var result=new{targetPixelsPerUnit=232,minimumBefore=minimum,scales,materials=changed,geometryUnchanged=true,nativeSourcesUnchanged=true,shader=ShaderName};
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
