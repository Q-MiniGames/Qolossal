using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

// Material-only depth correction. Does not rebuild scenes or alter passage collision.
public static class CaveDepthArt
{
    const string Art="Assets/Art/Prototypes/CaveDepth_20261028/";
    const string Original="Assets/Art/Prototypes/CradleRockwork_20261027/MR01_C01_Rockwork.png";
    const string Return="Assets/Art/Prototypes/CradleUndergroundExit_20261027/MR01_C01_ReturnRim.png";
    public static void Surface(Material material,int region)
    {
        string source=region==4?Art+"Ribwood_DepthRim.png":region==5?Art+"Heights_DepthRim.png":region==6?Return:Original;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if(texture==null)throw new Exception("Missing depth artwork: "+source);
        material.SetTexture("_EdgeMask",texture);
        material.SetVector("_RimUV",region==6?new Vector4(1,1,0,0):new Vector4(-1,.77f,1,.23f));
        material.SetColor("_RimTint",region==3?new Color(.57f,.52f,.44f):region==6?new Color(.72f,.88f,1.10f):Color.white);
        if(region==6)SummitCaveArt.ConfigureRim(material,false);
        EditorUtility.SetDirty(material);
    }
    public static object Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
        var inventory=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CaveRollout_20261027/inventory_after.json"));
        int count=0;
        for(int region=0;region<7;region++)foreach(var point in inventory[region]["points"])
        {
            string id=(string)point["lowerRoomId"];if(id=="MR01_C01" || CaveAccessPolicy.IsRetiredEntrance(id))continue;
            var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Layouts/CaveRollout_20261027/Upper_"+id+"/Native outer rock.mat");
            if(m==null)throw new Exception(id);
            Surface(m,region);count++;
        }
        AssetDatabase.SaveAssets();
        return new{surfaceMaterials=count,lowerMaterials=51,sceneChanges=false};
    }
}
