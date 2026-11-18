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
public static class WorldRevisionTerracesDecor
{
    static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
    public static string ApplyCurrent()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.name!="MRAtlas_MR03_Terraces")throw new Exception("Open saved Terraces scene in edit mode");
        const string output="Tools/Design/WorldRevision_20261104/Surface";var paths=new[]{"Assets/Art/Codex/Decor/Decor_A5_Sheet.png","Assets/Art/Codex/Decor/Decor_Terraces_Sheet.png"};
        var layers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ParallaxLayer>(true)).Where(l=>l.isActiveAndEnabled&&l.name.StartsWith("Foreground - Decor_",StringComparison.Ordinal)&&l.texture!=null&&paths.Contains(AssetDatabase.GetAssetPath(l.texture))).ToArray();
        if(layers.Length!=13)throw new Exception("Expected exactly13 audited foreground quads; got "+layers.Length);
        var hashes=paths.ToDictionary(p=>p,Hash);Directory.CreateDirectory(output+"/Before");string backup=output+"/Before/"+scene.name+"_before_decor_size.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);var records=new List<object>();
        foreach(var layer in layers)
        {
            if(layer.sprite!=null||layer.repeat||Mathf.Abs(layer.depth-.75f)>.0001f||Mathf.Abs(layer.composedForSize-5)>.0001f)throw new Exception("Audited parallax source/zoom assumptions changed: "+layer.name);
            var camera=layer.targetCamera!=null?layer.targetCamera:Camera.main;if(camera==null)throw new Exception("Camera required for actual quad anchor validation");
            layer.Refresh(camera);float oldBottom=layer.transform.position.y,oldBaseY=layer.baseY,oldBaseX=layer.baseX,oldHeight=layer.height;
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(layer.texture));importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            double cropWidth=width*(double)layer.uvRect.width,cropHeight=height*(double)layer.uvRect.height;
            // Texture-backed layer: Height is the displayed crop height. Width
            // is Height * textureAspect * uvRect.width/uvRect.height, exactly
            // matching ParallaxLayer.Width. At size5/depth.75/composed5, zoom=1.
            float target=(float)(cropHeight/216.0);layer.height=Mathf.Min(oldHeight,target);layer.Refresh(camera);
            if(Mathf.Abs(layer.transform.position.y-oldBottom)>.00001f||layer.baseY!=oldBaseY||layer.baseX!=oldBaseX)throw new Exception("Actual quad lower edge changed: "+layer.name);
            double actualHeight=layer.height,actualWidth=actualHeight*width/height*layer.uvRect.width/layer.uvRect.height;
            double ppu=Math.Min(cropWidth/actualWidth,cropHeight/actualHeight);if(ppu<215.999)throw new Exception("Native216PPU requirement not reached: "+layer.name);
            EditorUtility.SetDirty(layer);records.Add(new{name=layer.name,source=AssetDatabase.GetAssetPath(layer.texture),cropPixels=new[]{cropWidth,cropHeight},oldHeight,newHeight=layer.height,newWidth=actualWidth,nativePPU=ppu,baseX=layer.baseX,baseY=layer.baseY,actualQuadBottomBefore=oldBottom,actualQuadBottomAfter=layer.transform.position.y,sourceCropAndAspectPreserved=true});
        }
        foreach(var pair in hashes)if(Hash(pair.Key)!=pair.Value)throw new Exception("Accepted source bytes changed");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string report=output+"/MR03_decor_native_sizes.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,records,sourceHashes=hashes,changes="Only thirteen ParallaxLayer.height values shrink; baseY, baseX, source UV crops, depth, tint, and all gameplay geometry remain unchanged. Small legacy foreground quads may now sit below frame; native terrain grass remains visible."},Formatting.Indented));return report;
    }
}
