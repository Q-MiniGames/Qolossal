using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Real Play-mode check of the Wind Leaf dash and the Glidecap glide in A0_TestRoom (batch), from
// a new game, pressing keys on a virtual keyboard. Without the relics C and a held Space do
// nothing special. With Wind Leaf: a level dash of about three units on the ground; one air dash
// that holds his height, renewed by landing. With the Glidecap: holding Space while falling
// slows him to a drift with the canopy open; letting go closes it. Renders a dash and a glide to
// <-captureDir>/dash.png and glide.png. The player's own save file is set aside and put back.
// Usage (no -quit): -executeMethod MovementAbilityPlayTest.Run
[InitializeOnLoad]
public static class MovementAbilityPlayTest
{
    const string Key = "Qolossal.MovementAbilityPlayTest";
    static string Backup => GameSave.FilePath + ".abilitytest";
    static readonly Vector2 Ground = new Vector2(-16f, .6f);   // A0's flat start ground (x -16 .. -8)
    static int stage, failures, errors, dashes; static float stageAt, y0, minVy; static Vector2 from;
    static Keyboard keys;
    static InputSettings.EditorInputBehaviorInPlayMode focusRule;
    static InputSettings.BackgroundBehavior backgroundRule;

    static MovementAbilityPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[AbilityTest] runtime error: " + m); } };
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

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[AbilityTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static Rigidbody2D Body => Qori.GetComponent<Rigidbody2D>();
    static void Place(Vector2 at) { Qori.PlaceAt(at, 1f, false); }
    static void Next(int s) { stage = s; stageAt = Time.time; }
    static void Press(params Key[] held) => InputSystem.QueueStateEvent(keys, new KeyboardState(held));
    static void Unlock(string relic)
    {
        foreach (var a in Relics.All) if (a.abilityId == relic) Qori.GetComponent<PlayerAbilityController>().Unlock(a);
    }

    static void NeedNewPress(bool on) =>
        typeof(PlayerMovement).GetField("glideNeedsNewPress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Qori, on);

    // A floor and a wall well away from the level; Qori stands at WallFoot, touching the wall.
    static readonly Vector2 WallFoot = new Vector2(303.62f, 50.6f);
    static void BuildWall()
    {
        int ground = LayerMask.NameToLayer("Ground");
        void Box(string name, Vector2 centre, Vector2 size)
        {
            var box = new GameObject(name) { layer = ground };
            box.transform.position = centre;
            box.AddComponent<BoxCollider2D>().size = size;
        }
        Box("Test Floor", new Vector2(300f, 49.5f), new Vector2(20f, 1f));
        Box("Test Wall", new Vector2(305f, 60f), new Vector2(2f, 20f));
    }

    // Holds D toward the wall and taps Jump (.25 s held, .15 s released) for three seconds,
    // counting wall jumps, the highest point and any glide. Writes each frame to the trace.
    static float climbTop; static int climbJumps, climbLaunch; static bool climbGlided, glideSeen;
    static (float top, int jumps) climbWithout;
    static StreamWriter trace;
    static void StartClimb()
    {
        Place(WallFoot); climbTop = WallFoot.y; climbJumps = 0; climbGlided = false; climbLaunch = Qori.LaunchVersion;
        if (trace == null)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-captureDir");
            string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/AbilityCaptures";
            Directory.CreateDirectory(folder);
            trace = new StreamWriter(Path.Combine(folder, "wall_trace.txt"));
        }
        trace.WriteLine($"--- climb, glidecap {Qori.GetComponent<PlayerAbilityController>().HasAbility(Relics.Glidecap)}");
    }
    static bool Climb(float age)
    {
        var q = Qori;
        if (age < .3f) { Press(UnityEngine.InputSystem.Key.D); return true; }   // settle against the wall
        if (age > 3.3f) { Press(); return false; }
        bool jump = (age - .3f) % .4f < .25f;
        if (jump) Press(UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.Space); else Press(UnityEngine.InputSystem.Key.D);
        if (q.LaunchVersion != climbLaunch) { climbLaunch = q.LaunchVersion; if (q.LastLaunchKind == PlayerMovement.LaunchKind.WallJump) climbJumps++; }
        climbTop = Mathf.Max(climbTop, Body.position.y);
        climbGlided |= q.IsGliding;
        trace?.WriteLine($"{age:F3} jump {(jump ? 1 : 0)} y {Body.position.y:F2} vy {Body.linearVelocity.y:F2} x {Body.position.x - WallFoot.x:F2} wall {q.WallDirection} slide {q.IsWallSliding} glide {q.IsGliding} launch {q.LastLaunchKind}#{q.LaunchVersion} ground {q.IsGrounded}");
        return true;
    }

    static void Shot(string name, Vector2 centre)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string folder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/AbilityCaptures";
        Directory.CreateDirectory(folder);
        var camera = Camera.main; Vector3 keep = camera.transform.position; float size = camera.orthographicSize;
        camera.transform.position = new Vector3(centre.x, centre.y, keep.z); camera.orthographicSize = 3f;
        var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null; camera.transform.position = keep; camera.orthographicSize = size;
        Object.DestroyImmediate(target); Object.DestroyImmediate(read);
    }

