using UnityEngine;

// The Thornheart, the rot's core wound round the titan's heart (R7's final guardian), drawn frame by frame from the
// Ludo.ai clips of the wide v4 model (Tools/ArtRequests/LudoRun/export_thornheart.py). First built for the Qori Walk
// Test room (16 Oct); the design is in Tools/ArtRequests/LUDO_PROMPTS_THORNHEART.md.
// It sleeps until Qori comes down to its floor, roars, then attacks, turning to face him before each attack:
//   Claw slam: the claw rises and opens (the warning), crashes down in front of its head (that spot hurts) and sends a
//              thorn shockwave rolling along the floor away from it (jump it).
//   Charge:    it rears back, then slides across the floor at him (its lower body hurts; jump over it) and skids.
// After an attack the heart in its chest glows: only hits on the heart hurt it, and only then. Everything else
// glances off. Three phases with a roar between: phase two chains the slam into the charge before the heart opens,
// phase three is quicker and picks its attacks at random. The slam's fall and the charge are made in code, since
// Ludo's clips pop on "smash" and reinvent the body when it moves far.
[RequireComponent(typeof(Rigidbody2D))]
public sealed class ThornheartBoss : EnemyBase
{
    [Header("Frames (export_thornheart.py; drawn facing right)")]
    public Sprite[] idle = new Sprite[0], raise = new Sprite[0], hurt = new Sprite[0], death = new Sprite[0];
    [Tooltip("The idle body's height in frame pixels, the feet centre and the heart, in canvas pixels (y down).")]
    public float figureHeightPx = 304f;
    public Vector2 pivotPx = new Vector2(380, 470), heartPx = new Vector2(336, 340);
    [Tooltip("The idle body's height in world units.")] [SerializeField] float bodyHeight = 4.6f;

    [Header("Fight")]
    [SerializeField] float wakeRange = 16f;
    [SerializeField] float roarTime = 1.4f, pauseTime = .9f;
    [SerializeField] float slamRaiseTime = .75f, slamHoldTime = .15f, slamDropTime = .1f, slamLingerTime = .45f;
    [Tooltip("How far ahead of its feet the claw lands (world units), and the slam's width.")]
    [SerializeField] float slamReach = 4.2f, slamWidth = 2.4f;
    [SerializeField] float shockSpeed = 9f, shockRange = 15f;
    [SerializeField] float chargeLeanTime = .6f, chargeSpeed = 12f, chargeSkidTime = .45f, chargeMaxDistance = 22f;
    [Tooltip("The charge's hurting part: the lower share of its body (jump over the rest).")]
    [SerializeField, Range(.2f, 1f)] float chargeHurtHeight = .55f;
    [SerializeField] float exposedTime = 1.8f, hurtTime = .35f;
    [SerializeField, Min(1)] int hitsPerWindow = 3;
    [Tooltip("Phase three runs every wind-up and window this much faster.")]
    [SerializeField, Min(1f)] float phaseThreeSpeed = 1.25f;

    public enum State { Dormant, Roar, Pause, SlamRaise, SlamHold, SlamDrop, SlamLinger, ChargeLean, Charge, Skid, Exposed, Hurt, Dead }
    public State Current { get; private set; }
    public int Phase => health > maximumHealth * 2f / 3f ? 1 : health > maximumHealth / 3f ? 2 : 3;
    public bool HeartOpen => IsAlive && (Current == State.Exposed || Current == State.Hurt);
    public bool Charging => IsAlive && Current == State.Charge;
    public bool SlamLanding => IsAlive && (Current == State.SlamDrop || Current == State.SlamLinger && Time.time - since < .12f);
    public float Health => health;

    SpriteRenderer view;
    Transform heartPoint, slamZone, lowerBody;
    float since, facing = -1f, scale, chargeStartX, nextFlash;
    int phaseSeen = 1, windowHits, attacks;
    bool chainCharge;
    State afterHurt;

    float Speed => Phase >= 3 ? phaseThreeSpeed : 1f;
    float Px => scale / 100f;   // world units per frame pixel (frames import at 100 pixels per unit)

    protected override void Awake()
    {
        base.Awake();
        maximumHealth = Mathf.Max(maximumHealth, 12); health = maximumHealth;
        body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true;
        scale = bodyHeight / (figureHeightPx / 100f);
        var art = new GameObject("Thornheart frames"); art.transform.SetParent(transform, false);
        art.transform.localScale = Vector3.one * scale;
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 6;
        if (idle.Length > 0) view.sprite = idle[0];

        // The body: hits glance off it (a trigger, so Qori can be run through by the charge rather than shoved).
        float w = 520f * Px, h = figureHeightPx * Px;
        var box = gameObject.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(w * .8f, h * .85f); box.offset = new Vector2(0, h * .45f);
        heartPoint = Child("Heart (where hits count)", ThornheartPart.Kind.Heart);
        heartPoint.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        heartPoint.GetComponent<CircleCollider2D>().radius = .8f;
        lowerBody = Child("Lower body (hurts while charging)", ThornheartPart.Kind.Charge);
        var lb = lowerBody.gameObject.AddComponent<BoxCollider2D>(); lb.isTrigger = true; lb.size = new Vector2(w * .85f, h * chargeHurtHeight); lowerBody.localPosition = new Vector3(0, h * chargeHurtHeight * .5f, 0);
        slamZone = Child("Claw slam (hurts as it lands)", ThornheartPart.Kind.Slam);
        var sz = slamZone.gameObject.AddComponent<BoxCollider2D>(); sz.isTrigger = true; sz.size = new Vector2(slamWidth, 1.8f);
        Enter(State.Dormant);
    }

