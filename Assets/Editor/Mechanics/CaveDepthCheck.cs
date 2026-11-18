using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The caves' depth layer in the real scene, in Play mode (WS7): every backdrop converted, each moving at its rate
// (measured from renders, not read from the code), no holes, and shots of rooms at two camera places.
// Run: -executeMethod CaveDepthCheck.Run -saveFile cavedepth_check.json -captureDir <dir> (no -quit). Exits 1 on any failure.
public static class CaveDepthCheck
{
    const string StateKey = "Qolossal.CaveDepthCheck";
    public static string Dir => SessionState.GetString(StateKey, "");

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("CaveDepthCheck needs -saveFile"); EditorApplication.Exit(1); return; }
        int at = System.Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/CaveDepth";
        Directory.CreateDirectory(dir);
        SessionState.SetString(StateKey, dir);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + "Qolossal_Underground.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (string.IsNullOrEmpty(Dir)) return;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(Dir))
                new GameObject("Cave depth check").AddComponent<Driver>();
        };
    }

    sealed class Driver : MonoBehaviour
    {
        const int W = 960, H = 540;
        int failures, errors;
        readonly List<string> report = new List<string>();
        Camera view;
        void Say(string s) { report.Add(s); Debug.Log("[CaveDepth] " + s); }
        void Expect(bool ok, string what) { if (!ok) failures++; Say((ok ? "PASS " : "FAIL ") + what); }

        IEnumerator Start()
        {
            Application.logMessageReceived += (m, st, t) => { if (t == LogType.Exception || t == LogType.Error) { errors++; Debug.Log("[CaveDepth] runtime error: " + m); } };
            float until = Time.realtimeSinceStartup + 30f;
            while (CaveDepth.Instance == null) { if (Time.realtimeSinceStartup > until) break; yield return null; }
            var depth = CaveDepth.Instance;
            Expect(depth != null, "the depth layer starts by itself");
            if (depth == null) { Finish(); yield break; }
            var all = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var far = all.Where(a => a.name == "UGP backdrop_far").ToArray();
            var near = all.Where(a => a.name == "UGP backdrop_near").ToArray();
            var passages = all.Where(a => a.name == "UGP passage backdrop").ToArray();
            Say($"INFO {far.Length} far, {near.Length} near, {passages.Length} passage and sheet backdrops; {depth.Converted} converted, {depth.Skipped} skipped, {far.Concat(near).Concat(passages).Select(a => a.sharedMaterial).Distinct().Count()} materials");
            Expect(depth.Skipped == 0 && depth.Converted == far.Length + near.Length + passages.Length, "every backdrop uses the depth layer");

            view = Camera.main;
            var rooms = FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(r => r.roomId).ToArray();
            foreach (var r in rooms) r.gameObject.SetActive(true);
            var keep = new HashSet<Renderer>(far.Concat(near).Concat(passages));
            var others = Resources.FindObjectsOfTypeAll<Renderer>().Where(x => !EditorUtility.IsPersistent(x) && !keep.Contains(x)).ToList();

            // The room shots, as played: the terrain moves 6 u between the two, the backdrops less.
            foreach (var follow in view.GetComponents<MonoBehaviour>().Where(b => !(b is CaveDepth))) follow.enabled = false;
            foreach (string id in new[] { "A01", "D03", "E05" })
            {
                var room = rooms.FirstOrDefault(r => r.roomId.EndsWith(id));
                if (room == null) continue;
                var c = room.WorldBounds.center;
                Shot(Path.Combine(Dir, $"{id}_left.jpg"), c + Vector3.left * 3f);
                Shot(Path.Combine(Dir, $"{id}_right.jpg"), c + Vector3.right * 3f);
            }

            foreach (var x in others) x.forceRenderingOff = true;

            // The speed, per layer: how far the picture moves on screen when the camera moves 2 u (the terrain: 2 u).
            var bad = new List<string>();
            int samples = 0;
            foreach (var (label, set, other, rate) in new[] { ("far", far, near, CaveDepth.FarRate), ("near", near, far, CaveDepth.NearRate) })
            {
                foreach (var x in other.Concat(passages)) x.forceRenderingOff = true;
                foreach (var x in set) x.forceRenderingOff = false;
                int seenRooms = 0;
                foreach (var art in set)
                {
                    var box = art.bounds;
                    if (box.size.x < 21f || box.size.y < 13f || seenRooms++ % 4 != 0) continue;
                    float unit = view.orthographicSize * 2f / H, want = 2f * (1f - rate);
                    var a = Grab(box.center - Vector3.right); var b = Grab(box.center + Vector3.right);
                    float across = BestShift(a, b, true) * unit;
                    a = Grab(box.center - Vector3.up); b = Grab(box.center + Vector3.up);
                    float up = BestShift(a, b, false) * unit;
                    samples++;
                    if (Mathf.Abs(across - want) > .12f) bad.Add($"{art.transform.parent.parent.name} {label}: moved {across:0.00} u across, wanted {want:0.00}");
                    if (Mathf.Abs(up - want) > .12f) bad.Add($"{art.transform.parent.parent.name} {label}: moved {up:0.00} u vertically, wanted {want:0.00}");
                }
                foreach (var x in set.Concat(other).Concat(passages)) x.forceRenderingOff = false;
            }
            Expect(samples > 0 && bad.Count == 0, $"each layer moves on screen at its rate, across and vertically: far {1f - CaveDepth.FarRate:0.##}, near {1f - CaveDepth.NearRate:0.##} of the terrain ({samples} rooms measured, {bad.Count} wrong)");
            foreach (var b in bad.Take(20)) Say("     " + b);

            // No backdrop leaves a hole with the camera anywhere in a room.
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = Color.magenta;
            var holes = new List<string>();
            foreach (var room in rooms)
            {
                var b = room.WorldBounds;
                foreach (var c in new[] { b.center, b.min + new Vector3(4f, 3f), b.max - new Vector3(4f, 3f) })
                {
                    var frame = Grab(new Vector3(c.x, c.y, 0f));
                    int hole = frame.Count(p => p.r > 245 && p.g < 10 && p.b > 245);
                    if (hole > 0) holes.Add($"{room.roomId} at {c.x:0},{c.y:0}: {hole * 100f / frame.Length:0.0}% uncovered");
                }
            }
            Expect(holes.Count == 0, $"no hole in the backdrops ({rooms.Length * 3} places, {holes.Count} with one)");
            foreach (var h in holes.Take(20)) Say("     " + h);
            Finish();
        }

        Color32[] Grab(Vector3 at)
        {
            view.transform.position = new Vector3(at.x, at.y, -10f);
            CaveDepth.Apply(view);
            var target = RenderTexture.GetTemporary(W, H, 24);
            view.targetTexture = target; view.Render();
            RenderTexture.active = target;
            var read = new Texture2D(W, H, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, W, H), 0, 0); read.Apply();
            var pixels = read.GetPixels32();
            view.targetTexture = null; RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target); Destroy(read);
            return pixels;
        }

        void Shot(string path, Vector3 at)
        {
            var pixels = Grab(at);
            var read = new Texture2D(W, H, TextureFormat.RGB24, false);
            read.SetPixels32(pixels); read.Apply();
            File.WriteAllBytes(path, read.EncodeToJPG(85));
            Destroy(read);
        }

        // The shift (pixels) that best lines the two frames up: the picture moves left or down as the camera moves right or up.
        static float BestShift(Color32[] a, Color32[] b, bool across)
        {
            int best = 0; float bestScore = float.MaxValue;
            for (int shift = 0; shift < H / 3; shift++)
            {
                long score = 0; int n = 0;
                int dx = across ? shift : 0, dy = across ? 0 : shift;
                for (int y = H / 6 + dy; y < H * 5 / 6; y += 3)
                    for (int x = W / 6 + dx; x < W * 5 / 6; x += 3)
                    {
                        Color32 p = a[y * W + x], q = b[(y - dy) * W + x - dx];
                        score += System.Math.Abs(p.r - q.r) + System.Math.Abs(p.g - q.g) + System.Math.Abs(p.b - q.b);
                        n++;
                    }
                float mean = score / (float)n;
                if (mean < bestScore) { bestScore = mean; best = shift; }
            }
            return best;
        }

        void Finish()
        {
            Say($"RESULT failures {failures}, runtime errors {errors}");
            File.WriteAllLines(Path.Combine(Dir, "cave_depth_results.txt"), report);
            SessionState.EraseString(StateKey);
            EditorApplication.Exit(failures + errors > 0 ? 1 : 0);
        }
    }
}
