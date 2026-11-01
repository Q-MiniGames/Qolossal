using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod UndergroundHitFeelTest.Run -saveFile ugp_feel.json -captureDir <dir> (no -quit).
// Lands one hit on a walker, a rooted creature, a flyer and a boss's open weak point, and renders the frames just after,
// so the white blink, the squash, the bars and the push can be looked at. Writes hit_feel.txt with the numbers measured.
public static class UndergroundHitFeelTest
{
    const string Key = "UGP.HitFeelTest.Dir";
    static string dir; static int index = -1, stage; static float at; static EnemyBase e; static Vector2 before; static List<EnemyBase> list;
    static readonly List<string> report = new List<string>();

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("UGP test needs -saveFile"); EditorApplication.Exit(1); return; }
        Directory.CreateDirectory(args[System.Array.IndexOf(args, "-captureDir") + 1]);
        GameSave.Clear();
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
        qori.GetComponent<PlayerHealth>().Heal(99);
        if (list == null)
        {
            var all = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            list = new[] { all.FirstOrDefault(x => x is SluiceCrabEnemy), all.FirstOrDefault(x => x is RootSnapperEnemy), all.FirstOrDefault(x => x is PumpBloaterEnemy), all.FirstOrDefault(x => x is GrainGolemBoss), all.FirstOrDefault(x => x is SluiceWardenBoss) }.Where(x => x != null).ToList();
            Next(qori); return;
        }
        if (e == null) { Next(qori); return; }
        float t = Time.realtimeSinceStartup - at;
        var g = e as UndergroundGuardian;
        switch (stage)
        {
            case 0:   // bring Qori near, wait for a boss's window
                qori.transform.position = e.transform.position + new Vector3(3f, 1f, 0f); qori.GetComponent<Rigidbody2D>().position = qori.transform.position;
                if (g != null && !g.WeakOpen) return;
                if (g == null && t < .5f) return;
                before = e.transform.position; report.Add($"{e.name}: health {e.HealthFraction:0.00} before");
                var blow = new CombatDamage { Damage = 1, Direction = Vector2.left, Point = (Vector2)e.transform.position + Vector2.up };
                if (g != null) g.HitWeak(blow); else e.ReceiveCombatHit(blow);
                at = Time.realtimeSinceStartup; stage = 1; break;
            case 1: if (t < .03f) return; Shot("flash"); Check("blink", e.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("WhiteFlash"))); stage = 2; break;
            case 2: if (t < .13f) return; Shot("squash"); Check("blink over", !e.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sharedMaterial != null && r.sharedMaterial.shader.name.Contains("WhiteFlash"))); stage = 3; break;
            case 3:
                if (t < .5f) return;
                Shot("bar");
                report.Add($"  health {e.HealthFraction:0.00}; moved {Vector2.Distance(before, e.transform.position):0.00} u; boss chip {BossHealthBar.ChipForTest:0.00}");
                Next(qori); break;
        }
    }

    static void Check(string what, bool ok) => report.Add($"  {(ok ? "ok" : "FAIL")} {what}");

    static void Next(PlayerMovement qori)
    {
        index++; stage = 0; at = Time.realtimeSinceStartup;
        if (index >= list.Count)
        {
            File.WriteAllLines(Path.Combine(dir, "hit_feel.txt"), report);
            EditorApplication.update -= Tick; SessionState.EraseString(Key);
            EditorApplication.ExitPlaymode(); EditorApplication.Exit(0); return;
        }
        e = list[index];
    }

    static void Shot(string step)
    {
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.1f, .1f, .12f);
        cam.transform.position = e.transform.position + new Vector3(0f, 2f, -50f);
        cam.orthographicSize = e is UndergroundGuardian ? 7f : 4f;
        var canvas = BossHealthBar.CanvasForTest;
        if (canvas != null) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 5f; Canvas.ForceUpdateCanvases(); }
        var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, e.name.Replace(' ', '_') + "_" + step + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        if (canvas != null) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.DestroyImmediate(cam.gameObject);
    }
}
