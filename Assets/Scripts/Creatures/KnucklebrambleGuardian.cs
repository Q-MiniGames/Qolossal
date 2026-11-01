using UnityEngine;

// B-00 Knucklebramble, the Grip Knot's guardian (R1): a knot of bramble rot fused into a giant
// Crawler, rooted in place. It teaches reading warnings and attack windows (world design 6):
// when Qori comes near it strikes with its three thorn-arms in turn, each one raised with a glint
// and a roar before it slams down on its own side (the front arm claws the floor ahead, the top
// arm chops over its head at jumping height, and it turns its back arm on him to rake the floor
// behind). After the third slam its body splits open and the knot inside is exposed for a few
// seconds. Blows only land then; before that they glance off the bramble. The exposed body
// closes again after a few hits, and the cycle starts over.
// It gets harder as it goes (14 Oct): the first cycle teaches the three arms one at a time; from
// the second cycle the arms chain into a combo, the next arm rising as the last one recovers;
// below half health (phase two) everything quickens, it strikes four times in a random order
// and the knot stays open for less time.
// Only a slamming arm hurts; the body can be stood against (and on). The rig and the pose table
// come from Codex's parts (KnucklebrambleRigBuilder, Tools/CreatureRigAuthoring/knucklebramble.py).
[RequireComponent(typeof(Rigidbody2D))]
public sealed class KnucklebrambleGuardian : EnemyBase
{
    [Header("Fight")]
    [SerializeField] float wakeRange = 9f;
    [SerializeField] float pauseTime = .35f, telegraphTime = .6f, slamTime = .14f, holdTime = .3f, recoverTime = .3f;
    [SerializeField] float openTime = .3f, exposedTime = 2.2f, closeTime = .3f;
    [Tooltip("From the second cycle the next arm rises straight out of the last slam's recovery, with this shorter warning.")]
    [SerializeField] float comboTelegraphTime = .42f;
    [Tooltip("Phase two starts at or below this share of its health.")]
    [SerializeField, Range(0f, 1f)] float phaseTwoAt = .5f;
    [Tooltip("In phase two every wind-up, hold and recovery runs this much faster, and the knot stays open for that much less.")]
    [SerializeField, Min(1f)] float phaseTwoSpeed = 1.3f;
    [SerializeField, Min(1)] int phaseTwoSlams = 4;
    [SerializeField, Min(1)] int hitsPerExposure = 3;
    [Tooltip("The order the arms strike in each cycle (1 back, 2 top, 3 front).")]
    [SerializeField] int[] armOrder = { 3, 2, 1 };

    [Header("Rig (set by KnucklebrambleRigBuilder)")]
    [SerializeField] string[] poseNames = new string[0];
    [Tooltip("Six world angles per pose (degrees): Arm1 upper, lower, Arm2 upper, lower, Arm3 upper, lower.")]
    [SerializeField] float[] poseAngles = new float[0];
    [SerializeField] SpriteRenderer bodyImage, glowImage;
    [SerializeField] Sprite bodySprite, exposedSprite, glowSprite, glowBareSprite;
    [Tooltip("Each arm's claw end, where the slam lands (index = arm - 1).")]
    [SerializeField] Transform[] claws = new Transform[3];

    public enum State { Dormant, Pause, Telegraph, Slam, Hold, Recover, Opening, Exposed, Closing }
    public State Current { get; private set; }
    public int StrikingArm { get; private set; }
    public bool KnotExposed => Current == State.Exposed;
    public float Health => health;
    public bool PhaseTwo => health <= maximumHealth * phaseTwoAt;
    public int Cycle => cycle;

    float since, facing = 1f, blockedShakeUntil;
    int swipe, exposedHits, cycle, slamsThisCycle = 3;
    float windup;   // this strike's warning time (the full one, or the combo's shorter one)
    readonly float[] angles = new float[6], rest = new float[6], from = new float[6];

    public override string BossBarName => "The Knucklebramble";

    protected override void Awake()
    {
        base.Awake();
        maximumHealth = Mathf.Max(maximumHealth, 9); health = maximumHealth;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        Pose("Rest", rest);
        System.Array.Copy(rest, angles, 6);
        Enter(State.Dormant);
    }

    bool woken;
    void Enter(State s)
    {
        System.Array.Copy(angles, from, 6);   // each state blends from wherever the arms are
        if (s == State.Pause && Current == State.Dormant && !woken) { woken = true; Sfx.Play("Guardian_Awaken", body.position); }
        else if (s == State.Telegraph) Sfx.Play("Guardian_ArmRaise", Claw(StrikingArm));
        Current = s; since = Time.time;
    }

    // True while `arm`'s claws are coming down or pinning the floor: only then does it hurt.
    public bool ArmStriking(int arm) => IsAlive && StrikingArm == arm &&
        (Current == State.Slam || Current == State.Hold && Time.time - since < holdTime * .5f);

