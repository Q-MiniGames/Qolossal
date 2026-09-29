using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Creates Assets/Resources/FX/FxLibrary.asset from the accepted Codex effect art.
// Menu: Qolossal > FX > Build FX Library
public static class FxLibraryBuilder
{
    const string LibraryPath = "Assets/Resources/FX/FxLibrary.asset";

    [MenuItem("Qolossal/FX/Build FX Library")]
    public static void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
        var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null) { lib = ScriptableObject.CreateInstance<FxLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
        Sprite One(string folder, string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Codex/{folder}/{name}.png")
            ?? throw new FileNotFoundException("Missing FX sprite; import accepted Codex art first.", name);
        Sprite[] Many(string folder, string prefix) => AssetDatabase.FindAssets(prefix, new[] { "Assets/Art/Codex/" + folder })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileNameWithoutExtension(p).StartsWith(prefix))
            .OrderBy(p => p).Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray();
        lib.dustLand = Many("Effects", "FX_Dust_Land_");
        lib.dustRun = Many("Effects", "FX_Dust_Run_");
        lib.leaves = Many("Effects", "FX_Leaf_Single_");
        lib.portalSwirl = Many("Effects", "FX_Portal_Swirl_");
        lib.deathPuff = Many("Enemies", "FX_EnemyDeath_Puff_");
        lib.hitSparks = Many("Weapons", "FX_Hit_Spark_");
        lib.wallScrape = One("Effects", "FX_WallSlide_Scrape");
        lib.checkpointMote = One("Effects", "FX_Checkpoint_Motes");
        lib.hitBlock = One("Weapons", "FX_Hit_Block");
        lib.telegraphGlint = One("Enemies", "FX_Telegraph_Glint");
        lib.sapOrb = One("Enemies", "Drop_SapOrb");
        lib.dashBurst = Many("Effects", "FX_Dash_Burst_");
        lib.dashTrail = One("Effects", "FX_Dash_Trail");
        lib.glidecapHeld = One("Weapons", "Glidecap_Held");
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FxLibraryBuilder] built: dust {lib.dustLand.Length}+{lib.dustRun.Length}, leaves {lib.leaves.Length}, swirl {lib.portalSwirl.Length}, puff {lib.deathPuff.Length}, sparks {lib.hitSparks.Length}, dash {lib.dashBurst.Length}");
    }
}
