using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Batch: -executeMethod UndergroundCombatBot.Run -saveFile ugp_bot.json -captureDir <dir> [-onlyDesigns E-17,M-01] (no -quit).
// Plays each underground creature through the real controls (a virtual keyboard and mouse, the real Reedblade, real
// hearts, real deaths), once per play style:
//   Masher  walks up and swings without dodging: a new player.
//   Skilled reads the telegraphs with a human reaction delay (.2 s), backs off, punishes the openings: a good player.
//   Hugger  (bosses) stands on the weak point and never moves: looks for safe spots.
//   Escape  (E-17) holds away and swings during the pull, then dashes with the Wind Leaf: can the pull be broken?
// Writes what happened to <dir>/combat_bot.txt. The save is a test save (-saveFile), cleared between passes.
public static class UndergroundCombatBot
{
    enum Style { Masher, Skilled, Hugger, Escape }
    const string Key = "UGP.CombatBot.Dir";
    const float Reaction = .2f, RegularLimit = 40f, BossLimit = 180f;

    sealed class Fight
    {
        public string design, room; public Style style; public EnemyBase enemy;
        public int swings, hits, blocked, ignored, interrupted, hurt, deaths, leftRoom; public float started, end, chase, hitDistance; public bool won;
        public Dictionary<string, int> hitsIn = new Dictionary<string, int>(), hurtBy = new Dictionary<string, int>();
        public Vector2 startQori, startEnemy; public string note = "";
    }

