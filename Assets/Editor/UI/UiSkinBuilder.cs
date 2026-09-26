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
        skin.weaponIds = new[] { "forest-0", "reedblade", "forest-1", "forest-2", "forest-3", "resin-sling" };
        skin.weaponIcons = new[] { Icon("LeafSword"), Icon("LeafSword"), Icon("WhipHandle"), Icon("SeedpodMace"), Icon("ThornSpear"), Icon("Sling") };
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
        Debug.Log("[UiSkinBuilder] Built " + SkinPath);
    }
}
