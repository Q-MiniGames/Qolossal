using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A region's native 4K background stack (the Terraces, Batch 28d, Review 86; the Cradle, Batch 34,
// Review 88; the Causeway, Batches 35 and 35d, Review 92; Qvale, Batches 36 and 36b, Review 93; the Ribwood, Batches 37 and 37d, Review 96; the user's "import cradle scene now", 19 Oct 2026). Five planes at 216 px/u, made for
// real vertical parallax, placed as tuned in play on the Terraces (19 Oct):
// - Sky: camera-locked wash, its bottom at the frame's bottom.
// - Far (depth 25): 0.4 u above the frame's bottom, sinking at half the real rate, at most 6 u.
// - High (depth 25): as the Far; a cloud sea fading in over the last 30% of the climb (MraClimbFade).
// - Mid (depth 10): its median skyline at 62% of the frame's height (y = 1.2), sinking at 0.35 of the
//   real rate, so it stays above the walk line for about the first 40% of a long climb.
// - Near (depth 5): tall open features passing at the region's Near rate, its bottom at the frame's
//   bottom, a touch darkened and cooled so the walkable rock reads in front.
// The builder uses a region's stack when its files are present (DepthLayers); the menu items swap a
// stack into the existing scene alone, keeping everything else in it.
public static class MraRegion4K
{
    const float Ppu = 216f;

    public sealed class Stack
    {
        public string region, scene, folder, sky, far, high, mid, near;
        public int midSkylineRow;   // the Mid's median skyline, in pixels from its top
        public float nearRate = 1f;
    }

    static Stack Set(string region, string scene, string name, int midSkylineRow, float nearRate, string sky = null) => new Stack
    {
        region = region, scene = scene, folder = $"Assets/Art/Codex/Backgrounds/{name}4K/",
        sky = sky ?? $"BG_{name}_Sky_4K", far = $"BG_{name}_Far_4K", high = $"BG_{name}_High_4K", mid = $"BG_{name}_Mid_4K", near = $"BG_{name}_Near_4K",
        midSkylineRow = midSkylineRow, nearRate = nearRate,
    };

    public static readonly Dictionary<string, Stack> Stacks = new Dictionary<string, Stack>
    {
        { "MR01", Set("MR01", "MRAtlas_MR01_Cradle", "Cradle", 347, 1f) },
        { "MR02", Set("MR02", "MRAtlas_MR02_Causeway", "Causeway", 350, 1f) },   // Batch 35 Sky/Far/High + 35d Mid/Near, Review 92
        { "MR04", Set("MR04", "MRAtlas_MR04_Qvale", "Qvale", 350, 1f) },         // Batch 36 Sky/Far/High + 36b Mid/Near, Review 93
        { "MR03", Set("MR03", "MRAtlas_MR03_Terraces", "Terraces", 346, 1f, "BG_Sky_Terraces_4K") },
        { "MR05", Set("MR05", "MRAtlas_MR05_Ribwood", "Ribwood", 350, .963f) },  // Batch 37 Far + 37d Near/Mid/High/Sky, Review 96
    };

    static IEnumerable<string> Files(Stack s) => new[] { s.sky, s.far, s.high, s.mid, s.near };

    public static bool Available(string region) => Stacks.TryGetValue(region, out var s) && Files(s).All(n => File.Exists(s.folder + n + ".png"));

