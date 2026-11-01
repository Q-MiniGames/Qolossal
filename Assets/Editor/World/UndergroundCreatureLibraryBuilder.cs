using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Imports the underground creatures' cut Ludo frames (Assets/Art/Underground/Creatures/<ID>/<Segment>/, written by
// Tools/ArtRequests/LudoRun/export_underground.py) as sprites pivoted on the canvas pivot, and writes the library
// UndergroundCreatureFrames reads: Resources/LudoFrames/UndergroundCreatureFrames.asset.
// Menu: Qolossal > Underground > Build Creature Frames. Batch: -executeMethod UndergroundCreatureLibraryBuilder.BuildBatch
public static class UndergroundCreatureLibraryBuilder
{
    const string Root = "Assets/Art/Underground/Creatures";
    const string LibraryPath = "Assets/Resources/LudoFrames/UndergroundCreatureFrames.asset";

    [System.Serializable] sealed class Info { public string id, pivot; public float heightUnits, figureHeightPx; public int[] canvas; public float[] pivotPx; }

    [MenuItem("Qolossal/Underground/Build Creature Frames")]
    public static void BuildMenu() => Build();
    public static void BuildBatch() => Build();

    static void Build()
    {
        var entries = new List<UndergroundCreatureLibrary.Entry>();
        foreach (var dir in Directory.GetDirectories(Root).OrderBy(d => d))
        {
            string id = Path.GetFileName(dir);
            string infoPath = Path.Combine(dir, id + "_info.json");
            if (!File.Exists(infoPath)) continue;
            var info = JsonUtility.FromJson<Info>(File.ReadAllText(infoPath));
            var pivot = new Vector2(info.pivotPx[0] / info.canvas[0], 1f - info.pivotPx[1] / info.canvas[1]);
            var sets = new List<UndergroundCreatureLibrary.Set>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var png in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(png.Replace('\\', '/'));
                    if (importer == null) continue;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    bool same = importer.textureType == TextureImporterType.Sprite && settings.spriteAlignment == (int)SpriteAlignment.Custom
                        && Mathf.Approximately(importer.spritePixelsPerUnit, 100f) && (settings.spritePivot - pivot).sqrMagnitude < 1e-8f;
                    if (same) continue;
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100f; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot; settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (var seg in Directory.GetDirectories(dir).OrderBy(d => d))
            {
                var frames = Directory.GetFiles(seg, "*.png").OrderBy(f => f)
                    .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'))).Where(x => x != null).ToArray();
                if (frames.Length > 0) sets.Add(new UndergroundCreatureLibrary.Set { name = Path.GetFileName(seg), frames = frames });
            }
            entries.Add(new UndergroundCreatureLibrary.Entry { id = id, pivot = info.pivot, heightUnits = info.heightUnits, figureHeightPx = info.figureHeightPx, sets = sets.ToArray() });
        }
        Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
        var library = AssetDatabase.LoadAssetAtPath<UndergroundCreatureLibrary>(LibraryPath);
        if (library == null) { library = ScriptableObject.CreateInstance<UndergroundCreatureLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath); }
        library.creatures = entries.ToArray();
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
        Debug.Log($"UGP creatures: {entries.Count} creatures, {entries.Sum(e => e.sets.Sum(s => s.frames.Length))} frames in {LibraryPath}");
    }
}
