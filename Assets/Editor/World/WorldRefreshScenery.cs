using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class WorldRefreshScenery
{
    const string Art="Assets/Art/Prototypes/WorldRefresh_20261104/";
    const string Marker="World refresh | regional scenery 20261104";
    public static Texture2D Import(string path,bool alpha=true)
    {
        if(!File.Exists(path))throw new FileNotFoundException(path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=TextureImporterType.Default;i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=4096;i.mipmapEnabled=true;
        i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=alpha;i.sRGBTexture=true;
        i.wrapModeU=TextureWrapMode.Mirror;i.wrapModeV=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.isReadable=false;i.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static ParallaxLayer Plane(Transform parent,string name,Texture2D texture,Camera camera,float width,float bottom,float depth,int order,Vector2 climb)
    {
        var p=new GameObject(name).AddComponent<ParallaxLayer>();p.enabled=false;p.transform.SetParent(parent,false);
        p.texture=texture;p.texturePixelsPerUnit=texture.width/width;p.height=width*texture.height/texture.width;
        p.targetCamera=camera;p.depth=depth;p.follow=Vector2.one;p.baseY=bottom;p.composedForSize=5;p.repeat=true;p.extendBelow=0;p.sortingOrder=order;
        p.climbProgression=depth>0;p.climbRange=climb;p.climbBudget=0;p.verticalRate=depth*.004f;p.sinkLimit=1;
        p.enabled=true;p.Refresh(camera);return p;
    }
    public static string ApplyCurrent()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new Exception("Save existing edits before scenery integration");
        string region=scene.name.Substring(8,4);if(!RegionalSceneCatalog.Surface.Contains(scene.name))throw new Exception("Surface scene required");
        if(scene.GetRootGameObjects().Any(g=>g.name==Marker))return region+" already installed";
        var camera=Camera.main;var w=MraWorldBuilder.Prepare().regions.First(r=>r.id.StartsWith(region));
        var climb=new Vector2(w.nodes.Min(n=>n.position.y),w.nodes.Max(n=>n.position.y));
        var root=new GameObject(Marker);
        if(region=="MR06")
        {
            var trial=Object.FindFirstObjectByType<HeightsParallaxTrial>();
            var old=trial.optionARoot.transform.Find("A | warm sky");if(old!=null)old.gameObject.SetActive(false);
            var sky=new GameObject("Sky gradient").AddComponent<WorldRefreshSky>();sky.transform.SetParent(root.transform,false);sky.targetCamera=camera;
            var cirrus=Plane(root.transform,"Native sunset cirrus",Import(Art+"Sky/SunsetCirrus.png"),camera,10,1.4f,125,-115,climb);cirrus.baseX=1.2f;cirrus.verticalRate=0;cirrus.climbShift=0;cirrus.tint=new Color(1,1,1,.72f);
            sky.Refresh();
        }
        else
        {
            foreach(var p in Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(p=>p.gameObject.scene==scene&&p.sortingOrder<=-50).ToArray())p.gameObject.SetActive(false);
            if(region=="MR07"){
                var s=new GameObject("Sky gradient").AddComponent<WorldRefreshSky>();s.transform.SetParent(root.transform,false);s.targetCamera=camera;s.zenith=new Color(.44f,.52f,.64f);s.middle=new Color(.72f,.78f,.83f);s.horizon=new Color(.92f,.86f,.76f);s.Refresh();
            }else{
                var stack=MraRegion4K.Stacks[region];var t=Import(stack.folder+stack.sky+".png",false);
                var sky=Plane(root.transform,"Native 4K sky",t,camera,t.width/216f,-5,0,-120,climb);sky.baseX=0;
            }
            var far=Plane(root.transform,"Far | regional vista",Import(Art+region+"/Far.png"),camera,10,-2.5f,125,-108,climb);far.baseX=-1.8f;
            var middle=Import(Art+region+"/Mid.png");
            var mid=Plane(root.transform,"Mid | landscape",middle,camera,10,-3.7f,1/.024f,-105,climb);mid.baseX=.8f;mid.verticalRate=mid.depth*.007f;mid.sinkLimit=1.52f;
            var near=Plane(root.transform,"Near | lower foothills",middle,camera,10,-5.3f,20,-102,climb);near.baseX=2.3f;near.verticalRate=.2f;near.sinkLimit=2.16f;near.tint=new Color(.88f,.9f,.88f,1);
            var weather=root.AddComponent<WorldRefreshWeather>();weather.targetCamera=camera;weather.clouds=new ParallaxLayer[4];weather.authoredX=new[]{-22f,-2f,18f,38f};
            for(int i=0;i<4;i++){
                var cloud=Import("Assets/Art/Prototypes/HeightsCloudsNeutral_20261103/Cloud_"+i+".png");
                var p=Plane(root.transform,"Weather | distant cloud "+i,cloud,camera,7.6f,1.6f+(i%2)*1.5f,6.25f,-101,climb);p.repeat=false;p.climbProgression=true;p.verticalRate=.01f;p.sinkLimit=.6f;p.follow=new Vector2(.84f,.84f);p.tint=new Color(1,1,1,region=="MR05"?.32f:.65f);weather.clouds[i]=p;
            }
            weather.Refresh(0);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return region+" scenery saved";
    }
}