    // Repeat U / Clamp V, full size (no 2048 cap), mipmaps, no power-of-two rescale.
    static void Configure(Stack s)
    {
        foreach (var n in Files(s))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(s.folder + n + ".png");
            if (importer == null) continue;
            bool dirty = importer.textureType != TextureImporterType.Default || importer.maxTextureSize != 8192 || importer.npotScale != TextureImporterNPOTScale.None
                || importer.wrapModeU != TextureWrapMode.Repeat || importer.wrapModeV != TextureWrapMode.Clamp || !importer.mipmapEnabled || !importer.alphaIsTransparency;
            if (!dirty) continue;
            importer.textureType = TextureImporterType.Default; importer.maxTextureSize = 8192; importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapModeU = TextureWrapMode.Repeat; importer.wrapModeV = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true; importer.alphaIsTransparency = true; importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }
    }

    // The bottom row's average colour, for the flat band under a plane that sinks past the frame.
    static Color BottomColour(string path)
    {
        var read = new Texture2D(2, 2);
        read.LoadImage(File.ReadAllBytes(path));
        var row = read.GetPixels(0, 0, read.width, 4);
        Object.DestroyImmediate(read);
        var opaque = row.Where(c => c.a > .5f).ToArray();
        if (opaque.Length == 0) return Color.white;
        return new Color(opaque.Average(c => c.r), opaque.Average(c => c.g), opaque.Average(c => c.b), 1f);
    }

    static ParallaxLayer Plane(Stack s, Transform root, Camera camera, Vector2 climb, string name, float depth, float bottom, int order, float extend)
    {
        string path = s.folder + name + ".png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var layer = new GameObject($"4K {name}" + (depth > 0f ? $" (depth {depth:0})" : " (camera-locked)")).AddComponent<ParallaxLayer>();
        layer.transform.SetParent(root, false);
        layer.texture = tex; layer.texturePixelsPerUnit = Ppu; layer.height = tex.height / Ppu; layer.repeat = true;
        layer.depth = depth; layer.baseY = bottom; layer.sortingOrder = order; layer.targetCamera = camera;
        layer.composedForSize = 5f; layer.extendBelow = extend; layer.belowBlend = 1.2f;
        layer.belowColor = extend > 0f ? BottomColour(path) : Color.white;
        if (depth > 0f)
        {
            layer.follow = new Vector2(1f - 1f / depth, .997f);   // for reading; FollowX comes from depth
            layer.climbProgression = true; layer.climbRange = climb; layer.climbBudget = 0f; layer.verticalRate = 1f;
        }
        else layer.follow = Vector2.one;
        layer.enabled = false; layer.enabled = true;   // OnEnable ran before the texture was set: apply it now
        return layer;
    }

    /// <summary>Adds a region's five planes under its Background group.</summary>
    public static void Apply(string region, Transform background, Camera camera, Vector2 climb)
    {
        var s = Stacks[region];
        Configure(s);
        Plane(s, background, camera, climb, s.sky, 0f, -5f, -110, 0f);
        // The highest peak sits at least 120 px under the Far's top, so 0.4 u up keeps it in frame.
        var far = Plane(s, background, camera, climb, s.far, 25f, -4.6f, -105, 7f);
        far.sinkLimit = 6f; far.verticalRate = .5f;
        var high = Plane(s, background, camera, climb, s.high, 25f, -4.6f, -104, 0f);
        high.sinkLimit = 6f; high.verticalRate = .5f; high.tint = new Color(1f, 1f, 1f, 0f);
        high.gameObject.AddComponent<MraClimbFade>();
        var midTex = AssetDatabase.LoadAssetAtPath<Texture2D>(s.folder + s.mid + ".png");
        var mid = Plane(s, background, camera, climb, s.mid, 10f, 1.2f - (midTex.height - s.midSkylineRow) / Ppu, -90, 0f);
        mid.verticalRate = .35f;
        var near = Plane(s, background, camera, climb, s.near, 5f, -5f, -80, 0f);
        near.verticalRate = s.nearRate; near.tint = new Color(.86f, .87f, .9f, 1f);
    }

    // The planes a 4K stack replaces: the old sky, the Far/Mid/Near set, the Terraces' mill landmark
    // (painted for the old set) and any earlier 4K planes. Foreground plants stay.
    static bool Replaced(Transform t) =>
        t.name.StartsWith("4K ") || t.name.StartsWith("Sky - ") || t.name.StartsWith("Far - ") || t.name.StartsWith("Mid - ") || t.name.StartsWith("Near - ")
        || t.name.StartsWith("Landmark - BG_Terraces_Mill_Landmark") || t.name.StartsWith("Vault rim - ") || t.name.StartsWith("Far ridge") || t.name.StartsWith("Far painting - ");

    public static void RemoveOld(Transform background)
    {
        foreach (var t in background.Cast<Transform>().Where(Replaced).ToList()) Object.DestroyImmediate(t.gameObject);
    }

    [MenuItem("Qolossal/Mountain Relief/4K backgrounds: the Cradle (Batch 34)")]
    static void CradleMenu() => SwapIntoScene("MR01");

    [MenuItem("Qolossal/Mountain Relief/4K backgrounds: the Causeway (Batch 35d)")]
    static void CausewayMenu() => SwapIntoScene("MR02");

    [MenuItem("Qolossal/Mountain Relief/4K backgrounds: Qvale (Batch 36b)")]
    static void QvaleMenu() => SwapIntoScene("MR04");

    [MenuItem("Qolossal/Mountain Relief/4K backgrounds: the Terraces (Batch 28d)")]
    static void TerracesMenu() => SwapIntoScene("MR03");

    [MenuItem("Qolossal/Mountain Relief/4K backgrounds: the Ribwood (Batch 37d)")]
    static void RibwoodMenu() => SwapIntoScene("MR05");

    // Opens the region's scene, replaces its background planes with the 4K stack and saves it.
    // Everything else in the scene (terrain, props, other work) is left as it is.
    public static void SwapIntoScene(string region)
    {
        var s = Stacks[region];
        if (!Available(region)) { Debug.LogError($"[MraRegion4K] {region}: the five 4K planes are missing from {s.folder}"); return; }
        string path = MraWorldBuilder.SceneFolder + s.scene + ".unity";
        var scene = EditorSceneManager.OpenScene(path);
        var background = scene.GetRootGameObjects().Select(g => g.transform.Find("Background") ?? (g.name == "Background" ? g.transform : null)).FirstOrDefault(t => t != null);
        if (background == null) { Debug.LogError("[MraRegion4K] no Background group in " + path); return; }
        var old = background.GetComponentsInChildren<ParallaxLayer>().FirstOrDefault(l => l.climbProgression);
        if (old == null) { Debug.LogError("[MraRegion4K] no climb layer to read the climb range from in " + path); return; }
        Vector2 climb = old.climbRange;
        RemoveOld(background);
        Apply(region, background, Camera.main, climb);
        RelinkSeatedVistas(background);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MraRegion4K] {region}: the 4K stack is in {path} (climb {climb.x:0}..{climb.y:0})");
    }

    // Qvale's listening overlook fades the street's valley layers out while Qori sits: point it at the
    // new planes (not the camera-locked sky, nor the cloud sea, whose opacity MraClimbFade owns).
    public static void RelinkSeatedVistas(Transform background)
    {
        foreach (var fade in background.GetComponentsInChildren<SeatedVistaFade>(true))
            fade.under = background.GetComponentsInChildren<ParallaxLayer>(true)
                .Where(l => l != fade.vista && l.follow.y < 1f && l.GetComponent<MraClimbFade>() == null).ToArray();
    }

    // Batch: swap a region's stack in, then shoot the climb at 0, 15, 30, 50, 75 and 100% along the route.
    // Usage: -executeMethod MraRegion4K.SwapAndCapture -mra4kRegion MR01 -captureDir <folder>
    public static void SwapAndCapture()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string region = Arg("-mra4kRegion", "MR03"), folder = Arg("-captureDir", "Temp/Captures/region4k");
        SwapIntoScene(region);
        Directory.CreateDirectory(folder);
        var world = MraWorldBuilder.Prepare();
        var r = world.regions.First(x => x.id == region);
        var layer = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).First(l => l.climbProgression);
        foreach (float p in new[] { 0f, .15f, .3f, .5f, .75f, 1f })
        {
            float y = Mathf.Lerp(layer.climbRange.x, layer.climbRange.y, p);
            var node = r.nodes.OrderBy(n => Mathf.Abs(MraWorldBuilder.Spawn(n).y + 1f - y)).First();
            var at = new Vector2(MraWorldBuilder.Spawn(node).x, y);
            Camera.main.transform.position = new Vector3(at.x, at.y, -10f);
            foreach (var fade in Object.FindObjectsByType<MraClimbFade>(FindObjectsSortMode.None)) fade.Apply();
            MraCapture.Shot(Path.Combine(folder, $"{region}_4k_{Mathf.RoundToInt(p * 100):000}.png"), at, 5f, 1920, 1080);
        }
        Debug.Log("[MraRegion4K] climb frames -> " + Path.GetFullPath(folder));
    }
}
