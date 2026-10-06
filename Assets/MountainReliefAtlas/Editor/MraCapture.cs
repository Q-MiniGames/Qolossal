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
