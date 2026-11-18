using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Qori's frames in one compressed sprite atlas (polish WS8, 3 Nov). As separate textures they import as raw RGBA32, about
// 1.4 GiB in memory; an atlas compresses whatever size each frame is, and leaves the source PNGs as they are.
// Batch: -executeMethod QoriFrameAtlasBuilder.Build -captureDir <dir> -quit
// Also drops the clips Qori no longer plays from the prefab (the slash, the strike and the Codex walk).
public static class QoriFrameAtlasBuilder
{
    const string Prefab = "Assets/Resources/LudoFrames/LudoQoriFrames.prefab";
    const string AtlasPath = "Assets/Characters/FrameAnimation/QoriFrames.spriteatlasv2";
    const string CreatureFolder = "Assets/Characters/FrameAnimation/Atlases/";

    public static void Build()
    {
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        var walk = root.GetComponentInChildren<QoriWalkFramesOnPlayer>(true);
        foreach (var unused in new[] { walk.slash, walk.strike, walk.codexWalk }) unused.frames = new Sprite[0];
        PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        var sprites = walk.PlayedClips.SelectMany(c => c.frames).Where(s => s != null).Distinct().ToArray();
        PrefabUtility.UnloadPrefabContents(root);
        Pack(new Dictionary<string, Sprite[]> { { AtlasPath, sprites } }, "qori_atlas.txt");
    }

    // The creatures too: one atlas each, so a creature's pages hold only its own frames.
    // Batch: -executeMethod QoriFrameAtlasBuilder.BuildCreatures -captureDir <dir> -quit
    public static void BuildCreatures()
    {
        var groups = new Dictionary<string, Sprite[]>();
        var library = AssetDatabase.LoadAssetAtPath<UndergroundCreatureLibrary>("Assets/Resources/LudoFrames/UndergroundCreatureFrames.asset");
        foreach (var e in library.creatures)
            groups[CreatureFolder + "UG_" + e.id + ".spriteatlasv2"] = e.sets.SelectMany(x => x.frames).Where(x => x != null).Distinct().ToArray();
        var surface = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/LudoFrames/LudoEnemyFrames.prefab");
        foreach (var kind in EditorUtility.CollectDependencies(new[] { surface }).OfType<Sprite>().Distinct().GroupBy(x => Kind(AssetDatabase.GetAssetPath(x))))
            groups[CreatureFolder + kind.Key + ".spriteatlasv2"] = kind.ToArray();
        Directory.CreateDirectory(CreatureFolder);
        Pack(groups, "creature_atlases.txt");
    }

    // ArtEnemies/Grub/Walk/x.png: Grub. ArtCrawler/Walk/x.png: ArtCrawler.
    static string Kind(string path)
    {
        var parts = path.Split('/');
        int at = System.Array.IndexOf(parts, "FrameAnimation");
        return parts[at + 1] == "ArtEnemies" ? parts[at + 2] : parts[at + 1];
    }

    static void Pack(Dictionary<string, Sprite[]> groups, string report)
    {
        // Trimmed meshes let an atlas pack close; pivots and rects don't change, so placement doesn't either.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var s in groups.Values.SelectMany(x => x))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(s));
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType == SpriteMeshType.Tight) continue;
                settings.spriteMeshType = SpriteMeshType.Tight;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }

        var atlases = new List<SpriteAtlas>();
        foreach (var g in groups)
        {
            var asset = new SpriteAtlasAsset();
            asset.Add(g.Value.Cast<Object>().ToArray());
            SpriteAtlasAsset.Save(asset, g.Key);
            AssetDatabase.ImportAsset(g.Key);
            var atlasImporter = (SpriteAtlasImporter)AssetImporter.GetAtPath(g.Key);
            atlasImporter.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = true, enableAlphaDilation = true, padding = 4 };
            atlasImporter.textureSettings = new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear };
            atlasImporter.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.BC7,
                textureCompression = TextureImporterCompression.CompressedHQ, compressionQuality = 50,
            });
            atlasImporter.SaveAndReimport();
            atlases.Add(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(g.Key));
        }
        SpriteAtlasUtility.PackAtlases(atlases.ToArray(), BuildTarget.StandaloneWindows64);
        AssetDatabase.SaveAssets();

        var lines = new List<string>();
        long total = 0;
        foreach (var atlas in atlases)
        {
            var packed = new Sprite[atlas.spriteCount]; atlas.GetSprites(packed);
            var pages = packed.Select(x => x.texture).Distinct().ToArray();
            long bytes = pages.Sum(p => (long)p.width * p.height);   // BC7: one byte a pixel
            total += bytes;
            lines.Add($"{atlas.name}: {atlas.spriteCount} frames, {pages.Length} pages, {bytes / 1048576} MiB");
        }
        lines.Add($"All: {total / 1048576} MiB");
        foreach (var l in lines) Debug.Log("[QoriAtlas] " + l);
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        if (at >= 0) File.WriteAllLines(Path.Combine(args[at + 1], report), lines);
    }
}
