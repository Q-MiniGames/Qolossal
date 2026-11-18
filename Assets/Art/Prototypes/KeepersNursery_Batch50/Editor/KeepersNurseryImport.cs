using UnityEditor;
using UnityEngine;
using System.IO;

// Batch50b only. Never changes installed Route A or accepted source resources.
public sealed class KeepersNurseryImport : AssetPostprocessor
{
    public const string Root = "Assets/Art/Prototypes/KeepersNursery_Batch50/";
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal) || !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;
        var t = (TextureImporter)assetImporter;
        string n = Path.GetFileNameWithoutExtension(assetPath);
        bool lightMap = n.StartsWith("LM_", System.StringComparison.Ordinal);
        t.textureType = TextureImporterType.Default;
        t.sRGBTexture = !lightMap; // Same linear channel-map interpretation as LM_A04.
        t.alphaSource = TextureImporterAlphaSource.FromInput;
        t.alphaIsTransparency = !lightMap;
        t.mipmapEnabled = false;
        t.npotScale = TextureImporterNPOTScale.None;
        t.maxTextureSize = 4096;
        t.filterMode = FilterMode.Bilinear;
        t.textureCompression = TextureImporterCompression.CompressedHQ;
        t.isReadable = false;
        t.wrapModeU = n.StartsWith("NK_Wall_", System.StringComparison.Ordinal) || n == "NK_Band_Terrace" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.wrapModeV = n.StartsWith("NK_Wall_", System.StringComparison.Ordinal) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        foreach (string platform in new[] { "Standalone", "PS5", "GameCoreXboxOne", "GameCoreScarlett" })
            t.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = platform, overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.BC7, compressionQuality = 100 });
        // Switch2: not set. Requires its module documentation and actual platform testing.
    }
}
