#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mra;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// The Mountain Relief Atlas play test (editor only; started by MraPlayTest.Run in batch mode with a
// dedicated save, "-saveFile mra_playtest.json", so the player's own save is never touched).
// It plays the real scenes with the live Qori rig and a virtual keyboard and gamepad:
//   1  the main route, Cradle to the end of the Descent, walking (an autopilot that runs and jumps
//      when blocked), through every region exit, past every shrine, knot and Waymark; the relic
//      gates checked without and with their relic (the thread lift, the glide span), the quake
//      gate before and after its knot, the sluice by its wheel, the reveal and the ending;
//   2  every side chamber: prerequisite refusal, entry, its mechanic, the reward once, the return
//      to a safe spot outside with the checkpoint kept, and no second reward on a revisit;
//   3  Qvale: cutaway homes, rescued residents at home, a purchase, pages for visited places,
//      song shells at the listening tree; Waymark travel there and back;
//   4  saving: everything above after reloading the save; the descent's reveal gate on direct load;
//      the Cradle's quake state after reload; checkpoint respawn; backward travel;
//   5  the Chart: marker correspondence at every beat, gamepad-only control with the mouse
//      removed, focus and zoom kept, a gamepad reconnect; captures at 1920x1080 and 1280x720.
// Results: "[MraTest] PASS/FAIL ..." lines and <captureDir>/mra_test_results.json.
public sealed partial class MraTestDriver : MonoBehaviour
{
    public string captureDir = "Temp/Captures";
    readonly List<string> results = new List<string>();
    int passes, failures;
    Keyboard keys; Gamepad pad;
    readonly Dictionary<string, List<float>> frameTimes = new Dictionary<string, List<float>>();
    readonly List<string> chartRows = new List<string>();
    bool polishOnly;   // -mraPolishOnly: the earlier parts (and the progress they build) are skipped

    World W => World.Load();
    PlayerMovement Qori => FindAnyObjectByType<PlayerMovement>();
    Rigidbody2D Body => Qori.GetComponent<Rigidbody2D>();
    PlayerAbilityController Abilities => Qori.GetComponent<PlayerAbilityController>();
    string SceneName => SceneManager.GetActiveScene().name;

