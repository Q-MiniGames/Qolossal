using System.IO;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEngine;

// The prototype's review-only images: the eight regional relief paintings (Resources/MRA/Maps) and
// the post-reveal final assembly concept (ReviewArt). They are design-review sources, not accepted
// art: imported here as sprites at their native size (1672x941 and 1920x1080, under the 2048 cap,
// so never downscaled), uncompressed for review fidelity, and referenced by the Chart's art asset
// together with the accepted map icons and frame.
public static partial class MraWorldBuilder
{
    public const string FinalAssemblyFile = "Batch11_A_Chart_Final_7Patches_DESIGNER_SPOILERS_CONCEPT_1920x1080.png";
    const string MapsFolder = Folder + "Resources/MRA/Maps/";

    static void ImportReviewArt()
    {
        foreach (string path in Directory.GetFiles(MapsFolder, "*.png").Select(p => p.Replace('\\', '/')).Append(Folder + "ReviewArt/" + FinalAssemblyFile))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { AssetDatabase.ImportAsset(path); importer = (TextureImporter)AssetImporter.GetAtPath(path); }
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single && importer.maxTextureSize == 2048
                && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled) continue;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    static void BuildChartArt()
    {
        const string path = Folder + "Resources/MRA/MraChartArt.asset";
        var art = AssetDatabase.LoadAssetAtPath<MraChartArt>(path);
        if (art == null) { art = ScriptableObject.CreateInstance<MraChartArt>(); AssetDatabase.CreateAsset(art, path); }
        art.maps = world.regions.OrderBy(r => r.ordinal).Select(r => AssetDatabase.LoadAssetAtPath<Sprite>(MapsFolder + r.map + ".png")).ToArray();
        art.frame = ArtOrNull("Chart/Chart_Frame_9Slice");
        art.qori = ArtOrNull("Chart/Map_Icon_Qori") ?? ArtOrNull("UI/Map_Icon_Qori");
        art.waymark = ArtOrNull("Chart/Map_Icon_Waymark");
        art.shrine = ArtOrNull("Chart/Map_Icon_AbilityShrine") ?? ArtOrNull("UI/Map_Icon_AbilityShrine");
        art.cave = ArtOrNull("Chart/Map_Icon_Secret") ?? ArtOrNull("UI/Map_Icon_Secret");
        art.unexplored = ArtOrNull("Chart/Map_Icon_Unexplored") ?? ArtOrNull("UI/Map_Icon_Unexplored");
        art.npc = ArtOrNull("Chart/Map_Icon_NPC");
        art.finalAssembly = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "ReviewArt/" + FinalAssemblyFile);
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
    }
}