    // Any exception ends the run as a failure, rather than repeating the step every frame.
    static void Tick()
    {
        try { Step(); }
        catch (System.Exception e)
        {
            Debug.LogError("[AbilityTest] aborted at stage " + stage + ": " + e);
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); }
            EditorApplication.Exit(1);
        }
    }

    static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;
        var q = Qori;
        if (q == null) return;
        minVy = Mathf.Min(minVy, Body.linearVelocity.y);
        switch (stage)
        {
            case 0 when t > 1f:
                // Batch mode has no focused Game view, which the editor otherwise requires for keys.
                focusRule = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                backgroundRule = InputSystem.settings.backgroundBehavior;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keys = InputSystem.AddDevice<Keyboard>("Ability Test Keyboard");
                var up = Physics2D.Raycast(new Vector2(-12f, 1f), Vector2.up, 12f, LayerMask.GetMask("Ground"));
                Expect(up.collider == null, "open sky above the start ground for the glide");
                Place(Ground); Next(1);
                break;
            // Without the relics nothing happens.
            case 1 when t - stageAt > .5f:
                from = Body.position; Press(UnityEngine.InputSystem.Key.C); Next(2);
                break;
            case 2 when t - stageAt > .1f:
                Press(); Next(3);
                break;
            case 3 when t - stageAt > .5f:
                Expect(q.DashVersion == 0 && Mathf.Abs(Body.position.x - from.x) < .3f, "without Wind Leaf, C does nothing");
                Place(Ground + new Vector2(4f, 8f)); minVy = 0f; Press(UnityEngine.InputSystem.Key.Space); Next(4);
                break;
            case 4 when t - stageAt > .8f:
                Expect(minVy < -6f && !q.IsGliding, $"without the Glidecap, holding Space he just falls (down to {minVy:F1} u/s)");
                Press(); Place(Ground); Unlock(Relics.WindLeaf); Unlock(Relics.Glidecap); Next(5);
                break;
            // Ground dash.
            case 5 when t - stageAt > .6f:
                from = Body.position; y0 = from.y; Press(UnityEngine.InputSystem.Key.C); Next(6);
                break;
            case 6 when t - stageAt > .08f:
                Expect(q.IsDashing, "with Wind Leaf, C starts a dash");
                Shot("dash", Body.position + new Vector2(-1f, .8f));
                Press(); Next(7);
                break;
            case 7 when t - stageAt > .5f:
            {
                float dx = Body.position.x - from.x;
                Expect(!q.IsDashing && dx > 2.4f && dx < 4.6f && Mathf.Abs(Body.position.y - y0) < .15f, $"a level ground dash of about three units ({dx:F2} u, rise {Body.position.y - y0:F2})");
                Place(Ground + new Vector2(0f, 5f)); Next(8);
                break;
            }
            // Air dash: once, holding height, renewed by landing.
            case 8 when t - stageAt > .15f:
                from = Body.position; dashes = q.DashVersion; Press(UnityEngine.InputSystem.Key.C); Next(9);
                break;
            case 9 when t - stageAt > .12f:
                Expect(q.IsDashing && q.AirDashUsed && Body.position.y > from.y - .45f, $"an air dash holds his height (dropped {from.y - Body.position.y:F2} u)");
                Press(); Next(10);
                break;
            case 10 when t - stageAt > .3f:
                Press(UnityEngine.InputSystem.Key.C); Next(11);
                break;
            case 11 when t - stageAt > .1f:
                Expect(!q.IsDashing && q.DashVersion == dashes + 1, "only one dash until he lands");
                Press(); Next(12);
                break;
            case 12 when q.IsGrounded || t - stageAt > 3f:
                Expect(q.IsGrounded && !q.AirDashUsed, "landing renews the air dash");
                // By default (no new press needed) a jump held past its top opens the Glidecap.
                minVy = 0f; Press(UnityEngine.InputSystem.Key.Space); Next(121);
                break;
            case 121 when t - stageAt > .9f:
                Expect(q.IsGliding, "holding Jump through a jump opens the Glidecap once he falls (default)");
                Press(); Next(123);
                break;
            case 123 when q.IsGrounded || t - stageAt > 4f:
                // With "Glide Needs New Press" the same held jump stays a jump.
                NeedNewPress(true);
                minVy = 0f; Press(UnityEngine.InputSystem.Key.Space); Next(124);
                break;
            case 124 when t - stageAt > 1.1f:
                Expect(!q.IsGliding && (minVy < -4f || q.IsGrounded), $"with Glide Needs New Press, holding Jump through a jump doesn't open it (fell at {minVy:F1} u/s)");
                Press(); Next(122);
                break;
            case 122 when t - stageAt > .3f:
                Place(Ground + new Vector2(4f, 9f)); Press(UnityEngine.InputSystem.Key.Space); Next(13);
                break;
            // Glide.
            case 13 when t - stageAt > .9f:
            {
                float vy = Body.linearVelocity.y;
                var cap = GameObject.Find("Glidecap");
                Expect(q.IsGliding && vy > -2.3f && vy < -1.3f, $"pressing Space in mid-air and holding it opens the Glidecap: a slow drift ({vy:F2} u/s)");
                var rig = q.GetComponentInChildren<QoriAnimator>();
                Expect(cap != null && cap.GetComponent<SpriteRenderer>().enabled && rig != null && rig.HandNear != null &&
                       Vector2.Distance(cap.transform.position, rig.HandNear.position) < .01f &&
                       cap.GetComponent<SpriteRenderer>().bounds.max.y > rig.head.bounds.max.y + .2f,
                       $"the canopy is open over his head, its grip in his free hand (hand {rig.HandNear.position.y - Body.position.y:F2} u up)");
                Expect(rig.weapon != null && rig.weapon.enabled && rig.weapon.sprite != null, "he keeps the sword in hand while gliding");
                Expect(rig.head.sprite != null && !rig.head.sprite.name.Contains("Up"), $"and looks ahead, not up ({rig.head.sprite?.name})");
                Shot("glide", Body.position + new Vector2(0f, .6f));
                Press(UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.D); Next(14);
                break;
            }
            case 14 when t - stageAt > .5f:
                Expect(q.IsGliding && Body.linearVelocity.x > 3f, $"it can be steered ({Body.linearVelocity.x:F1} u/s across)");
                Press(); minVy = 0f; Next(15);
                break;
            case 15 when t - stageAt > .5f:
            {
                var cap = GameObject.Find("Glidecap");
                Expect(!q.IsGliding && minVy < -4f && (cap == null || !cap.GetComponent<SpriteRenderer>().enabled), $"letting go closes it, and he falls ({minVy:F1} u/s)");
                // The wall: a floor and a tall clingable wall away from the level.
                BuildWall();
                Unlock(Relics.ClimbingMoss);
                q.GetComponent<PlayerAbilityController>().Lock(Relics.Glidecap);
                StartClimb(); Next(200);
                break;
            }
            // Climbing by pressing Jump against the wall, first without the Glidecap, then with it:
            // the Glidecap must change nothing.
            case 200:
                if (Climb(t - stageAt)) break;
                climbWithout = (climbTop, climbJumps);
                Unlock(Relics.Glidecap);
                StartClimb(); Next(201);
                break;
            case 201:
                if (Climb(t - stageAt)) break;
                Expect(!climbGlided, "climbing the wall never opens the Glidecap");
                Expect(climbJumps == climbWithout.jumps && Mathf.Abs(climbTop - climbWithout.top) < .3f,
                       $"and climbs as high with it as without (wall jumps {climbJumps} vs {climbWithout.jumps}, top {climbTop:F2} vs {climbWithout.top:F2})");
                // Sliding: holding toward the wall while falling beside it.
                Press(); Place(WallFoot + new Vector2(0f, 7f)); Press(UnityEngine.InputSystem.Key.D); Next(202);
                break;
            case 202 when t - stageAt > .6f:
                // The slide caps at 2.5 u/s before the physics step adds one step of gravity.
                Expect(q.IsWallSliding && !q.IsGliding && Body.linearVelocity.y > -3.3f, $"holding toward the wall slides down it ({Body.linearVelocity.y:F2} u/s)");
                // Gliding into the wall: the slide takes over.
                Press(); Place(WallFoot + new Vector2(-3f, 7f)); Next(203);
                break;
            case 203 when t - stageAt > .1f:
                Press(UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.D); Next(204);
                break;
            case 204 when t - stageAt > 1.2f:
                Expect(q.IsWallSliding && !q.IsGliding, $"gliding into the wall becomes a wall slide (sliding {q.IsWallSliding}, gliding {q.IsGliding}, x {Body.position.x - WallFoot.x:F2})");
                Press(UnityEngine.InputSystem.Key.D); Next(205);
                break;
            case 205 when t - stageAt > .1f:
                Press(UnityEngine.InputSystem.Key.D, UnityEngine.InputSystem.Key.Space); Next(206);
                break;
            case 206 when t - stageAt > .15f:
                Expect(q.LastLaunchKind == PlayerMovement.LaunchKind.WallJump && !q.IsGliding, $"and Jump from the slide is a wall jump ({q.LastLaunchKind})");
                Press(); Next(207);
                break;
            // A press a moment too early for a wall jump, falling just clear of the wall, is for the
            // wall: it must not open the Glidecap. Two units away the same press does.
            case 207 when t - stageAt > .3f:
                Place(WallFoot + new Vector2(-.35f, 6f)); Next(208);
                break;
            case 208 when t - stageAt > .1f:
                Press(UnityEngine.InputSystem.Key.Space); glideSeen = false; Next(209);
                break;
            case 209:
                glideSeen |= q.IsGliding;
                if (t - stageAt < .6f) break;
                Expect(!glideSeen, "a press just clear of the wall doesn't open the Glidecap");
                Press(); Place(WallFoot + new Vector2(-2.2f, 6f)); Next(210);
                break;
            case 210 when t - stageAt > .1f:
                Press(UnityEngine.InputSystem.Key.Space); Next(211);
                break;
            case 211 when t - stageAt > .4f:
                Expect(q.IsGliding, "two units from the wall the same press opens it");
                Press(); Next(299);
                break;
            case 299 when t - stageAt > .3f:
            {
                if (trace != null) { trace.Close(); trace = null; }
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[AbilityTest] finished with {failures} failure(s)");
                InputSystem.RemoveDevice(keys);
                InputSystem.settings.editorInputBehaviorInPlayMode = focusRule;
                InputSystem.settings.backgroundBehavior = backgroundRule;
                if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); } else GameSave.Clear();
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                break;
            }
        }
    }
}
