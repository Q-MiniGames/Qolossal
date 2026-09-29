using System.IO;
using UnityEditor;
using UnityEngine;

// Creates Assets/Resources/UI/UiSkin.asset from the accepted Codex UI art.
// Menu: Qolossal > UI > Build UI Skin
public static class UiSkinBuilder
{
    const string SkinPath = "Assets/Resources/UI/UiSkin.asset";

    [MenuItem("Qolossal/UI/Build UI Skin")]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));
        var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
        if (skin == null) { skin = ScriptableObject.CreateInstance<UiSkin>(); AssetDatabase.CreateAsset(skin, SkinPath); }
        Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/UI/" + name + ".png")
            ?? throw new FileNotFoundException("Missing UI sprite; import accepted Codex art first.", name);
        skin.hudFrame = Load("HUD_Frame");
        skin.leafFull = Load("HUD_Health_Leaf_Full");
        skin.leafHalf = Load("HUD_Health_Leaf_Half");
        skin.leafEmpty = Load("HUD_Health_Leaf_Empty");
        skin.panel = Load("UI_Panel_9Slice");
        skin.buttonNormal = Load("UI_Button_Normal");
        skin.buttonHover = Load("UI_Button_Hover");
        skin.buttonPressed = Load("UI_Button_Pressed");
        skin.ring = Load("HUD_Ring");
        skin.ringBacking = Load("HUD_Ring_Backing");
        skin.vine = Load("HUD_Vine_Segment");
        Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/Weapons/Icon_Weapon_" + name + ".png")
            ?? throw new FileNotFoundException("Missing weapon icon; import accepted Codex art first.", name);
        skin.weaponIds = new[] { "forest-0", "reedblade", "forest-2", "forest-3", "resin-sling" };
        skin.weaponIcons = new[] { Icon("LeafSword"), Icon("LeafSword"), Icon("SeedpodMace"), Icon("ThornSpear"), Icon("Sling") };
        Sprite Chart(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/Chart/" + name + ".png")
            ?? throw new FileNotFoundException("Missing Chart sprite; import accepted Codex art first.", name);
        skin.chartBase = Chart("Chart_Base"); skin.chartFog = Chart("Chart_Fog");
        skin.chartFrame = Chart("Chart_Frame_9Slice"); skin.chartVeinLine = Chart("Chart_Vein_Line");
        skin.chartRegions = new Sprite[7]; skin.chartStirGhosts = new Sprite[7]; skin.chartStirPoses = new Sprite[6];
        for (int i = 0; i < 7; i++)
        {
            skin.chartRegions[i] = Chart($"Chart_Region_R{i + 1}");
            skin.chartStirGhosts[i] = Chart($"Chart_Stir_Ghost_R{i + 1}");
            if (i < 6) skin.chartStirPoses[i] = Chart($"Chart_Stir_Pose_R{i + 1}");
        }
        skin.mapKnotDormant = Chart("Map_Icon_Knot_Dormant"); skin.mapKnotAwake = Chart("Map_Icon_Knot_Awake");
        skin.mapWaymark = Chart("Map_Icon_Waymark"); skin.mapWildVein = Chart("Map_Icon_WildVein");
        skin.mapQori = Load("Map_Icon_Qori"); skin.mapUnexplored = Load("Map_Icon_Unexplored");
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        Debug.Log("[UiSkinBuilder] Built " + SkinPath);
    }
}
