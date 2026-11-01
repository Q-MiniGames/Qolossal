using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Painted art for the underground's greybox pieces. A kit is a folder, Assets/Art/Underground/PlatformKit/<Set>/, holding
// PNGs and a kit.json that says which piece each one paints. UndergroundPlatformingBuilder dresses a piece from its room's
// kit when one is there and leaves it greybox when not, so art can arrive a piece at a time and colliders never change.
// The builder sets each PNG's import itself (sprite, full rect, PPU, slice borders) from kit.json.
public static class UndergroundArtKit
{
    public const string Root = "Assets/Art/Underground/PlatformKit";

    [Serializable] public sealed class Piece
    {
        public string kind;                                 // deck, crumble, mover, solid, rock, gate, seal, grille, thorns, water
        public string[] files;                              // one or more variants, chosen steadily by piece
        public int[] border = new int[4];                   // slice borders in pixels: left, bottom, right, top
        [Tooltip("World units painted above the collider's top (moss, kerb). The walk line is exactly this far under the sprite's top.")]
        public float overhang = .125f;
    }
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
        string set = SetOf(roomId);
        var kit = Load(set);
        string folder = Root + "/" + set;
        if (kit == null) { set = "Shared"; kit = Load(set); folder = Root + "/Shared"; }
        if (kit == null || kit.pieces == null) return false;
        var piece = Array.Find(kit.pieces, p => p.kind == kind);
        if (piece == null || piece.files == null || piece.files.Length == 0) return false;
        string file = piece.files[Mathf.Abs(seed) % piece.files.Length];
        var sprite = SpriteFor(folder + "/" + file, piece, kit.pixelsPerUnit);
        if (sprite == null) { Debug.LogError($"UGP kit: {set} lists {file}, which won't load."); return false; }

        art.sprite = sprite; art.color = Color.white;
        art.drawMode = SpriteDrawMode.Tiled; art.tileMode = SpriteTileMode.Continuous;
        art.transform.localScale = Vector3.one;
        // A thin piece keeps its painted thickness, hanging from the walk line. A thick one fills its rect, plus the overhang.
        float painted = sprite.rect.height / sprite.pixelsPerUnit;
        bool thin = size.y < painted - piece.overhang;
        float height = thin ? painted : size.y + piece.overhang;
        art.size = new Vector2(size.x, height);
        art.transform.localPosition = new Vector3(0f, size.y * .5f + piece.overhang - height * .5f, 0f);
        return true;
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
