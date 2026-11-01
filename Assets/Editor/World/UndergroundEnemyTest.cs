using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod UndergroundEnemyTest.Run -saveFile ugp_test.json -captureDir <dir> (no -quit).
// Stands Qori beside every creature with its own script, watches it run through its phases, notes whether it hurt him,
// then lands hits until it falls. Hits are sent by script: this proves the creature's logic, not how it feels to fight.
// A real save is never touched (-saveFile).
public static class UndergroundEnemyTest
{
    const string Key = "UGP.EnemyTest.Dir";
    const float Watch = 12f, KillBy = 45f;
    static string dir; static int index = -1; static float startedAt, placedAt, lastHit;
    static List<EnemyBase> all; static readonly HashSet<string> seen = new HashSet<string>(), shot = new HashSet<string>();
    static readonly List<string> report = new List<string>(); static int blocked, hits; static bool hurtQori; static Vector2 qoriAt, enemyAt;

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("UGP test needs -saveFile"); EditorApplication.Exit(1); return; }
        dir = args[System.Array.IndexOf(args, "-captureDir") + 1]; Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        SessionState.SetString(Key, dir);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod] static void Resume()
    {
        dir = SessionState.GetString(Key, "");
        if (!string.IsNullOrEmpty(dir)) EditorApplication.update += Tick;
    }

    static string State(EnemyBase e) => e is UndergroundEnemy u ? u.State : ((UndergroundGuardian)e).Current.ToString();

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < 3f) return;
        var player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;
        var health = player.GetComponent<PlayerHealth>();
        if (all == null)
        {
            all = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Where(e => e is UndergroundEnemy || e is MossMatriarchBoss || e is EchoWardenBoss || e is GrainGolemBoss).OrderBy(e => e.name).ToList();
            var args = System.Environment.GetCommandLineArgs(); int only = System.Array.IndexOf(args, "-onlyRooms");
            if (only >= 0) { var rooms = args[only + 1].Split(','); all = all.Where(e => rooms.Contains(RoomOf(e))).ToList(); }
            report.Add($"{all.Count} creatures with their own script");
            Next(player, health); return;
        }
        var enemy = all[index];
        if (health != null)
        {
            if (health.LastHitTime > startedAt) hurtQori = true;
            health.Heal(99);
        }
        float t = Time.time - startedAt;
        if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsAlive) { Finish(enemy, true); Next(player, health); return; }
        if (t > KillBy) { Finish(enemy, false); Next(player, health); return; }
        string state = State(enemy);
        seen.Add(state);
        string design = enemy.name.Replace("UGP enemy ", "").Split(' ')[0];
        if ((state == "Attack" || state == "Strike") && shot.Add(design)) Shot(enemy, design + "_" + state);
        if (Time.time - placedAt > 1.5f && Vector2.Distance(player.transform.position, enemy.transform.position) > (t < Watch ? 5f : 2.9f)) Place(player, enemy);
        if (t > 8f && qoriAt == Vector2.zero) { qoriAt = player.transform.position; enemyAt = enemy.transform.position; }
        if (t < Watch) return;
        if (Time.time - lastHit < .3f) return;
        lastHit = Time.time;
        CombatDamageResponse hit;
        if (enemy is UndergroundGuardian g) { if (!g.WeakOpen) return; hit = g.HitWeak(new CombatDamage { Damage = 1 }); }
        else hit = enemy.ReceiveCombatHit(new CombatDamage { Damage = 1 });
        if (hit.Disposition == CombatHitDisposition.Blocked) blocked++; else if (hit.Disposition == CombatHitDisposition.Damaged) hits++;
    }

    // Beside it on the side away from a wall, on the ground below it, or a few units under a flyer high above the ground.
    static void Place(PlayerMovement player, EnemyBase e)
    {
        Vector2 at = e.transform.position;
        var room = e.GetComponentInParent<UndergroundRoom>(true);
        float side = room != null ? Mathf.Sign(room.WorldBounds.center.x - at.x) : 1f;
        if (Physics2D.Raycast(at + Vector2.up * .5f, new Vector2(side, 0f), 3f, GroundLayer.Mask)) side = -side;
        float gap = e is SiltLeechEnemy ? 0f : 2.5f;
        // A leech hangs under its perch, so the ground is looked for from below it. A swarm can sit high over a floor: meet it there.
        var ground = Physics2D.Raycast(at + Vector2.up * (e is SiltLeechEnemy ? -1f : .3f), Vector2.down, 80f, GroundLayer.Mask);
        float y = ground ? Mathf.Max(ground.point.y + 1f, at.y - 3f) : at.y - 3f;
        if (e is SeedSwarmEnemy) y = at.y - .5f;
        player.transform.position = new Vector3(at.x + side * gap, y, 0f);
        placedAt = Time.time;
    }

    static void Finish(EnemyBase e, bool fell)
    {
        var line = $"{(e != null ? e.name : "?")} in {RoomOf(e)}: {(fell ? "FELL" : "DID NOT FALL")} after {hits} hits ({blocked} blocked) at {Time.time - startedAt:0}s; hurt Qori: {(hurtQori ? "yes" : "no")}; phases [{string.Join(", ", seen.OrderBy(s => s))}]; at 8s it stood at {enemyAt}, Qori at {qoriAt}";
        report.Add(line); Debug.Log("UGP test " + line);
    }

    static string RoomOf(EnemyBase e) { var r = e != null ? e.GetComponentInParent<UndergroundRoom>(true) : null; return r != null ? r.roomId : "?"; }

    static void Next(PlayerMovement player, PlayerHealth health)
    {
        index++; seen.Clear(); hits = blocked = 0; qoriAt = enemyAt = Vector2.zero; hurtQori = false; startedAt = Time.time; lastHit = 0f;
        if (index >= all.Count)
        {
            File.WriteAllLines(Path.Combine(dir, "enemy_test.txt"), report);
            EditorApplication.update -= Tick; SessionState.EraseString(Key);
            EditorApplication.ExitPlaymode(); EditorApplication.Exit(0); return;
        }
        Place(player, all[index]);
    }

    static void Shot(EnemyBase e, string name)
    {
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.transform.position = e.transform.position + new Vector3(0f, 1.5f, -50f);
        cam.orthographicSize = 6f;
        var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(cam.gameObject);
    }
}
