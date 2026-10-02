using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Real Play-mode check of hit-stop and the camera's room edges (batch), with a virtual keyboard.
// Hit-stop (A0): a freeze stops the world and lets it go after its real-time length; pausing
// during a freeze holds it, and resuming finishes it; Qori taking a hit freezes the game; a killing
// blow is reported as one; the option turns it off. Room edges: in A0 Qori walks to the west end
// and the camera holds at the room's edge while he walks on toward the side of the screen; then in
// R1 the guardian floor's lock holds the camera level while he jumps. Renders the edge to
// <-captureDir>/edge_*.png. The player's save file is set aside and put back.
// Usage (no -quit): -executeMethod CameraFeelPlayTest.Run
[InitializeOnLoad]
public static class CameraFeelPlayTest
{
    const string Key = "Qolossal.CameraFeelPlayTest";
    static string Backup => GameSave.FilePath + ".camerafeeltest";
    static int stage, failures, errors, ignored, startCount; static float stageAt, real0, maxDrift, minCamY, maxCamY, maxEdgeOver;
    static bool wasEnabled = true;
    static Keyboard keys;
    static InputSettings.EditorInputBehaviorInPlayMode focusRule;
    static InputSettings.BackgroundBehavior backgroundRule;

    static CameraFeelPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) =>
        {
            if (t != LogType.Error && t != LogType.Exception) return;
            if (m.Contains("TraceRenderingLayerMask")) { ignored++; return; }
            errors++; Debug.Log("[FeelTest] runtime error: " + m);
        };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[FeelTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static void Next(int s) { stage = s; stageAt = Time.realtimeSinceStartup; }
    static void Press(params Key[] k) => InputSystem.QueueStateEvent(keys, new KeyboardState(k));
    static float Real => Time.realtimeSinceStartup;

    static void Shot(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/FeelCaptures";
        Directory.CreateDirectory(folder);
        var camera = Camera.main; camera.aspect = 16f / 9f;
        foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    static void Tick()
    {
        try { Step(); }
        catch (System.Exception e) { Debug.LogError("[FeelTest] aborted at stage " + stage + ": " + e); Finish(1); }
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
        HitStop.Enabled = wasEnabled;
        if (keys != null)
        {
            InputSystem.RemoveDevice(keys);
            InputSystem.settings.editorInputBehaviorInPlayMode = focusRule;
            InputSystem.settings.backgroundBehavior = backgroundRule;
        }
        if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); } else GameSave.Clear();
        EditorApplication.Exit(code);
    }

    static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        float since = Real - stageAt;
        var q = Qori; if (q == null) return;
        var cam = Camera.main;
        switch (stage)
        {
            case 0 when Time.time > 1f:
                focusRule = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                backgroundRule = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keys = InputSystem.AddDevice<Keyboard>("Feel Test Keyboard");
                Camera.main.aspect = 16f / 9f;   // batch mode's screen is another shape; follow and render as a 16:9 game
                wasEnabled = HitStop.Enabled; HitStop.Enabled = true;
                startCount = HitStop.Count; real0 = Real;
                HitStop.Freeze(.12f);
                Next(1);
                break;

            // A freeze stops the world, then lets it go after about its real-time length.
            case 1:
                if (Real - real0 < .05f)
                {
                    if (!HitStop.IsFrozen || Time.timeScale > .01f) { Expect(false, $"a freeze stops the world (scale {Time.timeScale})"); Next(2); }
                    break;
                }
                if (!HitStop.IsFrozen)
                {
                    float took = Real - real0;
                    Expect(Mathf.Approximately(Time.timeScale, 1f) && took > .1f && took < .3f, $"a 0.12 s freeze stops the world and lets it go ({took:F2} s real, scale {Time.timeScale})");
                    Next(2);
                }
                else if (Real - real0 > 1f) { Expect(false, "the freeze ends"); Next(2); }
                break;

            // Pausing during a freeze holds it; resuming finishes it.
            case 2 when since > .3f:
                HitStop.Freeze(.15f);
                Press(UnityEngine.InputSystem.Key.Escape); Next(3);
                break;
            case 3 when since > .05f:
                Press(); Next(4);
                break;
            case 4 when since > .5f:
                Expect(GamePauseMenu.IsPaused && HitStop.IsFrozen && Time.timeScale == 0f, $"paused during a freeze: the game is paused and the freeze waits (frozen {HitStop.IsFrozen}, scale {Time.timeScale})");
                Press(UnityEngine.InputSystem.Key.Escape); Next(5);
                break;
            case 5 when since > .05f:
                Press(); Next(6);
                break;
            case 6 when since > .5f:
                Expect(!GamePauseMenu.IsPaused && !HitStop.IsFrozen && Mathf.Approximately(Time.timeScale, 1f), $"resumed: the freeze finished and time runs again (scale {Time.timeScale})");
                Next(7);
                break;

            // Qori takes a hit: the game freezes.
            case 7 when since > .3f:
            {
                startCount = HitStop.Count;
                var health = q.GetComponent<PlayerHealth>();
                health.TakeDamage((Vector2)q.transform.position + Vector2.right, new Vector2(4f, 3f));
                Expect(HitStop.Count == startCount + 1 && HitStop.IsFrozen, "Qori taking a hit freezes the game");
                Next(8);
                break;
            }
            // A killing blow says so (it's what freezes on a kill).
            case 8 when since > .5f:
            {
                bool any = false, killed = true;
                foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    if (!e.IsAlive) continue;
                    any = true;
                    var r = e.ReceiveCombatHit(new CombatDamage { Damage = 999f, Direction = Vector2.down });
                    if (r.Disposition == CombatHitDisposition.Damaged && r.Damage > 0f) { killed = r.Killed && !e.IsAlive; break; }
                }
                var crawler = Object.FindFirstObjectByType<GroundCreature>();
                bool crawlerKilled = crawler == null || crawler.ReceiveCombatHit(new CombatDamage { Damage = 999f }).Killed;
                Expect(any && killed && crawlerKilled, "a killing blow is reported as one (enemies and crawlers)");
                Next(9);
                break;
            }
            // Turned off: nothing freezes.
            case 9 when since > .3f:
                HitStop.Enabled = false; startCount = HitStop.Count;
                HitStop.Freeze(.2f);
                Expect(HitStop.Count == startCount && !HitStop.IsFrozen && Mathf.Approximately(Time.timeScale, 1f), "with Hit pause off, nothing freezes");
                HitStop.Enabled = true;
                // To the room's west end: Qori walks left, the camera holds at the edge.
                q.PlaceAt(new Vector2(-12f, .6f), -1f, false);
                maxEdgeOver = -99f;
                Next(10);
                break;
            case 10:
            {
                Press(UnityEngine.InputSystem.Key.A);
                CameraBounds.TryGetRoom(out Rect room, out _);
                float left = cam.transform.position.x - cam.orthographicSize * cam.aspect;
                maxEdgeOver = Mathf.Max(maxEdgeOver, room.xMin - left);
                if (since > 1.5f)   // short of the cliff at the west end (x -24): A0 has no wall there
                {
                    Press();
                    float qx = q.transform.position.x, cx = cam.transform.position.x;
                    Expect(maxEdgeOver < .05f, $"the view never shows past the room's west edge (by at most {maxEdgeOver:F2} u; edge {room.xMin:F1})");
                    Expect(qx < cx - 4f, $"at the edge the camera holds while Qori walks on toward the side of the screen (Qori {qx:F1}, camera {cx:F1})");
                    Shot("edge_a0_west");
                    SceneManager.LoadScene("R1_GripKnot");
                    Next(11);
                }
                break;
            }
            // R1: the guardian floor's lock holds the camera level while Qori jumps.
            case 11 when since > 1.5f:
                Camera.main.aspect = 16f / 9f;
                foreach (var g in Object.FindObjectsByType<KnucklebrambleGuardian>(FindObjectsSortMode.None)) g.gameObject.SetActive(false);   // only the camera is under test
                q.PlaceAt(new Vector2(22f, .6f), 1f, false);
                minCamY = 99f; maxCamY = -99f;
                Next(12);
                break;
            case 12 when since > 1.2f:
            {
                bool jump = ((since - 1.2f) % .7f) < .3f;
                if (since < 4f) { if (jump) Press(UnityEngine.InputSystem.Key.Space); else Press(); }
                if (since > 1.6f) { minCamY = Mathf.Min(minCamY, cam.transform.position.y); maxCamY = Mathf.Max(maxCamY, cam.transform.position.y); }
                if (since > 4.2f)
                {
                    Press();
                    Expect(CameraLockZone.Current != null && maxCamY - minCamY < .15f && Mathf.Abs(minCamY - 4f) < .2f,
                        $"on the guardian floor the camera holds level at the chamber's middle while Qori jumps (y {minCamY:F2}..{maxCamY:F2})");
                    Shot("edge_r1_arena");
                    Next(13);
                }
                break;
            }
            case 13:
                Expect(errors == 0, $"no runtime errors ({errors}; {ignored} known scratch-copy shader errors ignored)");
                Debug.Log($"[FeelTest] finished with {failures} failure(s)");
                Finish(failures == 0 ? 0 : 1);
                break;
        }
    }
}