    static string dir; static Style[] passes = { Style.Masher, Style.Skilled, Style.Hugger, Style.Escape };
    static int pass = -1, fightIndex; static List<Fight> fights; static Fight now;
    static readonly List<string> report = new List<string>();
    static Keyboard keys; static Mouse mouse; static PlayerMovement qori; static PlayerHealth health; static PlayerCombat combat;
    static readonly Queue<(float, string)> seen = new Queue<(float, string)>();
    static float nextSwing, swingUp, jumpUntil, nextJump, lastHealth, loadedAt = -1f, lastShot; static bool waiting, seenPull; static int shots;
    static float pullFrom, pullAt, dashedAt = -1f, swingsAtPull; static string[] only; static string readBefore = ""; static float readSince, sweepSide = 1f; static float lastTick; static string lastState = "";

    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-saveFile") < 0) { Debug.LogError("UGP bot needs -saveFile"); EditorApplication.Exit(1); return; }
        Directory.CreateDirectory(args[System.Array.IndexOf(args, "-captureDir") + 1]);
        GameSave.Clear();
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        SessionState.SetString(Key, args[System.Array.IndexOf(args, "-captureDir") + 1]);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod] static void Resume()
    {
        dir = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(dir)) return;
        var args = System.Environment.GetCommandLineArgs(); int at = System.Array.IndexOf(args, "-onlyDesigns");
        if (at >= 0) only = args[at + 1].Split(',');
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.time < 3f) return;
        if (keys == null)
        {
            Application.targetFrameRate = 60;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keys = InputSystem.AddDevice<Keyboard>("Bot Keyboard"); mouse = InputSystem.AddDevice<Mouse>("Bot Mouse");
            keys.MakeCurrent(); mouse.MakeCurrent();
            Application.logMessageReceived += (m, s, t) => { if ((t == LogType.Exception || t == LogType.Error) && !m.Contains("TraceRenderingLayerMask") && report.Count < 400) report.Add("RUNTIME ERROR: " + m); };
            NextPass(); return;
        }
        if (waiting) { if (Time.time - loadedAt < 2f || Object.FindAnyObjectByType<PlayerMovement>() == null) return; Begin(); return; }
        Play();
    }

    static void NextPass()
    {
        pass++;
        if (pass >= passes.Length) { Finish(); return; }
        Send();
        GameSave.Clear();
        EditorSceneManager.LoadSceneInPlayMode(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
        waiting = true; loadedAt = Time.time;
    }

    static void Begin()
    {
        waiting = false;
        qori = Object.FindAnyObjectByType<PlayerMovement>(); health = qori.GetComponent<PlayerHealth>(); combat = qori.GetComponent<PlayerCombat>();
        combat.OnAttackHit += OnHit;
        combat.OnAttackStarted += a => { if (now != null) now.swings++; };
        combat.OnAttackInterrupted += a => { if (now != null) now.interrupted++; };
        if (pass == 0) report.Add($"Fresh save, {health.MaximumHealth} hearts, weapon {combat.EquippedWeapon.weaponId}. Reaction delay for Skilled: {Reaction}s.");
        Style style = passes[pass];
        var chosen = new Dictionary<string, EnemyBase>();
        foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).OrderBy(e => e.name))
        {
            if (!(e is UndergroundEnemy || e is UndergroundGuardian)) continue;
            string id = Id(e);
            if (only != null && !only.Contains(id)) continue;
            if (style == Style.Hugger && !(e is UndergroundGuardian)) continue;
            if (style == Style.Escape && !(e is GaleWispEnemy)) continue;
            if (!chosen.ContainsKey(id)) chosen[id] = e;
        }
        fights = chosen.Values.Select(e => new Fight { enemy = e, design = e.name, room = e.GetComponentInParent<UndergroundRoom>(true)?.roomId, style = style }).ToList();
        fightIndex = -1;
        NextFight();
    }

    static string Id(EnemyBase e) { var parts = e.name.Split(' '); foreach (var p in parts) if (p.Length == 4 && p[1] == '-') return p; return e.name; }
    static bool Boss(EnemyBase e) => e is UndergroundGuardian;

    static void NextFight()
    {
        fightIndex++;
        if (fightIndex >= fights.Count) { now = null; NextPass(); return; }
        now = fights[fightIndex]; now.started = lastTick = Time.time; lastState = State(now.enemy); seen.Clear(); shots = 0; seenPull = false; dashedAt = -1f;
        health.Heal(99); lastHealth = health.Health;
        Grant(Relics.DoubleJump);
        if (now.style == Style.Escape) Grant(Relics.WindLeaf);
        Place();
        now.startQori = qori.transform.position; now.startEnemy = now.enemy.transform.position;
    }

    static void Grant(string id)
    {
        var abilities = qori.GetComponent<PlayerAbilityController>();
        var relic = Relics.All.FirstOrDefault(a => a != null && a.abilityId == id);
        if (abilities != null && relic != null) abilities.Unlock(relic);
    }

    // On the floor it stands on, about 8 u away where there is room, on the side toward the room's middle.
    static void Place()
    {
        var e = now.enemy; Vector2 at = e.transform.position;
        var room = e.GetComponentInParent<UndergroundRoom>(true);
        float toMiddle = room != null ? Mathf.Sign(room.WorldBounds.center.x - at.x) : 1f;
        // A leech perches on a ceiling: start on the floor below and to the side.
        if (e is SiltLeechEnemy) at += Vector2.down * 3f;
        foreach (float gap in now.style == Style.Escape ? new[] { 3f, 4f } : new[] { 8f, 6f, 5f, 4f, 3f, 10f, 12f, 16f })
            foreach (float side in new[] { toMiddle, -toMiddle })
            {
                Vector2 spot = at + new Vector2(side * gap, 1.5f);
                if (room != null && (spot.x < room.WorldBounds.min.x + 1f || spot.x > room.WorldBounds.max.x - 1f)) continue;
                if (room != null && (spot.y > room.WorldBounds.max.y - 1f || spot.y < room.WorldBounds.min.y)) continue;
                if (Physics2D.OverlapPoint(spot, GroundLayer.Mask)) continue;
                var ground = Physics2D.Raycast(spot, Vector2.down, e is UndergroundEnemy u && Fly(u) ? 14f : 6f, GroundLayer.Mask);
                if (!ground || Thorny(ground.point)) continue;
                Teleport(new Vector2(spot.x, ground.point.y + .7f)); return;
            }
        now.note += " no clean start spot;";
        Teleport(at + new Vector2(toMiddle * 3f, 0f));
    }
    static bool Thorny(Vector2 p)
    {
        foreach (var c in Physics2D.OverlapCircleAll(p, .7f)) if (c.GetComponent<ThornHazard>() != null) return true;
        return false;
    }
    // Pit or thorns ahead on the floor he is walking on.
    static bool BadFooting(Vector2 q, float dir)
    {
        if (dir == 0f) return false;
        var g = Physics2D.Raycast(q + new Vector2(dir * 1.4f, 0f), Vector2.down, 2.5f, GroundLayer.Mask);
        return !g || Thorny(g.point);
    }
    static bool Fly(UndergroundEnemy e) => e is SiltLeechEnemy || e is PumpBloaterEnemy || e is SeedSwarmEnemy || e is KiteRayEnemy || e is GaleWispEnemy;
    static void Teleport(Vector2 p) { var b = qori.GetComponent<Rigidbody2D>(); b.position = p; b.linearVelocity = Vector2.zero; qori.transform.position = p; }

    static string State(EnemyBase e) => e is UndergroundEnemy u ? u.State : ((UndergroundGuardian)e).Current.ToString();

    static void OnHit(AttackHitResult r)
    {
        if (now == null || r.Target == null || now.enemy == null) return;
        var c = r.Target as Component; if (c == null || !c.transform.IsChildOf(now.enemy.transform)) return;
        string state = lastState;   // the hit itself may already have changed it
        if (r.Response.Disposition == CombatHitDisposition.Damaged)
        {
            now.hits++; now.hitDistance += Vector2.Distance(qori.transform.position, now.enemy.transform.position);
            now.hitsIn.TryGetValue(state, out int n); now.hitsIn[state] = n + 1;
        }
        else if (r.Response.Disposition == CombatHitDisposition.Blocked) now.blocked++;
        else now.ignored++;
    }

    static string HurtBy()
    {
        Vector2 q = qori.transform.position; var e = now.enemy;
        foreach (var w in Object.FindObjectsByType<ThornShockwave>(FindObjectsSortMode.None)) if (Vector2.Distance(q, w.transform.position) < 3f) return "floor wave";
        foreach (var w in Object.FindObjectsByType<SpitterSeed>(FindObjectsSortMode.None)) if (Vector2.Distance(q, w.transform.position) < 2f) return "projectile";
        foreach (var w in Object.FindObjectsByType<UgFallingSeed>(FindObjectsSortMode.None)) if (Vector2.Distance(q, w.transform.position) < 2.5f) return "falling stone";
        foreach (var c in Physics2D.OverlapCircleAll(q, 1.1f))
        {
            var h = c.GetComponentInParent<IContactHazard>();
            if (h != null && h.HurtsOnContact && !c.transform.IsChildOf(e.transform) && !c.transform.IsChildOf(qori.transform)) return "room hazard " + h.GetType().Name;
        }
        foreach (var c in Physics2D.OverlapCircleAll(q, 1.2f)) if (c.GetComponent<ThornHazard>() != null) return "thorns on the floor";
        string state = lastState;
        if (e is EnemyBase && e.HurtsOnContact && Vector2.Distance(q, Target(e)) < 2.2f && state != "Attack" && state != "Strike") return "touching its body in " + state;
        return Vector2.Distance(q, e.transform.position) < 9f ? state : "away from it (" + state + ")";
    }

    static void Play()
    {
        var f = now; if (f == null) return;
        var e = f.enemy; float t = Time.time - f.started, limit = Boss(e) ? BossLimit : RegularLimit;
        if (e == null || !e.IsAlive || !e.gameObject.activeInHierarchy) { f.won = true; End(f); return; }
        if (t > limit || f.deaths >= 3) { End(f); return; }
        Vector2 q = qori.transform.position;
        var room = e.GetComponentInParent<UndergroundRoom>(true);

        // Hearts: what cost them, and deaths (a respawn puts him far away and refills him).
        if (health.Health < lastHealth)
        {
            f.hurt++; string by = HurtBy(); f.hurtBy.TryGetValue(by, out int n); f.hurtBy[by] = n + 1;
            if (shots < 2) { Shot(f.design.Replace(' ', '_') + "_" + f.style + "_hurt" + shots, e, q); shots++; }
        }
        if (health.Health >= lastHealth + 2) { f.deaths++; lastHealth = health.Health; Send(); Place(); return; }
        lastHealth = health.Health;
        if (room != null)
        {
            var b = room.WorldBounds; b.Expand(new Vector3(6f, 6f, 200f));
            if (!b.Contains(new Vector3(q.x, q.y, b.center.z)))
            {
                f.leftRoom++;
                Send(); Place(); lastHealth = health.Health; return;
            }
        }

        string state = State(e);
        seen.Enqueue((Time.time, state));
        string read = state;
        if (f.style == Style.Skilled) { while (seen.Count > 1 && Time.time - seen.Peek().Item1 > Reaction) seen.Dequeue(); read = seen.Peek().Item2; }

        Vector2 target = Target(e);
        float dx = target.x - q.x, adx = Mathf.Abs(dx), dy = target.y - q.y;
        if (adx > 1.8f || Mathf.Abs(dy) > 2.4f) f.chase += Time.time - lastTick;
        lastTick = Time.time; lastState = state;

        float move = 0f; bool jump = false, swing = false, up = false, dash = false;
        switch (f.style)
        {
            case Style.Masher:
                if (adx > .9f) move = Mathf.Sign(dx);
                if (WallAhead(q, move) || dy > 1.5f && adx < 4f) jump = true;
                swing = adx < 2.2f && Mathf.Abs(dy) < 3.2f; up = dy > 1f;
                break;
            case Style.Hugger:
                if (adx > .35f) move = Mathf.Sign(dx);
                if (WallAhead(q, move) || dy > 1.6f) jump = dy > 1.6f ? read == "Exposed" || read == "Hurt" || adx > 3f : true;
                swing = adx < 2.2f && Mathf.Abs(dy) < 3.2f; up = dy > 1f;
                break;
            case Style.Skilled: Skilled(e, read, q, dx, adx, dy, ref move, ref jump, ref swing, ref up); break;
            case Style.Escape:
                move = -Mathf.Sign(e.transform.position.x - q.x);   // hold away
                swing = true;
                if (read == "Warn" && !seenPull) { seenPull = true; pullAt = Time.time; pullFrom = q.x; swingsAtPull = f.swings; f.note += $" pull starts {Mathf.Abs(q.x - e.transform.position.x):0.0} u away;"; }
                if (seenPull && pullAt > 0f && Time.time - pullAt > .9f)
                {
                    f.note += $" holding away for .9 s moved him {(q.x - pullFrom) * Mathf.Sign(pullFrom - e.transform.position.x):0.0} u away from it, {f.swings - swingsAtPull:0} swings started, {f.interrupted} cut off;";
                    pullAt = -1f; dashedAt = Time.time; pullFrom = q.x;
                }
                if (dashedAt > 0f && Time.time - dashedAt < .3f) dash = true;
                if (dashedAt > 0f && Time.time - dashedAt > .6f) { f.note += $" dash version {qori.DashVersion}, moved {(q.x - pullFrom) * Mathf.Sign(pullFrom - e.transform.position.x):0.0} u away from it;"; f.won = false; End(f); return; }
                break;
        }
        if (BadFooting(q, move) && qori.IsGrounded) { if (f.style == Style.Skilled && Mathf.Sign(move) != Mathf.Sign(dx)) move = 0f; else jump = true; }
        Send(move, jump, swing, up, dash);
    }

    static Vector2 Target(EnemyBase e)
    {
        if (e is UndergroundGuardian)
        {
            var weak = e.transform.Find("Weak point (where hits count)");
            if (weak != null) return weak.position;
        }
        var c = e.GetComponent<Collider2D>();
        return c != null ? (Vector2)c.bounds.center : (Vector2)e.transform.position;
    }

    static bool WallAhead(Vector2 q, float dir) => dir != 0f && Physics2D.Raycast(q, new Vector2(dir, 0f), 1f, GroundLayer.Mask);

    static void Skilled(EnemyBase e, string read, Vector2 q, float dx, float adx, float dy, ref float move, ref bool jump, ref bool swing, ref bool up)
    {
        float away = dx == 0f ? 1f : -Mathf.Sign(dx);
        bool danger = read == "Warn" || read == "Attack" || read == "Strike";
        foreach (var w in Object.FindObjectsByType<ThornShockwave>(FindObjectsSortMode.None))
            if (Mathf.Abs(w.transform.position.x - q.x) < 2.8f && Mathf.Abs(w.transform.position.y - q.y) < 2f) jump = true;
        foreach (var w in Object.FindObjectsByType<UgFallingSeed>(FindObjectsSortMode.None))
            if (Mathf.Abs(w.transform.position.x - q.x) < 1.2f) move = w.transform.position.x > q.x ? -1f : 1f;
        if (move != 0f) return;
        if (e is BrambleCreeperEnemy && read == "Attack") { if (adx < 3.5f) { jump = true; move = Mathf.Sign(dx); } return; }
        if (danger)
        {
            // The Warden's sweep is meant to be jumped: go up just before it lands.
            if (e is SluiceWardenBoss)
            {
                // Its sweep reaches 10.5 u out: be past that before it lands.
                float fromIt = q.x - e.transform.position.x;
                if (Mathf.Abs(fromIt) < 11.5f) { move = fromIt == 0f ? 1f : Mathf.Sign(fromIt); if (WallAhead(q, move)) jump = true; }
                return;
            }
            if (adx < 7f)
            {
                move = away;
                if (WallAhead(q, move) || !Physics2D.Raycast(q + new Vector2(move * 1.5f, 0f), Vector2.down, 3f, GroundLayer.Mask)) { move = -move; jump = true; }
            }
            return;
        }
        bool open;
        float bait = 0f;
        if (e is UndergroundGuardian) { open = read == "Exposed" || read == "Open" || read == "Hurt"; bait = e is SluiceWardenBoss ? 12f : 5f; }
        else if (e is SluiceCrabEnemy) { open = read == "Recover"; bait = 3f; }
        else if (e is BrambleCreeperEnemy) { open = read == "Recover"; bait = 6f; }
        else open = true;
        if (open)
        {
            if (adx > 1.2f) move = Mathf.Sign(dx);
            if (WallAhead(q, move) || dy > 1.5f && adx < 2.5f) jump = true;
            swing = adx < 2.2f && Mathf.Abs(dy) < 3.2f; up = dy > 1f;
        }
        else if (adx > bait + .5f) move = Mathf.Sign(dx);
        else if (adx < bait - .5f) { move = away; if (WallAhead(q, move)) move = 0f; }
    }

    static void Send(float move = 0f, bool jump = false, bool swing = false, bool up = false, bool dash = false)
    {
        var held = new List<Key>();
        if (move > 0f) held.Add(UnityEngine.InputSystem.Key.D); else if (move < 0f) held.Add(UnityEngine.InputSystem.Key.A);
        if (up) held.Add(UnityEngine.InputSystem.Key.W);
        if (jump && Time.time > nextJump) { jumpUntil = Time.time + .25f; nextJump = Time.time + .55f; }
        if (Time.time < jumpUntil) held.Add(UnityEngine.InputSystem.Key.Space);
        if (dash) held.Add(UnityEngine.InputSystem.Key.C);
        InputSystem.QueueStateEvent(keys, new KeyboardState(held.ToArray()));
        var mouseState = new MouseState();
        if (swing && Time.time > nextSwing) { swingUp = Time.time + .05f; nextSwing = Time.time + .16f; }
        if (Time.time < swingUp) mouseState = mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, mouseState);
    }

    static void End(Fight f)
    {
        f.end = Time.time - f.started;
        var e = f.enemy;
        string left = "";
        if (!f.won && e != null) left = $", left it on {GetHealth(e)}, in {State(e)}";
        string hitsIn = string.Join(", ", f.hitsIn.Select(p => p.Key + " " + p.Value));
        string hurtBy = string.Join(", ", f.hurtBy.Select(p => p.Key + " " + p.Value));
        string line = $"{f.style,-7} {f.design} [{f.room}]: {(f.won ? "WON" : "not won")} in {f.end:0.0}s{left}; swings {f.swings}, hits {f.hits}, blocked {f.blocked}, glanced {f.ignored}, swings cut off {f.interrupted}; hearts lost {f.hurt}, deaths {f.deaths}, left room {f.leftRoom}; out of reach {100f * f.chase / Mathf.Max(.1f, f.end):0}% of the time; mean hit distance {(f.hits > 0 ? f.hitDistance / f.hits : 0f):0.0}; hit it in [{hitsIn}]; hurt by [{hurtBy}];{f.note}";
        report.Add(line); Debug.Log("UGP bot " + line);
        File.WriteAllLines(Path.Combine(dir, "combat_bot.txt"), report);
        NextFight();
    }

    static float GetHealth(EnemyBase e) => e is UndergroundGuardian g ? g.Health : (float)typeof(EnemyBase).GetField("health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(e);

    static void Finish()
    {
        File.WriteAllLines(Path.Combine(dir, "combat_bot.txt"), report);
        EditorApplication.update -= Tick; SessionState.EraseString(Key);
        EditorApplication.ExitPlaymode(); EditorApplication.Exit(0);
    }

    static void Shot(string name, EnemyBase e, Vector2 q)
    {
        var cam = new GameObject("capture").AddComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        Vector2 mid = ((Vector2)e.transform.position + q) * .5f;
        cam.transform.position = new Vector3(mid.x, mid.y + 1f, -50f);
        cam.orthographicSize = Mathf.Max(5f, Vector2.Distance(e.transform.position, q) * .5f + 3f);
        var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(cam.gameObject);
    }
}
