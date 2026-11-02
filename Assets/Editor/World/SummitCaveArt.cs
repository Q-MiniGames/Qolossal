using System;
using UnityEngine;
using UnityEditor;
public static class SummitCaveArt
{
    public const string Source="Assets/Art/Prototypes/SummitCaves_20261028/Summit_Recess.png";
    public static void ConfigureInner(Material m,bool lower){
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Source);if(tex==null)throw new Exception(Source);
        m.mainTexture=tex;
        // Keep the reflected light subdued; do not compensate the warm native
        // vertex palette with a blue gain, which makes the recess appear emissive.
        m.SetColor("_WallTint",lower?new Color(.65f,.72f,.85f):new Color(.72f,.78f,.88f));EditorUtility.SetDirty(m);
    }
    public static void ConfigureRim(Material m,bool lower){m.SetColor(lower?"_DepthBoost":"_RimTint",lower?new Color(1.40f,1.32f,1.18f):new Color(1.45f,1.38f,1.20f));EditorUtility.SetDirty(m);}
    public static object Apply(){
        int count=0;var inventory=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CaveRollout_20261027/inventory_after.json"));
        foreach(var p in inventory[6]["points"])foreach(bool lower in new[]{false,true}){
            string folder="Assets/Development/Layouts/CaveRollout_20261027/"+(lower?"Lower_":"Upper_")+(string)p["lowerRoomId"];
            ConfigureInner(AssetDatabase.LoadAssetAtPath<Material>(folder+"/Inner wall.mat"),lower);
            ConfigureRim(AssetDatabase.LoadAssetAtPath<Material>(folder+(lower?"/Registered regional rim.mat":"/Native outer rock.mat")),lower);count++;
        }
        AssetDatabase.SaveAssets();return new{endpoints=count,source=Source};
    }
}
