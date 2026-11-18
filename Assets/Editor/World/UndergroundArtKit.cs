using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Painted art for the underground's greybox pieces. A kit is a folder, Assets/Art/Underground/PlatformKit/<Set>/, holding
// PNGs and a kit.json that says which piece each one paints. UndergroundPlatformingBuilder dresses a piece from its room's
// kit when one is there and leaves it greybox when not, so art can arrive a piece at a time and colliders never change.
// The builder sets each PNG's import itself (sprite, full rect, PPU, slice borders) from kit.json.
// A set with no art for a kind, or no kit at all, falls back to the Shared set. DressRoom adds what has no collider: a
// room's backdrop layers (kinds backdrop_far, backdrop_near) and its own props, from Dressing/<roomId>.json.
public static class UndergroundArtKit
{
    public const string Root = "Assets/Art/Underground/PlatformKit", DressingRoot = "Assets/Art/Underground/Dressing";

    [Serializable] public sealed class Piece
    {
        public string kind;                                 // deck, crumble, mover, solid, rock, gate, seal, grille, thorns, water, spill, gust, shelter, pod, valve, anchor, quarter, quarterlight, backdrop_far, backdrop_near
        public string[] files;                              // one or more variants, chosen steadily by piece
        public int[] border = new int[4];                   // slice borders in pixels: left, bottom, right, top
        [Tooltip("World units painted above the collider's top (moss, kerb). The walk line is exactly this far under the sprite's top.")]
        public float overhang = .125f;
        [Tooltip("Drawn once at its painted size, not tiled: an object that may be bigger than its collider.")] public bool simple;
        [Tooltip("Drawn once, stretched to the collider's rect.")] public bool fit;
        [Tooltip("A simple piece's offset from the collider's centre, in world units.")] public float[] offset;
        [Tooltip("Sorting order, if not the kind's usual one.")] public int order;
        [Tooltip("Pixels per unit, if not the kit's: for soft, distant layers only.")] public float ppu;
    }
    [Serializable] sealed class Prop { public string file; public float x, y, scale = 1f; public bool flipX; public int order = -9; }
    [Serializable] sealed class Props { public Prop[] items = new Prop[0]; }
    [Serializable] sealed class Kit { public string set; public float pixelsPerUnit = 216f; public Piece[] pieces = new Piece[0]; }

    static readonly Dictionary<string, Kit> Kits = new Dictionary<string, Kit>();

    public static void Forget() => Kits.Clear();

    // A room's set: A, C, D, E, H (the Heart), P (the pockets), or MR01..MR07 for the caves.
    public static string SetOf(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return "Shared";
        if (roomId == "HEART" || roomId[0] == 'H') return "H";
        if (roomId.StartsWith("MR")) return roomId.Substring(0, 4);
        return roomId.Substring(0, 1);
    }

    static Kit Load(string set)
    {
        if (Kits.TryGetValue(set, out var kit)) return kit;
        string folder = Path.Combine(Root, set).Replace('\\', '/'), path = folder + "/kit.json";
        kit = File.Exists(path) ? JsonUtility.FromJson<Kit>(File.ReadAllText(path)) : null;
        Kits[set] = kit;
        return kit;
    }

    // Dresses `art` (the greybox child of a piece whose collider rect has size `size`). Returns false to leave it greybox.
    public static bool Dress(SpriteRenderer art, string roomId, string kind, Vector2 size, int seed)
    {
        var piece = Find(roomId, kind, seed, out var sprite);
        if (piece == null) return false;

        art.sprite = sprite; art.color = Color.white;
        if (piece.order != 0) art.sortingOrder = piece.order;
        if (piece.simple || piece.fit)
        {
            Vector2 painted1 = sprite.bounds.size;
            art.drawMode = SpriteDrawMode.Simple;
            art.transform.localScale = piece.fit ? new Vector3(size.x / painted1.x, size.y / painted1.y, 1f) : Vector3.one;
            // A thread anchor draws on the piece itself: only an Art child may be moved.
            if (art.gameObject.name == "Art")
                art.transform.localPosition = piece.offset != null && piece.offset.Length == 2 ? new Vector3(piece.offset[0], piece.offset[1], 0f) : Vector3.zero;
            return true;
        }
        art.drawMode = SpriteDrawMode.Tiled; art.tileMode = SpriteTileMode.Continuous;
        art.transform.localScale = Vector3.one;
        // A thin piece keeps its painted thickness, hanging from the walk line. Any other fills its rect plus the overhang, whatever its canvas size.
        float painted = sprite.rect.height / sprite.pixelsPerUnit;
        bool thin = kind is "deck" or "crumble" or "mover";
        float height = thin ? painted : size.y + piece.overhang;
        art.size = new Vector2(size.x, height);
        art.transform.localPosition = new Vector3(0f, size.y * .5f + piece.overhang - height * .5f, 0f);
        return true;
    }

