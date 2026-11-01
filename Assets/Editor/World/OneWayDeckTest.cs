using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod OneWayDeckTest.Run -saveFile ugp_deck.json -captureDir <dir> (no -quit).
// Throws Qori up from under a one-way deck so his rise peaks at set heights around it, as a jump would, and writes where he
// comes to rest: if his feet never clear the top he should fall away; only with his feet above the top should he stand.
public static class OneWayDeckTest
{
    const string Key = "UGP.DeckTest.Dir";
    static readonly float[] Depths = { -1f, -.6f, -.4f, -.32f, -.28f, -.24f, -.2f, -.16f, -.12f, -.08f, -.04f, 0f, .04f, .1f, .2f, .4f };   // where his feet should peak, relative to the deck's top
    static string dir; static int step = -1; static float at, peak; static Collider2D deck;
    static readonly List<string> report = new List<string>();

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("UGP test needs -saveFile"); EditorApplication.Exit(1); return; }
        Directory.CreateDirectory(args[System.Array.IndexOf(args, "-captureDir") + 1]);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        SessionState.SetString(Key, args[System.Array.IndexOf(args, "-captureDir") + 1]);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod] static void Resume()
    {
        dir = SessionState.GetString(Key, "");
        if (!string.IsNullOrEmpty(dir)) EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < 3f) return;
        var qori = Object.FindAnyObjectByType<PlayerMovement>();
        if (qori == null) return;
        if (deck == null)
        {
            var room = Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).First(r => r.roomId == "A02");
            deck = room.GetComponentsInChildren<PlatformEffector2D>().Select(e => e.GetComponent<Collider2D>()).Where(c => c.bounds.size.x >= 5f).OrderByDescending(c => c.bounds.size.x).First();
        }
        var box = qori.GetComponent<BoxCollider2D>();
        if (step >= 0) peak = Mathf.Max(peak, box.bounds.min.y);
        if (step >= 0 && Time.time - at < 1.1f) return;
        if (step >= 0)
        {
            float feet = box.bounds.min.y, top = deck.bounds.max.y;
            report.Add($"aimed {Depths[step]:0.00}: peaked {peak - top:0.00} from the top; after 1.1 s feet {feet - top:0.00} from the top, grounded {qori.IsGrounded}");
        }
        step++;
        if (step >= Depths.Length)
        {
            report.Insert(0, $"deck thickness {deck.bounds.size.y:0.00}");
            File.WriteAllLines(Path.Combine(dir, "one_way_deck.txt"), report);
            EditorApplication.update -= Tick; SessionState.EraseString(Key);
            EditorApplication.ExitPlaymode(); EditorApplication.Exit(0); return;
        }
        float offset = qori.transform.position.y - box.bounds.min.y;
        // A jump's rise is 2.39 u at 12 u/s (measured); start that far under the aimed peak.
        Vector2 spot = new Vector2(deck.bounds.center.x, deck.bounds.max.y + Depths[step] - 2.39f + offset);
        var b = qori.GetComponent<Rigidbody2D>(); b.position = spot; b.linearVelocity = new Vector2(0f, 12f); qori.transform.position = spot;
        peak = float.NegativeInfinity;
        Physics2D.SyncTransforms();
        at = Time.time;
    }
}
