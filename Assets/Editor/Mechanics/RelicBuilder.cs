using System.IO;
using UnityEditor;
using UnityEngine;

// Creates the relic abilities in Assets/Resources/Relics from the accepted Codex relic art and
// HUD icons. Menu: Qolossal > Relics > Build Relics
public static class RelicBuilder
{
    const string Folder = "Assets/Resources/Relics/";

    [MenuItem("Qolossal/Relics/Build Relics")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        Make<RelicAbilityDefinition>("LivingThread", Relics.LivingThread, "Living Thread", "Thread",
            "Right-click near a glowing seed to cast the thread and swing. Hold W to climb it, S to let it out.");
        Make<RelicAbilityDefinition>("ClimbingMoss", Relics.ClimbingMoss, "Climbing Moss", "WallJump",
            "Cling to walls to slide down them slowly, and press Jump to leap off.");
        var bloom = Make<PogoAbilityDefinition>("Bloomfall", Relics.Bloomfall, "Bloomfall", "Pogo",
            "In mid-air, hold S and attack to strike downward and bounce off whatever you hit.");
        bloom.bounceSpeed = 10f;
        EditorUtility.SetDirty(bloom);
        // The titan's own gifts, from the Breath Knot (R3) and the Bloom Knot (R5).
        Make<RelicAbilityDefinition>("WindLeaf", Relics.WindLeaf, "Wind Leaf", "Dash",
            "Press C to dash through the air. It renews when you land, cling to a wall or catch a thread.");
        Make<RelicAbilityDefinition>("Glidecap", Relics.Glidecap, "Glidecap", "Glide",
            "Hold Jump while falling to open the Glidecap and drift down slowly.");
        AssetDatabase.SaveAssets();
        Debug.Log("[RelicBuilder] built the five relics in " + Folder);
    }

    static T Make<T>(string name, string id, string display, string icon, string hint) where T : AbilityDefinition
    {
        string path = Folder + name + ".asset";
        var ability = AssetDatabase.LoadAssetAtPath<T>(path);
        if (ability == null)
        {
            AssetDatabase.DeleteAsset(path);   // replaces an asset of another type
            ability = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(ability, path);
        }
        Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/Props/" + file + ".png")
            ?? throw new FileNotFoundException("Missing relic art; import accepted Codex art first.", file);
        ability.abilityId = id; ability.displayName = display; ability.hint = hint;
        ability.relic = Load("Relic_" + name); ability.icon = Load("Icon_Ability_" + icon);
        EditorUtility.SetDirty(ability);
        return ability;
    }
}
