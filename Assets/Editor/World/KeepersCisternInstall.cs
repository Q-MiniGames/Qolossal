using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Installs the approved Route A "Keepers' Cistern" backgrounds (Batch 49 through 49d-r1, Claude Review 105)
// into Qolossal_Underground behind one on/off switch. The old cave art is untouched and returns when off.
public static class KeepersCisternInstall
{
    const string Prototype = "Assets/Art/Prototypes/KeepersCistern_Batch49/";
    static readonly string[] Revisions = { "49d-r1/", "49d/", "49c/", "49b-r3/", "49b-r2/", "49b-r1/", "" }; // newest first, as the preview resolves them
    static readonly HashSet<string> Unused = new() { "CK_Glow_Mint", "CK_Roots_Crawl_01", "CK_Roots_Crawl_02", "CK_Wall_Bedrock" };
    public const string Folder = "Assets/Art/Underground/Backgrounds/KeepersCistern/";
    const string ShaderPath = Folder + "KeepersCistern.shader";
    const string ScenePath = "Assets/Scenes/Game/Qolossal_Underground.unity";
    const string RootName = "Route A Backdrop - Keepers' Cistern";
    const string Report = "Tools/Design/KeepersCistern_Integration_20261109/";

    [MenuItem("Qolossal/Backgrounds/Route A Keepers' Cistern/Install or update")] static void InstallMenu() => Install(true);
    [MenuItem("Qolossal/Backgrounds/Route A Keepers' Cistern/Use new art")] static void On() => SetActive(true);
    [MenuItem("Qolossal/Backgrounds/Route A Keepers' Cistern/Use old art")] static void Off() => SetActive(false);

    public static void InstallBatch() { try { Install(true); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }
    public static void CheckBatch() { try { Check(); EditorApplication.Exit(0); } catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } }

    static Dictionary<string, string> ResolveSources()
    {
        var map = new Dictionary<string, string>();
        foreach (var rev in Revisions)
        {
            string dir = Prototype + rev;
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.png", SearchOption.TopDirectoryOnly))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (Unused.Contains(n) || map.ContainsKey(n)) continue;
                map[n] = dir + Path.GetFileName(f);
            }
        }
        return map;
    }

    public static void Install(bool saveScene)
    {
        Directory.CreateDirectory(Report);
        // The Editor-only previews would draw a second copy on top: switch them off for good.
        KeepersCisternPreview.SetEnabled(false);
        CisternContinuousPreview.SetEnabled(false);

        Directory.CreateDirectory(Folder);
        var sources = ResolveSources();
        var copied = new List<object>();
        foreach (var kv in sources.OrderBy(k => k.Key))
        {
            string dest = Folder + kv.Key + ".png";
            bool same = File.Exists(dest) && File.ReadAllBytes(dest).SequenceEqual(File.ReadAllBytes(kv.Value));
            if (!same)
            {
                if (File.Exists(dest)) AssetDatabase.DeleteAsset(dest);
                if (!AssetDatabase.CopyAsset(kv.Value, dest)) throw new Exception("Copy failed: " + kv.Value);
            }
            copied.Add(new { name = kv.Key, source = kv.Value, dest, unchanged = same });
        }
        AssetDatabase.Refresh();
        // Unity 6.3 only block-compresses a texture with mipmaps when it is power-of-two; every painting here is NPOT
        // (216 px/u), so with mipmaps it silently imports as RGBA32 (measured 9 Nov 2026). These layers are drawn at
        // 1:1 on a 4K screen, so mipmaps off + BC7 is the measured choice. Light maps are already mip-free.
        int reimported = 0;
        foreach (var n in sources.Keys)
        {
            if (n.StartsWith("LM_")) continue;
            var imp = (TextureImporter)AssetImporter.GetAtPath(Folder + n + ".png");
            if (!imp || !imp.mipmapEnabled) continue;
            imp.mipmapEnabled = false; imp.SaveAndReimport(); reimported++;
        }
        Debug.Log($"Keepers' Cistern: mipmaps switched off on {reimported} paintings so they compress to BC7.");
        string shaderText = File.ReadAllText(Prototype + "KeepersCisternPreview.shader").Replace("Qolossal/Prototypes/KeepersCistern49", "Qolossal/Environment/KeepersCistern");
        if (!File.Exists(ShaderPath) || File.ReadAllText(ShaderPath) != shaderText) File.WriteAllText(ShaderPath, shaderText);
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (!shader) throw new Exception("Shader did not import: " + ShaderPath);
        var textures = sources.Keys.Select(n => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + n + ".png")).ToList();
        if (textures.Any(t => !t)) throw new Exception("Missing copied textures: " + string.Join(", ", sources.Keys.Where(n => !AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + n + ".png"))));

        var scene = OpenScene();
        var root = FindRoot(scene);
        bool created = !root;
        if (!root) { root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene); }
        var c = root.GetComponent<KeepersCisternBackdrop>() ?? root.AddComponent<KeepersCisternBackdrop>();
        c.Configure(shader, textures);
        EditorUtility.SetDirty(c);
        EditorSceneManager.MarkSceneDirty(scene);
        if (saveScene) EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report + "install_receipt.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            time = DateTime.Now.ToString("s"), scene = ScenePath, root = RootName, created, active = root.activeSelf,
            textureCount = textures.Count, shader = ShaderPath, files = copied
        }, Newtonsoft.Json.Formatting.Indented));
        Debug.Log($"Keepers' Cistern installed: {textures.Count} textures, root {(created ? "created" : "updated")}, active={root.activeSelf}.");
    }

    static Scene OpenScene()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
        return scene;
    }

    static GameObject FindRoot(Scene scene) => scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);

    static void SetActive(bool on)
    {
        var scene = OpenScene(); var root = FindRoot(scene);
        if (!root) { Debug.LogWarning("Install the Keepers' Cistern backdrop first."); return; }
        root.SetActive(on); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Route A backgrounds: " + (on ? "new Keepers' Cistern art" : "old art"));
    }

    // Edit-mode check: builds the backdrop once (unsaved), reports pieces, missing paintings and the
    // imported texture formats, and renders the same review cameras as Batch 49.
    static void Check()
    {
        Directory.CreateDirectory(Report + "Captures");
        var scene = OpenScene(); var root = FindRoot(scene);
        if (!root) throw new Exception("Backdrop not installed");
        var c = root.GetComponent<KeepersCisternBackdrop>();
        CavePassagePresentation.Create();
        c.BuildForCheck(true);

        var formats = new List<object>();
        long storage = 0, runtime = 0;
        var getStorage = Type.GetType("UnityEditor.TextureUtil, UnityEditor")?.GetMethod("GetStorageMemorySizeLong", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        foreach (var t in c.Paintings)
        {
            string path = AssetDatabase.GetAssetPath(t);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            var s = imp.GetPlatformTextureSettings("Standalone");
            long st = getStorage != null ? Convert.ToInt64(getStorage.Invoke(null, new object[] { t })) : -1;
            long rt = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
            storage += Math.Max(0, st); runtime += rt;
            formats.Add(new { name = t.name, t.width, t.height, loadedFormat = t.format.ToString(), mips = t.mipmapCount, standaloneOverride = s.overridden, standaloneFormat = s.format.ToString(), storageBytes = st, editorRuntimeBytes = rt });
        }

        var cam = new GameObject("Keepers check camera") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 5; cam.aspect = 16f / 9; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.014f, .027f, .031f);
        var views = new (string name, float x, float y)[] {
            ("A01",1494,174),("A02_top",1534,174),("A02_middle",1534,150),("A02_bottom",1534,132),("A03",1515,106),
            ("A04_bottom",1568,88),("A04_middle",1588,99),("A04_approach",1610,107.5f),("A05_bottom",1636,101),("A05_top",1636,137),
            ("A06_left",1576,158),("A06_right",1604,176),("A07",1644,174),("A08",1708,172) };
        var rt2 = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            cam.targetTexture = rt2;
            foreach (var v in views)
            {
                cam.transform.position = new Vector3(v.x, v.y, -50);
                CaveDepth.Apply(cam); c.ApplyForCheck(cam);
                cam.Render();
                RenderTexture.active = rt2; tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply(); RenderTexture.active = null;
                File.WriteAllBytes(Report + "Captures/" + v.name + ".png", tex.EncodeToPNG());
            }
        }
        finally { cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(cam.gameObject); rt2.Release(); UnityEngine.Object.DestroyImmediate(tex); }

        File.WriteAllText(Report + "check.json", Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            time = DateTime.Now.ToString("s"), pieces = c.PieceCount, missing = c.MissingPaintings, textures = formats.Count,
            totalStorageMiB = storage / 1048576.0, totalEditorRuntimeMiB = runtime / 1048576.0, activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            formats
        }, Newtonsoft.Json.Formatting.Indented));
        c.Clear();
        Debug.Log($"Keepers' Cistern check: {c.PieceCount} pieces, missing [{string.Join(",", c.MissingPaintings)}], storage {storage / 1048576.0:F1} MiB.");
    }
}
