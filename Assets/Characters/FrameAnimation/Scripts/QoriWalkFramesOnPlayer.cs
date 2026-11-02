using UnityEngine;
using UnityEngine.InputSystem;

// Test-only: the Young Adventurer worn by the real Player in the QoriWalkTest scene, as frame animations:
// the Ludo.ai idle (standing), walk and run (A/D; running is the normal pace), and for comparison the Codex Batch 27b walk (C), plus the Ludo air,
// wall, ledge, dash, glide and attack clips. The current Qori rig keeps running underneath (it drives the hands and
// weapon points) but is never shown, except when V switches to it for comparison.
// Playback: each clip loops at its own cycle time, sped up or slowed with the Player's speed against the speed it is
// meant for (clamped 0.6-1.6, like the rig). B locks frames to the distance travelled instead (planted feet exact).
// Keys: V current Qori, F7 Codex walk, N run frames off, B playback, [ ] size, H help.
[DefaultExecutionOrder(200)]
public sealed class QoriWalkFramesOnPlayer : MonoBehaviour
{
    [System.Serializable]
    public sealed class Clip
    {
        public string label;
        public Sprite[] frames;
        public float strideCanvasPx;     // ground a planted foot covers per cycle, in frame pixels
        public float figureHeightPx;     // top of the ears to the soles, in frame pixels
        public float cycleSeconds;       // loop time at refSpeed
        public float refSpeed;           // the Player speed (u/s) this clip is played for
        public int standFrame;           // frame held when standing still
        public float fps = 15f;          // one-shots and air loops play at this rate
        public bool facesLeft;           // drawn facing left (the wall jump): flipped the other way
        public AudioClip sound;          // Ludo's sound for the source clip (3 s, in step with its 36 frames)
        public float soundStart, soundEnd; // the stretch of it that belongs to these frames (seconds)
        public bool soundLoop;
        public bool pingPong;            // loops play forward then back (a hold, not a cycle)
        public float holdSway;           // a held pose's gentle up-and-down (fraction of Qori's height; the ledge hang)
        public int contactFrame;         // attacks: the frame where the hit starts (the attack's wind-up plays the frames before it)
        [System.NonSerialized] public float scale, phase;   // world units per frame pixel; position in the loop
    }

    public Clip idle = new Clip(), walk = new Clip(), run = new Clip(), codexWalk = new Clip();
    [Header("Ludo clips 1-5 and the slash (cut by export_ludo_segments.py)")]
    public Clip jumpTakeoff = new Clip(), jumpRise = new Clip(), jumpFall = new Clip(), jumpLand = new Clip(), dash = new Clip(),
                glideOpen = new Clip(), glide = new Clip(), wallSlide = new Clip(), wallJump = new Clip(), ledgeHang = new Clip(),
                ledgeClimb = new Clip(), slash = new Clip(), strike = new Clip();
    [Header("Ludo attack set A1-A4 (the Leaf Staff's moves)")]
    public Clip combo1 = new Clip(), combo2 = new Clip(), combo3 = new Clip(), upAttack = new Clip(),
                sprintAttack = new Clip(), chargeStart = new Clip(), chargeHold = new Clip(), chargeRelease = new Clip();
    [Header("Double jump (waits for its own Ludo clip; the rise frames play meanwhile)")]
    public Clip doubleJump = new Clip();
    [Header("Plunge (Down + attack in the air): into the dive, the dive (looped), the landing burst")]
    public Clip plungeStart = new Clip(), plungeDive = new Clip(), plungeLand = new Clip();
    [Header("Evade (the dodge): its own clip once Ludo's is cut; the dash frames meanwhile")]
    public Clip evade = new Clip();
    [Header("Rope hang (on the thread) and the staff shot (Q: aim and shoot)")]
    public Clip ropeHang = new Clip(), staffShot = new Clip();
    [Tooltip("Where the hands grip in the rope frames (frame pixels, y down): the thread starts there and the pose turns toward the anchor.")]
    public Vector2 ropeHandsPx = new Vector2(431, 279);
    public bool showFrames = true, useRun = true, useCodexWalk, distanceLocked, help = true;
    [Tooltip("The test room's keys and help text (V, F7, K, N, B, H, [ ]). Off in the game (LudoFramesInstaller's prefab).")]
    public bool testControls = true;
    [Tooltip("Play Ludo's sounds with the frames that have one (the game's own Qori sounds step aside meanwhile). K toggles.")]
    public bool ludoSounds = true;
    [Header("Idle: a calm rest, with the full breath now and then")]
    [Tooltip("The idle frames played back and forth while resting (0-based; the redone idle: all 36, its end does not meet its start).")]
    public int idleRestFirst = 0, idleRestLast = 35;
    public float idleRestFps = 12f, idleActionFps = 15f;
    [Tooltip("Seconds of rest between breaths (random in this range). The breath is the whole clip, from frame 36 round to 29.")]
    public Vector2 idleActionEvery = new Vector2(5f, 10f);
    [Tooltip("Play the full breath (the yawn) now and then. Off: the idle only rests.")]
    public bool idleBreath;
    public const int OldLayer = 30, NewLayer = 31;

