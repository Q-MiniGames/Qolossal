using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The scripted climb capture (polish plan, WS7): every surface region's background stack at 0, 15, 30,
// 50, 75 and 100% of its climb, on one comparable sheet (top row the frame as played, bottom row the
// planes alone), plus numbers per plane: where it sits in the frame, how much of the frame it fills,
// how far it sinks. It fails on what a player would see as a fault: a part of the frame no plane
// covers, or a plane's cut edge inside the frame. Imported-resource renders in the editor, not a
// device test; nothing is saved to the scenes.
// Usage: -executeMethod ClimbCapture.Run -climbTag <before|after> -captureDir <folder> [-mraOnly MR05]
public static class ClimbCapture
{
    static readonly float[] Stages = { 0f, .15f, .3f, .5f, .75f, 1f };
    const int CellW = 640, CellH = 360;
    const float NearestVertical = .15f;

    sealed class Art
    {
        public string asset; public int width, height;
        public float[] rowAlpha;   // mean alpha per sampled row, bottom to top
        public float topAlpha, bottomAlpha, wrap;
    }

    static readonly Dictionary<string, Art> Arts = new Dictionary<string, Art>();

    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string tag = Arg("-climbTag", "capture"), folder = Path.Combine(Arg("-captureDir", "Temp/Captures/climb"), tag), only = Arg("-mraOnly", "");
        Directory.CreateDirectory(folder);
        var report = new StringBuilder();
        report.AppendLine($"Climb capture '{tag}'. Editor renders of imported resources; says nothing about any console.");
        int faults = 0;
        var world = MraWorldBuilder.Prepare();
        foreach (var r in world.regions)
        {
            if (only != "" && !r.id.StartsWith(only)) continue;
            string path = MraWorldBuilder.ScenePath(r.scene);
            if (!File.Exists(path)) { report.AppendLine().AppendLine($"== {r.id}: no scene ({path})"); continue; }
            EditorSceneManager.OpenScene(path);
            faults += Region(r, folder, report);
        }
        report.AppendLine().AppendLine($"FAULTS {faults}");
        File.WriteAllText(Path.Combine(folder, "climb_report.txt"), report.ToString());
        Debug.Log($"[ClimbCapture] {faults} faults -> " + Path.GetFullPath(folder));
        EditorApplication.Exit(faults > 0 ? 1 : 0);
    }

    static int Region(Mra.Region r, string folder, StringBuilder report)
    {
        var camera = Camera.main;
        var layers = Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).OrderBy(l => l.sortingOrder).ToArray();
        var behind = layers.Where(l => l.sortingOrder < 0 && l.GetComponent<SeatedVistaFade>() == null).ToArray();
        var climbLayer = layers.FirstOrDefault(l => l.climbProgression);
        Vector2 climb = climbLayer != null ? climbLayer.climbRange : new Vector2(r.cameraMin.y, r.cameraMax.y);
        report.AppendLine().AppendLine($"== {r.id} {r.scene}: {behind.Length} planes behind the terrain, {layers.Length - behind.Length} other; climb {climb.x:0.#}..{climb.y:0.#} ({climb.y - climb.x:0.#} u), camera {r.cameraMin.y:0.#}..{r.cameraMax.y:0.#}");

        void Place(Vector2 at, float size)
        {
            camera.aspect = 16f / 9f; camera.transform.position = new Vector3(at.x, at.y, -10f); camera.orthographicSize = size;
            foreach (var fade in Object.FindObjectsByType<MraClimbFade>(FindObjectsSortMode.None)) fade.Apply();
            foreach (var layer in layers) layer.Refresh(camera);
        }
        Vector2 Beat(float p)
        {
            float y = Mathf.Lerp(climb.x, climb.y, p);
            var node = r.nodes.OrderBy(n => Mathf.Abs(MraWorldBuilder.Spawn(n).y + 1f - y)).First();
            return new Vector2(MraWorldBuilder.Spawn(node).x, y);
        }

        // Every other renderer can be hidden, for the planes alone and the gap test.
        // (The generated terrain meshes are hidden objects, so the plain search misses them.)
        var others = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(x => !EditorUtility.IsPersistent(x) && !behind.Any(l => x.transform == l.transform || x.transform.IsChildOf(l.transform))).ToArray();
        void Others(bool show) { foreach (var x in others) x.forceRenderingOff = !show; }

        // The sheet.
        var sheet = new Texture2D(CellW * Stages.Length, CellH * 2, TextureFormat.RGB24, false);
        for (int i = 0; i < Stages.Length; i++)
        {
            Place(Beat(Stages[i]), 5f);
            Others(true); Blit(sheet, camera, i, 1);
            Others(false); Blit(sheet, camera, i, 0);
        }
        File.WriteAllBytes(Path.Combine(folder, r.id + "_climb.jpg"), sheet.EncodeToJPG(85));
        Object.DestroyImmediate(sheet);

        // The gap test: planes alone over a colour nothing uses, along the climb, at both ends of the
        // region and at the widest view a lookout uses.
        int faults = 0, gapped = 0; string worstGap = "";
        var clearFlags = camera.clearFlags; var clear = camera.backgroundColor;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta;
        foreach (float size in new[] { 5f, 8f })
            for (int s = 0; s <= 12; s++)
                foreach (float x in new[] { r.cameraMin.x, (r.cameraMin.x + r.cameraMax.x) * .5f, r.cameraMax.x })
                {
                    Vector2 at = new Vector2(x, Mathf.Lerp(climb.x, climb.y, s / 12f));
                    Place(at, size);
                    float share = Uncovered(camera);
                    if (share <= .0005f) continue;
                    gapped++;
                    worstGap = $"{share:P1} at x {at.x:0}, y {at.y:0}, size {size:0}";
                }
        camera.clearFlags = clearFlags; camera.backgroundColor = clear;
        Others(true);
        report.AppendLine(gapped == 0 ? "PASS no part of the frame is uncovered (78 places, sizes 5 and 8)" : $"FAIL {gapped} of 78 places leave the frame uncovered; last: {worstGap}");
        if (gapped > 0) faults++;


        // Per plane, along the climb.
        var speeds = new List<(string name, float depth, float speed)>();
        report.AppendLine("plane | px/u | depth | rate | fit | limit | sinks | bottom in frame at 0/50/100% | fills frame % at 0/15/30/50/75/100");
        foreach (var layer in behind)
        {
            var art = Read(layer);
            if (art == null) continue;
            Place(Beat(0f), 5f);
            float h = layer.GetComponent<MeshFilter>().sharedMesh.bounds.max.y;
            var fills = new List<string>(); var bottoms = new List<string>();
            float topAt = float.NaN, bottomAt = float.NaN;
            foreach (float p in Stages)
            {
                Place(Beat(p), 5f);
                float bottom = layer.transform.position.y - camera.transform.position.y;
                float fill = Fill(art, bottom, h) * layer.tint.a;
                fills.Add($"{fill * 100f:0}");
                if (p == 0f || p == .5f || p == 1f) bottoms.Add($"{bottom:0.0}");
                // A cut edge: the painting's top or bottom row is not clear, inside the frame, and no band hides it.
                float top = bottom + h;
                if (layer.tint.a > .02f && Mathf.Abs(top) < 5f && art.topAlpha > .05f && float.IsNaN(topAt)) topAt = p;
                float low = bottom - (layer.extendBelow > 0f ? layer.extendBelow : 0f);
                if (layer.tint.a > .02f && low > -5f && low < 5f && (layer.extendBelow > 0f || art.bottomAlpha > .05f) && float.IsNaN(bottomAt)) bottomAt = p;
            }
            // Vertical parallax, measured from where the plane lands: how far it moves down the frame per 1 u the camera rises
            // (the terrain: 100%). Sampled low, middle and high, because a sink limit can stop a plane part way up.
            float Vertical(float p)
            {
                float y = Mathf.Lerp(climb.x, climb.y, p), x = Beat(p).x;
                Place(new Vector2(x, y), 5f); float a = layer.transform.position.y - camera.transform.position.y;
                Place(new Vector2(x, y + 1f), 5f); return a - (layer.transform.position.y - camera.transform.position.y);
            }
            float low5 = Vertical(.05f), middle = Vertical(.5f), high95 = Vertical(.95f);
            report.AppendLine($"     vertical: {low5:P1} / {middle:P1} / {high95:P1} of the terrain's speed at 5 / 50 / 95% of the climb");
            if (layer.depth >= 1f) speeds.Add((layer.name, layer.depth, middle));
            if (layer.depth >= 1f && middle > 0f && high95 <= 0f) report.AppendLine($"WARN {layer.name}: reaches its sink limit and stops moving vertically before the top");
            float sink = layer.climbProgression ? layer.Sink(climb.y) : climb.y - climb.x - (climb.y - climb.x) * layer.follow.y;
            report.AppendLine($"{layer.name} | {art.height / h:0} | {layer.depth:0.##} | {(layer.climbProgression ? layer.verticalRate.ToString("0.###") : "follow " + layer.follow.y.ToString("0.###"))} | budget {layer.climbBudget:0} | {(layer.sinkLimit >= 0f ? layer.sinkLimit.ToString("0.#") : "auto")} | {sink:0.#} u | {string.Join("/", bottoms)} | {string.Join(" ", fills)}");
            if (!float.IsNaN(topAt)) { faults++; report.AppendLine($"FAIL {layer.name}: its top edge shows in the frame at {topAt:P0} of the climb (top row {art.topAlpha:P0} opaque)"); }
            if (!float.IsNaN(bottomAt)) { faults++; report.AppendLine($"FAIL {layer.name}: its bottom edge shows in the frame at {bottomAt:P0} of the climb"); }
            if (layer.repeat && layer.FollowX < 1f && layer.uvRect.width >= 1f && art.wrap > .02f) report.AppendLine($"WARN {layer.name}: left and right edges differ by {art.wrap:P1}, a seam as it repeats");
        }

        // Nearer planes must move faster, and the nearest fast enough to read as depth during a climb.
        var ordered = speeds.OrderByDescending(s => s.depth).ToList();
        for (int i = 1; i < ordered.Count; i++)
            if (ordered[i].speed < ordered[i - 1].speed - .0001f) { faults++; report.AppendLine($"FAIL {ordered[i].name} is nearer than {ordered[i - 1].name} but moves slower vertically"); }
        if (ordered.Count > 0) report.AppendLine(ordered[ordered.Count - 1].speed >= NearestVertical
            ? $"PASS the nearest plane moves at {ordered[ordered.Count - 1].speed:P0} of the terrain vertically"
            : $"WARN the nearest plane moves at only {ordered[ordered.Count - 1].speed:P0} of the terrain vertically (the 4K stacks: 16-20%)");
        return faults;
    }

    // Reads the render straight into a cell of the sheet (row 1 on top).
    static void Blit(Texture2D sheet, Camera camera, int column, int row)
    {
        var texture = new RenderTexture(CellW, CellH, 24);
        camera.targetTexture = texture; camera.Render();
        RenderTexture.active = texture;
        sheet.ReadPixels(new Rect(0, 0, CellW, CellH), column * CellW, row * CellH);
        sheet.Apply();
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(texture);
    }

    static float Uncovered(Camera camera)
    {
        const int W = 240, H = 135;
        var texture = new RenderTexture(W, H, 24);
        camera.targetTexture = texture; camera.Render();
        RenderTexture.active = texture;
        var read = new Texture2D(W, H, TextureFormat.RGB24, false);
        read.ReadPixels(new Rect(0, 0, W, H), 0, 0); read.Apply();
        int count = read.GetPixels32().Count(c => c.r > 245 && c.g < 10 && c.b > 245);
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(texture); Object.DestroyImmediate(read);
        return count / (float)(W * H);
    }

    // The share of the frame (10 u tall, y -5..5 around the camera) the plane's painted pixels fill.
    static float Fill(Art art, float bottom, float h)
    {
        const int Rows = 40;
        float sum = 0f;
        for (int i = 0; i < Rows; i++)
        {
            float y = -5f + 10f * (i + .5f) / Rows;
            float v = (y - bottom) / h;
            if (v < 0f || v >= 1f) continue;
            sum += art.rowAlpha[Mathf.Min(art.rowAlpha.Length - 1, (int)(v * art.rowAlpha.Length))];
        }
        return sum / Rows;
    }

    // The painting's alpha by row, read from the source file (the imported copy isn't readable).
    static Art Read(ParallaxLayer layer)
    {
        var texture = layer.sprite != null ? layer.sprite.texture : layer.texture;
        if (texture == null) return null;
        string path = AssetDatabase.GetAssetPath(texture);
        Rect rect = layer.sprite != null
            ? new Rect(layer.sprite.rect.x / texture.width, layer.sprite.rect.y / texture.height, layer.sprite.rect.width / texture.width, layer.sprite.rect.height / texture.height)
            : layer.uvRect;
        if (layer.sprite == null && (rect.width <= 0f || rect.height <= 0f)) rect = new Rect(0f, 0f, 1f, 1f);
        string key = path + rect;
        if (Arts.TryGetValue(key, out var known)) return known;
        var read = new Texture2D(2, 2);
        read.LoadImage(File.ReadAllBytes(path));
        int x0 = Mathf.RoundToInt(rect.x * read.width), y0 = Mathf.RoundToInt(rect.y * read.height);
        int w = Mathf.Clamp(Mathf.RoundToInt(rect.width * read.width), 1, read.width - x0), h = Mathf.Clamp(Mathf.RoundToInt(rect.height * read.height), 1, read.height - y0);
        var pixels = read.GetPixels32();
        Color32 At(int x, int y) => pixels[(y0 + y) * read.width + x0 + x];
        const int Step = 8;
        var rows = new List<float>();
        for (int y = 0; y < h; y += Step)
        {
            long sum = 0; int n = 0;
            for (int x = 0; x < w; x += Step) { sum += At(x, y).a; n++; }
            rows.Add(sum / (255f * n));
        }
        float Row(int y) { long sum = 0; for (int x = 0; x < w; x++) sum += At(x, y).a; return sum / (255f * w); }
        float wrap = 0f;
        for (int y = 0; y < h; y++)
        {
            Color32 a = At(0, y), b = At(w - 1, y);
            wrap += (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)) * Mathf.Min(a.a, b.a) / 255f + Mathf.Abs(a.a - b.a);
        }
        var art = new Art { asset = path, width = w, height = h, rowAlpha = rows.ToArray(), topAlpha = Row(h - 1), bottomAlpha = Row(0), wrap = wrap / (h * 255f * 4f) };
        Object.DestroyImmediate(read);
        Arts[key] = art;
        return art;
    }
}