    Transform Child(string name, ThornheartPart.Kind kind)
    {
        var obj = new GameObject(name); obj.transform.SetParent(transform, false);
        obj.AddComponent<ThornheartPart>().Set(this, kind);
        return obj.transform;
    }

    public override bool HurtsOnContact => false;   // only its parts hurt: the slam, the shockwave, the charge

    void Enter(State s)
    {
        Current = s; since = Time.time;
        switch (s)
        {
            case State.Roar: Sfx.Play("Guardian_Awaken", HeartWorld); CombatCameraShake.Kick(.12f); break;
            case State.SlamRaise: Fx.Glint(ClawWorld + Vector2.up * 1.5f); Sfx.Play("Guardian_ArmRaise", ClawWorld); break;
            case State.SlamDrop:
                Sfx.Play("Guardian_ArmSlam", ClawWorld); CombatCameraShake.Kick(.16f);
                if (Fx.Library != null) Fx.Play(Fx.Library.dustLand, ClawWorld, 14f, 2.6f, 30);
                Fx.Leaves(ClawWorld + Vector2.up * .3f, 4, 1f);
                ThornShockwave.Launch(ClawWorld, facing, shockSpeed * Speed, shockRange);
                break;
            case State.ChargeLean: Fx.Glint(HeartWorld + Vector2.up * 1.2f); Sfx.Play("Guardian_ArmRaise", HeartWorld); break;
            case State.Charge: chargeStartX = transform.position.x; break;
            case State.Exposed: windowHits = 0; break;
        }
    }

