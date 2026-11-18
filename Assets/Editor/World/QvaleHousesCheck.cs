using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Qvale's homes in Play mode: from the street, each door leads into its room, its residents are inside and not outside,
// Qori stands on the floor, walking back into the painted door lands him outside that door. Saves a shot of each room.
// Run: -executeMethod QvaleHousesCheck.Run -saveFile houses_check.json -captureDir <dir> (no -quit). Exits 1 on any failure.
public static class QvaleHousesCheck
{
    const string StateKey = "Qolossal.QvaleHousesCheck";
    public static string Dir => SessionState.GetString(StateKey, "");

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("QvaleHousesCheck needs -saveFile"); EditorApplication.Exit(1); return; }
        int at = System.Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/QvaleHouses";
        Directory.CreateDirectory(dir);
        SessionState.SetString(StateKey, dir);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + "MRAtlas_MR04_Qvale.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (string.IsNullOrEmpty(Dir)) return;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(Dir))
                new GameObject("Qvale houses check").AddComponent<Driver>();
        };
    }

    sealed class Driver : MonoBehaviour
    {
        int failures, errors;
        readonly List<string> report = new List<string>();
        void Say(string s) { report.Add(s); Debug.Log("[QvaleHouses] " + s); }
        void Expect(bool ok, string what) { if (!ok) failures++; Say((ok ? "PASS " : "FAIL ") + what); }

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += (m, st, t) => { if (t == LogType.Exception || t == LogType.Error) { errors++; Debug.Log("[QvaleHouses] runtime error: " + m); } };
            yield return Wait(1.5f);
            var world = Mra.World.Load();
            var homes = world.ChambersOf("MR04").Where(c => c.IsHouse).ToArray();
            Expect(homes.Length == 5, "five homes in the world data");
            var street = FindObjectsByType<TownNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(n => n.name).ToArray();
            Say("INFO on the street: " + string.Join(", ", street));
            foreach (string gone in new[] { "NPC Sorrel", "NPC Scribble", "NPC Pip", "NPC Marrow", "NPC Grandfather Tallow" })
                Expect(!street.Contains(gone), gone + " isn't on the street");

            foreach (var home in homes)
            {
                var door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == home.id);
                Expect(door != null, home.id + " has a door");
                if (door == null) continue;
                Vector2 doorAt = door.transform.position;
                Expect(AreaTransition.CanTravelTo(home.scene), home.scene + " is in the build");
                Place(doorAt + Vector2.up * .6f);
                yield return Wait(.3f);
                Expect(door.Enter(), home.id + ": the door opens");
                yield return Until(() => SceneManager.GetActiveScene().name == home.scene && !AreaTransition.IsTransitioning, 15f);
                Expect(SceneManager.GetActiveScene().name == home.scene, home.id + ": arrives in its room");
                yield return Wait(1.5f);

                var qori = FindAnyObjectByType<PlayerMovement>();
                Expect(qori != null && qori.IsGrounded, home.id + ": Qori stands on the floor");
                var inside = FindObjectsByType<TownNpc>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(n => n.name).ToArray();
                Say($"INFO {home.id} {home.name}: {string.Join(", ", inside)}");
                Expect(inside.Length > 0 || home.id == "MR04_C06", home.id + ": someone lives here");
                Shot(Path.Combine(Dir, home.id + ".jpg"));

                var exit = FindAnyObjectByType<MraChamberReturn>();
                Expect(exit != null, home.id + ": the way out exists");
                if (exit == null) { SceneManager.LoadScene("MRAtlas_MR04_Qvale"); yield return Wait(2f); continue; }
                Place(exit.transform.position);
                yield return Until(() => SceneManager.GetActiveScene().name == "MRAtlas_MR04_Qvale" && !AreaTransition.IsTransitioning, 15f);
                Expect(SceneManager.GetActiveScene().name == "MRAtlas_MR04_Qvale", home.id + ": back on the street");
                yield return Wait(.6f);
                var back = FindAnyObjectByType<PlayerMovement>();
                float gap = back != null ? Vector2.Distance(back.transform.position, doorAt) : 999f;
                Expect(gap < 3f, $"{home.id}: lands outside its own door ({gap:0.0} u away)");
            }
            Say($"RESULT failures {failures}, runtime errors {errors}");
            File.WriteAllLines(Path.Combine(Dir, "houses_results.txt"), report);
            SessionState.EraseString(StateKey);
            EditorApplication.Exit(failures + errors > 0 ? 1 : 0);
        }

        static void Place(Vector2 at)
        {
            var qori = FindAnyObjectByType<PlayerMovement>();
            if (qori == null) return;
            var body = qori.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = at; body.linearVelocity = Vector2.zero; }
            qori.transform.position = at;
        }

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        static IEnumerator Until(System.Func<bool> done, float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < until) yield return null;
        }

        static void Shot(string path)
        {
            const int W = 960, H = 540;
            var view = Camera.main;
            if (view == null) return;
            var target = RenderTexture.GetTemporary(W, H, 24);
            float aspect = view.aspect;
            view.aspect = 16f / 9f; view.targetTexture = target; view.Render();
            RenderTexture.active = target;
            var read = new Texture2D(W, H, TextureFormat.RGB24, false);
            read.ReadPixels(new Rect(0, 0, W, H), 0, 0); read.Apply();
            File.WriteAllBytes(path, read.EncodeToJPG(85));
            view.targetTexture = null; view.aspect = aspect; RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target); Destroy(read);
        }
    }
}