    // The piece for a kind in this room's set, or in Shared; its sprite is loaded and imported.
    static Piece Find(string roomId, string kind, int seed, out Sprite sprite)
    {
        sprite = null;
        foreach (string set in new[] { SetOf(roomId), "Shared" })
        {
            var kit = Load(set);
            var piece = kit?.pieces == null ? null : Array.Find(kit.pieces, p => p.kind == kind);
            if (piece == null || piece.files == null || piece.files.Length == 0) continue;
            string file = piece.files[Mathf.Abs(seed) % piece.files.Length];
            sprite = SpriteFor(Root + "/" + set + "/" + file, piece, piece.ppu > 0f ? piece.ppu : kit.pixelsPerUnit);
            if (sprite == null) { Debug.LogError($"UGP kit: {set} lists {file}, which won't load."); return null; }
            return piece;
        }
        return null;
    }

    // The far backdrop behind a passage between rooms. It sits below every room's backdrop, so where the box overlaps a
    // room only the room's own shows; the terrain hides the rest. Far only: one opaque layer is all a narrow passage needs.
    public static void DressPassage(string roomId, Rect rect, Transform parent, int order = -105)
    {
        var piece = Find(roomId, "backdrop_far", 0, out var sprite);
        if (piece == null) return;
        var go = new GameObject("UGP passage backdrop");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(rect.center.x, rect.center.y, 0f);
        var art = go.AddComponent<SpriteRenderer>();
        art.sprite = sprite; art.sortingOrder = order;
        art.drawMode = SpriteDrawMode.Simple;
        art.transform.localScale = new Vector3(rect.width / sprite.bounds.size.x, rect.height / sprite.bounds.size.y, 1f);
        WorldRefreshUnderground.ConfigureRockField(art,.3f);
    }

    // Backdrop layers and the room's own props, all without colliders, under `parent` so a rebuild replaces them.
    public static void DressRoom(UndergroundRoom room, Transform parent)
    {
        var bounds = room.WorldBounds;
        foreach (var (kind, order) in new[] { ("backdrop_far", -100), ("backdrop_near", -90) })
        {
            var piece = Find(room.roomId, kind, 0, out var sprite);
            if (piece == null) continue;
            var go = new GameObject("UGP " + kind);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
            var art = go.AddComponent<SpriteRenderer>();
            art.sprite = sprite; art.sortingOrder = piece.order != 0 ? piece.order : order;
            art.drawMode = SpriteDrawMode.Tiled; art.tileMode = SpriteTileMode.Continuous;
            art.size = new Vector2(bounds.size.x, bounds.size.y);
        }
        string path = DressingRoot + "/" + room.roomId + ".json";
        if (!File.Exists(path)) return;
        var props = JsonUtility.FromJson<Props>(File.ReadAllText(path));
        foreach (var prop in props?.items ?? new Prop[0])
        {
            var sprite = SpriteFor(DressingRoot + "/Art/" + prop.file, new Piece(), 216f);
            if (sprite == null) { Debug.LogError($"UGP dressing: {room.roomId} lists {prop.file}, which won't load."); continue; }
            var go = new GameObject("UGP prop " + prop.file);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(bounds.min.x + prop.x, bounds.min.y + prop.y, 0f);
            go.transform.localScale = new Vector3(prop.scale, prop.scale, 1f);
            var art = go.AddComponent<SpriteRenderer>();
            art.sprite = sprite; art.flipX = prop.flipX; art.sortingOrder = prop.order;
        }
    }

    static Sprite SpriteFor(string path, Piece piece, float ppu)
    {
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return null;
        var border = piece.border != null && piece.border.Length == 4 ? new Vector4(piece.border[0], piece.border[1], piece.border[2], piece.border[3]) : Vector4.zero;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (importer.textureType != TextureImporterType.Sprite || settings.spriteMode != (int)SpriteImportMode.Single || settings.spriteMeshType != SpriteMeshType.FullRect
            || !Mathf.Approximately(settings.spritePixelsPerUnit, ppu) || settings.spriteBorder != border || settings.spriteAlignment != (int)SpriteAlignment.Center || settings.wrapMode != TextureWrapMode.Clamp)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single; settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = ppu; settings.spriteBorder = border; settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.wrapMode = TextureWrapMode.Clamp; settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
