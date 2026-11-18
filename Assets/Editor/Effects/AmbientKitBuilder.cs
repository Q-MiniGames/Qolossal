using UnityEditor;
using UnityEngine;

// Creates Assets/Resources/FX/AmbientKit.asset from the accepted Codex effect art.
public static class AmbientKitBuilder
{
    const string KitPath = "Assets/Resources/FX/AmbientKit.asset";

    [MenuItem("Qolossal/FX/Build Ambient Kit")]
    public static void Build()
    {
        var kit = AssetDatabase.LoadAssetAtPath<AmbientKit>(KitPath);
        if (kit == null) { kit = ScriptableObject.CreateInstance<AmbientKit>(); AssetDatabase.CreateAsset(kit, KitPath); }
        Sprite One(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Codex/{path}.png")
            ?? throw new System.IO.FileNotFoundException("Missing ambient sprite.", path);
        kit.mist = One("Effects/FX_Ambient_Mist");
        kit.firefly = One("Effects/FX_Ambient_Firefly");
        kit.ripple = One("Effects/FX_Water_Ripple");
        kit.gust = One("Enemies/FX_Gust");
        kit.leaf = One("Effects/FX_Leaf_Single_01");
        kit.forgeFire = Frames("ForgeFire", "Forge_Fire_", 120f, false);
        kit.birdPerch = Frames("Bird", "Bird_Perch_", 100f, true);
        kit.birdRise = Frames("Bird", "Bird_Rise_", 100f, true);
        kit.birdFlap = Frames("Bird", "Bird_Flap_", 100f, true);
        var twig = Frames("Bird", "Bird_Twig", 100f, true);
        kit.birdTwig = twig.Length > 0 ? twig[0] : null;
        var butterfly = Frames("Butterfly", "Butterfly", 100f, true);
        kit.butterfly = butterfly.Length > 0 ? butterfly[0] : null;
        kit.splash = Frames("Splash", "Splash_", 100f, true);
        EditorUtility.SetDirty(kit);
        Residents();
        AssetDatabase.SaveAssets();
    }

    // One asset per figure, so a scene loads only the figures it holds. Feet at the sprite's bottom centre.
    static void Residents()
    {
        const string Art = "Assets/Art/Ludo/Residents", Output = "Assets/Resources/LudoFrames/Residents";
        if (!AssetDatabase.IsValidFolder(Art)) return;
        System.IO.Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        foreach (var folder in AssetDatabase.GetSubFolders(Art))
        {
            string rig = System.IO.Path.GetFileName(folder), path = $"{Output}/{rig}.asset";
            var set = AssetDatabase.LoadAssetAtPath<ResidentIdle>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<ResidentIdle>(); AssetDatabase.CreateAsset(set, path); }
            set.idle = Frames("Residents/" + rig, rig + "_Idle_", 216f, false, SpriteAlignment.BottomCenter);
            EditorUtility.SetDirty(set);
        }
    }

    // Ludo frames from Assets/Art/Ludo. The fire is at the smithy painting's own pixels per unit, so it lines up pixel for pixel.
    static Sprite[] Frames(string folder, string prefix, float pixelsPerUnit, bool mipmaps, SpriteAlignment pivot = SpriteAlignment.Center)
    {
        var sprites = new System.Collections.Generic.List<Sprite>();
        string path = "Assets/Art/Ludo/" + folder;
        if (!AssetDatabase.IsValidFolder(path)) return sprites.ToArray();
        foreach (var guid in AssetDatabase.FindAssets(prefix + " t:Texture2D", new[] { path }))
        {
            string file = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileName(file).StartsWith(prefix, System.StringComparison.Ordinal)) continue;
            var import = (TextureImporter)AssetImporter.GetAtPath(file);
            import.textureType = TextureImporterType.Sprite; import.spriteImportMode = SpriteImportMode.Single;
            import.spritePixelsPerUnit = pixelsPerUnit; import.mipmapEnabled = mipmaps; import.alphaIsTransparency = true;
            import.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            import.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)pivot;
            import.SetTextureSettings(settings);
            import.SaveAndReimport();
            sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(file));
        }
        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }
}
