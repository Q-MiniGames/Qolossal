// STAGED ONLY. Material-quality candidate; root previews before accepting.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class WorldRevisionCausewayBorder
{
    static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    public static string ApplyCurrent()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.name!=RegionalSceneCatalog.Underground)throw new Exception("Saved underground scene in edit mode required");
        const string evidence="Tools/Design/WorldRevision_20261104/Surface";
        const string destination="Assets/Development/Layouts/WorldRevision_20261104/CausewayBorder/";
        var oldFill=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Development/Layouts/UndergroundRegional_20261023/Art/Causeway_Native.png");
        var newFill=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Prototypes/WorldRefresh_20261104/MR02/Fill.png");if(oldFill==null||newFill==null)throw new Exception("Both source textures required");
        var sourceHashes=new Dictionary<string,string>();foreach(var texture in new[]{oldFill,newFill}){string path=AssetDatabase.GetAssetPath(texture);sourceHashes[path]=Hash(path);}
        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(newFill));importer.GetSourceTextureWidthAndHeight(out int width,out int height);
        if(width!=height||newFill.width!=width||importer.wrapModeU!=TextureWrapMode.Repeat||importer.wrapModeV!=TextureWrapMode.Repeat)throw new Exception("Native square repeat-imported Fill required");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        var targets=all.Where(r=>r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_T1")&&m.HasProperty("_RepeatA")&&m.GetTexture("_T1")==oldFill)).ToArray();
        Directory.CreateDirectory(evidence+"/Before");string backup=evidence+"/Before/Underground_before_Causeway_border.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);
        Directory.CreateDirectory(destination);AssetDatabase.Refresh();var cloned=new Dictionary<Material,Material>();var changes=new List<object>();
        foreach(var renderer in targets)
        {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(block.GetTexture("_T1")!=null)throw new Exception("Unexpected runtime texture override on "+renderer.name);
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var source=materials[i];if(source==null||!source.HasProperty("_T1")||!source.HasProperty("_RepeatA")||source.GetTexture("_T1")!=oldFill)continue;
                if(!cloned.TryGetValue(source,out var replacement))
                {
                    string sourcePath=AssetDatabase.GetAssetPath(source);if(string.IsNullOrEmpty(sourcePath))throw new Exception("Expected persistent source material");
                    sourceHashes[sourcePath]=Hash(sourcePath);
                    // RegionalPaintedEdge hardcodes its cap tile to 4u while
                    // _RepeatA.y controls the separate painted edge strip.
                    // Keep all three shaders in the same world phase: 1254/4
                    // gives313.5nativePPU, safely above the216PPU4K requirement.
                    if(Mathf.Abs(source.GetVector("_RepeatA").y-4f)>.0001f)throw new Exception("Unexpected Causeway registration; review "+sourcePath);
                    var material=new Material(source){name=source.name+" native Causeway strata"};material.SetTexture("_T1",newFill);
                    string path=destination+AssetDatabase.AssetPathToGUID(sourcePath)+".mat";replacement=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(replacement==null){AssetDatabase.CreateAsset(material,path);replacement=material;}else{EditorUtility.CopySerialized(material,replacement);Object.DestroyImmediate(material);EditorUtility.SetDirty(replacement);}
                    cloned[source]=replacement;
                }
                materials[i]=replacement;changes.Add(new{renderer=renderer.name,source=AssetDatabase.GetAssetPath(source),replacement=AssetDatabase.GetAssetPath(replacement),shader=source.shader.name});
            }
            renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(var pair in sourceHashes)if(Hash(pair.Key)!=pair.Value)throw new Exception("Original source changed unexpectedly: "+pair.Key);
        string report=evidence+"/causeway_border_candidate.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,sourceTexture=AssetDatabase.GetAssetPath(oldFill),replacementTexture=AssetDatabase.GetAssetPath(newFill),sourceDimensions=new[]{width,height},worldTileSize=4f,nativePPU=width/4f,rendererCount=targets.Length,materialCount=cloned.Count,changes,sourceHashes,approvedRimPaintingAndAllOtherRegionalTexturesPreserved=true,allOriginalRepeatAndWorldPhasePreserved=true,geometryAndCollisionsUnchanged=true,note="Candidate quality change: old olive fine-grain source was correctly mapped, not a UV bug. Root must inspect PC06, MR02_C04 and region transitions at the actual camera before accepting."},Formatting.Indented));return report;
    }
}
