using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorldRevisionRoofClosure
{
    const string Folder="Assets/Art/Prototypes/WorldRevision_20261104/RoofClosure";
    public static object Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save scenes before closure pass");
        var scene=EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Surface[6]+".unity");
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var masks=new Dictionary<Texture2D,Texture2D>();var records=new List<object>();int materials=0;
        foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)))
        {
            var material=renderer.sharedMaterial;
            if(material==null||material.shader.name!="Qolossal/WorldRevision/Approved Rim Native Attachment")continue;
            if(!material.HasProperty("_RoofClosure"))throw new Exception("Import updated closure shader first");
            var source=material.GetTexture("_EdgeMask") as Texture2D;if(source==null)throw new Exception("Missing approved alpha");
            if(!masks.TryGetValue(source,out var mask))
            {
                string sourcePath=AssetDatabase.GetAssetPath(source);byte[] native=File.ReadAllBytes(sourcePath);
                var read=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!ImageConversion.LoadImage(read,native,false))throw new Exception("Read source PNG failed");
                var pixels=read.GetPixels32();var coverage=new Color32[pixels.Length];int filled=0,columns=0;
                // Isolated one-byte alpha flecks exist near the image bottom.
                // Find the largest 8-connected nonzero-alpha component (the
                // actual painted rock), without a guessed height/alpha cutoff.
                var seen=new bool[pixels.Length];var largest=new List<int>();var queue=new Queue<int>();
                for(int seed=0;seed<pixels.Length;seed++)
                {
                    if(seen[seed]||pixels[seed].a==0)continue;
                    var component=new List<int>();seen[seed]=true;queue.Enqueue(seed);
                    while(queue.Count>0)
                    {
                        int k=queue.Dequeue();component.Add(k);int cx=k%read.width,cy=k/read.width;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                        {
                            int nx=cx+dx,ny=cy+dy;if(nx<0||nx>=read.width||ny<0||ny>=read.height)continue;
                            int next=ny*read.width+nx;if(seen[next]||pixels[next].a==0)continue;seen[next]=true;queue.Enqueue(next);
                        }
                    }
                    if(component.Count>largest.Count)largest=component;
                }
                var body=new bool[pixels.Length];foreach(int index in largest)body[index]=true;
                for(int x=0;x<read.width;x++)
                {
                    byte peak=0;for(int y=0;y<read.height;y++)if(body[y*read.width+x])peak=Math.Max(peak,pixels[y*read.width+x].a);
                    // Use this column's own opaque plateau: the approved roof
                    // often peaks at253, so a fixed254 cutoff reaches the floor.
                    // Cover the upper antialias fringe through95% of that peak.
                    int top=-1;for(int y=read.height-1;y>=0;y--)if(body[y*read.width+x]&&pixels[y*read.width+x].a>=peak*.95f){top=y;break;}
                    if(top>=0&&top<read.height/2)throw new Exception("Roof plateau unexpectedly below upper half; manual source review required");
                    if(top>=0)columns++;
                    for(int y=0;y<read.height;y++)
                    {
                        // Strictly above the TOPMOST painted pixel: even a floor
                        // fleck cannot propagate upward through the cave opening.
                        byte value=(byte)(top>=0&&y>=top?255:0);
                        if(value!=0)filled++;
                        coverage[y*read.width+x]=new Color32(value,value,value,255);
                    }
                }
                var encoded=new Texture2D(read.width,read.height,TextureFormat.RGBA32,false,true);encoded.SetPixels32(coverage);encoded.Apply();
                string path=Folder+"/AbovePaint_"+AssetDatabase.AssetPathToGUID(sourcePath)+".png";File.WriteAllBytes(path,encoded.EncodeToPNG());
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                mask=AssetDatabase.LoadAssetAtPath<Texture2D>(path);masks.Add(source,mask);
                records.Add(new{source=sourcePath,coverage=path,width=read.width,height=read.height,filledPixels=filled,columnsWithPaint=columns,connectedRockPixels=largest.Count,originalRGBAndAlphaUnchanged=true,belowUpperRoofPlateauCoverageAlwaysZero=true,upperAntialiasFringeCovered=true,plateauFraction=.95,classification="Derived geometry coverage, not new painted colour or enlarged native detail"});
                UnityEngine.Object.DestroyImmediate(read);UnityEngine.Object.DestroyImmediate(encoded);
            }
            material.SetTexture("_RoofClosure",mask);material.SetFloat("_UseRoofClosure",1);material.SetFloat("_OpaqueRoofStart",1.01f);EditorUtility.SetDirty(material);materials++;
        }
        if(materials!=8)throw new Exception("Expected eight Summit entrance rims, found "+materials);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var result=new{scene=scene.name,materials,records,geometryAndCollisionUnchanged=true};
        File.WriteAllText("Tools/Design/WorldRevision_20261104/Surface/summit_roof_closure.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
