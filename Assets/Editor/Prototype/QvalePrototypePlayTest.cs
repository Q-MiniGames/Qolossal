using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Real Play-mode run through the Qvale prototype (Proto_Qvale, batch), pressing keys on a virtual
// keyboard: Qori walks down into town picking up Amber; walks into Grandfather Tallow's house (the
// front cuts away) and talks to him with Up (he can't walk off while talking; Space reads on);
// climbs the crates onto the Brambles' roof; buys a heart seed and the lamp oil at the smithy (Esc
// leaves the shop, not into the pause menu); sits at the listening tree, plays the first track,
// finds the second locked, gets up; finds both song shells; touches the knot for the quake; and
// the old man now says something else. Renders key moments (with the UI) to
// <-captureDir>/qplay_*.png. The player's own save file is set aside and put back.
// Usage (no -quit): -executeMethod QvalePrototypePlayTest.Run
[InitializeOnLoad]
public static class QvalePrototypePlayTest
{
    const string Key = "Qolossal.QvalePrototypePlayTest";
    static string Backup => GameSave.FilePath + ".qvaletest";
    static int stage, failures, errors, ignored, taps, heartsBefore, amberBefore; static float stageAt, maxY; static Vector2 held;
    static readonly Queue<Key> tapQueue = new Queue<Key>(); static int tapPhase;
    static Keyboard keys;
    static InputSettings.EditorInputBehaviorInPlayMode focusRule;
    static InputSettings.BackgroundBehavior backgroundRule;

