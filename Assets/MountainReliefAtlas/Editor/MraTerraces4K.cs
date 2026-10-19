using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The Terraces' 4K background stack (Batch 28d, Claude Review 86; the user's "import so i can test",
// 19 Oct 2026): five native 4K planes at 216 px/u, made for real vertical parallax.
// - Sky: camera-locked dawn wash, 15 u.
// - Far (depth 25): the ranges, sinks up to 6 u.
// - High (depth 25): a cloud sea over the Far, fading in over the last 30% of the climb (MraClimbFade).
// - Mid (depth 10): a side-on valley wall, its skyline at 40% of the frame at the bottom of the climb,
//   sinking at 0.6 of the real rate so it lasts to about 60% of the climb.
// - Near (depth 5): tall open limestone spires passing at the real rate, a touch darkened and cooled
//   so the walkable rock (the same cream limestone) reads in front.
// Registration follows the Batch 28d proofs (QA/CLIMB_REGISTRATION.json). The builder uses this stack
// for MR03 when the files are present; the menu item swaps it into the existing Terraces scene alone.
public static class MraTerraces4K
{
    public const string Folder = "Assets/Art/Codex/Backgrounds/Terraces4K/";
    const float Ppu = 216f;
    static readonly string[] Names = { "BG_Sky_Terraces_4K", "BG_Terraces_Far_4K", "BG_Terraces_High_4K", "BG_Terraces_Mid_4K", "BG_Terraces_Near_4K" };

    public static bool Available => Names.All(n => File.Exists(Folder + n + ".png"));

