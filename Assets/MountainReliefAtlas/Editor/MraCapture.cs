using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode in-engine captures of the built scenes (imported-resource renders, not device tests):
// for each region its start, Waymark, shrine, knot, module and exit beats at 1920x1080, a 1280x720
// readability copy of each, and a wide overview; one shot per side chamber.
// Usage: -executeMethod MraCapture.Run -captureDir <folder> [-mraOnly MR01]
public static class MraCapture
{
    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "mra");
        string only = Arg("-mraOnly", "");
        Directory.CreateDirectory(folder);
        var world = MraWorldBuilder.Prepare();
        foreach (var r in world.regions)
        {
            if (only != "" && !r.id.StartsWith(only)) continue;
            EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath(r.scene));
            var shots = new List<(string, Vector2, float)>();
            void Beat(string label, Node n, float size = 5f) { if (n != null) shots.Add((label, MraWorldBuilder.Spawn(n) + new Vector2(0f, 1f), size)); }
            Beat("01_start", r.NodeById(r.entry));
            Beat("02_waymark", r.NodeById(r.primaryWaymark));
            Beat("03_shrine", r.nodes.FirstOrDefault(n => !string.IsNullOrEmpty(n.ability)));
            Beat("04_knot", r.nodes.FirstOrDefault(n => !string.IsNullOrEmpty(n.knot)));
            foreach (var e in r.edges.Where(e => e.module.Present)) shots.Add(("05_module_" + e.module.template, e.module.position + new Vector2(0f, 2f), 7f));
            if (r.id == "MR06") shots.Add(("05_module_glide-span", MraWorldBuilder.Pos(r.NodeById("MR06_N10")) + new Vector2(13f, 0f), 9f));
            Beat("06_exit", r.NodeById(r.exit));
            var reveal = Object.FindAnyObjectByType<MraReveal>();
            if (reveal != null && reveal.lookBackSize > 0f) { shots.Add(("08_reveal_lookback", reveal.lookBack, reveal.lookBackSize)); shots.Add(("09_reveal_pullout", reveal.pullCentre, reveal.pullSize)); }
            shots.Add(("07_overview", (r.cameraMin + r.cameraMax) * .5f, (r.cameraMax.y - r.cameraMin.y) * .55f));
            foreach (var (label, at, size) in shots)
            {
                Shot(Path.Combine(folder, $"{r.id}_{label}_1080.png"), at, size, 1920, 1080);
                if (!label.StartsWith("07")) Shot(Path.Combine(folder, $"{r.id}_{label}_720.png"), at, size, 1280, 720);
            }
        }
        foreach (var c in world.chambers.Where(c => !c.InPlace && (only == "" || c.id.StartsWith(only))))
        {
            EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath(c.scene));
            Shot(Path.Combine(folder, $"{c.id}_chamber_1080.png"), c.size * .5f, Mathf.Max(c.size.y * .6f, c.size.x * .32f), 1920, 1080);
        }
        Debug.Log("[MraCapture] wrote " + Path.GetFullPath(folder));
    }

    // One camera-height shot per riser of every stepped terrain piece in a scene, at 1920x1080 and
    // 1280x720, plus a sheet-friendly 1080 shot per riser at twice the zoom.
    // Usage: -executeMethod MraCapture.Steps -captureDir <folder> -mraScene <scene path> -stepTag <tag>
    public static void Steps()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string tag = Arg("-stepTag", "after"), folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "steps_" + tag);
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene(Arg("-mraScene", MraWorldBuilder.SceneFolder + "MRAtlas_MR01_Cradle.unity"));
        int n = 0;
        foreach (var t in Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).OrderBy(t => t.name))
        {
            var s = t.surface; Vector2 o = t.transform.position;
            for (int i = 0; i < s.Length - 1; i++)
            {
                Vector2 a = s[i] + o, b = s[i + 1] + o;
                if (Mathf.Abs(b.x - a.x) > 1e-4f || Mathf.Abs(b.y - a.y) < .4f) continue;
                n++;
                Vector2 at = new Vector2(a.x, (a.y + b.y) * .5f + 1f);
                string stem = Path.Combine(folder, $"riser_{n:00}_x{a.x:0.0}");
                Shot(stem + "_1080.png", at, 5f, 1920, 1080);
                Shot(stem + "_720.png", at, 5f, 1280, 720);
                Shot(stem + "_close_1080.png", at, 2.5f, 1920, 1080);
            }
        }
        Debug.Log($"[MraCapture] {n} risers -> " + Path.GetFullPath(folder));
    }

    // One 1920x1080 shot of a scene at a point. Usage: -executeMethod MraCapture.At -captureDir <folder>
    // -mraScene <scene path> -at <x>,<y> -size <ortho size> -stepTag <file stem>
    public static void At()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string folder = Arg("-captureDir", "Temp/Captures"); Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene(Arg("-mraScene", MraWorldBuilder.SceneFolder + "MRAtlas_MR01_Cradle.unity"));
        // -showVariants: every MraVariant's children shown (e.g. the crossing after the quake).
        if (System.Array.IndexOf(args, "-showVariants") >= 0)
            foreach (var v in Object.FindObjectsByType<MraVariant>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (Transform c in v.transform) c.gameObject.SetActive(true);
        var xy = Arg("-at", "0,0").Split(',');
        var at = new Vector2(float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
        Shot(Path.Combine(folder, Arg("-stepTag", "at") + ".png"), at, float.Parse(Arg("-size", "5"), System.Globalization.CultureInfo.InvariantCulture), 1920, 1080);
        Debug.Log("[MraCapture] shot at " + at);
    }

    public static void Shot(string path, Vector2 at, float size, int w, int h)
    {
        var camera = Camera.main; camera.aspect = (float)w / h;
        camera.transform.position = new Vector3(at.x, at.y, -10f); camera.orthographicSize = size;
        foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var texture = new RenderTexture(w, h, 24);
        camera.targetTexture = texture; camera.Render();
        RenderTexture.active = texture;
        var read = new Texture2D(w, h, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply();
        File.WriteAllBytes(path, read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(texture); Object.DestroyImmediate(read);
    }
}