    public override bool HurtsOnContact => false;   // the body is harmless; GuardianArmHazard does the hurting

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        if (Current != State.Exposed)
        {
            blockedShakeUntil = Time.time + .15f;
            return WeaponKinds.Blocked();
        }
        var response = base.ReceiveCombatHit(hit);
        if (IsAlive) Sfx.Play("Guardian_Hurt", DeathPoint);
        if (IsAlive && ++exposedHits >= hitsPerExposure) Enter(State.Closing);
        return response;
    }

    // Opens the body at once (the fight's own path is three slams; tests use this to skip them).
    public void Expose() { if (IsAlive) { StrikingArm = 0; Enter(State.Opening); } }

    protected override Vector2 DeathPoint => glowImage != null ? (Vector2)glowImage.transform.position : base.DeathPoint;

    protected override void Die()
    {
        Vector2 at = DeathPoint;
        base.Die();
        Sfx.Play("Guardian_Defeat", at);
        Fx.DeathPuff(at + Vector2.left * .6f, 2.2f); Fx.DeathPuff(at + Vector2.right * .6f, 2.2f);
        Fx.Leaves(at, 8, 1.2f);
        CombatCameraShake.Kick(FeedbackTuning.Current.guardianKilledShake);
        HitStop.Freeze(HitStop.GuardianKilled);
    }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 knot = glowImage != null ? (Vector2)glowImage.transform.position : body.position + Vector2.up;
        bool sees = CanSee(knot, wakeRange, 4f, out Vector2 to);
        switch (Current)
        {
            case State.Dormant:
                if (sees) { swipe = 0; slamsThisCycle = SlamsThisCycle(); Enter(State.Pause); }
                break;
            case State.Pause:
                if (t < pauseTime / Speed) break;
                if (!sees) { Enter(State.Dormant); break; }
                BeginStrike(to, telegraphTime);
                break;
            case State.Telegraph:
                if (t >= windup) Enter(State.Slam);
                break;
            case State.Slam:
                if (t >= slamTime)
                {
                    Vector2 claw = Claw(StrikingArm);
                    var lib = Fx.Library;
                    if (lib != null) Fx.Play(lib.dustLand, new Vector2(claw.x, body.position.y + .6f), 14f, 1.8f, 31);
                    Fx.Leaves(claw, 2, .5f);
                    Sfx.Play("Guardian_ArmSlam", claw);
                    CombatCameraShake.Kick(FeedbackTuning.Current.guardianSlamShake);
                    Enter(State.Hold);
                }
                break;
            case State.Hold:
                if (t >= holdTime / Speed) Enter(State.Recover);
                break;
            case State.Recover:
                // In a combo the next arm starts rising while this one is still settling.
                bool chain = cycle > 0 && swipe + 1 < slamsThisCycle && sees;
                if (t < recoverTime / Speed * (chain ? .5f : 1f)) break;
                StrikingArm = 0;
                if (++swipe >= slamsThisCycle) { swipe = 0; cycle++; Enter(State.Opening); }
                else if (chain) BeginStrike(to, comboTelegraphTime);
                else Enter(State.Pause);
                break;
            case State.Opening:
                if (t >= openTime) { exposedHits = 0; Enter(State.Exposed); HitStop.Freeze(HitStop.GuardianExposed); }
                break;
            case State.Exposed:
                if (t >= exposedTime / Speed) Enter(State.Closing);
                break;
            case State.Closing:
                if (t >= closeTime / Speed) { slamsThisCycle = SlamsThisCycle(); Enter(State.Pause); }
                break;
        }
    }

    float Speed => PhaseTwo ? phaseTwoSpeed : 1f;
    int SlamsThisCycle() => PhaseTwo ? phaseTwoSlams : armOrder.Length;

    // Picks the next arm, turns that arm toward Qori and raises it with a glint.
    void BeginStrike(Vector2 to, float warning)
    {
        int last = StrikingArm > 0 ? StrikingArm : lastArm;
        if (PhaseTwo)
        {
            // A random arm, never the same one twice in a row, so the order can't be learned by rote.
            int arm = Random.Range(1, 3);
            StrikingArm = arm >= last ? arm + 1 : arm;
        }
        else StrikingArm = armOrder[swipe % armOrder.Length];
        lastArm = StrikingArm;
        // It faces Qori with the arm that will strike: the back arm (1) strikes behind it.
        float side = Mathf.Abs(to.x) > .05f ? Mathf.Sign(to.x) : facing;
        facing = StrikingArm == 1 ? -side : side;
        windup = warning / Speed;
        Fx.Glint(Claw(StrikingArm) + Vector2.up * .2f);
        Enter(State.Telegraph);
    }

    int lastArm;

    Vector2 Claw(int arm) => arm >= 1 && arm <= claws.Length && claws[arm - 1] != null ? (Vector2)claws[arm - 1].position : body.position;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        float t = Time.time - since;
        float breathe = Mathf.Sin(Time.time * 1.7f);

        // The arms: blend from where they were toward this state's pose.
        string target; float k;
        switch (Current)
        {
            case State.Telegraph: target = $"Arm{StrikingArm}_Raised"; k = Smooth(t / windup); break;
            case State.Slam: target = $"Arm{StrikingArm}_Slam"; k = Mathf.Clamp01(t / slamTime); k *= k; break;
            case State.Hold: target = $"Arm{StrikingArm}_Slam"; k = 1f; break;
            case State.Recover: target = "Rest"; k = Smooth(t / (recoverTime / Speed)); break;
            case State.Opening: target = "Knot_Exposed"; k = Smooth(t / openTime); break;
            case State.Exposed: target = "Knot_Exposed"; k = 1f; break;
            case State.Closing: target = "Rest"; k = Smooth(t / (closeTime / Speed)); break;
            default: target = "Rest"; k = Smooth(t / .5f); break;
        }
        var goal = new float[6]; Pose(target, goal);
        for (int i = 0; i < 6; i++) angles[i] = Mathf.LerpAngle(from[i], goal[i], k);
        // A tremble as the raised arm gathers itself, and a slow sway at rest.
        float tremble = Current == State.Telegraph && t > windup * .6f ? Mathf.Sin(Time.time * 70f) * 2.5f : 0f;
        for (int arm = 1; arm <= 3; arm++)
        {
            int u = 2 * (arm - 1);
            float sway = arm == StrikingArm && Current != State.Dormant && Current != State.Pause ? tremble : breathe * (2f + arm);
            rig.Pose($"Arm{arm}_Upper", Mathf.DeltaAngle(rest[u], angles[u]) + sway);
            rig.Pose($"Arm{arm}_Lower", Mathf.DeltaAngle(rest[u + 1] - rest[u], angles[u + 1] - angles[u]) + sway * .6f);
            // The striking arm comes to the front for the top arm's chop over the head.
            SetArmInFront(arm, arm == 2 && StrikingArm == 2 && (Current == State.Slam || Current == State.Hold || Current == State.Recover && t < recoverTime / Speed * .5f));
        }

        // Body and head: a slow breath, a flinch back while an arm gathers, a heave on the slam.
        float gather = Current == State.Telegraph ? Smooth(t / windup) : 0f;
        float heave = Current == State.Slam || Current == State.Hold ? 1f : Current == State.Recover ? 1f - Smooth(t / (recoverTime / Speed)) : 0f;
        float shake = Time.time < blockedShakeUntil ? Mathf.Sin(Time.time * 90f) * .03f : 0f;
        rig.Pose("Body", -2f * gather + 3f * heave, new Vector2(shake - .05f * gather, .02f * breathe - .06f * heave));
        rig.Pose("Head", 8f * gather - 4f * heave);
        rig.Pose("Jaw", -22f * gather - 10f * heave);

        // The knot: the exposed body and the bare knot while open; a pulse that quickens as it opens.
        bool open = Current == State.Exposed || Current == State.Opening && t > openTime * .5f || Current == State.Closing && t < closeTime * .5f;
        if (bodyImage != null) bodyImage.sprite = open ? exposedSprite : bodySprite;
        if (glowImage != null)
        {
            glowImage.sprite = open ? glowBareSprite : glowSprite;
            float pulse = open ? 1.1f + .12f * Mathf.Sin(Time.time * 8f) : 1f + .04f * breathe;
            glowImage.transform.localScale = Vector3.one * pulse;
        }
    }

    readonly System.Collections.Generic.Dictionary<Renderer, int> baseOrder = new System.Collections.Generic.Dictionary<Renderer, int>();

    void SetArmInFront(int arm, bool front)
    {
        foreach (string part in new[] { "_Upper", "_Lower" })
        {
            Transform bone = rig.Bone($"Arm{arm}{part}");
            if (bone == null) continue;
            foreach (var r in bone.GetComponentsInChildren<SpriteRenderer>())
            {
                if (r.transform.parent != bone) continue;   // its own art, not the lower arm's
                if (!baseOrder.TryGetValue(r, out int order)) baseOrder[r] = order = r.sortingOrder;
                r.sortingOrder = front ? order + 20 : order;
            }
        }
    }

    static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    void Pose(string name, float[] into)
    {
        int p = System.Array.IndexOf(poseNames, name);
        if (p < 0) p = 0;
        System.Array.Copy(poseAngles, p * 6, into, 0, 6);
    }
}
