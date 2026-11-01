using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch: -executeMethod UndergroundGuardianTest.Run -saveFile ugp_test.json -captureDir <dir> (no -quit).
// Puts Qori in each guardian's arena, watches it wake and run its states, lands hits on its open weak point until it
// falls, and checks that its flag is saved and the gate onward opens. A real save is never touched (-saveFile).
public static class UndergroundGuardianTest
{
    const string Key = "UGP.GuardianTest.Dir";
    static readonly string[] Rooms = { "A08", "C08", "D08", "MR01_C02", "MR02_C03", "MR03_C04" };
    static bool sealSeen, rewardHidden, resetTried, resetWorked;
    static string dir; static int room = -1; static float phaseAt, resetAt = -1f; static HashSet<string> seen = new HashSet<string>();
    static readonly List<string> report = new List<string>(); static float nextShot; static int shots, hitsLanded;

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        GameSave.Clear();
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

    static UndergroundRoom Find(string id) => Object.FindObjectsByType<UndergroundRoom>(FindObjectsSortMode.None).FirstOrDefault(r => r.roomId == id);

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < 3f) return;
        var player = Object.FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;
        var health = player.GetComponent<PlayerHealth>();
        if (health != null) health.AddMaximum(0);   // watching the boss, not the player
        if (room < 0 || Time.time - phaseAt > 180f) { if (room >= 0) Finish(false); Next(player); return; }
        var r = Find(Rooms[room]);
        var boss = r.GetComponentInChildren<UndergroundGuardian>(true);
        if (boss == null) { report.Add(Rooms[room] + ": NO GUARDIAN"); Next(player); return; }
        var b = r.WorldBounds;
        if (resetAt < 0f && !b.Contains(new Vector3(player.transform.position.x, player.transform.position.y, b.center.z))) Place(player, r);
        sealSeen |= r.GetComponentsInChildren<UgArenaSeal>(true).Any(s => s.solid != null && s.solid.enabled);
        var held = r.GetComponentInChildren<MraArenaGuardian>(true);
        rewardHidden |= held != null && held.rewards.Length > 0 && held.rewards.All(x => x != null && !x.gameObject.activeSelf);
        // Halfway through its first window, Qori leaves: it must sleep with its health back.
        if (!resetTried && hitsLanded >= 2 && boss.IsAlive)
        {
            resetTried = true; player.transform.position = new Vector3(b.max.x + 40f, b.max.y + 40f, 0f); player.GetComponent<Rigidbody2D>().position = player.transform.position;
            resetAt = Time.time; return;
        }
        if (resetTried && resetAt > 0f) { if (Time.time - resetAt < .4f) return; resetWorked = boss.Asleep && boss.Health >= boss.MaximumHealthForTest; resetAt = -1f; hitsLanded = 0; Place(player, r); return; }
        seen.Add(boss.Current.ToString() + (boss.Current == UndergroundGuardian.State.Strike ? boss.Attack.ToString() : ""));
        if (Time.time > nextShot && shots < 6) { Shot(r, Rooms[room] + "_" + shots + "_" + boss.Current); shots++; nextShot = Time.time + 2.5f; }
        if (boss.WeakOpen && boss.Current == UndergroundGuardian.State.Exposed)
        {
            boss.HitWeak(new CombatDamage { Damage = 1 }); hitsLanded++;
        }
        if (!boss.IsAlive && Time.time - phaseAt > 1f && seen.Contains("Dead") ) { Finish(true); Next(player); return; }
        if (!boss.IsAlive) seen.Add("Dead");
    }

    static void Place(PlayerMovement player, UndergroundRoom r)
    {
        var b = r.WorldBounds; float x = b.min.x + (r.roomId == "A08" ? 13f : r.roomId == "C08" ? 7f : 7f);
        player.transform.position = new Vector3(x, b.min.y + (r.roomId == "A08" || r.roomId == "C08" ? 6f : 6f), 0f);
    }

    static void Finish(bool died)
    {
        var r = Find(Rooms[room]);
        var keeper = r.GetComponentInChildren<MraArenaGuardian>(true);
        var gate = r.GetComponentInChildren<MraGate>(true);
        report.Add($"{Rooms[room]}: {(died ? "FELL" : "DID NOT FALL")} after {hitsLanded} hits in {Time.time - phaseAt:0}s; states [{string.Join(", ", seen.OrderBy(s => s))}]; seal shut {sealSeen}; reward held back {rewardHidden} and out {(keeper != null && keeper.rewards.Length > 0 ? keeper.rewards.All(x => x != null && x.gameObject.activeSelf).ToString() : "n/a")}; reset on leaving {resetWorked}; flag {(keeper != null && GameSave.HasFlag(keeper.flag) ? "saved" : "MISSING")}; gate {(gate != null ? (gate.IsOpen ? "open" : "SHUT") : "none")}");
        Debug.Log("UGP test " + report[report.Count - 1]);
    }

    static void Next(PlayerMovement player)
    {
        room++; sealSeen = rewardHidden = resetTried = resetWorked = false; resetAt = -1f; seen.Clear(); shots = 0; hitsLanded = 0; phaseAt = Time.time; nextShot = Time.time + 1.5f;
        if (room >= Rooms.Length)
        {
            File.WriteAllLines(Path.Combine(dir, "guardian_test.txt"), report);
            EditorApplication.update -= Tick; SessionState.EraseString(Key);
            EditorApplication.ExitPlaymode(); EditorApplication.Exit(0); return;
        }
        Place(player, Find(Rooms[room]));
    }

    static void Shot(UndergroundRoom room, string name)
    {
        var b = room.WorldBounds;
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(b.center.x, b.center.y, -50f);
        cam.orthographicSize = Mathf.Max(b.size.y / 2f + 1f, (b.size.x / 2f + 1f) * 9f / 16f);
        var rt = new RenderTexture(1280, 720, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(cam.gameObject);
    }
}
