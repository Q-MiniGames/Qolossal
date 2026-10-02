using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Real Play-mode run through the Palm prototype (Proto_Palm, batch), from a new game, pressing keys
// on a virtual keyboard: Qori walks the ridge from the moss nest (the camera pulls out on its high
// point); the crack drops into the side chamber, where roots bar the nook until the seed switch
// opens them (signalled directly: aiming the sling isn't scripted), the find is collected and the
// tunnel climbs back out; before the quake the cliff stops him; walking into the knot starts the
// quake and the pillar falls across the ravine; he crosses it onto the cliff top and climbs the
// causeway to the end. Renders what the camera sees at the key moments to
// <-captureDir>/play_*.png. The player's own save file is set aside and put back.
// Usage (no -quit): -executeMethod PalmPrototypePlayTest.Run
[InitializeOnLoad]
public static class PalmPrototypePlayTest
{
    const string Key = "Qolossal.PalmPrototypePlayTest";
    static string Backup => GameSave.FilePath + ".palmtest";
    static int stage, failures, errors, ignored; static float stageAt, maxVista;
    static bool shotA, shotB, shotC;
    static Keyboard keys;
    static InputSettings.EditorInputBehaviorInPlayMode focusRule;
    static InputSettings.BackgroundBehavior backgroundRule;

