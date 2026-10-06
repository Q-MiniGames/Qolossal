using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Level-polish evidence: in-engine captures at camera positions taken from the route data only
// (beat spawns, chamber doors, and samples every 24 u between beats), so before and after shots of a
// rebuild line up exactly. Gameplay framing (orthographic size 5, camera 1 u above the spot); 1920x1080
// JPEGs for every spot, 1280x720 for beats with doors and the screenshot spots.
// Usage: -executeMethod MraPolishCapture.Run -captureDir <folder> -polishTag before|after [-mraOnly MR03]
public static class MraPolishCapture
{
    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string tag = Arg("-polishTag", "after"), only = Arg("-mraOnly", "");
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "polish_" + tag);
        Directory.CreateDirectory(folder);
        var world = MraWorldBuilder.Prepare();
        foreach (var r in world.regions)
        {
            if (only != "" && !only.Split(',').Any(o => r.id.StartsWith(o))) continue;
            EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath(r.scene));
            var spots = Spots(world, r);
            if (r.id == "MR04")
            {
                // The listening overlook (the bench about 7 u past MR04_N09) and each house front.
                Vector2 n9 = MraWorldBuilder.Pos(r.NodeById("MR04_N09"));
                spots.Add(("overlook", n9 + new Vector2(6f, 2f), true));
                foreach (var c in world.ChambersOf("MR04").Where(c => c.IsHouse))
                    spots.Add(($"house_{c.id}", new Vector2(c.entrance.x + 1f, 10.5f), true));
            }
            foreach (var (label, at, wide) in spots)
            {
                Shot(Path.Combine(folder, $"{r.id}_{label}_1080.jpg"), at, 5f, 1920, 1080);
                if (wide) Shot(Path.Combine(folder, $"{r.id}_{label}_720.jpg"), at, 5f, 1280, 720);
            }
            // A wider composition view per 80 u of the route (orthographic size 9).
            var nodes = r.nodes.Select(MraWorldBuilder.Pos).OrderBy(p => p.x).ToList();
            for (float x = nodes[0].x; x <= nodes[nodes.Count - 1].x; x += 80f)
                Shot(Path.Combine(folder, $"{r.id}_wide_{x:000}_1080.jpg"), new Vector2(x, RouteY(nodes, x) + 2f), 9f, 1920, 1080);
        }
        // Side chambers, framed as MraCapture frames them (so the earlier QA shots are the "before").
        if (only == "" || only.Contains("C"))
            foreach (var c in world.chambers.Where(c => !c.InPlace))
            {
                if (!File.Exists(MraWorldBuilder.ScenePath(c.scene))) continue;
                EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath(c.scene));
                Shot(Path.Combine(folder, $"{c.id}_chamber_1080.jpg"), c.size * .5f, Mathf.Max(c.size.y * .6f, c.size.x * .32f), 1920, 1080);
            }
        Debug.Log("[MraPolishCapture] wrote " + Path.GetFullPath(folder));
    }

    // The data spots: beats (spawn), doors, and route samples between beats.
    public static List<(string, Vector2, bool)> Spots(World world, Region r)
    {
        var spots = new List<(string, Vector2, bool)>();
        var doorNodes = new HashSet<string>(world.ChambersOf(r.id).Where(c => !c.InPlace).Select(c => c.entranceNode));
        foreach (var n in r.nodes)
            spots.Add(($"beat_{n.id}", MraWorldBuilder.Spawn(n) + new Vector2(0f, 1f), doorNodes.Contains(n.id)));
        foreach (var c in world.ChambersOf(r.id).Where(c => !c.InPlace))
            spots.Add(($"door_{c.id}", MraWorldBuilder.Pos(r.NodeById(c.entranceNode)) + new Vector2(2.5f, 1.5f), true));
        foreach (var e in r.edges)
        {
            Vector2 a = MraWorldBuilder.Pos(r.NodeById(e.source)), b = MraWorldBuilder.Pos(r.NodeById(e.target));
            int n = Mathf.Max(1, Mathf.RoundToInt((b.x - a.x) / 24f));
            for (int i = 1; i < n + 1; i++)
            {
                float t = i / (n + 1f);
                spots.Add(($"edge_{e.id}_{i}", Vector2.Lerp(a, b, t) + new Vector2(0f, 1.5f), false));
            }
        }
        return spots;
    }

    static float RouteY(List<Vector2> nodes, float x)
    {
        for (int i = 0; i < nodes.Count - 1; i++)
            if (x <= nodes[i + 1].x) return Mathf.Lerp(nodes[i].y, nodes[i + 1].y, Mathf.InverseLerp(nodes[i].x, nodes[i + 1].x, x));
        return nodes[nodes.Count - 1].y;
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
        File.WriteAllBytes(path, read.EncodeToJPG(88));
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(texture); Object.DestroyImmediate(read);
    }
}
