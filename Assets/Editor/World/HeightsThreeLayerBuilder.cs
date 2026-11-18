using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public static class HeightsThreeLayerBuilder
{
    const string Art = "Assets/Art/Prototypes/HeightsThreeLayers_20261104/";
    static string ArtPath(string name) => name == "Far"
        ? "Assets/Art/Prototypes/HeightsRiverDetail_20261104/Far_River.png"
        : Art + name + ".png";
    [MenuItem("Qolossal/Backgrounds/Heights A test/Use three land layers")]
    public static void UseThree() => Switch(true);
    [MenuItem("Qolossal/Backgrounds/Heights A test/Use one valley")]
    public static void UseOne() => Switch(false);
    static void Switch(bool three)
    {
        var t = Object.FindFirstObjectByType<HeightsParallaxTrial>();
        if (t == null || t.threeLayerRoot == null) throw new InvalidOperationException("Open the Heights three-layer trial first.");
        Undo.RecordObject(t,"Compare Heights landscape"); t.SetLandLayers(three);
        if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
    }

    public static string Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Stop Play and preserve unsaved edits before installation.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in new[]{"Far","Mid","Near"})
        {
            var imp=(TextureImporter)AssetImporter.GetAtPath(ArtPath(name));
            imp.textureType=TextureImporterType.Default; imp.sRGBTexture=true;
            imp.alphaSource=TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency=true;
            imp.mipmapEnabled=true; imp.npotScale=TextureImporterNPOTScale.None; imp.maxTextureSize=4096;
            imp.textureCompression=TextureImporterCompression.Uncompressed; imp.isReadable=false;
            imp.wrapModeU=TextureWrapMode.Mirror; imp.wrapModeV=TextureWrapMode.Clamp;
            imp.filterMode=FilterMode.Bilinear; imp.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene(HeightsParallaxTrialBuilder.ScenePath);
        var t=Object.FindFirstObjectByType<HeightsParallaxTrial>();
        if(t == null || t.threeLayerRoot != null) throw new InvalidOperationException("Missing trial or already installed.");
        t.singleValley=t.optionARoot.transform.Find("A | continuous valley").gameObject;
        t.threeLayerRoot=new GameObject("Three land layers | comparison");
        t.threeLayerRoot.transform.SetParent(t.optionARoot.transform,false);
        string[] names={"Far","Mid","Near"};
        float[] depths={125f,1f/.024f,20f}, rates={.004f,.007f,.010f}, bottoms={-2.5f,-3.7f,-5.3f};
        Color[] lower={new Color(.69205f,.59590f,.59929f),new Color(.43459f,.36823f,.21163f),new Color(.41311f,.34189f,.15348f)};
        for(int i=0;i<3;i++)
        {
            var go=new GameObject("Valley | "+names[i]); go.transform.SetParent(t.threeLayerRoot.transform,false);
            var p=go.AddComponent<ParallaxLayer>(); p.enabled=false;
            p.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath(names[i]));
            p.texturePixelsPerUnit=p.texture.width/10f; p.height=p.texture.height/p.texturePixelsPerUnit;
            p.baseY=bottoms[i]; p.baseX=i==0?-1.8f:i==1?.8f:2.3f;
            p.depth=depths[i]; p.composedForSize=5; p.repeat=true;
            p.climbProgression=true; p.climbRange=new Vector2(17,233); p.climbBudget=0;
            p.verticalRate=rates[i]*depths[i]; p.sinkLimit=rates[i]*216f;
            p.extendBelow=0; p.belowColor=lower[i]; p.belowBlend=.55f;
            p.sortingOrder=-108+i*3; p.targetCamera=t.targetCamera; p.tint=Color.white;
            p.enabled=true; p.Refresh(t.targetCamera);
        }
        t.SetLandLayers(true); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Three land layers installed; one-valley option retained.";
    }
}