    static PalmPrototypePlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) =>
        {
            if (t != LogType.Error && t != LogType.Exception) return;
            // Known scratch-copy noise: the copied PackageCache loses a render-pipeline shader.
            if (m.Contains("TraceRenderingLayerMask")) { ignored++; return; }
            errors++; Debug.Log("[PalmTest] runtime error: " + m);
        };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(PalmPrototypeBuilder.ScenePath);
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[PalmTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static Rigidbody2D Body => Qori.GetComponent<Rigidbody2D>();
    static PalmGripStir Stir => Object.FindFirstObjectByType<PalmGripStir>();
    static void Place(Vector2 at) => Qori.PlaceAt(at, 1f, false);
    static void Next(int s) { stage = s; stageAt = Time.time; }
    static void Press(params Key[] held) => InputSystem.QueueStateEvent(keys, new KeyboardState(held));

    // Holds D (right); with `hop`, also taps Space: held .25 s, released .2 s.
    static void Walk(bool hop)
    {
        bool jump = hop && (Time.time - stageAt) % .45f < .25f;
        if (jump) Press(UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.Space); else Press(UnityEngine.InputSystem.Key.D);
    }

    // What the camera sees right now.
    static void Shot(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/PalmCaptures";
        Directory.CreateDirectory(folder);
        var camera = Camera.main; camera.aspect = 16f / 9f;
        foreach (var layer in Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);   // batch mode's screen is another shape
        var target = new RenderTexture(1600, 900, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1600, 900, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    static void Tick()
    {
        try { Step(); }
        catch (System.Exception e)
        {
            Debug.LogError("[PalmTest] aborted at stage " + stage + ": " + e);
            Finish(1);
        }
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
        var view = Camera.main;
        const float B = PalmPrototypeBuilder.Scale;
        switch (stage)
        {
            case 0 when t > 1f:
                focusRule = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                backgroundRule = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keys = InputSystem.AddDevice<Keyboard>("Palm Test Keyboard");
                Shot("play_01_start");
                Next(1);
                break;

            // Along the ridge (over or through the chamber); the camera pulls out on its high point.
            case 1:
                Walk(true);
                if (p.x > 78f) maxVista = Mathf.Max(maxVista, view.orthographicSize);
                if (!shotA && p.x > 96f) { shotA = true; Shot("play_02_ridge_top"); }
                if (p.x > 106f)
                {
                    Expect(true, $"walks the ridge from the nest ({since:F1} s)");
                    Expect(maxVista > 9f, $"the camera pulls out on the ridge's high point (orthographic size {maxVista:F1})");
                    Press(); Place(new Vector2((PalmPrototypeBuilder.CrackL + PalmPrototypeBuilder.CrackR) * .5f, 21f)); Next(10);
                }
                else if (since > 30f) { Expect(false, $"walk the ridge (stuck at {p.x:F1}, {p.y:F1})"); Press(); Next(20); }
                break;

            // The side chamber: drop through the crack; the roots bar the nook; the seed switch opens them.
            case 10 when since > 2.5f:
                Expect(p.y < PalmPrototypeBuilder.RoomFloor + 1.5f, $"the crack drops into the chamber (y {p.y:F1})");
                Shot("play_03_chamber");
                Press(UnityEngine.InputSystem.Key.A); Next(11);
                break;
            case 11 when since > 3f:
                Press();
                Expect(p.x > PalmPrototypeBuilder.GateX + .2f, $"the roots bar the nook (x {p.x:F1})");
                var seed = Object.FindFirstObjectByType<SeedSwitch>();
                typeof(MechanismSwitch).GetMethod("Signal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(seed, new object[] { true });
                Next(12);
                break;
            case 12 when since > 1f:
                Press(UnityEngine.InputSystem.Key.A);
                if (Object.FindFirstObjectByType<PrototypeFind>().Found) { Expect(true, "with the switch open, the find in the nook"); Press(); Next(13); }
                else if (since > 6f) { Expect(false, $"reach the find (at {p.x:F1}, {p.y:F1})"); Press(); Next(13); }
                break;
            case 13:
                Walk(true);
                if (p.x > PalmPrototypeBuilder.TunnelOut + 2f && p.y > 20f) { Expect(true, $"the tunnel climbs back out to the ridge ({since:F1} s)"); Press(); Place(new Vector2(80f * B, 5.3f * B)); Next(20); }
                else if (since > 20f) { Expect(false, $"climb out through the tunnel (stuck at {p.x:F1}, {p.y:F1})"); Press(); Place(new Vector2(80f * B, 5.3f * B)); Next(20); }
                break;

            // Before the quake the cliff beyond the ravine stops him.
            case 20:
                Walk(true);
                if (since > 5f)
                {
                    Expect(p.x < 92f * B, $"before the quake the cliff stops him (x {p.x:F1})");
                    Press(); Place(new Vector2(150f, SurfaceAt(150f) + .6f)); Next(30);
                }
                break;

            // Walking into the knot starts the quake.
            case 30:
                Walk(false);
                if (PalmGripStir.IsPlaying) { Expect(true, "walking into the knot starts the quake"); Press(); Next(40); }
                else if (since > 10f) { Expect(false, $"the quake starts (Qori at {p.x:F1}, {p.y:F1})"); Finish(1); }
                break;
            case 40:
                if (!shotB && since > 3.6f) { shotB = true; Shot("play_04_quake"); }
                if (Stir.Done)
                {
                    bool solid = true;
                    foreach (var s in Stir.thumbSegments) solid &= s.GetComponent<PolygonCollider2D>().enabled;
                    Expect(solid, "the fallen pillar is solid ground");
                    Expect(Mathf.Abs(view.orthographicSize - 5f) < .05f, $"the camera is back to normal ({view.orthographicSize:F2})");
                    Next(50);
                }
                else if (since > 16f) { Expect(false, "the quake finishes"); Finish(1); }
                break;

            // Over the fallen pillar, onto the cliff top and across the dip.
            case 50:
                Walk(true);
                if (!shotC && p.x > 86f * B && p.x < 91f * B) { shotC = true; Shot("play_05_on_pillar"); }
                if (p.x > 116f * B) { Expect(p.y > 30f, $"crosses the fallen pillar onto the cliff top and over the dip ({since:F1} s, y {p.y:F1})"); Next(60); }
                else if (since > 25f) { Expect(false, $"cross the fallen pillar (stuck at {p.x:F1}, {p.y:F1})"); Next(60); }
                break;

            // Up the causeway to the end.
            case 60:
                Walk(true);
                if (p.x > 158f * B) { Next(70); }
                else if (since > 30f) { Expect(false, $"reach the causeway's end (stuck at {p.x:F1}, {p.y:F1})"); Next(80); }
                break;
            case 70:
                Press();
                if (since > 2.2f)
                {
                    Expect(view.orthographicSize > 10f, $"climbs the causeway to the end, where the view opens out (size {view.orthographicSize:F1})");
                    Shot("play_06_causeway_end"); Next(80);
                }
                break;

            case 80:
                Press();
                Expect(errors == 0, $"no runtime errors ({errors}; {ignored} known scratch-copy shader errors ignored)");
                Debug.Log($"[PalmTest] finished with {failures} failure(s)");
                Finish(failures == 0 ? 0 : 1);
                break;
        }
    }

    static float SurfaceAt(float x)
    {
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 150f), Vector2.down, 200f, LayerMask.GetMask("Ground"));
        return hit ? hit.point.y : 0f;
    }
}