    void Expect(bool ok, string what)
    {
        if (ok) passes++; else failures++;
        string line = (ok ? "PASS " : "FAIL ") + what;
        results.Add(line); Debug.Log("[MraTest] " + line);
    }

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keys = InputSystem.AddDevice<Keyboard>("MRA Test Keyboard");
        Expect(Path.GetFileName(GameSave.FilePath) == "mra_playtest.json", "the test uses its own save file (" + Path.GetFileName(GameSave.FilePath) + ")");
        GameSave.Clear();
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-mraDepthCapture");
            if (at >= 0)
            {
                // A recorded run for the background comparison (MraDepthCapture.cs).
                foreach (var id in new[] { Relics.ClimbingMoss, Relics.LivingThread, Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap }) GameSave.AddRelic(id);
                yield return Run("depth capture", DepthCapture(at + 1 < args.Length ? args[at + 1] : "run"));
                Finish(); yield break;
            }
        }
        if (System.Environment.GetCommandLineArgs().Contains("-mraAudioOnly"))
        {
            // The sound-effect hooks alone (MraAudioTest.cs).
            foreach (var id in new[] { Relics.ClimbingMoss, Relics.LivingThread, Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap }) GameSave.AddRelic(id);
            foreach (var r in W.regions) GameSave.SetFlag(MraState.VisitedFlag(r.id));
            foreach (var f in new[] { "mra:MR02_C01:reward", "mra:MR03_C03:reward", "mra:MR05_C01:reward" }) GameSave.SetFlag(f);   // Brannick and others home
            yield return Run("audio", AudioChecks());
            Finish(); yield break;
        }
        if (System.Environment.GetCommandLineArgs().Contains("-mraEncountersOnly"))
        {
            yield return Run("encounters", EncounterChecks());
            Finish(); yield break;
        }
        if (System.Environment.GetCommandLineArgs().Contains("-mraPolishOnly"))
        {
            polishOnly = true;
            // The polish checks alone: Qvale's houses and overlook, transitions and parallax.
            foreach (var id in new[] { Relics.ClimbingMoss, Relics.LivingThread, Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap }) GameSave.AddRelic(id);
            foreach (var r in W.regions) GameSave.SetFlag(MraState.VisitedFlag(r.id));
            foreach (var f in new[] { "mra:MR02_C01:reward", "mra:MR03_C03:reward", "mra:MR05_C01:reward" }) GameSave.SetFlag(f);
            GameSave.SetFlag("mra:MR01_C01:reward"); TownState.Set("shell:mra:MR01_C01:reward");
            yield return Run("climb repeat", ClimbRepeat("MRAtlas_MR03_Terraces", new Vector2(189.5f, 118.3f), 197.5f, 5));
            yield return Run("Qvale", Town());
            yield return Run("transitions and parallax", TransitionChecks());
            Finish(); yield break;
        }
        yield return Load("MRAtlas_MR01_Cradle");
        yield return Run("main route", MainRoute());
        yield return Run("side chambers", Chambers());
        yield return Run("Qvale", Town());
        yield return Run("saving", Saving());
        yield return Run("Chart", ChartChecks());
        yield return Run("encounters", EncounterChecks());
        yield return Run("transitions and parallax", TransitionChecks());
        Finish();
    }

    IEnumerator Run(string label, IEnumerator body)
    {
        Debug.Log("[MraTest] --- " + label);
        // Each part runs on; an exception fails the part and the test moves on.
        while (true)
        {
            object current;
            try { if (!body.MoveNext()) break; current = body.Current; }
            catch (Exception e) { Expect(false, label + " threw: " + e.Message + "\n" + e.StackTrace); Press(); break; }
            yield return current;
        }
    }

    void Finish()
    {
        Press();
        Directory.CreateDirectory(captureDir);
        var timing = frameTimes.Select(kv => $"  \"{kv.Key}\": {{\"frames\": {kv.Value.Count}, \"mean_ms\": {kv.Value.Average() * 1000f:F2}, \"p95_ms\": {Percentile(kv.Value, .95f) * 1000f:F2}, \"max_ms\": {(kv.Value.Max() * 1000f).ToString("F2")}" + "}");
        File.WriteAllText(Path.Combine(captureDir, "mra_test_results.json"),
            "{\n \"passes\": " + passes + ", \"failures\": " + failures + ",\n \"results\": [\n" + string.Join(",\n", results.Select(r => "  \"" + r.Replace("\\", "/").Replace("\"", "'").Replace("\n", " ") + "\"")) +
            "\n ],\n \"chart_correspondence\": [\n" + string.Join(",\n", chartRows) + "\n ],\n \"editor_frame_times\": {\n" + string.Join(",\n", timing) + "\n }\n}\n");
        Debug.Log($"[MraTest] DONE {passes} passed, {failures} failed");
        UnityEditor.EditorApplication.Exit(failures > 0 ? 1 : 0);
    }

    static float Percentile(List<float> v, float p) { var s = v.OrderBy(x => x).ToList(); return s[Mathf.Clamp(Mathf.RoundToInt(p * (s.Count - 1)), 0, s.Count - 1)]; }

    // ---------------------------------------------------------------- input and scenes

    void Press(params Key[] held) => InputSystem.QueueStateEvent(keys, new KeyboardState(held));
    // Holds `held`, plus Space while `jump`.
    void PressJump(bool jump, params Key[] held) => Press(jump ? held.Append(Key.Space).ToArray() : held);

    IEnumerator Tap(Key key) { Press(key); yield return null; yield return null; Press(); yield return null; }
    // Real time for physics to run (batch-mode frames are uncapped: frame counts mean nothing).
    // Unscaled: the open Chart pauses game time.
    IEnumerator Wait(float seconds) { float until = Time.unscaledTime + seconds; while (Time.unscaledTime < until) yield return null; }

    IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return null; yield return null;
        yield return Settle();
    }

    IEnumerator Settle()
    {
        while (AreaTransition.IsTransitioning) yield return null;
        yield return Wait(0.25f);
        var enc = GameObject.Find(MraWorldBuilderNames.Generated + "/Encounters");
        if (enc != null) enc.SetActive(false);   // the route test walks; encounters are checked separately
    }

    IEnumerator WaitScene(string scene, float timeout = 8f)
    {
        float t0 = Time.realtimeSinceStartup;
        while ((SceneName != scene || AreaTransition.IsTransitioning) && Time.realtimeSinceStartup - t0 < timeout) yield return null;
        yield return Settle();
    }

    void Place(Vector2 at) { Qori.PlaceAt(at, 1f, false); }

    bool InGround()
    {
        var box = Qori.GetComponent<BoxCollider2D>();
        var hits = Physics2D.OverlapBoxAll(Body.position + box.offset, box.size * .9f, 0f, LayerMask.GetMask("Ground"));
        return hits.Any(h => !h.isTrigger && h.attachedRigidbody != Body);
    }

    float GroundAt(float x, float fromY) { var hit = Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, fromY + 60f, LayerMask.GetMask("Ground")); return hit ? hit.point.y : float.NaN; }

    void Unlock(string relic) { foreach (var a in Relics.All) if (a.abilityId == relic) Abilities.Unlock(a); }

    // ---------------------------------------------------------------- 1 the main route

    IEnumerator MainRoute()
    {
        foreach (var r in W.regions)
        {
            if (SceneName != r.scene) { Expect(false, $"{r.id}: expected to be in {r.scene}, but in {SceneName}"); yield return Load(r.scene); }
            float t0 = Time.time;
            Expect(!InGround(), $"{r.id}: Qori arrives clear of the ground at {Body.position}");
            Expect(GameSave.HasFlag(MraState.VisitedFlag(r.id)), $"{r.id}: visit saved");
            yield return WalkRegion(r);
            Expect(true, $"{r.id}: walked in {Time.time - t0:F0} s");
        }
        Expect(GameSave.HasFlag(MraState.RevealFlag), "the reveal saved mra:reveal-complete");
        Expect(GameSave.HasFlag(MraState.EndingFlag), "the careful ending saved mra:careful-ending");
        foreach (var id in new[] { Relics.ClimbingMoss, Relics.LivingThread, Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap })
            Expect(GameSave.HasRelic(id), "relic saved on the route: " + id);
        foreach (var k in new[] { "grip", "reach", "spring", "breath", "bloom", "sight", "heart" })
            Expect(GameSave.IsKnotAwake(k), "knot woken on the route: " + k);
    }

    // Runs right through a region until its exit loads the next one (or, in the descent, the end).
    IEnumerator WalkRegion(Region r)
    {
        var times = frameTimes[r.id] = new List<float>();
        var seenNodes = new HashSet<string>();
        float stall = 0f, jumpUntil = -1f, stuckSince = -1f, lastX = Body.position.x, bestX = lastX;
        float tEnd = Time.time + 300f;
        bool liftDone = false, glideDone = false, gateChecked = false, sluiceChecked = false;
        Vector2 last = Pos(r.NodeById(r.exit));
        while (SceneName == r.scene)
        {
            if (Time.time > tEnd) { Expect(false, $"{r.id}: route timed out at {Body.position}"); yield break; }
            times.Add(Time.unscaledDeltaTime);
            var q = Qori; var body = Body;
            float x = body.position.x;
            // Chart correspondence: Qori's marker against each beat's authored map position.
            foreach (var n in r.nodes)
                if (!seenNodes.Contains(n.id) && Mathf.Abs(x - Pos(n).x) < 1.5f && MraRouteTracker.Current != null && MraRouteTracker.Current.HasFix && MraRouteTracker.Current.Fix.distance < 2.5f)
                {
                    seenNodes.Add(n.id);
                    float d = Vector2.Distance(MraRouteTracker.Current.Fix.uv, n.chartUv);
                    chartRows.Add("  {\"node\": \"" + n.id + "\", \"uv_error\": " + d.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "}");
                    if (d > .03f) Expect(false, $"{n.id}: Chart marker {MraRouteTracker.Current.Fix.uv} is {d:F3} from the beat's {n.chartUv}");
                }

            // Cutscenes and dialogue: wait, tapping through the stills.
            if (MraKnot.IsPlaying || MraReveal.IsPlaying || MraEnding.IsPlaying || DialogueBox.IsOpen || AreaTransition.IsTransitioning)
            {
                stuckSince = Time.time;   // cutscenes don't count as being stuck
                Press(); yield return null;
                if (MraReveal.IsPlaying || MraEnding.IsPlaying || DialogueBox.IsOpen) { yield return Tap(Key.Enter); yield return Wait(0.33f); }
                continue;
            }

            // Module checks, each once.
            if (r.id == "MR01" && !gateChecked && x > 214f) { gateChecked = true; yield return QuakeGate(r); stuckSince = Time.time; continue; }
            if (r.id == "MR02" && !liftDone && x > 252f) { liftDone = true; yield return ThreadLift(r); stuckSince = Time.time; continue; }
            if (r.id == "MR03" && !sluiceChecked && x > 238f) { sluiceChecked = true; yield return SluiceGate(r); stuckSince = Time.time; continue; }
            if (r.id == "MR06" && !glideDone && x > Pos(r.NodeById("MR06_N10")).x + 1f) { glideDone = true; yield return GlideSpan(r); stuckSince = Time.time; continue; }
            if (r.id == "MR08")
            {
                var ending = FindAnyObjectByType<MraEnding>();
                if (ending != null && !ending.Done && Mathf.Abs(x - ending.transform.position.x) < 1.5f) { Press(); ending.Play(); yield return null; continue; }
                if (x > last.x + 4f) { Expect(true, "MR08: reached the ending overlook"); Press(); yield break; }
            }

            // The autopilot: run right; jump when blocked on the ground.
            // Blocked on the ground, or hanging from a ledge (Climbing Moss): jump, holding up.
            float vx = body.linearVelocity.x;
            bool hanging = q.IsLedgeHanging || q.IsWallSliding;
            stall = (q.IsGrounded || hanging) && Mathf.Abs(vx) < .6f ? stall + Time.deltaTime : 0f;
            if (stall > .12f && Time.time > jumpUntil) { jumpUntil = Time.time + .42f; stall = 0f; }
            // Truly stuck (no progress for 6 s): report and nudge on with a teleport so the rest still runs.
            if (x > bestX + .5f) { bestX = x; stuckSince = Time.time; }
            if (stuckSince < 0f) stuckSince = Time.time;
            if (Time.time - stuckSince > 6f)
            {
                Expect(false, $"{r.id}: stuck at {body.position} (the route isn't walkable here)");
                float nx = x + 3f; Place(new Vector2(nx, GroundAt(nx, body.position.y + 30f) + 1f)); stuckSince = Time.time;
            }
            var held = new List<Key> { Key.D, Key.LeftShift };
            if (Time.time < jumpUntil) held.Add(Key.Space);
            if (q.IsLedgeHanging) held.Add(Key.W);   // climb up from a hang (never near a door otherwise)
            Press(held.ToArray());
            lastX = x;
            yield return null;
        }
        Press();
        yield return Settle();
        Expect(seenNodes.Count >= r.nodes.Length - 1, $"{r.id}: Chart marker checked at {seenNodes.Count} of {r.nodes.Length} beats");
    }

    static Vector2 Pos(Node n) => n.id == "MR06_N11" ? new Vector2(440f, 270f) : n.position;

    // The Cradle's quake crossing: the gate holds until the knot (passed just before) has woken.
    IEnumerator QuakeGate(Region r)
    {
        var gate = FindObjectsByType<MraGate>(FindObjectsSortMode.None).FirstOrDefault(g => g.requires != null && g.requires.Contains("knot:grip"));
        Expect(gate != null, "MR01: the quake gate exists");
        if (gate == null) yield break;
        Expect(GameSave.IsKnotAwake("grip") && gate.IsOpen, "MR01: walking past the knot woke it, and the gate is open after the quake");
        // Before the quake (the knot put back to sleep for this check): the gate holds.
        GameSave.SetKnotAwake("grip", false); gate.Refresh();
        yield return Wait(0.67f);
        float y = GroundAt(217f, 40f) + 1f;
        Place(new Vector2(217f, y));
        float t = Time.time;
        while (Time.time - t < 3f) { PressJump((Time.time - t) % .6f < .4f, Key.D, Key.LeftShift); yield return null; }
        Press();
        Expect(Body.position.x < 221f, $"MR01: before the quake the gate and its cap hold Qori back (x {Body.position.x:F1})");
        var bridge = FindObjectsByType<MraVariant>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(v => v.requires.Contains("knot:grip"));
        Expect(bridge != null && !bridge.Shown, "MR01: no crossing before the quake");
        GameSave.SetKnotAwake("grip", true);
        yield return Wait(0.83f);
        Expect(gate.IsOpen && bridge != null && bridge.Shown, "MR01: after the quake the gate opens and the fallen crossing is there");
    }

    // The Causeway's thread lift: no way up without the Living Thread; with it, cast, pull, step off.
    IEnumerator ThreadLift(Region r)
    {
        var e = r.edges.First(x => x.module.template == "flat-thread-gap");
        float lip = e.module.position.x + .5f, yU = GroundAt(lip + 3f, 200f), yL = GroundAt(lip - 1f, yU - .5f);
        Expect(yU - yL > 3.5f, $"MR02: the lift wall is {yU - yL:F1} u above its foot step (a jump rises about 2.45 u)");
        Expect(GameSave.HasRelic(Relics.LivingThread), "MR02: the Living Thread shrine (before the lift) was collected on the way");
        Abilities.Lock(Relics.LivingThread);
        Place(new Vector2(lip - 1.2f, yL + 1f));
        float t = Time.time;
        while (Time.time - t < 3f) { PressJump((Time.time - t) % .6f < .4f, Key.D); yield return null; }
        Press();
        Expect(Body.position.y < yU - 1f, $"MR02: without the thread Qori can't get up the lift (best y {Body.position.y:F1} vs ledge {yU:F1})");
        Unlock(Relics.LivingThread);
        var thread = Qori.GetComponent<PlayerThread>();
        Place(new Vector2(lip - 1.2f, yL + 1f));
        yield return Wait(0.33f);
        Press(Key.Space); t = Time.time;
        // Cast on every frame of the jump until it catches, as a player mashing the button would.
        var nearest = ThreadAnchor.Active.OrderBy(a => Vector2.Distance(a.transform.position, Body.position)).FirstOrDefault();
        float bestDist = float.MaxValue;
        while (Time.time - t < 1.2f && !thread.IsAttached)
        {
            if (nearest != null) bestDist = Mathf.Min(bestDist, Vector2.Distance(nearest.transform.position, Body.position));
            if (thread.HasTarget) thread.ToggleHook();
            yield return null;
        }
        Expect(thread.IsAttached, $"MR02: the thread catches the anchor from a jump (anchors {ThreadAnchor.Active.Count}, nearest at {(nearest != null ? (Vector2)nearest.transform.position : Vector2.zero)}, " +
            $"closest approach {bestDist:F2} u, unlocked {thread.Unlocked}, input blocked {GamePauseMenu.BlocksGameplayInput}, modal {ModalUi.IsOpen})");
        Press(Key.W); t = Time.time;
        while (Time.time - t < 3f && Body.position.y < yU + .6f) yield return null;
        Press(Key.W, Key.D);
        yield return Wait(0.25f);
        if (thread.IsAttached) thread.ToggleHook();
        Press(Key.D);
        yield return Wait(1.00f);
        Press();
        Expect(Body.position.x > lip && Body.position.y > yU - .3f, $"MR02: with the thread Qori is up on the ledge ({Body.position})");
    }

    // The Terraces' sluice: the gate holds until its wheel turns; then it stays open.
    IEnumerator SluiceGate(Region r)
    {
        var gate = FindObjectsByType<MraGate>(FindObjectsSortMode.None).First(g => g.latchFlag.Contains("sluice"));
        float gx = gate.transform.position.x;
        float t = Time.time;
        while (Time.time - t < 3f) { PressJump((Time.time - t) % .6f < .4f, Key.D); yield return null; }
        Press();
        Expect(Body.position.x < gx, $"MR03: the sluice gate holds while closed (x {Body.position.x:F1} vs gate {gx:F1})");
        var wheel = FindObjectsByType<MraLever>(FindObjectsSortMode.None).OrderBy(l => Mathf.Abs(l.transform.position.x - gx)).First();
        Place(wheel.transform.position + Vector3.up);
        yield return Wait(0.25f);
        bool near = wheel.Near;
        yield return Tap(Key.W);
        yield return Wait(0.83f);
        Expect(gate.IsOpen && GameSave.HasFlag(gate.latchFlag), $"MR03: Up at the wheel opens the sluice for good (saved) [near {near}, keyboard {Keyboard.current?.name}, lever on {wheel.IsOn}, busy {MraState.Busy}]");
    }

    // The Heights' glide span: no crossing with a running jump and a dash; with the Glidecap, yes.
    IEnumerator GlideSpan(Region r)
    {
        Vector2 n10 = Pos(r.NodeById("MR06_N10"));
        float launch = n10.x + 4f, landing = launch + 18f;
        float run = launch - 6f;
        Expect(GameSave.HasRelic(Relics.Glidecap), "MR06: the Glidecap shrine (before the span) was collected on the way");
        Abilities.Lock(Relics.Glidecap);
        yield return Leap(run, launch, n10.y, false);
        Expect(Body.position.x < landing - .5f, $"MR06: without the Glidecap, a running jump and a dash fall short (x {Body.position.x:F1}, landing at {landing:F1})");
        Unlock(Relics.Glidecap);
        yield return Leap(run, launch, n10.y, true);
        Expect(Body.position.x > landing && Body.position.y > n10.y - 3.5f, $"MR06: with the Glidecap Qori glides onto the landing ({Body.position})");
    }

    IEnumerator Leap(float from, float edge, float ground, bool glide)
    {
        Place(new Vector2(from, ground + 1f));
        yield return Wait(0.33f);
        float t = Time.time;
        while (Body.position.x < edge - .3f && Time.time - t < 3f) { Press(Key.D, Key.LeftShift); yield return null; }
        Press(Key.D, Key.LeftShift, Key.Space);
        t = Time.time;
        bool dashed = false;
        while (Time.time - t < 6f)
        {
            if (!glide && !dashed && Time.time - t > .38f) { Press(Key.D, Key.LeftShift, Key.C); dashed = true; }
            else if (!glide && dashed && Time.time - t > .45f) Press(Key.D, Key.LeftShift);
            if (Time.time - t > .3f && Qori.IsGrounded) break;
            yield return null;
        }
        Press();
        yield return Wait(0.25f);
    }

    // Each placed enemy, live, with Qori far away at the region's start: it settles on its shelf
    // (ground under it, or hovering over it for flyers) and keeps to it for a few seconds.
    IEnumerator EncounterChecks()
    {
        foreach (var r in W.regions.Where(x => W.encounters.Any(e => e.region == x.id)))
        {
            yield return Load(r.scene);
            // The traversal parts hide the group (Settle); Find skips inactive objects, so go by the root.
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == MraWorldBuilderNames.Generated);
            var group = root != null ? root.transform.Find("Encounters")?.gameObject : null;
            if (group == null) { Expect(false, r.id + ": no encounters group"); continue; }
            group.SetActive(true);
            var start = new Dictionary<Transform, Vector3>();
            foreach (Transform t in group.transform) start[t] = t.position;
            yield return Wait(3f);
            foreach (var kv in start)
            {
                var t = kv.Key; if (t == null) { Expect(false, r.id + ": an enemy vanished"); continue; }
                bool flying = t.name.Contains("Thornwing") || t.name.Contains("GustMoth");
                float ground = GroundAt(t.position.x, t.position.y + .5f);
                bool onShelf = Mathf.Abs(t.position.x - kv.Value.x) < 7f && (flying ? t.position.y - ground < 5f && t.position.y > ground : t.position.y - ground < 1.6f && kv.Value.y - t.position.y < 1.5f);
                Expect(onShelf, $"{r.id}: {t.name} keeps to its shelf ({kv.Value} -> {t.position}, ground {ground:F1})");
            }
        }
    }

    // A climb tried `times` times by the main route's autopilot, from `from` until past `pastX`.
    IEnumerator ClimbRepeat(string scene, Vector2 from, float pastX, int times)
    {
        yield return Load(scene);
        // Only what Qori has there on the main route: the later relics are put away for the climb.
        var later = new[] { Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap }.Where(id => Abilities.HasAbility(id)).ToList();
        foreach (var id in later) Abilities.Lock(id);
        for (int i = 0; i < times; i++)
        {
            Place(from); yield return Wait(0.4f);
            float t0 = Time.time, stall = 0f, jumpUntil = -1f;
            while (Body.position.x < pastX && Time.time - t0 < 15f)
            {
                var q = Qori; bool held = q.IsGrounded || q.IsLedgeHanging || q.IsWallSliding;
                stall = held && Mathf.Abs(Body.linearVelocity.x) < .6f ? stall + Time.deltaTime : 0f;
                if (stall > .12f && Time.time > jumpUntil) { jumpUntil = Time.time + .42f; stall = 0f; }
                var keys = new List<Key> { Key.D, Key.LeftShift };
                if (Time.time < jumpUntil) keys.Add(Key.Space);
                if (q.IsLedgeHanging) keys.Add(Key.W);
                Press(keys.ToArray());
                yield return null;
            }
            Press();
            Expect(Body.position.x >= pastX, $"{scene}: climb {i + 1} from {from} gets past x {pastX} in {Time.time - t0:F1} s (at {Body.position})");
        }
        foreach (var id in later) Unlock(id);
    }

    // Region thresholds and the arrival name; the background's stability while standing, jumping and
    // landing, and its controlled sinking over a sustained climb.
    IEnumerator TransitionChecks()
    {
        yield return Load("MRAtlas_MR01_Cradle");
        yield return Wait(0.3f);
        Expect(MraMusicPlayer.Current != null && MraMusicPlayer.Current.name == "Music_AwakeningInTheTitansPalm", "music: the Cradle plays its track (" + (MraMusicPlayer.Current != null ? MraMusicPlayer.Current.name : "none") + ")");
        var on = FindObjectsByType<MraExit>(FindObjectsSortMode.None).FirstOrDefault(e => e.direction > 0f);
        Expect(on != null && on.destinationName == "The Long Causeway", "MR01: the way on is named (" + (on != null ? on.destinationName : "none") + ")");
        var arch = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name.StartsWith("Threshold arch") && Mathf.Abs(r.transform.position.x - on.transform.position.x) < 2f);
        Expect(arch != null, "MR01: a threshold arch stands over the way on");
        if (arch != null)
        {
            float g = GroundAt(arch.transform.position.x, arch.bounds.max.y + 1f);
            Expect(Mathf.Abs(arch.bounds.min.y - g) < .5f, $"MR01: the arch stands on the ground (base {arch.bounds.min.y:F2}, ground {g:F2})");
        }

        // Parallax: standing, then jumping and landing in place: the layers hold their place on screen.
        var layers = FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).Where(l => l.climbProgression).ToList();
        Expect(layers.Count >= 3, $"MR01: {layers.Count} background layers on the climb progression");
        var n0 = W.RegionById("MR01").NodeById("MR01_N01");
        Place(n0.spawn); yield return Wait(6f);   // the climb smoothing settles
        var cam = Camera.main;
        float Rel(ParallaxLayer l) => l.transform.position.y - cam.transform.position.y;
        var rest = layers.ToDictionary(l => l, Rel);
        float standDrift = 0f, jumpDrift = 0f, camMove = 0f, camStart = cam.transform.position.y;
        float t0 = Time.time;
        while (Time.time - t0 < 1f) { standDrift = Mathf.Max(standDrift, layers.Max(l => Mathf.Abs(Rel(l) - rest[l]))); yield return null; }
        Frame("parallax_1_standing");
        t0 = Time.time; bool apexShot = false, landShot = false;
        while (Time.time - t0 < 3f)
        {
            if (!apexShot && Body.linearVelocity.y < 0f && !Qori.IsGrounded && Time.time - t0 > .2f) { Frame("parallax_2_jump_apex"); apexShot = true; }
            if (apexShot && !landShot && Qori.IsGrounded) { Frame("parallax_3_landed"); landShot = true; }
            PressJump((Time.time - t0) % 1f < .45f);
            jumpDrift = Mathf.Max(jumpDrift, layers.Max(l => Mathf.Abs(Rel(l) - rest[l])));
            camMove = Mathf.Max(camMove, Mathf.Abs(cam.transform.position.y - camStart));
            yield return null;
        }
        Press();
        Expect(standDrift < .01f, $"parallax: standing still, the layers don't move on screen ({standDrift:F3} u)");
        Expect(jumpDrift < .06f, $"parallax: jumping and landing (camera moved {camMove:F2} u), the layers hold their place on screen ({jumpDrift:F3} u)");
        // A sustained climb: every layer sinks a little, the near ones most, in depth order.
        var region = W.RegionById("MR01");
        var top = region.nodes.OrderByDescending(n => n.position.y).First();
        var before = layers.ToDictionary(l => l, Rel);
        Place(top.spawn); yield return Wait(6f);
        Frame("parallax_4_after_climb");
        var sunk = layers.ToDictionary(l => l, l => before[l] - Rel(l));
        var near = layers.OrderByDescending(l => l.sortingOrder).First(); var far = layers.OrderBy(l => l.sortingOrder).First();
        Expect(sunk.Values.All(v => v > -.01f) && sunk[near] >= sunk[far] - .01f, $"parallax: after climbing the layers sink in depth order (near {sunk[near]:F2} u, far {sunk[far]:F2} u)");
        Expect(sunk.Values.All(v => v < 1.3f), "parallax: the sinking is bounded (" + string.Join(", ", sunk.Values.Select(v => v.ToString("F2"))) + ")");

        // Crossing into the Causeway: the arrival names it once; back and forth again shows nothing new.
        // (Earlier parts of the run travelled here; the 90 s memory starts clean for this check.)
        AreaTransition.ForgetShownNames();
        int shown = AreaTransition.NamesShown;
        on = FindObjectsByType<MraExit>(FindObjectsSortMode.None).First(e => e.direction > 0f);
        Place((Vector2)on.transform.position + new Vector2(-4f, -4f)); yield return Wait(0.4f);
        Frame("transition_1_arch_approach");
        t0 = Time.time;
        while (SceneName == "MRAtlas_MR01_Cradle" && Time.time - t0 < 6f) { Press(Key.D); yield return null; }
        Press();
        yield return WaitScene("MRAtlas_MR02_Causeway");
        Expect(SceneName == "MRAtlas_MR02_Causeway" && AreaTransition.LastShownName == "The Long Causeway" && AreaTransition.NamesShown == shown + 1,
            $"crossing on: the Long Causeway is named on arrival (last '{AreaTransition.LastShownName}', shown {AreaTransition.NamesShown - shown})");
        yield return Wait(0.4f);
        Frame("transition_2_arrival_name");
        Expect(MraMusicPlayer.Current != null && MraMusicPlayer.Current.name == "Music_TheWetlandsSecret", "music: crossing into the Causeway switches to its track (" + (MraMusicPlayer.Current != null ? MraMusicPlayer.Current.name : "none") + ")");
        Expect(Camera.main != null && Mathf.Abs(Camera.main.transform.position.x - Body.position.x) < 3f, "crossing on: the camera frames Qori at the landing");
        for (int i = 0; i < 2; i++)
        {
            var back = FindObjectsByType<MraExit>(FindObjectsSortMode.None).First(e => e.direction < 0f);
            Place((Vector2)back.transform.position + new Vector2(3f, -4f)); yield return Wait(0.3f);
            string from = SceneName; t0 = Time.time;
            while (SceneName == from && Time.time - t0 < 6f) { Press(Key.A); yield return null; }
            Press(); yield return Settle();
            var fwd = FindObjectsByType<MraExit>(FindObjectsSortMode.None).First(e => e.direction > 0f);
            Place((Vector2)fwd.transform.position + new Vector2(-3f, -4f)); yield return Wait(0.3f);
            from = SceneName; t0 = Time.time;
            while (SceneName == from && Time.time - t0 < 6f) { Press(Key.D); yield return null; }
            Press(); yield return Settle();
        }
        Expect(SceneName == "MRAtlas_MR02_Causeway" && AreaTransition.NamesShown <= shown + 2,
            $"back and forth twice more: no repeated names ({AreaTransition.NamesShown - shown} shown in all: the Causeway once, the Cradle at most once)");
    }

    // ---------------------------------------------------------------- 2 side chambers

    IEnumerator Chambers()
    {
        foreach (var c in W.chambers.Where(c => !c.InPlace && !c.IsHouse))
        {
            var region = W.RegionById(c.region);
            yield return Load(region.scene);
            var door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == c.id);
            if (door == null) { Expect(false, c.id + ": no door in " + region.scene); continue; }
            Place((Vector2)door.transform.position + Vector2.up);
            yield return Wait(0.5f);
            string checkpointBefore = GameSave.CheckpointIn(SceneManager.GetActiveScene().path);
            Expect(GameSave.HasFlag(door.DiscoveredFlag), c.id + ": discovered at its mouth");
            if (!string.IsNullOrEmpty(c.prerequisite))
            {
                Abilities.Lock(c.prerequisite); GameSave.RemoveRelic(c.prerequisite);
                Expect(!door.Enter() && SceneName == region.scene, $"{c.id}: refused without the {c.prerequisite}");
                GameSave.AddRelic(c.prerequisite); Unlock(c.prerequisite);
            }
            Expect(door.Enter(), c.id + ": entered");
            yield return WaitScene(c.scene);
            Expect(SceneName == c.scene, c.id + ": the chamber scene loaded");
            Expect(Vector2.Distance(Body.position, c.playerSpawn) < .8f && !InGround(), $"{c.id}: Qori arrives at its safe spot {Body.position}");
            yield return Trial(c);
            var reward = FindAnyObjectByType<MraReward>(); var resident = FindAnyObjectByType<MraResident>();
            if (reward != null) { Place(reward.transform.position); yield return Wait(0.25f); Expect(reward.Taken, $"{c.id}: reward {c.reward} taken by touch"); }
            else if (resident != null) { resident.Rescue(); Expect(resident.Rescued && TownState.Has("resident:" + resident.role), $"{c.id}: {resident.displayName} rescued (resident:{resident.role})"); }
            else Expect(false, c.id + ": no reward");
            // Out through the return threshold.
            Place(new Vector2(.3f, c.playerSpawn.y));
            yield return WaitScene(region.scene);
            Expect(SceneName == region.scene, c.id + ": the return leads back to " + region.name);
            Expect(Vector2.Distance(Body.position, c.returnSpawn) < .8f && !InGround(), $"{c.id}: back outside at {Body.position} (return spot {c.returnSpawn})");
            yield return Wait(.3f);
            string after = GameSave.CheckpointIn(SceneManager.GetActiveScene().path);
            var atReturn = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).FirstOrDefault(k => Vector2.Distance(k.SpawnPosition, c.returnSpawn) < 2.5f);
            Expect(after == checkpointBefore || atReturn != null && after == atReturn.CheckpointId,
                $"{c.id}: the return keeps the checkpoint ({checkpointBefore}) or takes only the one standing at the return spot (now {after})");
            // A second visit: the reward stays taken; nothing more is paid.
            int flags = GameSave.Flags.Count, heartsSaved = GameSave.HeartSeeds, amberSaved = TownState.Amber;
            door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).First(d => d.chamberId == c.id);
            door.Enter(); yield return WaitScene(c.scene);
            var again = FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (again != null)
            {
                Expect(!again.gameObject.activeInHierarchy, c.id + ": the reward is gone on a second visit");
                Expect(!again.Collect(Qori.GetComponent<PlayerHealth>()), c.id + ": collecting it again is refused");
            }
            var rescued = FindAnyObjectByType<MraResident>();
            if (rescued != null) Expect(!rescued.Rescue(), c.id + ": a second rescue is refused");
            Expect(GameSave.Flags.Count == flags && GameSave.HeartSeeds == heartsSaved && TownState.Amber == amberSaved, c.id + ": the revisit pays nothing more");
        }
    }

    IEnumerator Trial(Chamber c)
    {
        var gates = FindObjectsByType<MraGate>(FindObjectsSortMode.None);
        switch (c.type)
        {
            case "pressure-plate":
            {
                var plate = FindAnyObjectByType<PressurePlate>();
                var crate = GameObject.Find(MraWorldBuilderNames.Generated + "/Trial/Crate (placeholder art)").GetComponent<Rigidbody2D>();
                Expect(gates.All(g => !g.IsOpen), c.id + ": the gate starts closed");
                crate.position = (Vector2)plate.transform.position + new Vector2(0f, 1.2f);
                yield return Wait(1.00f);
                Expect(plate.IsPressed && gates.All(g => g.IsOpen), c.id + ": the crate on the plate holds the gate open");
                break;
            }
            case "sequence":
            {
                var seq = FindAnyObjectByType<MraSequence>();
                if (seq == null) { Expect(true, c.id + ": a climb to the find (no lights in its layout)"); break; }
                seq.Press(seq.lights[1]);
                Expect(!seq.Solved && seq.lights.All(l => !l.IsOn), c.id + ": a wrong light resets the sequence only");
                foreach (int i in seq.order) seq.Press(seq.lights[i]);
                yield return Wait(0.83f);
                Expect(seq.Solved && gates.All(g => g.IsOpen), c.id + ": 1-3-2 opens the niche");
                break;
            }
            case "rescue":
            {
                Expect(gates.All(g => !g.IsOpen), c.id + ": the root gate starts closed");
                foreach (var l in FindObjectsByType<MraLever>(FindObjectsSortMode.None)) l.Use();
                yield return Wait(0.83f);
                Expect(gates.All(g => g.IsOpen), c.id + ": the winch opens the gate (no weapon needed)");
                break;
            }
            case "water-level":
            {
                var water = FindAnyObjectByType<MraWater>();
                foreach (var l in FindObjectsByType<MraLever>(FindObjectsSortMode.None)) l.Use();
                yield return Wait(2.00f);
                Expect(water.Drained && !water.GetComponent<BoxCollider2D>().enabled, c.id + ": the sluice lever drains the water");
                break;
            }
            case "thread-trial":
            {
                var anchors = FindObjectsByType<ThreadAnchor>(FindObjectsSortMode.None);
                Expect(anchors.Length == c.anchors.Length, $"{c.id}: {anchors.Length} thread anchors placed");
                break;
            }
            case "pogo-trial":
            {
                var pods = FindObjectsByType<MraPod>(FindObjectsSortMode.None);
                Expect(pods.Length == c.pods.Length && pods.All(p => p.ReceiveCombatHit(default).SupportsPogo), c.id + ": the pods bounce a downward strike (pogo) and can't break");
                break;
            }
            case "dash-trial":
            {
                var hz = FindAnyObjectByType<MraTimedHazard>();
                bool sawUp = false, sawDown = false; float t = Time.time;
                while (Time.time - t < 3.2f) { sawUp |= hz.IsUp; sawDown |= !hz.IsUp; yield return null; }
                Expect(sawUp && sawDown && Mathf.Approximately(hz.safeSeconds, 1.2f), c.id + ": the thorns cycle with a 1.2 s safe window");
                break;
            }
        }
    }

    // ---------------------------------------------------------------- 3 Qvale

    IEnumerator Town()
    {
        yield return Load("MRAtlas_MR04_Qvale");
        TownState.BindToSave();
        var region = W.RegionById("MR04");
        // The background, as one painting: frames 4 u apart outside the street zoom, then while the view
        // eases out into the street and back (offset measured in Tools/MountainReliefAtlas/QA).
        {
            float gy(float x) => GroundAt(x, 30f) + 1f;
            Place(new Vector2(62f, gy(62f))); yield return Wait(2.5f); Frame("bg_1_x62");
            Place(new Vector2(66f, gy(66f))); yield return Wait(2.5f); Frame("bg_2_x66");
            float zx = W.ChambersOf("MR04").Min(c => c.entrance.x) - 10f;
            Place(new Vector2(zx - 2f, gy(zx - 2f))); yield return Wait(2.5f); Frame("bg_3_before_zoom");
            float t0 = Time.time;
            while (Time.time - t0 < 2.6f) { Press(Key.D); if (Mathf.Abs(Time.time - t0 - 1.1f) < .02f) Frame("bg_4_zooming"); yield return null; }
            Press(); yield return Wait(1.5f); Frame("bg_5_zoomed_out");
        }
        // Every home: in by its door (Up), the right room, its people; out by walking back through
        // the door, landing safely outside the same house.
        var expected = new Dictionary<string, string> { { "MR04_C02", "herbalist" }, { "MR04_C03", "scribble" }, { "MR04_C04", "pip" }, { "MR04_C05", "tallow" } };
        foreach (var c in W.chambers.Where(c => c.IsHouse))
        {
            if (SceneName != region.scene) yield return Load(region.scene);
            TownState.BindToSave();
            var door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == c.id);
            if (door == null) { Expect(false, c.id + ": no door on the street"); continue; }
            float ground = GroundAt(door.transform.position.x, door.transform.position.y + 3f);
            Expect(Mathf.Abs(ground - door.transform.position.y) < .15f, $"{c.id}: the door stands on the street (door {door.transform.position.y:F2}, ground {ground:F2})");
            Place((Vector2)door.transform.position + Vector2.up);
            yield return Wait(1.8f);   // the street view eases out first
            Frame($"house_{c.id}_1_door");
            var view = Camera.main;
            var front = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(sr => sr.transform.parent == door.transform.parent && (sr.name.StartsWith("Exterior") || sr.name.StartsWith("Loft doorway")));
            if (front != null && view != null)
            {
                float hh = view.orthographicSize, hw = hh * view.aspect; var b = front.bounds; var cp = view.transform.position + Vector3.up * VistaZone.CurrentLift;
                Expect(b.max.y <= cp.y + hh + .05f && b.min.y >= cp.y - hh - .05f, $"{c.id}: the whole house front fits the view vertically (house {b.min.y:F1}..{b.max.y:F1}, view {cp.y - hh:F1}..{cp.y + hh:F1})");
            }
            Expect(GameSave.HasFlag(c.DiscoveredFlag), c.id + ": discovered at its door");
            yield return Tap(Key.W);
            yield return WaitScene(c.scene);
            Expect(SceneName == c.scene, $"{c.id}: Up at the door goes into {c.name}");
            Expect(MraMusicPlayer.Current != null && MraMusicPlayer.Current.name == "Music_ASmallLightInTheDark", $"{c.id}: the town's track carries on indoors");
            Expect(Vector2.Distance(Body.position, c.playerSpawn) < .8f && !InGround(), $"{c.id}: Qori arrives inside, clear of the walls ({Body.position})");
            yield return Wait(0.6f); Frame($"house_{c.id}_2_inside");
            var inside = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(sr => sr.name == "Interior (cutaway)");
            if (inside != null && Camera.main != null)
            {
                // At 16:9 (batch mode's game view is 4:3, so the live camera clamps for a narrower screen).
                var v2 = Camera.main; float hh = v2.orthographicSize, hw = hh * 16f / 9f; var b = inside.bounds;
                CameraBounds.TryGetRoom(out Rect room16, out bool top16);
                Vector3 cp = CameraBounds.Clamp(v2.transform.position, hw, hh, room16, top16);
                Expect(b.max.y <= cp.y + hh + .05f && b.min.x >= cp.x - hw - .3f && b.max.x <= cp.x + hw + .3f, $"{c.id}: the whole room painting is in view ({b.min.x:F1}..{b.max.x:F1} x {b.min.y:F1}..{b.max.y:F1}; view {cp.x - hw:F1}..{cp.x + hw:F1} x {cp.y - hh:F1}..{cp.y + hh:F1})");
            }
            if (expected.TryGetValue(c.id, out string who))
                Expect(FindObjectsByType<TownNpc>(FindObjectsSortMode.None).Any(n => n.id == who), $"{c.id}: {who} is at home inside");
            if (c.id == "MR04_C04") Expect(FindObjectsByType<TownNpc>(FindObjectsSortMode.None).Any(n => n.id == "marrow"), "Qvale: Marrow is home after his rescue");
            if (c.id == "MR04_C03" && !polishOnly)
            {
                // Scribble: pages for visited places only.
                var scribble = FindObjectsByType<TownNpc>(FindObjectsSortMode.None).First(n => n.id == "scribble").GetComponent<TownShop>();
                scribble.Open();
                var listed = (List<int>)typeof(TownShop).GetField("listed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(scribble);
                int visited = W.regions.Count(r => GameSave.HasFlag(MraState.VisitedFlag(r.id)));
                Expect(listed.Count == visited, $"Qvale: Scribble lists {listed.Count} pages, one per visited place ({visited})");
                scribble.Close();
                yield return Wait(0.2f);
            }
            if (c.id == "MR04_C06")
            {
                // The loft's climb, indoors now: up the steps by input to the song shell.
                var reward = FindAnyObjectByType<MraReward>();
                Expect(reward != null, "MR04_C06: the song shell is in the loft");
                if (reward != null)
                {
                    float t0 = Time.time, jumpUntil = -1f;
                    float stall = 0f;
                    while (!reward.Taken && Time.time - t0 < 12f)
                    {
                        bool held = Qori.IsGrounded || Qori.IsLedgeHanging || Qori.IsWallSliding;
                        stall = held && Mathf.Abs(Body.linearVelocity.x) < .6f ? stall + Time.deltaTime : 0f;
                        if (stall > .12f && Time.time > jumpUntil) { jumpUntil = Time.time + .42f; stall = 0f; }
                        var keys = new List<Key> { Key.D };
                        if (Time.time < jumpUntil) keys.Add(Key.Space);
                        if (Qori.IsLedgeHanging) keys.Add(Key.W);
                        Press(keys.ToArray());
                        yield return null;
                    }
                    Press();
                    Expect(reward.Taken, $"MR04_C06: the loft's steps climbed by input to the song shell ({Body.position})");
                }
            }
            // Out: walk left through the door.
            Place(c.playerSpawn); yield return Wait(0.3f);
            float tOut = Time.time;
            while (SceneName == c.scene && Time.time - tOut < 6f) { Press(Key.A); yield return null; }
            Press();
            yield return WaitScene(region.scene);
            Expect(SceneName == region.scene, $"{c.id}: walking back out of the door leads to Qvale");
            var doorAgain = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == c.id);
            Expect(doorAgain != null && Mathf.Abs(doorAgain.transform.position.x - Body.position.x) < 1.2f && !InGround(), $"{c.id}: back outside at its own door ({Body.position}, door {(doorAgain != null ? doorAgain.transform.position.x : 0f):F1})");
            var cam = Camera.main;
            yield return Wait(0.3f);
            Expect(cam != null && Mathf.Abs(cam.transform.position.x - Body.position.x) < 3f && Mathf.Abs(cam.transform.position.y - Body.position.y) < 3.5f, $"{c.id}: the camera frames Qori on arrival");
        }
        if (SceneName != region.scene) yield return Load(region.scene);
        TownState.BindToSave();
        // The overlook: level, open ground from the shelf's start to past the bench; sit, stand, leave.
        var spotAt = FindAnyObjectByType<ListeningSpot>();
        if (spotAt != null)
        {
            float bx = spotAt.transform.position.x, by = spotAt.transform.position.y;
            float tx = region.NodeById("MR04_N09").position.x;
            float lowest = float.MaxValue, highest = float.MinValue;
            for (float x = tx - 3.5f; x <= bx + 5f; x += .5f) { float g = GroundAt(x, by + 6f); lowest = Mathf.Min(lowest, g); highest = Mathf.Max(highest, g); }
            Expect(highest - lowest < .1f, $"overlook: level ground from the shelf to past the bench (varies {highest - lowest:F2} u)");
            Expect(FindObjectsByType<MraReward>(FindObjectsSortMode.None).All(rw => Mathf.Abs(rw.transform.position.x - bx) > 8f), "overlook: no climb or find crowds the bench (the loft is indoors)");
            var enc = GameObject.Find(MraWorldBuilderNames.Generated + "/Encounters");
            Expect(enc == null || enc.transform.childCount == 0, "overlook: no enemies in Qvale");
            Place(new Vector2(tx - 3f, by + 1f)); yield return Wait(0.4f);
            float t0 = Time.time;
            while (Body.position.x < bx - .2f && Time.time - t0 < 6f) { Press(Key.D); yield return null; }
            Press(); yield return Wait(0.3f);
            Expect(Mathf.Abs(Body.position.x - bx) < 1.2f && Qori.IsGrounded, $"overlook: walked to the bench without a jump ({Body.position.x:F1} vs bench {bx:F1})");
            yield return Tap(Key.W); yield return Wait(0.6f);
            Expect(ListeningSpot.IsSitting, "overlook: Up sits on the bench");
            yield return Wait(1.2f); Frame("overlook_seated_view");
            var rig = Qori.GetComponentInChildren<QoriAnimator>();
            float seatY = spotAt.seat.y;
            Expect(rig != null && rig.HeldState == "Sit" && !Body.simulated && Mathf.Abs(Body.position.x - spotAt.seat.x) < .8f && Body.position.y > seatY - 1f,
                $"overlook: Qori sits on the seat in the Sit pose (at {Body.position}, seat {spotAt.seat}, pose {(rig != null ? rig.HeldState : "none")})");
            FrameAt("overlook_sitting_closeup", spotAt.seat + new Vector2(0f, .4f), 1.8f);
            yield return Tap(Key.Backspace); yield return Wait(1.9f);   // the view eases back to Qori first
            Expect(!ListeningSpot.IsSitting, "overlook: Cancel stands up");
            Expect(Body.simulated && (rig == null || rig.HeldState == null) && !InGround(), $"overlook: up off the seat, standing again ({Body.position})");
            float x0 = Body.position.x; t0 = Time.time;
            while (Time.time - t0 < 1.5f) { Press(Key.D); yield return null; }
            Press();
            Expect(Body.position.x > x0 + 3f && !InGround(), $"overlook: walks on past the bench freely ({x0:F1} -> {Body.position.x:F1})");
        }
        var smith = FindObjectsByType<TownNpc>(FindObjectsSortMode.None).FirstOrDefault(n => n.id == "brannick");
        Expect(smith != null && smith.isActiveAndEnabled, "Qvale: Brannick is at his forge after his rescue");
        Expect(FindObjectsByType<TownNpc>(FindObjectsSortMode.None).Any(n => n.id == "wick"), "Qvale: Wick and the lit lamps after his rescue");
        // A purchase, kept after reloading the save.
        int amber = TownState.Amber;
        TownState.AddAmber(100);
        var shop = smith != null ? smith.GetComponent<TownShop>() : null;
        int hearts = Qori.GetComponent<PlayerHealth>().MaximumHealth;
        Expect(shop != null && shop.Buy(0), "Qvale: a heart seed bought at the forge");
        Expect(Qori.GetComponent<PlayerHealth>().MaximumHealth == hearts + 1 && TownState.Amber == amber + 40, $"Qvale: +1 heart and 60 Amber spent ({TownState.Amber})");
        GameSave.Reload(); TownState.BindToSave();
        Expect(TownState.Amber == amber + 40 && shop.IsSold(0), "Qvale: Amber and the purchase survive reloading the save");
        if (polishOnly) yield break;
        // Song shells: owned shells open their tracks at the listening tree.
        var spot = FindAnyObjectByType<ListeningSpot>();
        int owned = spot.tracks.Count(t => !string.IsNullOrEmpty(t.shell) && GameSave.HasFlag(t.shell));
        int unlocked = Enumerable.Range(0, spot.tracks.Count).Count(i => !string.IsNullOrEmpty(spot.tracks[i].shell) && spot.IsUnlocked(i));
        Expect(owned > 0 && unlocked == owned, $"Qvale: {unlocked} song shells found play at the listening tree");
        // Waymark travel: from Qvale's Waymark to the Causeway's and back.
        var wm = region.NodeById(region.primaryWaymark);
        Place(wm.spawn);
        yield return Wait(0.33f);
        MraChart.Open(); MraChart.Instance.SelectRegion("MR02");
        Expect(MraChart.Instance.TryTravel(), "Qvale: Waymark travel to the Long Causeway offered and taken");
        yield return WaitScene("MRAtlas_MR02_Causeway");
        var cw = W.RegionById("MR02").NodeById("MR02_N05");
        Expect(SceneName == "MRAtlas_MR02_Causeway" && Vector2.Distance(Body.position, cw.spawn) < 2f, $"arrived at the Causeway's Waymark ({Body.position})");
        yield return Wait(0.33f);
        MraChart.Open(); MraChart.Instance.SelectRegion("MR04");
        Expect(MraChart.Instance.TryTravel(), "Waymark travel back to Qvale");
        yield return WaitScene("MRAtlas_MR04_Qvale");
        Expect(SceneName == "MRAtlas_MR04_Qvale", "back in Qvale by Waymark");
    }

    // ---------------------------------------------------------------- 4 saving

    IEnumerator Saving()
    {
        var before = GameSave.Flags.ToList();
        GameSave.Reload();
        Expect(before.All(GameSave.HasFlag) && GameSave.Flags.Count == before.Count, $"all {before.Count} saved flags read back from disk");
        Expect(new[] { Relics.ClimbingMoss, Relics.LivingThread, Relics.Bloomfall, Relics.WindLeaf, Relics.Glidecap }.All(GameSave.HasRelic), "relics read back");
        Expect(W.chambers.Where(c => !c.InPlace).All(c => GameSave.HasFlag(c.rewardId)), "every chamber's reward read back");
        // The quake's state, on reloading the Cradle.
        yield return Load("MRAtlas_MR01_Cradle");
        var gate = FindObjectsByType<MraGate>(FindObjectsSortMode.None).First(g => g.requires.Contains("knot:grip"));
        yield return Wait(0.67f);
        Expect(gate.IsOpen, "the Cradle's quake gate is open on loading the scene again");
        // Checkpoint and respawn: the Cradle's last checkpoint.
        var cp = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).Last();
        Place(cp.SpawnPosition); yield return Wait(0.25f);
        Place(new Vector2(cp.SpawnPosition.x - 30f, GroundAt(cp.SpawnPosition.x - 30f, 200f) + 1f));
        Qori.Respawn(); yield return Wait(0.25f);
        Expect(Vector2.Distance(Body.position, cp.SpawnPosition) < .5f, "a respawn returns Qori to his last checkpoint");
        Expect(GameSave.CheckpointIn(SceneManager.GetActiveScene().path) == cp.CheckpointId, "the checkpoint is saved by id");
        // Backward travel: the Causeway's west exit back into the Cradle.
        yield return Load("MRAtlas_MR02_Causeway");
        var west = FindObjectsByType<MraExit>(FindObjectsSortMode.None).First(e => e.targetScene == "MRAtlas_MR01_Cradle");
        Place(west.transform.position);
        yield return WaitScene("MRAtlas_MR01_Cradle");
        var link = W.links.First(l => l.id == "MR01_MR02");
        Expect(SceneName == "MRAtlas_MR01_Cradle" && Vector2.Distance(Body.position, link.reverseArrival) < 1f && !InGround(), $"walking back west arrives at the Cradle's far end ({Body.position})");
        // The descent's gate on direct load: without the reveal it sends Qori back to the Summit.
        GameSave.ClearFlag(MraState.RevealFlag);
        yield return Load("MRAtlas_MR08_Descent");
        yield return WaitScene("MRAtlas_MR07_Summit", 6f);
        Expect(SceneName == "MRAtlas_MR07_Summit", "loading the descent directly before the reveal returns to the Summit");
        GameSave.SetFlag(MraState.RevealFlag);
        yield return Load("MRAtlas_MR08_Descent");
        yield return Wait(1.50f);
        Expect(SceneName == "MRAtlas_MR08_Descent", "after the reveal the descent loads directly");
        var ending = FindAnyObjectByType<MraEnding>();
        Expect(ending != null && ending.Done, "the ending is remembered (it doesn't replay)");
    }

    // ---------------------------------------------------------------- 5 the Chart

    IEnumerator ChartChecks()
    {
        yield return Load("MRAtlas_MR04_Qvale");
        // Gamepad only: the mouse is unplugged and a pad plugged in.
        var mouse = Mouse.current; if (mouse != null) InputSystem.RemoveDevice(mouse);
        pad = InputSystem.AddDevice<Gamepad>("MRA Test Pad");
        yield return null;
        yield return PadTap(GamepadButton.Select);
        Expect(MraChart.IsOpen && ModalUi.IsOpen && Time.timeScale == 0f, "View opens the Chart and pauses play");
        var chart = MraChart.Instance;
        Expect(chart.Viewing == "MR04" && chart.QoriMarker != null && chart.QoriMarker.gameObject.activeSelf, "the Chart opens on Qvale with Qori's marker");
        Expect(chart.RouteSegmentsShown > 0, $"walked paths are drawn ({chart.RouteSegmentsShown} segments)");
        Expect(chart.CavesShown == W.ChambersOf("MR04").Count(c => GameSave.HasFlag(c.DiscoveredFlag)), "only discovered places are marked");
        yield return Capture("chart_MR04");
        float zoom = chart.Zoom; Vector2 pan = chart.Pan;
        InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1f }); yield return Wait(0.33f);
        InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
        Expect(chart.Zoom > zoom, $"the right trigger zooms in ({zoom:F2} -> {chart.Zoom:F2})");
        float zoomed = chart.Zoom;
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(1f, 0f) }); yield return Wait(0.33f);
        InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
        Expect(chart.Pan != pan, "the left stick pans");
        yield return PadTap(GamepadButton.RightShoulder);
        Expect(chart.Viewing != "MR04", "the right shoulder switches region (" + chart.Viewing + ")");
        yield return Capture("chart_" + chart.Viewing);
        yield return PadTap(GamepadButton.LeftShoulder);
        yield return PadTap(GamepadButton.North);
        Expect(chart.Viewing == "MR04", "the left shoulder switches back; Y centres on Qori");
        yield return PadTap(GamepadButton.East);
        Expect(!MraChart.IsOpen && Time.timeScale > 0f, "B closes the Chart and play resumes");
        yield return Wait(0.25f);
        Expect(!ModalUi.IsOpen && !GamePauseMenu.BlocksGameplayInput, "focus returns to play after closing");
        yield return PadTap(GamepadButton.Select);
        Expect(MraChart.IsOpen && chart.Zoom >= zoomed - .01f, $"reopening keeps the region's zoom ({chart.Zoom:F2})");
        yield return PadTap(GamepadButton.Select);
        // Reconnect: the pad unplugged and plugged back in still drives the Chart.
        InputSystem.RemoveDevice(pad); yield return null;
        pad = InputSystem.AddDevice<Gamepad>("MRA Test Pad (reconnected)"); yield return null;
        yield return PadTap(GamepadButton.Select);
        Expect(MraChart.IsOpen, "after a reconnect the pad opens the Chart");
        yield return PadTap(GamepadButton.East);
        Expect(!MraChart.IsOpen, "and closes it");
        // Post-reveal: the whole climb is on the list; before the reveal it never is.
        MraChart.Open();
        var choices = (List<string>)typeof(MraChart).GetField("choices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(chart);
        Expect(choices.Contains(MraChart.WholeClimb) == MraState.Revealed, "the final assembly is listed only after the reveal");
        chart.Hide();
        GameSave.ClearFlag(MraState.RevealFlag);
        MraChart.Open();
        Expect(!choices.Contains(MraChart.WholeClimb) && !choices.Contains("MR08"), "before the reveal: no whole-world view and no descent page");
        chart.Hide();
        GameSave.SetFlag(MraState.RevealFlag);
        if (mouse != null) InputSystem.AddDevice(mouse);
    }

    IEnumerator PadTap(GamepadButton button)
    {
        var state = new GamepadState().WithButton(button);
        InputSystem.QueueStateEvent(pad, state); yield return null; yield return null;
        InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
    }

    // Renders the open Chart (a screen-space overlay) through the camera at both sizes.
    // A gameplay frame at 1280x720 (the camera, plus the area-name card when one is up), for the
    // polish evidence: <captureDir>/polish_frames/<name>.jpg.
    void Frame(string name)
    {
        var camera = Camera.main; if (camera == null) return;
        var card = GameObject.Find("Area Transition Canvas")?.GetComponent<Canvas>();
        RenderMode was = card != null ? card.renderMode : RenderMode.ScreenSpaceOverlay;
        if (card != null) { card.renderMode = RenderMode.ScreenSpaceCamera; card.worldCamera = camera; card.planeDistance = 1f; }
        float lift = VistaZone.CurrentLift; Vector3 camWas = camera.transform.position;
        Vector3 at = camWas + Vector3.up * lift;   // as rendered in play
        // The room's edges at 16:9 (the batch-mode game view is 4:3, so the live clamp is narrower).
        if (CameraBounds.TryGetRoom(out Rect room, out bool top) && VistaZone.CurrentLift == 0f)
            at = (Vector3)CameraBounds.Clamp(at, camera.orthographicSize * 16f / 9f, camera.orthographicSize, room, top) + new Vector3(0f, 0f, at.z);
        camera.transform.position = at;
        var rt = new RenderTexture(1280, 720, 24); camera.targetTexture = rt; float aspect = camera.aspect; camera.aspect = 1280f / 720f;
        foreach (var layer in FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);   // cover the capture's aspect
        Canvas.ForceUpdateCanvases(); camera.Render();
        RenderTexture.active = rt;
        var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
        Directory.CreateDirectory(Path.Combine(captureDir, "polish_frames"));
        File.WriteAllBytes(Path.Combine(captureDir, "polish_frames", name + ".jpg"), read.EncodeToJPG(88));
        camera.targetTexture = null; camera.aspect = aspect; RenderTexture.active = null; Destroy(rt); Destroy(read);
        camera.transform.position = camWas;
        if (card != null) card.renderMode = was;
    }

    // A close frame at a given spot and camera size (the view is put back afterwards).
    void FrameAt(string name, Vector2 at, float size)
    {
        var camera = Camera.main; if (camera == null) return;
        Vector3 was = camera.transform.position; float wasSize = camera.orthographicSize;
        camera.transform.position = new Vector3(at.x, at.y, was.z); camera.orthographicSize = size;
        var rt = new RenderTexture(1280, 720, 24); camera.targetTexture = rt; float aspect = camera.aspect; camera.aspect = 1280f / 720f;
        foreach (var layer in FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) layer.Refresh(camera);
        camera.Render(); RenderTexture.active = rt;
        var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
        Directory.CreateDirectory(Path.Combine(captureDir, "polish_frames"));
        File.WriteAllBytes(Path.Combine(captureDir, "polish_frames", name + ".jpg"), read.EncodeToJPG(90));
        camera.targetTexture = null; camera.aspect = aspect; RenderTexture.active = null; Destroy(rt); Destroy(read);
        camera.transform.position = was; camera.orthographicSize = wasSize;
    }

    IEnumerator Capture(string name)
    {
        yield return null;
        var canvas = MraChart.Instance.GetComponentInChildren<Canvas>();
        var camera = Camera.main;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        foreach (var (w, h) in new[] { (1920, 1080), (1280, 720) })
        {
            var rt = new RenderTexture(w, h, 24); camera.targetTexture = rt; camera.aspect = (float)w / h;
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply();
            Directory.CreateDirectory(Path.Combine(captureDir, "mra"));
            File.WriteAllBytes(Path.Combine(captureDir, "mra", $"{name}_{h}.png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; Destroy(rt); Destroy(read);
        }
        camera.ResetAspect();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }
}

// The builder's generated root name, shared with the test without referencing the editor assembly.
public static class MraWorldBuilderNames { public const string Generated = "Generated (MRA builder)"; }
#endif
