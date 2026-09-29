using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Copies reviewed Codex art from Tools/IncomingArt into Assets/Art/Codex and applies its sprite
// import settings. Tools/ArtImport/codex_v2_accepted.json is the source of truth: a file is only
// imported when its hash still matches the one that was reviewed.
public static class CodexArtImporter
{
    public const string ManifestPath = "Tools/ArtImport/codex_v2_accepted.json";
    public const string DestinationRoot = "Assets/Art/Codex/";
    public const int DefaultMaxSize = 2048;

    [Serializable] public class Manifest { public string source; public string destination; public Entry[] assets; }

    [Serializable] public class Entry
    {
        public string name, category, sha256, dest, wrapU, wrapV, mesh, basis;
        public float ppu;
        [Tooltip("Largest imported size (px); 0 means the default 2048. The manifest raises it for larger sources so none are downscaled.")]
        public int maxSize;
        public float[] pivot, border;
        public bool mipmaps;
        public SubSprite[] sprites;
    }

    [Serializable] public class SubSprite { public string name; public float[] rect, pivot; }

    private static Dictionary<string, Entry> cache;

    public static Entry Find(string assetPath)
    {
        if (cache == null)
        {
            Manifest manifest = Load();
            cache = manifest.assets.ToDictionary(entry => entry.dest, StringComparer.OrdinalIgnoreCase);
        }
        return cache.TryGetValue(assetPath, out Entry found) ? found : null;
    }

    private static Manifest Load()
    {
        string path = Path.Combine(ProjectRoot, ManifestPath);
        if (!File.Exists(path)) throw new FileNotFoundException("Codex art manifest not found", path);
        return JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
    }

    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

    [MenuItem("Qolossal/Art/Import Accepted Codex Art")]
    public static void ImportAll()
    {
        cache = null;
        Manifest manifest = Load();
        string sourceRoot = Path.Combine(ProjectRoot, manifest.source);
        int copied = 0, unchanged = 0;
        var rejected = new List<string>();
        var toImport = new List<string>();

        foreach (Entry entry in manifest.assets)
        {
            string source = Path.Combine(sourceRoot, entry.category, entry.name + ".png");
            string target = Path.Combine(ProjectRoot, entry.dest);
            if (!File.Exists(source)) { rejected.Add($"{entry.name}: source missing"); continue; }
            if (Hash(source) != entry.sha256) { rejected.Add($"{entry.name}: source changed since review"); continue; }
            if (File.Exists(target) && Hash(target) == entry.sha256) { unchanged++; toImport.Add(entry.dest); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target, true);
            toImport.Add(entry.dest);
            copied++;
        }

        AssetDatabase.StartAssetEditing();
        try { foreach (string path in toImport) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
        finally { AssetDatabase.StopAssetEditing(); }

        // Sub-sprite rects need the texture imported once before the sprite data provider can write them.
        foreach (Entry entry in manifest.assets.Where(e => e.sprites != null && e.sprites.Length > 0 && toImport.Contains(e.dest)))
            ApplySubSprites(entry);

        foreach (string reason in rejected) Debug.LogError("[CodexArtImporter] Skipped " + reason);
        Debug.Log($"[CodexArtImporter] {copied} copied, {unchanged} already current, {rejected.Count} skipped, {toImport.Count} imported.");
    }

    [MenuItem("Qolossal/Art/Verify Codex Art Import Settings")]
    public static void VerifyAll()
    {
        cache = null;
        int problems = 0;
        foreach (Entry entry in Load().assets)
        {
            var importer = AssetImporter.GetAtPath(entry.dest) as TextureImporter;
            if (importer == null) { Debug.LogError($"[CodexArtImporter] Not imported: {entry.dest}"); problems++; continue; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, entry.ppu) || importer.textureType != TextureImporterType.Sprite)
            { Debug.LogError($"[CodexArtImporter] Wrong settings: {entry.dest}"); problems++; }
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            if (Mathf.Max(width, height) > importer.maxTextureSize)
            { Debug.LogError($"[CodexArtImporter] {entry.dest} is {width}x{height} but imports at most {importer.maxTextureSize}: it is being downscaled"); problems++; }
            if (entry.sprites != null && entry.sprites.Length > 0)
            {
                int count = AssetDatabase.LoadAllAssetsAtPath(entry.dest).OfType<Sprite>().Count();
                if (count != entry.sprites.Length)
                { Debug.LogError($"[CodexArtImporter] {entry.dest} has {count} sprites, expected {entry.sprites.Length}"); problems++; }
            }
        }
        Debug.Log($"[CodexArtImporter] Verify finished with {problems} problem(s).");
    }

    private static void ApplySubSprites(Entry entry)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var importer = AssetImporter.GetAtPath(entry.dest);
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r);
        var rects = entry.sprites.Select(sub =>
        {
            SpriteRect rect = existing.TryGetValue(sub.name, out SpriteRect kept) ? kept : new SpriteRect { spriteID = GUID.Generate() };
            rect.name = sub.name;
            rect.rect = new Rect(sub.rect[0], sub.rect[1], sub.rect[2], sub.rect[3]);
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = new Vector2(sub.pivot[0], sub.pivot[1]);
            return rect;
        }).ToArray();
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static string Hash(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
    }
}

// Applies the manifest's settings whenever a texture under Assets/Art/Codex is (re)imported.
public sealed class CodexArtPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(CodexArtImporter.DestinationRoot, StringComparison.OrdinalIgnoreCase)) return;
        CodexArtImporter.Entry entry = CodexArtImporter.Find(assetPath);
        if (entry == null) { Debug.LogWarning($"[CodexArtImporter] {assetPath} is not in the accepted manifest."); return; }

        var importer = (TextureImporter)assetImporter;
        bool sheet = entry.sprites != null && entry.sprites.Length > 0;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = sheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = entry.ppu;
        importer.mipmapEnabled = entry.mipmaps;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapModeU = entry.wrapU == "Repeat" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.wrapModeV = entry.wrapV == "Repeat" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.maxTextureSize = entry.maxSize > 0 ? entry.maxSize : CodexArtImporter.DefaultMaxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = entry.mesh == "FullRect" ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        settings.spriteGenerateFallbackPhysicsShape = false;
        if (!sheet)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
        }
        importer.SetTextureSettings(settings);
        importer.spriteBorder = entry.border != null && entry.border.Length == 4
            ? new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3])
            : Vector4.zero;
    }
}
