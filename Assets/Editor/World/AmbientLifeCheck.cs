using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// The filmed census and check for the ambient layer (polish plan, WS5), in each real scene in Play mode: frames of the
// same places without and with it, how much of the picture moves, what it found, and that it left every collider alone.
// Run: -executeMethod AmbientLifeCheck.Run -saveFile ambient_check.json -captureDir <dir> [-mraOnly MR05] (no -quit).
// Exits 1 on any failure. Editor renders: says nothing of feel, or of any console.
public static class AmbientLifeCheck
{
    const string StateKey = "Qolossal.AmbientLifeCheck", OnlyKey = "Qolossal.AmbientLifeCheck.Only";
    public static string Dir => SessionState.GetString(StateKey, "");

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("AmbientLifeCheck needs -saveFile"); EditorApplication.Exit(1); return; }
        string Arg(string key, string fallback) { int i = System.Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        string dir = Arg("-captureDir", "Temp/AmbientLife");
        Directory.CreateDirectory(dir);
        SessionState.SetString(StateKey, dir);
        SessionState.SetString(OnlyKey, Arg("-mraOnly", ""));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (string.IsNullOrEmpty(Dir)) return;
        AmbientLife.AutoStart = false;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(Dir))
                new GameObject("Ambient life check").AddComponent<Driver>();
        };
    }

    sealed class Driver : MonoBehaviour
    {
        const int W = 960, H = 540, FilmFrames = 8;
        int failures, errors;
        readonly List<string> report = new List<string>();
        Camera view;
        void Say(string s) { report.Add(s); Debug.Log("[AmbientLife] " + s); }
        void Expect(bool ok, string what) { if (!ok) failures++; Say((ok ? "PASS " : "FAIL ") + what); }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += (m, st, t) =>
            {
                if (t != LogType.Exception && t != LogType.Error) return;
                if (m.Contains("[AmbientLife]")) return;
                errors++; Debug.Log("[AmbientLife] runtime error: " + m);
            };
            string only = SessionState.GetString(OnlyKey, "");
            foreach (var path in RegionalSceneCatalog.BuildPaths.Where(p => !RegionalSceneCatalog.Interiors.Contains(Path.GetFileNameWithoutExtension(p))).OrderBy(p => p))
            {
                string scene = Path.GetFileNameWithoutExtension(path);
                if (only != "" && !scene.Contains(only)) continue;
                yield return Region(path, scene);
            }
            Say($"RESULT failures {failures}, runtime errors {errors}");
            File.WriteAllLines(Path.Combine(Dir, "ambient_results.txt"), report);
            SessionState.EraseString(StateKey);
            EditorApplication.Exit(failures + errors > 0 ? 1 : 0);
        }

        IEnumerator Region(string path, string scene)
        {
            var load = EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return Wait(1f);
            view = Camera.main;
            if (view == null) { Expect(false, scene + ": no camera"); yield break; }
            foreach (var b in view.GetComponents<MonoBehaviour>()) b.enabled = false;
            view.aspect = 16f / 9f;
            bool cave = scene.Contains("Underground");
            if (cave) foreach (var r in FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None)) r.gameObject.SetActive(true);
            view.orthographicSize = 5f;
            var qori = FindAnyObjectByType<PlayerMovement>();
            var sites = Sites(cave);
            Say($"== {scene}: {sites.Count} places");

            var colliders = Collide();
            var before = new Dictionary<string, float>();
            foreach (var (label, at) in sites)
            {
                Place(qori, at);
                view.transform.position = new Vector3(at.x, at.y, -10f);
                yield return Wait(4f);
                var first = Grab(at);
                yield return Wait(2f);
                var second = Grab(at);
                before[label] = Moving(first, second);
                Save(Path.Combine(Dir, $"{scene}_{label}_before.jpg"), second);
            }

            var life = AmbientLife.Create();
            yield return null;
            Expect(life != null && life.enabled, scene + ": the layer starts");
            float cost = 0f; int costs = 0, peak = 0;
            foreach (var (label, at) in sites)
            {
                Place(qori, at);
                view.transform.position = new Vector3(at.x, at.y, -10f);
                yield return Wait(4f);
                var first = Grab(at);
                Color32[] last = first;
                for (int i = 0; i < FilmFrames; i++)
                {
                    var frame = Grab(at);
                    Save(Path.Combine(Dir, $"{scene}_{label}_after_{i:00}.jpg"), frame);
                    last = frame;
                    cost += life.CostMs; costs++; peak = Mathf.Max(peak, life.LiveParticles);
                    yield return Wait(.25f);
                }
                float after = Moving(first, last);
                Say($"INFO {label} at ({at.x:0.0}, {at.y:0.0}): picture moving {before[label]:0.0}% before, {after:0.0}% after");
                // Not "more than before": residents walking and enemies moving already count, by chance, in either pass.
                Expect(after > .2f, $"{scene} {label}: the picture moves");
            }
            Say($"INFO {life.ProfileName}: {life.SwayCount} plants, {life.WaterCount} water, {life.ThornCount} thorns, {life.GustCount} gust lanes, {life.ShrineCount} shrines, {life.LampCount} lamps; particles peak {peak} of {life.MaxParticles}; layer's own script {cost / Mathf.Max(1, costs):0.000} ms a frame");
            Expect(Collide().SequenceEqual(colliders), scene + ": every collider where it was");
            Expect(life.MaxParticles <= 260, scene + ": particle budget");
            if (cave)
            {
                Expect(life.ThornCount == 76, "all 76 thorns stir");
                Expect(life.GustCount == 19, "all 19 gust lanes have streaks");
                Expect(life.WaterCount >= 14, "water moves");
                Expect(life.ShrineCount >= 80, "rest shrines flicker");
            }
            else
            {
                Expect(life.SwayCount > 0, scene + ": plants sway");
                Expect(life.ShrineCount > 0, scene + ": shrines flicker");
            }
            if (scene.Contains("Causeway")) yield return Tiers();
            Destroy(AmbientLife.Instance.gameObject);
            yield return null;
        }

        // The lower tiers on a scene: they start, spend less, and raise no error.
        IEnumerator Tiers()
        {
            foreach (int tier in new[] { 1, 2 })
            {
                Destroy(AmbientLife.Instance.gameObject);
                yield return null;
                AmbientLife.Tier = tier;
                var life = AmbientLife.Create();
                yield return Wait(2f);
                Say($"INFO tier {tier}: particles {life.LiveParticles} of {life.MaxParticles}");
                Expect(life.MaxParticles < 260, $"tier {tier} starts and stays in budget");
            }
            AmbientLife.Tier = 0;
        }

        List<(string, Vector2)> Sites(bool cave)
        {
            var result = new List<(string, Vector2)>();
            var art = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.sprite != null && r.gameObject.activeInHierarchy).ToArray();
            void Add(string label, System.Func<string, bool> pick, float up = 1.5f)
            {
                // Dressing at order -27 and under can sit behind the terrain: prefer what's surely in view.
                var chosen = art.Where(r => pick(r.sprite.name.ToLowerInvariant())).ToList();
                var seen = chosen.Where(r => r.sortingOrder > -27 || r.name.StartsWith("Hanging")).ToList();
                var points = (seen.Count > 0 ? seen : chosen).Select(r => (Vector2)r.bounds.center).ToList();
                if (points.Count == 0) return;
                var best = points.OrderByDescending(p => points.Count(o => Vector2.Distance(o, p) < 7f)).First();
                result.Add((label, best + Vector2.up * up));
            }
            Add("plants", n => n.Contains("fern") || n.Contains("grass") || n.Contains("hanging") || n.Contains("reed") || n.Contains("flowers") || n.Contains("lash_root"));
            Add("shrine", n => n.StartsWith("checkpoint_shrine") || n.Contains("streetlamp_glow"), .5f);
            if (cave)
            {
                Add("thorns", n => n.StartsWith("thorns_"));
                Add("water", n => n.StartsWith("water_0"));
                Add("spill", n => n.StartsWith("spill_"));
                Add("gust", n => n.StartsWith("gust_"), 0f);
            }
            return result;
        }

        static void Place(PlayerMovement qori, Vector2 at)
        {
            if (qori == null) return;
            // Out of the physics, so he can't fall, take a door or be hurt while he stands in the shot.
            var body = qori.GetComponent<Rigidbody2D>();
            if (body != null) { body.simulated = false; body.linearVelocity = Vector2.zero; }
            qori.transform.position = at;
        }

        static string[] Collide() => FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.GetComponentInParent<PlayerMovement>() == null && c.attachedRigidbody == null)
            .Select(c => c.name + c.bounds.center.ToString("0.00") + c.bounds.size.ToString("0.00") + c.isTrigger).OrderBy(s => s, System.StringComparer.Ordinal).ToArray();

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
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

        static float Moving(Color32[] a, Color32[] b)
        {
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
                if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g) + Mathf.Abs(a[i].b - b[i].b) > 8) changed++;
            return 100f * changed / a.Length;
        }

        static void Save(string path, Color32[] pixels)
        {
            var read = new Texture2D(W, H, TextureFormat.RGB24, false);
            read.SetPixels32(pixels); read.Apply();
            File.WriteAllBytes(path, read.EncodeToJPG(82));
            Destroy(read);
        }
    }
}