    public PlayerMovement Player { get; private set; }
    public float CurrentQoriHeight { get; private set; }
    public Clip Current { get; private set; }
    public int Frame { get; private set; }
    public bool FramesVisible { get; private set; }

    QoriAnimator anim;
    QoriFx fx;
    PlayerThread thread;
    LineRenderer threadLine;
    QoriSounds gameSounds;
    AudioSource voice;
    Clip sounding;
    PlayerCombat combat;
    SpriteRenderer view;
    Vector2 feetOffset;      // from the Player's position to the current Qori's pelvis column on the feet line
    float lastX, sizeTweak = 1f;
    float idleT, nextBreath; int idleFrame, idleDir = 1; bool breathing;
    // one-shot timers and the previous frame's state
    float takeoffAt = -9f, landAt = -9f, wallJumpAt = -9f, glideAt = -9f, slashAt = -9f, climbAt = -9f, chargeAt = -9f, flipAt = -9f, airborneSince;
    int launchSeen, evadeSeen; float evadeAt = -9f;
    bool wasGrounded = true, wasWall, wasGliding, wasAttacking, wasClimbing, wasCharging;
    Clip[] All => new[] { idle, walk, run, codexWalk, jumpTakeoff, jumpRise, jumpFall, jumpLand, dash, glideOpen, glide, wallSlide, wallJump, ledgeHang, ledgeClimb, slash, strike,
                          combo1, combo2, combo3, upAttack, doubleJump, plungeStart, plungeDive, plungeLand, evade, ropeHang, staffShot, sprintAttack, chargeStart, chargeHold, chargeRelease };
    int attacks;   // attacks alternate: the strike, then the slash

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var f = FindDeep(c, name); if (f != null) return f; }
        return null;
    }

    static bool Has(Clip c) => c != null && c.frames != null && c.frames.Length > 0 && c.frames[0] != null;

    bool Attach()
    {
        Player = FindAnyObjectByType<PlayerMovement>();
        if (Player == null || !(Has(walk) || Has(codexWalk))) return false;
        anim = Player.GetComponentInChildren<QoriAnimator>();
        combat = Player.GetComponent<PlayerCombat>();
        gameSounds = Player.GetComponentInChildren<QoriSounds>();
        if (anim == null) return false;
        // The current Qori's standing size and feet line (body sprites only, no weapon).
        Bounds b = default; bool any = false;
        Transform weapon = anim.weapon != null ? anim.weapon.transform : null;
        foreach (var r in anim.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled || r.sprite == null || (weapon != null && r.transform.IsChildOf(weapon))) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) return false;
        CurrentQoriHeight = b.size.y;
        var body = FindDeep(anim.transform, "Body");
        float pelvisX = body != null ? body.position.x : b.center.x;
        feetOffset = new Vector2(pelvisX - Player.transform.position.x, b.min.y - Player.transform.position.y);
        // Each clip at the current Qori's overall height (ears to soles).
        foreach (var c in All)
            if (Has(c)) { c.scale = CurrentQoriHeight / Mathf.Max(1f, c.figureHeightPx); c.phase = c.standFrame / (float)c.frames.Length; }

        var go = new GameObject("Young Adventurer frames (test)");
        go.layer = NewLayer;
        view = go.AddComponent<SpriteRenderer>();
        view.sortingOrder = 40;
        voice = go.AddComponent<AudioSource>();
        voice.playOnAwake = false; voice.spatialBlend = 0f; voice.volume = .8f;
        lastX = Player.transform.position.x;
        foreach (var c in All)
            if (Has(c)) Debug.Log("[QoriWalkTest] " + c.label + ": " + c.frames.Length + " frames, cycle " + c.cycleSeconds + " s at " + c.refSpeed
                                  + " u/s; distance-locked it would cycle " + (c.refSpeed / (c.strideCanvasPx * c.scale)).ToString("0.0") + " times a second");
        Debug.Log("[QoriWalkTest] current Qori " + CurrentQoriHeight.ToString("0.000") + " units tall; game walk " + Player.WalkSpeed + ", run " + Player.RunSpeed + " u/s");
        return true;
    }

    void LateUpdate()
    {
        if (view == null && !Attach()) return;
        Keys();
        var p = Player;
        bool attacking = combat != null && combat.IsAttackPoseActive && combat.CurrentAttack != null;
        bool onGround = p.IsGrounded && !p.IsDashing && !p.IsLedgeHanging && !p.IsLedgeClimbing && !p.IsWallSliding && !attacking;
        float t = Time.time, vy = p.ObservedVelocity.y;
        // events that start one-shots
        if (wasGrounded && !p.IsGrounded) { airborneSince = t; if (vy > .5f) takeoffAt = t; }
        if (!wasGrounded && p.IsGrounded && t - airborneSince > .12f) landAt = t;
        if (wasWall && !p.IsWallSliding && !p.IsGrounded && vy > 0f) wallJumpAt = t;
        if (!wasGliding && p.IsGliding) glideAt = t;
        if (!wasAttacking && attacking) { slashAt = t; attacks++; }
        if (!wasClimbing && p.IsLedgeClimbing) climbAt = t;
        if (p.LaunchVersion != launchSeen) { launchSeen = p.LaunchVersion; if (p.LastLaunchKind == PlayerMovement.LaunchKind.DoubleJump) flipAt = t; }
        if (p.EvadeVersion != evadeSeen) { evadeSeen = p.EvadeVersion; evadeAt = t; }
        bool charging = combat != null && combat.IsCharging;
        if (!wasCharging && charging) chargeAt = t;
        wasCharging = charging;
        wasGrounded = p.IsGrounded; wasWall = p.IsWallSliding; wasGliding = p.IsGliding; wasAttacking = attacking; wasClimbing = p.IsLedgeClimbing;
        Clip special = PickSpecial(p, attacking, t, vy, out int specialFrame);
        // the old rig never shows unless V asks for it: every state has a Young Adventurer clip (or the nearest one)
        FramesVisible = showFrames;

        float x = p.transform.position.x, dx = Mathf.Abs(x - lastX); lastX = x;
        float speed = Mathf.Abs(p.ObservedVelocity.x);
        Clip walking = useCodexWalk || !Has(walk) ? codexWalk : walk;
        bool standing = onGround && speed < .25f;
        Clip c = special != null ? special
               : standing && Has(idle) && !useCodexWalk ? idle
               : useRun && Has(run) && onGround && (p.IsRunning || speed > (p.WalkSpeed + p.RunSpeed) * .5f) ? run : walking;
        if (c != Current && Current != null && c != idle && Current != idle) c.phase = Current.phase;   // keep the step when switching walk <-> run
        if (c == idle && Current != idle) { idleFrame = idleRestFirst; idleDir = 1; breathing = false; idleT = 0f; nextBreath = Time.time + Random.Range(idleActionEvery.x, idleActionEvery.y); }
        Current = c;
        int n = c.frames.Length;
        if (c == special) { Frame = specialFrame; }
        else if (c == idle) { Frame = IdleStep(n); }
        else
        {
        if (!onGround || speed < .25f) c.phase = c.standFrame / (float)n;
        else if (distanceLocked) c.phase += dx / (c.strideCanvasPx * c.scale * sizeTweak);
        else c.phase += Time.deltaTime / c.cycleSeconds * Mathf.Clamp(speed / c.refSpeed, .6f, 1.85f);   // up to the sprint's pace
        c.phase -= Mathf.Floor(c.phase);
        Frame = Mathf.Min(n - 1, Mathf.FloorToInt(c.phase * n));
        }

        float s = c.scale * sizeTweak * c.frames[Frame].pixelsPerUnit;   // world units per frame px x px per sprite unit
        view.sprite = c.frames[Frame];
        view.flipX = (p.FacingDirection < 0f) != c.facesLeft;
        view.transform.position = new Vector3(x + feetOffset.x * (p.FacingDirection < 0f ? -1f : 1f), p.transform.position.y + feetOffset.y, p.transform.position.z);
        view.transform.localScale = new Vector3(s, s, 1f);   // a scene root object: follows the Player, not parented to it
        // On the thread: the pose leans along the thread toward its anchor, its hands where the thread starts.
        view.transform.rotation = Quaternion.identity;
        if (c == ropeHang && thread != null && thread.IsAttached)
        {
            if (threadLine == null) threadLine = thread.GetComponentInChildren<LineRenderer>(true);
            Vector3 start = threadLine != null && threadLine.enabled && threadLine.positionCount > 0 ? threadLine.GetPosition(0) : view.transform.position;
            Vector2 toAnchor = thread.AnchorPosition - (Vector2)start;
            view.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toAnchor.y, toAnchor.x) * Mathf.Rad2Deg - 90f);
            var sp = c.frames[Frame];
            Vector2 local = new Vector2(ropeHandsPx.x - sp.pivot.x, (sp.rect.height - ropeHandsPx.y) - sp.pivot.y) / sp.pixelsPerUnit;
            if (view.flipX) local.x = -local.x;
            view.transform.position = start - view.transform.rotation * (Vector3)(local * s);
        }
        if (c.holdSway > 0f) view.transform.position += Vector3.up * Mathf.Sin(Time.time * Mathf.PI * 1.2f) * c.holdSway * CurrentQoriHeight;
        view.enabled = true;
        Sound(FramesVisible ? c : null);

        // The current Qori goes to OldLayer, and with it everything else drawn under the Player (the weapon's slash arc,
        // the sling's cords); the camera shows one or the other. Its dust, dash gust and wall scrape stop while the
        // frames show (the frames draw their own); hit sparks stay.
        // (the living thread's line stays: it belongs to the world, not to the old rig)
        foreach (var r in Player.GetComponentsInChildren<Renderer>(true))
            if (r.gameObject.layer != OldLayer && !r.gameObject.name.StartsWith("Living Thread")) r.gameObject.layer = OldLayer;
        if (fx == null) fx = Player.GetComponent<QoriFx>();
        if (fx != null) fx.movementEffects = !FramesVisible;
        var cam = Camera.main;
        if (cam != null) cam.cullingMask = (cam.cullingMask | (1 << OldLayer) | (1 << NewLayer)) & ~(1 << (FramesVisible ? OldLayer : NewLayer));
    }

    // Air, walls, ledges, dash, glide, attack and the landing: the Ludo clip for the state, if there is one (else null,
    // and the ground clips or the current rig take over). One-shots play from their event at the clip's rate and hold
    // their last frame; loops cycle at the clip's rate.
    Clip PickSpecial(PlayerMovement p, bool attacking, float t, float vy, out int frame)
    {
        frame = 0;
        if (combat != null && combat.IsCharging && Has(chargeHold))
            return Has(chargeStart) && t - chargeAt < chargeStart.frames.Length / chargeStart.fps ? Once(chargeStart, chargeAt, t, out frame) : Loop(chargeHold, t, out frame);
        if (attacking && combat.CurrentAttack.slingProjectile && Has(staffShot)) return Phased(staffShot, combat, out frame);
        if (thread == null) thread = p.GetComponent<PlayerThread>();
        if (thread != null && thread.IsAttached && Has(ropeHang)) return Loop(ropeHang, t, out frame);
        if (attacking && combat.CurrentAttack.plungeSpeed > 0f && Has(plungeDive))
        {
            if (combat.Phase == AttackPhase.Startup && Has(plungeStart))
            {
                frame = Mathf.Min(plungeStart.frames.Length - 1, Mathf.FloorToInt(combat.PhaseProgress * plungeStart.frames.Length));
                return plungeStart;
            }
            return Loop(plungeDive, t, out frame);
        }
        if (attacking && combat.CurrentAttack.attackId.Contains("Plunge landing") && Has(plungeLand)) return Phased(plungeLand, combat, out frame);
        if (attacking && AttackClip(combat.CurrentAttack) is Clip move) return Phased(move, combat, out frame);
        if (attacking && (Has(strike) || Has(slash)))
            return Once(Has(strike) && (attacks % 2 == 1 || !Has(slash)) ? strike : slash, slashAt, t, out frame);
        if (p.IsLedgeClimbing && Has(ledgeClimb)) return Once(ledgeClimb, climbAt, t, out frame);
        if (p.IsLedgeHanging && Has(ledgeHang)) return Loop(ledgeHang, t, out frame);
        if (p.IsWallSliding && Has(wallSlide)) return Loop(wallSlide, t, out frame);
        if (p.IsEvading) return Has(evade) ? Once(evade, evadeAt, t, out frame) : Has(dash) ? Loop(dash, t, out frame) : null;
        if (p.IsDashing && Has(dash)) return Loop(dash, t, out frame);
        if (Has(doubleJump) && !p.IsGrounded && t - flipAt < doubleJump.frames.Length / doubleJump.fps) return Once(doubleJump, flipAt, t, out frame);
        if (p.IsGliding && Has(glide))
            return Has(glideOpen) && t - glideAt < glideOpen.frames.Length / glideOpen.fps ? Once(glideOpen, glideAt, t, out frame) : Loop(glide, t, out frame);
        if (!p.IsGrounded)
        {
            if (Has(wallJump) && t - wallJumpAt < wallJump.frames.Length / wallJump.fps) return Once(wallJump, wallJumpAt, t, out frame);
            if (Has(jumpTakeoff) && t - takeoffAt < jumpTakeoff.frames.Length / jumpTakeoff.fps) return Once(jumpTakeoff, takeoffAt, t, out frame);
            if (vy > .5f && Has(jumpRise)) return Loop(jumpRise, t, out frame);
            if (Has(jumpFall)) return Loop(jumpFall, t, out frame);     // also the rope and anything else in the air
            return null;
        }
        if (Has(jumpLand) && t - landAt < jumpLand.frames.Length / jumpLand.fps) return Once(jumpLand, landAt, t, out frame);
        return null;
    }

    // Ludo's sound for the clip on screen: its stretch of the source track, from the start when the clip begins, looped
    // for loops, stopped at the stretch's end for one-shots.
    void Sound(Clip c)
    {
        Clip want = ludoSounds && c != null && c.sound != null ? c : null;
        // the game's own Qori sounds step aside only while a Ludo sound plays (walk, idle and slash have none of their own)
        if (gameSounds != null && gameSounds.enabled != (want == null)) gameSounds.enabled = want == null;
        if (want != sounding)
        {
            sounding = want;
            voice.Stop();
            if (want != null) { voice.clip = want.sound; voice.time = Mathf.Min(want.soundStart, want.sound.length - .01f); voice.Play(); }
        }
        else if (want != null && voice.clip != null && (voice.time >= want.soundEnd || !voice.isPlaying))
        {
            if (want.soundLoop) { voice.time = want.soundStart; if (!voice.isPlaying) voice.Play(); }
            else voice.Stop();
        }
    }

    // The Leaf Staff's moves (QoriArmoryFactory.BuildStaffMoves names them); others keep the strike and slash.
    Clip AttackClip(AttackDefinition a)
    {
        if (a == null || !a.attackId.StartsWith("Leaf Staff")) return null;
        string id = a.attackId;
        Clip c = id.Contains("Backswing") ? combo2 : id.Contains("Spin finisher") ? combo3
               : id.Contains("Sprint lunge") ? sprintAttack : id.Contains("Charged sweep") ? chargeRelease
               : a.direction == AttackAim.Up ? upAttack : a.direction == AttackAim.Front ? combo1 : null;
        return Has(c) ? c : null;
    }

    // An attack's frames follow its phases: the wind-up plays the frames before the contact frame, the hit and the
    // follow-through play the rest, so the swing on screen lands when the hit does whatever the tuning says.
    static Clip Phased(Clip c, PlayerCombat combat, out int frame)
    {
        var a = combat.CurrentAttack; float e = combat.AttackElapsed, start = Mathf.Max(.001f, a.startup);
        int n = c.frames.Length, k = Mathf.Clamp(c.contactFrame, 0, n - 1);
        float f = e < start ? e / start * k : k + (e - start) / Mathf.Max(.001f, a.TotalDuration - start) * (n - k);
        frame = Mathf.Clamp(Mathf.FloorToInt(f), 0, n - 1);
        return c;
    }

    static Clip Once(Clip c, float start, float t, out int frame)
    {
        frame = Mathf.Clamp(Mathf.FloorToInt((t - start) * c.fps), 0, c.frames.Length - 1);
        return c;
    }

    static Clip Loop(Clip c, float t, out int frame)
    {
        int n = c.frames.Length, i = (int)Mathf.Floor(t * c.fps);
        frame = c.pingPong && n > 1 ? (int)Mathf.PingPong(i, n - 1) : (int)Mathf.Repeat(i, n);
        return c;
    }

    // Rest: back and forth over the calm frames. Every few seconds, from the end of the rest stretch, the whole clip plays
    // once (the breath, eyes closing, leaves drifting) and comes back round to the start of the rest stretch.
    int IdleStep(int n)
    {
        idleT += Time.deltaTime * (breathing ? idleActionFps : idleRestFps);
        while (idleT >= 1f)
        {
            idleT -= 1f;
            if (breathing)
            {
                idleFrame = (idleFrame + 1) % n;
                if (idleFrame == idleRestFirst) { breathing = false; idleDir = 1; nextBreath = Time.time + Random.Range(idleActionEvery.x, idleActionEvery.y); }
            }
            else if (idleBreath && idleFrame >= idleRestLast && idleDir > 0 && Time.time >= nextBreath) { breathing = true; idleFrame = (idleFrame + 1) % n; }
            else
            {
                idleFrame += idleDir;
                if (idleFrame >= idleRestLast) { idleFrame = idleRestLast; idleDir = -1; }
                else if (idleFrame <= idleRestFirst) { idleFrame = idleRestFirst; idleDir = 1; }
            }
        }
        return Mathf.Clamp(idleFrame, 0, n - 1);
    }

    void Keys()
    {
        var k = Keyboard.current;
        if (k == null || !testControls) return;
        if (k.vKey.wasPressedThisFrame) showFrames = !showFrames;
        if (k.f7Key.wasPressedThisFrame) useCodexWalk = !useCodexWalk;   // F7: C is the dash
        if (k.kKey.wasPressedThisFrame) ludoSounds = !ludoSounds;
        if (k.nKey.wasPressedThisFrame) useRun = !useRun;
        if (k.bKey.wasPressedThisFrame) distanceLocked = !distanceLocked;
        if (k.hKey.wasPressedThisFrame) help = !help;
        if (k.leftBracketKey.wasPressedThisFrame) sizeTweak /= 1.05f;
        if (k.rightBracketKey.wasPressedThisFrame) sizeTweak *= 1.05f;
    }

    void OnGUI()
    {
        if (!help || !testControls || Player == null || Current == null) return;
        float speed = Mathf.Abs(Player.ObservedVelocity.x);
        GUI.Label(new Rect(12, 12, 1200, 22), "Young Adventurer test. A/D run, Shift sprint, Ctrl evade, Space jump.   V: " + (showFrames ? "show current Qori" : "show Young Adventurer")
                  + "   K: sounds = " + (ludoSounds ? "Ludo" : "game") + "   F7: walk = " + (useCodexWalk ? "Codex" : "Ludo") + "   N: run frames " + (useRun ? "on" : "off") + "   B: " + (distanceLocked ? "feet locked to distance" : "fixed cycle")
                  + "   [ ]: size " + sizeTweak.ToString("0.00") + "x   H: hide");
        GUI.Label(new Rect(12, 34, 1200, 22), "Showing: " + (FramesVisible ? Current.label + " frame " + (Frame + 1) + "/" + Current.frames.Length : "current Qori rig")
                  + "   speed " + speed.ToString("0.0") + " u/s");
    }
}
