using UnityEngine;

// B-00 Knucklebramble, the Grip Knot's guardian (R1): a knot of bramble rot fused into a giant
// Crawler, rooted in place. It teaches reading warnings and attack windows (world design 6):
// when Qori comes near it strikes with its three thorn-arms in turn, each one raised with a glint
// and a roar before it slams down on its own side (the front arm claws the floor ahead, the top
// arm chops over its head at jumping height, and it turns its back arm on him to rake the floor
// behind). After the third slam its body splits open and the knot inside is exposed for a few
// seconds. Blows only land then; before that they glance off the bramble. The exposed body
// closes again after a few hits, and the cycle starts over.
// Only a slamming arm hurts; the body can be stood against (and on). The rig and the pose table
// come from Codex's parts (KnucklebrambleRigBuilder, Tools/CreatureRigAuthoring/knucklebramble.py).
[RequireComponent(typeof(Rigidbody2D))]
public sealed class KnucklebrambleGuardian : EnemyBase
{
    [Header("Fight")]
    [SerializeField] float wakeRange = 9f;
    [SerializeField] float pauseTime = .6f, telegraphTime = .85f, slamTime = .16f, holdTime = .4f, recoverTime = .45f;
    [SerializeField] float openTime = .35f, exposedTime = 3f, closeTime = .4f;
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

    float since, facing = 1f, blockedShakeUntil;
    int swipe, exposedHits;
    readonly float[] angles = new float[6], rest = new float[6], from = new float[6];

    protected override void Awake()
    {
        base.Awake();
        maximumHealth = Mathf.Max(maximumHealth, 6); health = maximumHealth;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        Pose("Rest", rest);
        System.Array.Copy(rest, angles, 6);
        Enter(State.Dormant);
    }

    void Enter(State s)
    {
        System.Array.Copy(angles, from, 6);   // each state blends from wherever the arms are
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
        Fx.DeathPuff(at + Vector2.left * .6f, 2.2f); Fx.DeathPuff(at + Vector2.right * .6f, 2.2f);
        Fx.Leaves(at, 8, 1.2f);
        CombatCameraShake.Kick(.06f);
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
                if (sees) { swipe = 0; Enter(State.Pause); }
                break;
            case State.Pause:
                if (t < pauseTime) break;
                if (!sees) { Enter(State.Dormant); break; }
                StrikingArm = armOrder[swipe % armOrder.Length];
                // It faces Qori with the arm that will strike: the back arm (1) strikes behind it.
                float side = Mathf.Abs(to.x) > .05f ? Mathf.Sign(to.x) : facing;
                facing = StrikingArm == 1 ? -side : side;
                Fx.Glint(Claw(StrikingArm) + Vector2.up * .2f);
                Enter(State.Telegraph);
                break;
            case State.Telegraph:
                if (t >= telegraphTime) Enter(State.Slam);
                break;
            case State.Slam:
                if (t >= slamTime)
                {
                    Vector2 claw = Claw(StrikingArm);
                    var lib = Fx.Library;
                    if (lib != null) Fx.Play(lib.dustLand, new Vector2(claw.x, body.position.y + .6f), 14f, 1.8f, 31);
                    Fx.Leaves(claw, 2, .5f);
                    CombatCameraShake.Kick(.04f);
                    Enter(State.Hold);
                }
                break;
            case State.Hold:
                if (t >= holdTime) Enter(State.Recover);
                break;
            case State.Recover:
                if (t < recoverTime) break;
                StrikingArm = 0;
                if (++swipe >= armOrder.Length) { swipe = 0; Enter(State.Opening); }
                else Enter(State.Pause);
                break;
            case State.Opening:
                if (t >= openTime) { exposedHits = 0; Enter(State.Exposed); }
                break;
            case State.Exposed:
                if (t >= exposedTime) Enter(State.Closing);
                break;
            case State.Closing:
                if (t >= closeTime) Enter(State.Pause);
                break;
        }
    }

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
            case State.Telegraph: target = $"Arm{StrikingArm}_Raised"; k = Smooth(t / telegraphTime); break;
            case State.Slam: target = $"Arm{StrikingArm}_Slam"; k = Mathf.Clamp01(t / slamTime); k *= k; break;
            case State.Hold: target = $"Arm{StrikingArm}_Slam"; k = 1f; break;
            case State.Recover: target = "Rest"; k = Smooth(t / recoverTime); break;
            case State.Opening: target = "Knot_Exposed"; k = Smooth(t / openTime); break;
            case State.Exposed: target = "Knot_Exposed"; k = 1f; break;
            case State.Closing: target = "Rest"; k = Smooth(t / closeTime); break;
            default: target = "Rest"; k = Smooth(t / .5f); break;
        }
        var goal = new float[6]; Pose(target, goal);
        for (int i = 0; i < 6; i++) angles[i] = Mathf.LerpAngle(from[i], goal[i], k);
        // A tremble as the raised arm gathers itself, and a slow sway at rest.
        float tremble = Current == State.Telegraph && t > telegraphTime * .6f ? Mathf.Sin(Time.time * 70f) * 2.5f : 0f;
        for (int arm = 1; arm <= 3; arm++)
        {
            int u = 2 * (arm - 1);
            float sway = arm == StrikingArm && Current != State.Dormant && Current != State.Pause ? tremble : breathe * (2f + arm);
            rig.Pose($"Arm{arm}_Upper", Mathf.DeltaAngle(rest[u], angles[u]) + sway);
            rig.Pose($"Arm{arm}_Lower", Mathf.DeltaAngle(rest[u + 1] - rest[u], angles[u + 1] - angles[u]) + sway * .6f);
            // The striking arm comes to the front for the top arm's chop over the head.
            SetArmInFront(arm, arm == 2 && StrikingArm == 2 && (Current == State.Slam || Current == State.Hold || Current == State.Recover && t < recoverTime * .5f));
        }

        // Body and head: a slow breath, a flinch back while an arm gathers, a heave on the slam.
        float gather = Current == State.Telegraph ? Smooth(t / telegraphTime) : 0f;
        float heave = Current == State.Slam || Current == State.Hold ? 1f : Current == State.Recover ? 1f - Smooth(t / recoverTime) : 0f;
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