    Vector2 Feet => transform.position;
    Vector2 ClawWorld => Feet + new Vector2(facing * slamReach, 0f);
    Vector2 HeartWorld => heartPoint != null ? (Vector2)heartPoint.position : Feet;

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since, sp = Speed;
        var p = Player;
        switch (Current)
        {
            case State.Dormant:
                if (p != null && Mathf.Abs(p.transform.position.x - Feet.x) < wakeRange && p.transform.position.y < Feet.y + 3f) { Face(); Enter(State.Roar); }
                break;
            case State.Roar: if (t >= roarTime) Enter(State.Pause); break;
            case State.Pause:
                if (t >= pauseTime / sp)
                {
                    Face();
                    bool slam = Phase >= 3 ? Random.value < .5f : attacks % 2 == 0;
                    attacks++;
                    chainCharge = Phase == 2 && slam;
                    Enter(slam ? State.SlamRaise : State.ChargeLean);
                }
                break;
            case State.SlamRaise: if (t >= slamRaiseTime / sp) Enter(State.SlamHold); break;
            case State.SlamHold: if (t >= slamHoldTime / sp) Enter(State.SlamDrop); break;
            case State.SlamDrop: if (t >= slamDropTime) Enter(State.SlamLinger); break;
            case State.SlamLinger:
                if (t >= slamLingerTime / sp)
                {
                    if (chainCharge) { chainCharge = false; Face(); Enter(State.ChargeLean); }
                    else Enter(State.Exposed);
                }
                break;
            case State.ChargeLean: if (t >= chargeLeanTime / sp) Enter(State.Charge); break;
            case State.Charge:
            {
                float step = facing * chargeSpeed * sp * Time.fixedDeltaTime;
                float half = 520f * Px * .45f;
                bool wall = Physics2D.Raycast(Feet + Vector2.up * 1.2f, new Vector2(facing, 0), half + Mathf.Abs(step) + .3f, GroundLayer.Mask);
                bool passed = p != null && (p.transform.position.x - Feet.x) * facing < -6f;
                if (wall || passed || Mathf.Abs(Feet.x - chargeStartX) > chargeMaxDistance) { Enter(State.Skid); break; }
                body.MovePosition(Feet + new Vector2(step, 0));
                if (Fx.Library != null && Time.time >= nextFlash) { nextFlash = Time.time + .1f; Fx.Play(Fx.Library.dustRun, Feet - new Vector2(facing * half, 0) + Vector2.up * .1f, 14f, 1.4f, 30, facing > 0); }
                break;
            }
            case State.Skid:
            {
                float k = 1f - Mathf.Clamp01(t / (chargeSkidTime / sp));
                float half = 520f * Px * .45f;
                bool wall = Physics2D.Raycast(Feet + Vector2.up * 1.2f, new Vector2(facing, 0), half + .3f, GroundLayer.Mask);
                if (!wall) body.MovePosition(Feet + new Vector2(facing * chargeSpeed * sp * .5f * k * Time.fixedDeltaTime, 0));
                if (t >= chargeSkidTime / sp) { CombatCameraShake.Kick(.06f); Enter(State.Exposed); }
                break;
            }
            case State.Exposed: if (t >= exposedTime / sp || windowHits >= hitsPerWindow) EndWindow(); break;
            case State.Hurt: if (t >= hurtTime) { if (windowHits >= hitsPerWindow) EndWindow(); else { Current = State.Exposed; } } break;
        }
    }

    void EndWindow()
    {
        if (Phase != phaseSeen) { phaseSeen = Phase; Face(); Enter(State.Roar); }
        else Enter(State.Pause);
    }

    void Face()
    {
        var p = Player;
        if (p != null && Mathf.Abs(p.transform.position.x - Feet.x) > .5f) facing = Mathf.Sign(p.transform.position.x - Feet.x);
    }

    // Hits on the body glance off (silently while the heart is open, so the heart's hit is the one that counts).
    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive || HeartOpen) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        return WeaponKinds.Blocked();
    }

    public CombatDamageResponse HitHeart(CombatDamage hit)
    {
        if (!HeartOpen) return ReceiveCombatHit(hit);
        var response = base.ReceiveCombatHit(hit);
        if (response.Disposition == CombatHitDisposition.Damaged && IsAlive)
        {
            windowHits++; Sfx.Play("Guardian_Hurt", HeartWorld);
            Current = State.Hurt; since = Time.time;
        }
        return response;
    }

    protected override Vector2 DeathPoint => HeartWorld;

    protected override void Die()
    {
        Current = State.Dead; since = Time.time;
        Sfx.Play("Guardian_Defeat", HeartWorld);
        HitStop.Freeze(HitStop.GuardianKilled);
        CombatCameraShake.Kick(FeedbackTuning.Current.guardianKilledShake);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        diedAt = Time.time;
    }

    // The frames, the facing, and where the heart, the slam and the lower body sit this frame.
    protected override void LateUpdate()
    {
        if (view == null || idle.Length == 0) return;
        float t = Time.time - since, sp = Speed;
        Vector3 offset = Vector3.zero;
        Sprite s = idle[0];
        switch (Current)
        {
            case State.Dormant: case State.Pause:
                s = idle[(int)Mathf.PingPong(Time.time * 7f, 5f)]; break;
            case State.Roar:
            {
                float half = roarTime * .5f, u = t < half ? t / half : 1f - (t - half) / half;   // the claw up and down again
                s = Frame(raise, Mathf.Clamp01(u * 1.3f)); offset.x = Mathf.Sin(Time.time * 60f) * .04f; break;
            }
            case State.SlamRaise: s = Frame(raise, t / (slamRaiseTime / sp)); break;
            case State.SlamHold: s = raise[raise.Length - 1]; offset.x = Mathf.Sin(Time.time * 70f) * .05f; break;
            case State.SlamDrop: s = Frame(raise, 1f - t / slamDropTime); break;
            case State.SlamLinger: s = idle[0]; offset.y = -.06f * (1f - t / slamLingerTime); break;
            case State.ChargeLean: s = idle[0]; offset.x = -facing * .6f * Mathf.SmoothStep(0, 1, t / (chargeLeanTime / sp)) + Mathf.Sin(Time.time * 55f) * .04f; break;
            case State.Charge: s = idle[(int)Mathf.Repeat(Time.time * 14f, 6f)]; offset.y = Mathf.Abs(Mathf.Sin(Time.time * 16f)) * .18f; break;
            case State.Skid: s = idle[0]; offset.x = facing * .3f * (1f - t / (chargeSkidTime / sp)); break;
            case State.Exposed: s = idle[6 + (int)Mathf.PingPong(Time.time * 10f, 5f)]; break;
            case State.Hurt: s = Frame(hurt, t / hurtTime); break;
            case State.Dead: s = Frame(death, t / 2.4f); break;
        }
        view.sprite = s;
        view.flipX = facing < 0f;
        view.transform.localPosition = offset;
        bool flash = IsFlashing && Current != State.Dead;
        view.color = flash ? new Color(1f, .75f, .75f) : Color.white;
        // the parts follow the facing
        float hx = (heartPx.x - pivotPx.x) * Px * facing, hy = (pivotPx.y - heartPx.y) * Px;
        heartPoint.localPosition = new Vector3(hx, hy, 0) + offset;
        slamZone.localPosition = new Vector3(facing * slamReach, .9f, 0);
        lowerBody.localPosition = new Vector3(offset.x, lowerBody.localPosition.y, 0);
    }

    static Sprite Frame(Sprite[] frames, float u) => frames[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * frames.Length), 0, frames.Length - 1)];
}