    static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + name + ".png");

    // Repeat U / Clamp V, full size (no 2048 cap), mipmaps, no power-of-two rescale.
    public static void Configure()
    {
        foreach (var n in Names)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + n + ".png");
            if (importer == null) continue;
            bool sky = n.StartsWith("BG_Sky");
            bool dirty = importer.textureType != TextureImporterType.Default || importer.maxTextureSize != 8192 || importer.npotScale != TextureImporterNPOTScale.None
                || importer.wrapModeU != TextureWrapMode.Repeat || importer.wrapModeV != TextureWrapMode.Clamp || !importer.mipmapEnabled || !importer.alphaIsTransparency;
            if (!dirty) continue;
            importer.textureType = TextureImporterType.Default; importer.maxTextureSize = 8192; importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapModeU = TextureWrapMode.Repeat; importer.wrapModeV = sky ? TextureWrapMode.Clamp : TextureWrapMode.Clamp;
            importer.mipmapEnabled = true; importer.alphaIsTransparency = true; importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }
    }

    // The bottom row's average colour, for the flat band under a plane that sinks past the frame.
    static Color BottomColour(string name)
    {
        var read = new Texture2D(2, 2);
        read.LoadImage(File.ReadAllBytes(Folder + name + ".png"));
        var row = read.GetPixels(0, 0, read.width, 4);
        Object.DestroyImmediate(read);
        var opaque = row.Where(c => c.a > .5f).ToArray();
        if (opaque.Length == 0) return Color.white;
        return new Color(opaque.Average(c => c.r), opaque.Average(c => c.g), opaque.Average(c => c.b), 1f);
    }

    static ParallaxLayer Plane(Transform root, Camera camera, Vector2 climb, string name, float depth, float bottom, int order, float extend)
    {
        var tex = Tex(name);
        var layer = new GameObject($"4K {name}" + (depth > 0f ? $" (depth {depth:0})" : " (camera-locked)")).AddComponent<ParallaxLayer>();
        layer.transform.SetParent(root, false);
        layer.texture = tex; layer.texturePixelsPerUnit = Ppu; layer.height = tex.height / Ppu; layer.repeat = true;
        layer.depth = depth; layer.baseY = bottom; layer.sortingOrder = order; layer.targetCamera = camera;
        layer.composedForSize = 5f; layer.extendBelow = extend; layer.belowBlend = 1.2f;
        layer.belowColor = extend > 0f ? BottomColour(name) : Color.white;
        if (depth > 0f)
        {
            layer.follow = new Vector2(1f - 1f / depth, .997f);   // for reading; FollowX comes from depth
            layer.climbProgression = true; layer.climbRange = climb; layer.climbBudget = 0f; layer.verticalRate = 1f;
        }
        else layer.follow = Vector2.one;
        layer.enabled = false; layer.enabled = true;   // OnEnable ran before the texture was set: apply it now
        return layer;
    }

    /// <summary>Adds the five planes under a region's Background group.</summary>
    public static void Apply(Transform background, Camera camera, Vector2 climb)
    {
        Configure();
        Plane(background, camera, climb, "BG_Sky_Terraces_4K", 0f, -5f, -110, 0f);
        // Far and High 0.4 u above the frame's bottom (the highest peak, row 123, stays in frame at y 4.8),
        // sinking at half the real rate (2.9 u over the climb) so the ranges stay in view above the Mid.
        var far = Plane(background, camera, climb, "BG_Terraces_Far_4K", 25f, -4.6f, -105, 7f);
        far.sinkLimit = 6f; far.verticalRate = .5f;
        var high = Plane(background, camera, climb, "BG_Terraces_High_4K", 25f, -4.6f, -104, 0f);
        high.sinkLimit = 6f; high.verticalRate = .5f; high.tint = new Color(1f, 1f, 1f, 0f);
        high.gameObject.AddComponent<MraClimbFade>();
        // The skyline (row 346 of 3900) at 62% of the frame's height from the bottom (y = 1.2), sinking at
        // 0.35 of the real rate (still faster than the Far): above the walk line (about 40%) to about 43%
        // of the climb, gone by about 75%. (The user, 19 Oct: at 40% it showed only over the first 10% of
        // the map; at 75% it covered most of the Far.)
        var mid = Plane(background, camera, climb, "BG_Terraces_Mid_4K", 10f, 1.2f - (3900f - 346f) / Ppu, -90, 0f);
        mid.verticalRate = .35f;
        var near = Plane(background, camera, climb, "BG_Terraces_Near_4K", 5f, -5f, -80, 0f);
        near.tint = new Color(.86f, .87f, .9f, 1f);
    }

    // The planes the 4K stack replaces: the old sky, the Far/Mid/Near set and the mill landmark (painted
    // for the old set, it floats in the new sky; the new Mid has its own village), and any earlier 4K
    // planes. The foreground plants stay.
    static bool Replaced(Transform t) =>
        t.name.StartsWith("4K ") || t.name.StartsWith("Sky - ") || t.name.StartsWith("Far - ") || t.name.StartsWith("Mid - ") || t.name.StartsWith("Near - ")
        || t.name.StartsWith("Landmark - BG_Terraces_Mill_Landmark");

    public static void RemoveOld(Transform background)
    {
        foreach (var t in background.Cast<Transform>().Where(Replaced).ToList()) Object.DestroyImmediate(t.gameObject);
    }

    static string ScenePath => MraWorldBuilder.SceneFolder + "MRAtlas_MR03_Terraces.unity";

    [MenuItem("Qolossal/Mountain Relief/Terraces: use the 4K backgrounds (Batch 28d)")]
    public static void SwapIntoScene()
    {
        if (!Available) { Debug.LogError("[MraTerraces4K] the five 4K planes are missing from " + Folder); return; }
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var background = scene.GetRootGameObjects().Select(g => g.transform.Find("Background") ?? (g.name == "Background" ? g.transform : null)).FirstOrDefault(t => t != null);
        if (background == null) { Debug.LogError("[MraTerraces4K] no Background group in " + ScenePath); return; }
        var old = background.GetComponentsInChildren<ParallaxLayer>().FirstOrDefault(l => l.climbProgression);
        Vector2 climb = old != null ? old.climbRange : new Vector2(13f, 159f);
        RemoveOld(background);
        Apply(background, Camera.main, climb);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MraTerraces4K] the 4K stack is in {ScenePath} (climb {climb.x:0}..{climb.y:0})");
    }

    // Batch: swap the stack in, then shoot the climb at 0, 15, 30, 50, 75 and 100% along the route.
    // Usage: -executeMethod MraTerraces4K.SwapAndCapture -captureDir <folder>
    public static void SwapAndCapture()
    {
        SwapIntoScene();
        string[] args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-captureDir");
        string folder = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Temp/Captures/terraces4k";
        Directory.CreateDirectory(folder);
        var world = MraWorldBuilder.Prepare();
        var region = world.regions.First(r => r.id == "MR03");
        var layer = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).First(l => l.climbProgression);
        foreach (float p in new[] { 0f, .15f, .3f, .5f, .75f, 1f })
        {
            float y = Mathf.Lerp(layer.climbRange.x, layer.climbRange.y, p);
            var node = region.nodes.OrderBy(n => Mathf.Abs(MraWorldBuilder.Spawn(n).y + 1f - y)).First();
            var at = new Vector2(MraWorldBuilder.Spawn(node).x, y);
            Camera.main.transform.position = new Vector3(at.x, at.y, -10f);
            foreach (var fade in Object.FindObjectsByType<MraClimbFade>(FindObjectsSortMode.None)) fade.Apply();
            MraCapture.Shot(Path.Combine(folder, $"terraces4k_{Mathf.RoundToInt(p * 100):000}.png"), at, 5f, 1920, 1080);
        }
        Debug.Log("[MraTerraces4K] climb frames -> " + Path.GetFullPath(folder));
    }
}