    static QvalePrototypePlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) =>
        {
            if (t != LogType.Error && t != LogType.Exception) return;
            if (m.Contains("TraceRenderingLayerMask")) { ignored++; return; }   // known scratch-copy noise
            errors++; Debug.Log("[QvaleTest] runtime error: " + m);
        };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(QvalePrototypeBuilder.ScenePath);
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[QvaleTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static Rigidbody2D Body => Qori.GetComponent<Rigidbody2D>();
    static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>();
    static TownNpc Npc(string id) { foreach (var n in Object.FindObjectsByType<TownNpc>(FindObjectsSortMode.None)) if (n.id == id) return n; return null; }
    static void Place(Vector2 at) => Qori.PlaceAt(at, 1f, false);
    static void Next(int s) { stage = s; stageAt = Time.time; }
    static void Press(params Key[] k) => InputSystem.QueueStateEvent(keys, new KeyboardState(k));
    static void Tap(Key k, int n = 1) { for (int i = 0; i < n; i++) tapQueue.Enqueue(k); }
    static bool Idle => tapQueue.Count == 0 && tapPhase == 0;

    // Each tap: the key down for 3 frames, then up for 3.
    static void PumpTaps()
    {
        if (tapQueue.Count == 0 && tapPhase == 0) return;
        if (tapPhase == 0) { Press(tapQueue.Peek()); tapPhase = 1; return; }
        if (++tapPhase == 4) Press();
        if (tapPhase >= 7) { tapQueue.Dequeue(); taps++; tapPhase = 0; }
    }

    // What the camera sees, with the screen-space UI drawn into it.
    static void Shot(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/QvaleCaptures";
        Directory.CreateDirectory(folder);
        var camera = Camera.main; camera.aspect = 16f / 9f;
        foreach (var layer in Object.FindObjectsByType<ParallaxShape>(FindObjectsSortMode.None)) layer.Refresh(camera);
        var canvases = new List<Canvas>();
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.enabled) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = camera; c.planeDistance = 1f; canvases.Add(c); }
        var target = new RenderTexture(1600, 900, 24); camera.targetTexture = target;
        Canvas.ForceUpdateCanvases(); camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1600, 900, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    static void Tick()
    {
        try { Step(); }
        catch (System.Exception e) { Debug.LogError("[QvaleTest] aborted at stage " + stage + ": " + e); Finish(1); }
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(Key, false);
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
        float t = Time.time, since = t - stageAt;
        var q = Qori; if (q == null) return;
        Vector2 p = Body.position;
        if (keys != null) PumpTaps();
        const float OldX = QvalePrototypeBuilder.OldX, HouseX = QvalePrototypeBuilder.HouseX, TreeX = QvalePrototypeBuilder.TreeX;
        switch (stage)
        {
            case 0 when t > 1f:
                focusRule = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                backgroundRule = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keys = InputSystem.AddDevice<Keyboard>("Qvale Test Keyboard");
                Shot("qplay_01_arrival");
                Next(1);
                break;

            // Down the slope into town, picking up Amber on the way.
            case 1:
                Press(UnityEngine.InputSystem.Key.D);
                if (p.x > 34f) { Press(); Expect(TownState.Amber >= 20, $"walks down into town, picking up Amber ({TownState.Amber})"); Place(new Vector2(OldX + 2.5f, .6f)); Next(2); }
                else if (since > 20f) { Press(); Expect(false, $"walk into town (stuck at {p.x:F1}, {p.y:F1})"); Place(new Vector2(OldX + 2.5f, .6f)); Next(2); }
                break;

            // Inside the old man's house: the front cuts away; Up talks; he can't walk off; Space reads on.
            case 2 when since > 1f:
            {
                var house = HouseAt(OldX);
                Expect(house != null && house.QoriInside && house.Openness > .95f, $"inside the old man's house the front cuts away (openness {house?.Openness:F2})");
                Shot("qplay_02_cutaway");
                Tap(UnityEngine.InputSystem.Key.W); Next(3);
                break;
            }
            case 3 when Idle && since > .3f:
                Expect(DialogueBox.IsOpen, "Up beside Grandfather Tallow starts the conversation");
                Shot("qplay_03_dialogue");
                held = p; Press(UnityEngine.InputSystem.Key.A); Next(4);
                break;
            case 4 when since > .6f:
                Press();
                Expect(Mathf.Abs(p.x - held.x) < .05f, $"Qori can't walk off while talking (moved {Mathf.Abs(p.x - held.x):F2})");
                taps = 0; Tap(UnityEngine.InputSystem.Key.Space, 24); Next(5);
                break;
            case 5:
                if (!DialogueBox.IsOpen) { tapQueue.Clear(); tapPhase = 0; Press(); Expect(Npc("tallow").LastTalk == 0 && TownState.Has("talk:tallow:0"), $"Space reads through his first warning and closes it ({taps} presses)"); Next(6); }
                else if (Idle) { Expect(false, "the conversation closes"); Next(6); }
                break;
            case 6 when since > .5f:
                Place(new Vector2(OldX - 5f, .6f)); Next(7);
                break;
            case 7 when since > 1f:
                Expect(HouseAt(OldX).Openness < .05f, $"outside again, the front closes (openness {HouseAt(OldX).Openness:F2})");
                Place(new Vector2(HouseX - 9f, .6f)); maxY = 0f; Next(8);
                break;

            // Up the crates onto the Brambles' roof (hopping right).
            case 8:
            {
                bool hop = (since % .5f) < .28f;
                if (hop) Press(UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.Space); else Press(UnityEngine.InputSystem.Key.D);
                maxY = Mathf.Max(maxY, p.y);
                if (p.y > 6.5f && p.x > HouseX) { Press(); Expect(true, $"climbs the crates onto the roof ({since:F1} s)"); Next(9); }
                else if (since > 8f) { Press(); Expect(false, $"climb onto the roof (highest {maxY:F1})"); Next(9); }
                break;
            }

            // The smithy: Up, read on, the shop opens; buy the heart seed and the oil; Esc leaves.
            case 9 when since > .3f:
                TownState.AddAmber(100);
                heartsBefore = Find<PlayerHealth>().MaximumHealth; amberBefore = TownState.Amber;
                Place(new Vector2(QvalePrototypeBuilder.SmithyX + 6f, .6f)); Next(10);
                break;
            case 10 when since > .6f:
                Tap(UnityEngine.InputSystem.Key.W, 9); Next(11);   // Up talks, then reads on (Space would buy once the shop opens)
                break;
            case 11:
                if (TownShop.IsOpenAny) { tapQueue.Clear(); tapPhase = 0; Press(); Expect(true, "after Brannick's greeting the shop opens"); Next(12); }
                else if (Idle && since > 1f) { Expect(false, "the shop opens"); Next(15); }
                break;
            case 12 when since > .4f:
                typeof(TownShop).GetField("selected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Npc("brannick").GetComponent<TownShop>(), 0);   // stray Up presses may have moved it
                Shot("qplay_04_shop");
                Tap(UnityEngine.InputSystem.Key.Space); Tap(UnityEngine.InputSystem.Key.S); Tap(UnityEngine.InputSystem.Key.Space); Next(13);
                break;
            case 13 when Idle && since > .3f:
                Expect(Find<PlayerHealth>().MaximumHealth == heartsBefore + 1, $"the heart seed adds a heart ({heartsBefore} -> {Find<PlayerHealth>().MaximumHealth})");
                Expect(TownState.Has("lamps") && TownState.Amber == amberBefore - 90, $"the oil is paid for, 90 Amber spent ({amberBefore} -> {TownState.Amber})");
                Tap(UnityEngine.InputSystem.Key.Escape); Next(14);
                break;
            case 14 when Idle && since > .4f:
                Expect(!TownShop.IsOpenAny && !GamePauseMenu.IsPaused, "Esc leaves the shop without pausing the game");
                Expect(LampLit(), "the street lamps are lit");
                Next(15);
                break;

            // The listening tree: sit, play the first track, the second is locked, get up.
            case 15 when since > .3f:
                Place(new Vector2(TreeX + 1f, .6f)); Next(16);
                break;
            case 16 when since > .6f:
                Tap(UnityEngine.InputSystem.Key.W); Next(17);
                break;
            case 17 when Idle && since > 1.8f:
            {
                var spot = Find<ListeningSpot>();
                Expect(ListeningSpot.IsSitting && Camera.main.orthographicSize > 6f, $"Up at the bench sits down, and the view eases out (size {Camera.main.orthographicSize:F1})");
                Tap(UnityEngine.InputSystem.Key.Space); Next(18);
                break;
            }
            case 18 when Idle && since > 2f:
            {
                var spot = Find<ListeningSpot>();
                Expect(spot.Playing == 0 && spot.Source.isPlaying && spot.Source.volume > .3f, $"Space plays the first track (playing {spot.Playing}, volume {spot.Source.volume:F2})");
                Shot("qplay_05_listening");
                Tap(UnityEngine.InputSystem.Key.S); Tap(UnityEngine.InputSystem.Key.Space); Next(19);
                break;
            }
            case 19 when Idle && since > .3f:
            {
                var spot = Find<ListeningSpot>();
                Expect(spot.Playing == 0 && !spot.IsUnlocked(1), "the second track is locked until its song shell is found");
                Tap(UnityEngine.InputSystem.Key.Escape); Next(20);
                break;
            }
            case 20 when Idle && since > 2f:
                Expect(!ListeningSpot.IsSitting && Camera.main.GetComponent<CameraFollow>().enabled && !GamePauseMenu.IsPaused, "Esc gets up: the view follows Qori again, no pause menu");
                Place(new Vector2(HouseX + QvalePrototypeBuilder.HouseW * .5f, 10.6f)); Next(21);
                break;

            // The song shells: on the roof's peak, and on the old man's loft.
            case 21 when since > .6f:
                Expect(TownState.Has("shell:roof"), "the song shell on the roof");
                Place(new Vector2(OldX + 14.5f, 4.6f)); Next(22);
                break;
            case 22 when since > .6f:
                Expect(TownState.Has("shell:lullaby") && Find<ListeningSpot>().IsUnlocked(1) && Find<ListeningSpot>().IsUnlocked(2), "the song shell on the loft; both tracks are unlocked");
                Place(new Vector2(QvalePrototypeBuilder.KnotX - 4f, QvalePrototypeBuilder.SurfaceY(QvalePrototypeBuilder.KnotX - 4f) + .6f)); Next(23);
                break;

            // The quake, then the old man again.
            case 23:
                Press(UnityEngine.InputSystem.Key.D);
                if (TownQuake.IsPlaying) { Press(); Expect(true, "walking into the knot starts the quake"); Next(24); }
                else if (since > 6f) { Press(); Expect(false, $"the quake starts (Qori at {p.x:F1}, {p.y:F1})"); Next(26); }
                break;
            case 24 when since > 3f && !TownQuake.IsPlaying:
                Shot("qplay_06_quake");
                Expect(TownState.Quakes == 1 && CrackShown(), "the quake: the town has felt it, and a crack crosses the square");
                Place(new Vector2(OldX + 2.5f, .6f)); Next(25);
                break;
            case 25 when since > .8f:
                Tap(UnityEngine.InputSystem.Key.W); Next(26);
                break;
            case 26 when Idle && since > .5f:
                Expect(DialogueBox.IsOpen && Npc("tallow").LastTalk == 2, $"after the quake the old man says something new (conversation {Npc("tallow").LastTalk})");
                Shot("qplay_07_after_quake");
                Tap(UnityEngine.InputSystem.Key.Escape); Next(27);
                break;
            case 27 when Idle && since > .5f:
                Press();
                Expect(errors == 0, $"no runtime errors ({errors}; {ignored} known scratch-copy shader errors ignored)");
                Debug.Log($"[QvaleTest] finished with {failures} failure(s)");
                Finish(failures == 0 ? 0 : 1);
                break;
        }
    }

    static CutawayHouse HouseAt(float x)
    {
        foreach (var h in Object.FindObjectsByType<CutawayHouse>(FindObjectsSortMode.None))
            if (Mathf.Abs(h.GetComponent<BoxCollider2D>().bounds.min.x - x) < 1.5f) return h;
        return null;
    }

    static bool CrackShown() { foreach (var v in Object.FindObjectsByType<TownVariant>(FindObjectsSortMode.None)) if (v.condition == "quake") return v.transform.GetChild(0).gameObject.activeSelf; return false; }
    static bool LampLit() { foreach (var v in Object.FindObjectsByType<TownVariant>(FindObjectsSortMode.None)) if (v.condition == "flag:lamps") return v.transform.GetChild(0).gameObject.activeSelf; return false; }
}
