using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Real Play-mode check of the Knucklebramble fight in R1_GripKnot (batch), from a new game. The
// guardian sleeps until Qori comes near; then each arm rises with a warning glint and slams in
// turn (front, top, then the back arm, turned toward him); a slam on Qori costs a heart; blows
// glance off until the third slam opens the body, when the knot takes three hits before the
// body closes; a second opening finishes it, and the knot's thorns wither. Renders the poses to
// <-captureDir>/guardian_*.png. The player's own save file is set aside and put back.
// Usage (no -quit): -executeMethod GuardianPlayTest.Run
[InitializeOnLoad]
public static class GuardianPlayTest
{
    const string Key = "Qolossal.GuardianPlayTest";
    static string Backup => GameSave.FilePath + ".guardiantest";
    static int stage, failures, errors, heartsBefore; static float stageAt;
    static bool glint, qoriHit, blocked, faced;
    static readonly List<string> order = new List<string>();
    static readonly HashSet<string> shots = new HashSet<string>();

    static GuardianPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[GuardianTest] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + R1GripKnotBuilder.SceneName + ".unity");
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[GuardianTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static KnucklebrambleGuardian Guardian => Object.FindFirstObjectByType<KnucklebrambleGuardian>();
    static void Place(Vector2 at) { var body = Qori.GetComponent<Rigidbody2D>(); body.position = at; body.linearVelocity = Vector2.zero; }
    static void Next(int s) { stage = s; stageAt = Time.time; }
    static CombatDamage Sword => new CombatDamage { Damage = 1, Direction = Vector2.left };

    static void Shot(string name)
    {
        if (!shots.Add(name)) return;
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/GuardianCaptures";
        Directory.CreateDirectory(folder);
        var camera = Camera.main;
        Vector3 keep = camera.transform.position;
        camera.transform.position = new Vector3(29.5f, 2.6f, keep.z);
        var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, $"guardian_{name}.png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null; camera.transform.position = keep;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;
        var g = Guardian;
        if (g != null && g.IsAlive)
        {
            string now = g.Current + (g.StrikingArm > 0 ? " " + g.StrikingArm : "");
            if (order.Count == 0 || order[order.Count - 1] != now) order.Add(now);
            glint |= Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(x => x.name.StartsWith("FX FX_Telegraph_Glint"));
            if (g.Current == KnucklebrambleGuardian.State.Hold && g.StrikingArm > 0) Shot("slam" + g.StrikingArm);
            if (g.Current == KnucklebrambleGuardian.State.Exposed) Shot("exposed");
            if (g.Current == KnucklebrambleGuardian.State.Dormant && stage == 1) Shot("rest");
        }
        switch (stage)
        {
            case 0 when t > 1f:
                Expect(g != null && g.Current == KnucklebrambleGuardian.State.Dormant, "the Knucklebramble sleeps while Qori is far off");
                Next(1);
                break;
            case 1 when t - stageAt > .3f:
                // Stand where the front arm's claws land.
                heartsBefore = Qori.GetComponent<PlayerHealth>().Health;
                Place(new Vector2(30.4f, .3f));
                Next(2);
                break;
            case 2 when g.Current == KnucklebrambleGuardian.State.Telegraph || t - stageAt > 4f:
                Expect(g.Current == KnucklebrambleGuardian.State.Telegraph && g.StrikingArm == 3, $"Qori close by wakes it: the front arm rises first ({g.Current} {g.StrikingArm})");
                Shot("raise3");
                float before = g.Health;
                blocked = g.ReceiveCombatHit(Sword).Disposition == CombatHitDisposition.Blocked && Mathf.Approximately(g.Health, before);
                Expect(blocked, "a blow while the body is closed glances off");
                Next(3);
                break;
            case 3 when g.Current == KnucklebrambleGuardian.State.Recover || t - stageAt > 4f:
            {
                Expect(glint, "a warning glint comes before the slam");
                int hearts = Qori.GetComponent<PlayerHealth>().Health;
                qoriHit = hearts < heartsBefore;
                Expect(qoriHit, $"the slam lands on Qori where he stands ({heartsBefore} -> {hearts} hearts)");
                Next(4);
                break;
            }
            case 4:
            {
                Qori.GetComponent<PlayerHealth>().Heal(10);   // keep him standing through the rest
                if (g.Current == KnucklebrambleGuardian.State.Telegraph && g.StrikingArm == 1 && !faced)
                {
                    float scale = g.GetComponentInChildren<CreatureRig>().facing.localScale.x;
                    faced = true;
                    Expect(scale < 0f, $"it turns its back arm toward Qori for the third strike (facing {scale})");
                }
                if (g.Current == KnucklebrambleGuardian.State.Exposed || t - stageAt > 12f)
                {
                    Expect(g.Current == KnucklebrambleGuardian.State.Exposed, "after three slams its knot is exposed");
                    var bodyArt = g.GetComponentsInChildren<SpriteRenderer>().First(r => r.transform.parent.name == "Body");
                    Expect(bodyArt.sprite.name.Contains("KnotExposed"), $"the body shows the opened art ({bodyArt.sprite.name})");
                    string strikes = string.Join(", ", order.Where(s => s.StartsWith("Slam")));
                    Expect(strikes == "Slam 3, Slam 2, Slam 1", $"the arms strike front, top, back ({strikes})");
                    for (int i = 0; i < 3; i++) Expect(g.ReceiveCombatHit(Sword).Disposition == CombatHitDisposition.Damaged, $"a blow on the exposed knot lands ({i + 1})");
                    Expect(g.Health == 3f && g.Current == KnucklebrambleGuardian.State.Closing && g.IsAlive, $"after three hits it closes again ({g.Health} left, {g.Current})");
                    Next(5);
                }
                break;
            }
            case 5:
                Qori.GetComponent<PlayerHealth>().Heal(10);
                if (g.Current == KnucklebrambleGuardian.State.Exposed || t - stageAt > 14f)
                {
                    Expect(g.Current == KnucklebrambleGuardian.State.Exposed, "the next cycle opens it again");
                    for (int i = 0; i < 3; i++) g.ReceiveCombatHit(Sword);
                    Expect(!g.IsAlive, "the second opening finishes it");
                    Next(6);
                }
                break;
            case 6 when t - stageAt > 1.2f:
            {
                var knot = Object.FindFirstObjectByType<TitanKnot>();
                Expect(Guardian == null, "the fallen guardian is gone");
                Expect(knot.Current == TitanKnot.State.Ready, $"and the knot's thorns wither ({knot.Current})");
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[GuardianTest] states: {string.Join(" > ", order.Take(40))}");
                Debug.Log($"[GuardianTest] finished with {failures} failure(s)");
                if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); } else GameSave.Clear();
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                break;
            }
        }
    }
}
