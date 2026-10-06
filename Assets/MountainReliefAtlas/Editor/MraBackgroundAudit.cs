using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mra;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Background audit (read-only: scenes are opened, never saved). For every region it renders the
// gameplay framing at representative spots - the lowest and highest beats, the middle beat, the
// steepest climb, both camera boundaries, each lookout zone at its own zoom, an encounter - with
// Qori moved to the spot, at 1920x1080 (and 1280x720 for the key spots). Each spot also gets a
// "background only" render (terrain, props and characters hidden) to show the layer composition.
// It writes BACKGROUND_LAYERS.json: every layer's source size, on-screen height and magnification
// at 720p/1080p/1440p/2160p, its horizontal coverage across the region's camera range, and where
// its skyline sits on screen at each spot.
// Usage: -executeMethod MraBackgroundAudit.Run -captureDir <folder> [-mraOnly MR04]
public static class MraBackgroundAudit
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string folder = Path.Combine(Arg("-captureDir", "Temp/Captures"), "bg_audit");
        string only = Arg("-mraOnly", "");
        bool matte = System.Array.IndexOf(args, "-fgMatte") >= 0;
        Directory.CreateDirectory(folder);
        var world = MraWorldBuilder.Prepare();
        var json = new StringBuilder("{\n \"regions\": [\n");
        bool firstRegion = true;
        foreach (var r in world.regions)
        {
            if (only != "" && !only.Split(',').Any(o => r.id.StartsWith(o))) continue;
            EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath(r.scene));
            var spots = Spots(r);
            var layers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).OrderBy(l => l.sortingOrder).ToList();
            if (!firstRegion) json.Append(",\n"); firstRegion = false;
            json.Append($"  {{\"id\": \"{r.id}\", \"name\": \"{r.name}\", \"scene\": \"{r.scene}\", \"camera_x\": [{F(r.cameraMin.x)}, {F(r.cameraMax.x)}], \"camera_y\": [{F(r.cameraMin.y)}, {F(r.cameraMax.y)}],\n   \"layers\": [\n");
            json.Append(string.Join(",\n", layers.Select(l => "    " + LayerJson(l, r))));
            json.Append("\n   ],\n   \"spots\": [\n");
            var spotRows = new List<string>();
            foreach (var (label, at, size, wide) in spots)
            {
                PlaceQori(r, at);
                string stem = $"{r.id}_{label}";
                Shot(Path.Combine(folder, stem + "_1080.png"), at, size, 1920, 1080, false);
                Shot(Path.Combine(folder, stem + "_bgonly_1080.png"), at, size, 1920, 1080, true);
                if (wide) Shot(Path.Combine(folder, stem + "_720.png"), at, size, 1280, 720, false);
                if (matte)
                {
                    // The foreground alone, over black and over white (a difference matte for mockups).
                    Foreground(Path.Combine(folder, stem + "_fg_black.png"), at, size, Color.black);
                    Foreground(Path.Combine(folder, stem + "_fg_white.png"), at, size, Color.white);
                }
                spotRows.Add($"    {{\"label\": \"{label}\", \"at\": [{F(at.x)}, {F(at.y)}], \"size\": {F(size)}, \"layers\": [" +
                    string.Join(", ", layers.Select(l => OnScreen(l, at, size))) + "]}");
            }
            json.Append(string.Join(",\n", spotRows)).Append("\n   ]}");
            // Resolution probe: the middle beat at 2560x1440 and 3840x2160 for 1:1 crops.
            if (r.id == "MR04" || r.id == "MR05" || r.id == "MR03")
            {
                var mid = spots.First(s => s.Item1 == "mid");
                PlaceQori(r, mid.Item2);
                Shot(Path.Combine(folder, $"{r.id}_mid_1440.png"), mid.Item2, mid.Item3, 2560, 1440, false);
                Shot(Path.Combine(folder, $"{r.id}_mid_2160.png"), mid.Item2, mid.Item3, 3840, 2160, false);
            }
        }
        json.Append("\n ]\n}\n");
        File.WriteAllText(Path.Combine(folder, "BACKGROUND_LAYERS.json"), json.ToString());
        Debug.Log("[MraBackgroundAudit] wrote " + Path.GetFullPath(folder));
    }

    static string F(float v) => v.ToString("0.###", Inv);

    // Spots: (label, camera centre, orthographic size, also at 720p).
    static List<(string, Vector2, float, bool)> Spots(Region r)
    {
        var s = new List<(string, Vector2, float, bool)>();
        var nodes = r.nodes.OrderBy(n => MraWorldBuilder.Pos(n).y).ToList();
        Vector2 Cam(Node n) => MraWorldBuilder.Spawn(n) + new Vector2(0f, 1f);
        s.Add(("low", Cam(nodes[0]), 5f, false));
        s.Add(("mid", Cam(nodes[nodes.Count / 2]), 5f, true));
        s.Add(("high", Cam(nodes[nodes.Count - 1]), 5f, false));
        // The steepest edge, half way up.
        var steep = r.edges.OrderByDescending(e =>
        {
            Vector2 a = MraWorldBuilder.Pos(r.NodeById(e.source)), b = MraWorldBuilder.Pos(r.NodeById(e.target));
            return Mathf.Abs(b.y - a.y) / Mathf.Max(1f, Mathf.Abs(b.x - a.x));
        }).FirstOrDefault();
        if (steep != null)
        {
            Vector2 a = MraWorldBuilder.Pos(r.NodeById(steep.source)), b = MraWorldBuilder.Pos(r.NodeById(steep.target));
            s.Add(("climb", (a + b) * .5f + new Vector2(0f, 1.5f), 5f, true));
        }
        // The camera's limits: both ends at the route's height there, and the lowest/highest the camera goes.
        var byX = r.nodes.OrderBy(n => MraWorldBuilder.Pos(n).x).ToList();
        float halfW = 5f * 16f / 9f;
        s.Add(("west_edge", new Vector2(r.cameraMin.x + halfW, Cam(byX[0]).y), 5f, false));
        s.Add(("east_edge", new Vector2(r.cameraMax.x - halfW, Cam(byX[byX.Count - 1]).y), 5f, false));
        // Lookouts and the street zoom, at their own size and lift.
        int v = 0;
        foreach (var zone in Object.FindObjectsByType<VistaZone>(FindObjectsSortMode.None).OrderBy(z => z.transform.position.x))
        {
            var box = zone.GetComponent<BoxCollider2D>();
            Vector2 c = box != null ? (Vector2)box.bounds.center : (Vector2)zone.transform.position;
            float ground = c.y - (box != null ? box.bounds.extents.y : 0f);
            s.Add(($"vista{v++}", new Vector2(c.x, ground + 1f + zone.lift), zone.size, true));
        }
        // The listening overlook's seated framing.
        foreach (var spot in Object.FindObjectsByType<ListeningSpot>(FindObjectsSortMode.None))
            s.Add(("overlook_seated", spot.frameCentre, spot.frameSize, true));
        // An encounter (separation of enemies from the background).
        var enc = GameObject.Find(MraWorldBuilderNames.Generated + "/Encounters");
        if (enc != null && enc.transform.childCount > 0)
        {
            var e = enc.transform.GetChild(0);
            s.Add(("encounter", (Vector2)e.position + new Vector2(-2f, .8f), 5f, true));
        }
        return s;
    }

    static void PlaceQori(Region r, Vector2 cameraAt)
    {
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null) return;
        // On the ground under the camera's spot (1 u below the framing centre when on a beat).
        var hit = Physics2D.Raycast(cameraAt + Vector2.up * 2f, Vector2.down, 30f, LayerMask.GetMask("Ground"));
        Vector2 feet = hit ? hit.point : cameraAt - Vector2.up;
        var col = player.GetComponent<Collider2D>();
        float offset = col != null ? player.transform.position.y - col.bounds.min.y : .5f;
        player.transform.position = new Vector3(cameraAt.x - 1.2f, feet.y + offset, player.transform.position.z);
        Physics2D.SyncTransforms();
    }

    static string LayerJson(ParallaxLayer l, Region r)
    {
        var tex = l.sprite != null ? l.sprite.texture : l.texture;
        string src = tex != null ? AssetDatabase.GetAssetPath(tex) : "";
        int rows = l.sprite != null ? (int)l.sprite.rect.height : tex != null ? tex.height : 0;
        int cols = l.sprite != null ? (int)l.sprite.rect.width : tex != null ? tex.width : 0;
        float h = l.height > 0f ? l.height : l.sprite != null ? l.sprite.rect.height / l.sprite.pixelsPerUnit : 0f;
        float w = rows > 0 ? h * cols / rows : 0f;
        // Screen pixels per source pixel at 10 u of view height (size 5) - constant under zoom, since
        // composedForSize scales the layer with the view.
        string Mag(int screenH) => F(rows > 0 ? h * screenH / 10f / rows : 0f);
        // Non-repeating: the camera x range over which the painting fills the full 16:9 view width.
        string coverage = "null";
        if (!l.repeat && w > 0f)
        {
            float halfW = 5f * 16f / 9f, drift = 1f - l.FollowX;
            // drift(cam) = baseX + cam*(follow-1) = baseX - cam*drift; covered while |drift(cam)| <= w/2 - halfW.
            float slack = w * .5f - halfW;
            if (slack <= 0f) coverage = "[0, 0, 0]";
            else
            {
                float lo = drift > 1e-4f ? (l.baseX - slack) / drift : float.NegativeInfinity, hi = drift > 1e-4f ? (l.baseX + slack) / drift : float.PositiveInfinity;
                float a = Mathf.Max(lo, r.cameraMin.x), b = Mathf.Min(hi, r.cameraMax.x);
                float share = Mathf.Clamp01((b - a) / Mathf.Max(1f, r.cameraMax.x - r.cameraMin.x));
                coverage = $"[{F(a)}, {F(b)}, {F(share)}]";
            }
        }
        return $"{{\"name\": \"{l.name}\", \"source\": \"{src}\", \"src_w\": {cols}, \"src_h\": {rows}, \"world_h\": {F(h)}, \"world_w\": {F(w)}, \"repeat\": {(l.repeat ? "true" : "false")}, " +
               $"\"depth\": {F(l.depth)}, \"follow\": [{F(l.FollowX)}, {F(l.follow.y)}], \"climb\": {(l.climbProgression ? "true" : "false")}, \"climb_shift\": {F(l.climbShift)}, \"base_y\": {F(l.baseY)}, \"order\": {l.sortingOrder}, " +
               $"\"tint\": [{F(l.tint.r)}, {F(l.tint.g)}, {F(l.tint.b)}], \"mag_720\": {Mag(720)}, \"mag_1080\": {Mag(1080)}, \"mag_1440\": {Mag(1440)}, \"mag_2160\": {Mag(2160)}, \"full_cover_camera_x\": {coverage}}}";
    }

    // The layer's painted band on screen at this spot: bottom and top as a share of the frame height.
    static string OnScreen(ParallaxLayer l, Vector2 at, float size)
    {
        var cam = Camera.main; cam.aspect = 16f / 9f; cam.orthographicSize = size;
        cam.transform.position = new Vector3(at.x, at.y, -10f);
        l.Refresh(cam);
        var mr = l.GetComponent<MeshRenderer>();
        if (mr == null || !mr.enabled) return "null";
        var b = mr.bounds;
        float bottom = cam.transform.position.y - size;
        float Share(float y) => (y - bottom) / (2f * size);
        float visibleW = Mathf.Clamp01(b.size.x / (2f * size * cam.aspect));
        return $"[{F(Share(b.min.y))}, {F(Share(b.max.y))}, {F(visibleW)}]";
    }

    // Gameplay layers only (terrain, props, characters): every ParallaxLayer and the haze veil off,
    // over a flat colour.
    static void Foreground(string path, Vector2 at, float size, Color clear)
    {
        var camera = Camera.main;
        var flags = camera.clearFlags; var bg = camera.backgroundColor;
        var layers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);
        foreach (var l in layers) l.enabled = false;
        var veils = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Haze veil") && r.enabled).ToList();
        foreach (var v in veils) v.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = clear;
        Shot(path, at, size, 1920, 1080, false);
        camera.clearFlags = flags; camera.backgroundColor = bg;
        foreach (var v in veils) v.enabled = true;
        foreach (var l in layers) l.enabled = true;
    }

    static void Shot(string path, Vector2 at, float size, int w, int h, bool backgroundOnly)
    {
        var camera = Camera.main; camera.aspect = (float)w / h;
        camera.transform.position = new Vector3(at.x, at.y, -10f); camera.orthographicSize = size;
        foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var hidden = new List<Renderer>();
        if (backgroundOnly)
            foreach (var rend in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                bool keep = rend.GetComponentInParent<ParallaxLayer>() != null || rend.name.StartsWith("Haze veil");
                if (!keep && rend.enabled) { rend.enabled = false; hidden.Add(rend); }
            }
        var texture = new RenderTexture(w, h, 24);
        camera.targetTexture = texture; camera.Render();
        RenderTexture.active = texture;
        var read = new Texture2D(w, h, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply();
        File.WriteAllBytes(path, read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(texture); Object.DestroyImmediate(read);
        foreach (var rend in hidden) rend.enabled = true;
    }
}
