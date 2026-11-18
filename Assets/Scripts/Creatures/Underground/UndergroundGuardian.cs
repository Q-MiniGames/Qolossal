using UnityEngine;

// An underground route's guardian (G-03 Sluice Warden, G-04 Root Matron, G-05 Tempest Owl), drawn from its Ludo.ai
// frames (UndergroundCreatureLibrary), on the Thornheart's pattern: it wakes when Qori enters its arena, roars, then
// attacks, facing him before each attack: a warning, the strike, a recovery. After an attack its weak point opens:
// only hits there hurt it, up to `hitsPerWindow` a window; everything else glances off. Three phases with a roar
// between; the last runs `phaseThreeSpeed` faster. Subclasses pick the attacks and what each strike does.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class UndergroundGuardian : EnemyBase
{
    public enum State { Dormant, Roar, Pause, Warn, Strike, Recover, Open, Exposed, Close, Hurt, Dead }

    [Header("Fight")]
    [SerializeField] protected float roarTime = 1.4f, pauseTime = .9f, warnTime = .8f, strikeTime = .4f, recoverTime = .5f;
    [SerializeField] protected float openTime = .35f, exposedTime = 2.2f, closeTime = .35f, hurtTime = .3f, deathTime = 2.4f;
    [SerializeField, Min(1)] protected int hitsPerWindow = 2;
    [SerializeField, Min(1f)] protected float phaseThreeSpeed = 1.25f;

    public State Current { get; private set; }
    public int Phase => health > maximumHealth * 2f / 3f ? 1 : health > maximumHealth / 3f ? 2 : 3;
    public bool WeakOpen => IsAlive && (Current == State.Exposed || Current == State.Hurt);
    public float Health => health;
    // Its name off the scene object: "UGP guardian G-03 Sluice Warden", "UGP enemy M-01 Moss Matriarch (mini-boss)".
    public override string BossBarName
    {
        get
        {
            string n = name.Replace(" (mini-boss)", ""); int at = n.IndexOf(DesignId);
            return at >= 0 ? n.Substring(at + DesignId.Length).Trim() : n;
        }
    }
    public override bool BossBarVisible => IsAlive && !Asleep;
    public float MaximumHealthForTest => maximumHealth;
    public int Attack { get; private set; }
    public bool Asleep => Current == State.Dormant;

    protected abstract string DesignId { get; }
    protected virtual int Hits => 20;
    // The weak point and the body, in world units from the feet, for a creature facing right.
    protected abstract Vector2 WeakOffset { get; }
    protected virtual float WeakRadius => 1.2f;
    protected abstract Vector2 BodySize { get; }
    // Which attack comes next (attackCount counts them), its strike, and where its strike hurts.
    protected abstract int NextAttack(int attackCount);
    protected virtual void OnStrike(int attack) { }
    protected virtual void OnStrikeTick(int attack, float t) { }
    protected virtual void OnRecoverTick(int attack, float t) { }
    protected virtual bool StrikeZone(int attack, out Vector2 centre, out Vector2 size) { centre = size = Vector2.zero; return false; }
    protected virtual string WarnSegment(int attack) => "Warn";
    protected virtual string StrikeSegment(int attack) => "Attack";
    protected virtual string RecoverSegment(int attack) => "Recover";
    protected virtual void OnPhaseChange(int phase) { }
    protected virtual void OnFightReset() { }

    protected Bounds Room;
    protected float facing = -1f, since;
    protected float Speed => Phase >= 3 ? phaseThreeSpeed : 1f;
    protected Vector2 Feet => transform.position;

    UndergroundCreatureLibrary.Entry entry;
    SpriteRenderer view;
    Transform weak, zone;
    BoxCollider2D zoneBox;
    AttackZoneTelegraph telegraph;
    int attacks, phaseSeen = 1, windowHits;
    bool hasRoared;
    Vector2 startedAt;
    int respawnSeen = -1;

    protected override void Awake()
    {
        maximumHealth = Hits;
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true; body.gravityScale = 0f;
        entry = UndergroundCreatureLibrary.Find(DesignId);
        var art = new GameObject(DesignId + " frames"); art.transform.SetParent(transform, false);
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 6;
        if (entry != null) art.transform.localScale = Vector3.one * entry.Scale;
        hitFeel.SetVisual(art.transform);
        var box = gameObject.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = BodySize; box.offset = new Vector2(0f, BodySize.y * .5f);
        weak = Part("Weak point (where hits count)", UndergroundGuardianPart.Kind.Weak);
        var wc = weak.gameObject.AddComponent<CircleCollider2D>(); wc.isTrigger = true; wc.radius = WeakRadius;
        zone = Part("Strike (hurts as it lands)", UndergroundGuardianPart.Kind.Strike);
        zoneBox = zone.gameObject.AddComponent<BoxCollider2D>(); zoneBox.isTrigger = true; zoneBox.enabled = false;
        var room = GetComponentInParent<UndergroundRoom>();
        Room = room != null ? room.WorldBounds : new Bounds(transform.position, Vector3.one * 60f);
        startedAt = transform.position;
        Enter(State.Dormant);
    }

    Transform Part(string name, UndergroundGuardianPart.Kind kind)
    {
        var obj = new GameObject(name); obj.transform.SetParent(transform, false);
        obj.AddComponent<UndergroundGuardianPart>().Set(this, kind);
        return obj.transform;
    }

    public override bool HurtsOnContact => IsAlive && Current == State.Strike;   // its strikes, and its body as it strikes: nowhere is safe
    public bool StrikeLive => IsAlive && Current == State.Strike;

    protected void Enter(State s)
    {
        Current = s; since = Time.time;
        switch (s)
        {
            case State.Roar: Sfx.Play("Guardian_Awaken", Feet); CombatCameraShake.Kick(.12f); break;
            case State.Warn: Fx.Glint(Feet + Vector2.up * BodySize.y * .8f); Sfx.Play("Guardian_ArmRaise", Feet); break;
            case State.Strike: Sfx.Play("Guardian_ArmSlam", Feet); CombatCameraShake.Kick(.14f); OnStrike(Attack); break;
            case State.Exposed: windowHits = 0; break;
        }
    }

    bool QoriInRoom(PlayerMovement p) { var r = Room; r.Expand(new Vector3(0f, 0f, 100f)); return p != null && r.Contains(p.transform.position); }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since, sp = Speed;
        var p = Player;
        // Qori left or fell: the fight starts over.
        bool respawned = p != null && respawnSeen >= 0 && p.RespawnVersion != respawnSeen;
        if (p != null) respawnSeen = p.RespawnVersion;
        if (Current != State.Dormant && (respawned || !QoriInRoom(p))) { ResetFight(); return; }
        switch (Current)
        {
            case State.Dormant: if (QoriInRoom(p)) { Face(); if (hasRoared) Enter(State.Pause); else { hasRoared = true; Enter(State.Roar); } } break;
            case State.Roar: if (t >= roarTime) Enter(State.Pause); break;
            case State.Pause:
                if (t >= pauseTime / sp) { Face(); Attack = NextAttack(attacks++); Enter(State.Warn); }
                break;
            case State.Warn: if (t >= warnTime / sp) Enter(State.Strike); break;
            case State.Strike: OnStrikeTick(Attack, t); if (t >= strikeTime / sp) Enter(State.Recover); break;
            case State.Recover: OnRecoverTick(Attack, t); if (t >= recoverTime / sp) Enter(State.Open); break;
            case State.Open: if (t >= openTime) Enter(State.Exposed); break;
            case State.Exposed: if (t >= exposedTime / sp || windowHits >= hitsPerWindow) Enter(State.Close); break;
            case State.Hurt: if (t >= hurtTime) { if (windowHits >= hitsPerWindow) Enter(State.Close); else { Current = State.Exposed; since = Time.time - hurtTime; } } break;
            case State.Close:
                if (t >= closeTime)
                {
                    if (Phase != phaseSeen) { phaseSeen = Phase; OnPhaseChange(Phase); Face(); HitStop.Freeze(FeedbackTuning.Current.bossPhaseFreeze); Enter(State.Roar); }
                    else Enter(State.Pause);
                }
                break;
        }
    }

    void ResetFight()
    {
        health = maximumHealth; attacks = 0; phaseSeen = 1; windowHits = 0;
        transform.position = startedAt; body.position = startedAt;
        OnFightReset();
        Enter(State.Dormant);
    }

    protected void Face()
    {
        var p = Player;
        if (p != null && Mathf.Abs(p.transform.position.x - Feet.x) > .5f) facing = Mathf.Sign(p.transform.position.x - Feet.x);
    }

    // Hits on the body glance off (silently while the weak point is open, so its hit is the one that counts).
    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive || WeakOpen) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        return WeaponKinds.Blocked();
    }

    public CombatDamageResponse HitWeak(CombatDamage hit)
    {
        if (!WeakOpen) return ReceiveCombatHit(hit);
        var response = base.ReceiveCombatHit(hit);
        if (response.Disposition == CombatHitDisposition.Damaged && IsAlive)
        {
            windowHits++; Sfx.Play("Guardian_Hurt", weak.position);
            var feel = FeedbackTuning.Current;
            HitStop.Freeze(feel.weakPointFreeze); CombatCameraShake.Kick(feel.weakPointShake);
            Current = State.Hurt; since = Time.time;
        }
        return response;
    }

    protected override Vector2 DeathPoint => weak != null ? (Vector2)weak.position : Feet;

    protected override void Die()
    {
        Current = State.Dead; since = Time.time; diedAt = Time.time;
        Sfx.Play("Guardian_Defeat", DeathPoint);
        HitStop.Freeze(HitStop.GuardianKilled);
        CombatCameraShake.Kick(FeedbackTuning.Current.guardianKilledShake);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        OnDefeated();
    }
    protected virtual void OnDefeated() { }

    // A mini-boss has fewer clips: its Walk stands in for Idle and Roar, its Vulnerable for the window's three parts.
    Sprite[] Seg(string name)
    {
        if (entry == null) return null;
        string[] order = name == "Open" || name == "Exposed" || name == "Close" ? new[] { name, "Vulnerable", "Idle", "Walk" } : new[] { name, "Idle", "Walk" };
        foreach (var n in order) { var f = entry.Get(n); if (f != null && f.Length > 0) return f; }
        return null;
    }
    static Sprite At(Sprite[] f, float u) => f == null || f.Length == 0 ? null : f[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * f.Length), 0, f.Length - 1)];
    static Sprite Loop(Sprite[] f, float t, float fps) => f == null || f.Length == 0 ? null : f[(int)Mathf.Repeat(t * fps, f.Length)];

    // The frames, the facing, and where the weak point and the strike sit this frame. (The dead body stays until its
    // collapse has played, then fades, instead of EnemyBase's quick vanish.)
    protected override void LateUpdate()
    {
        if (view == null || entry == null) return;
        float t = Time.time - since, sp = Speed;
        Sprite s = null;
        switch (Current)
        {
            case State.Dormant: case State.Pause: s = Loop(Seg("Idle"), Time.time, 8f); break;
            case State.Roar: s = At(Seg("Roar"), t / roarTime); break;
            case State.Warn: s = At(Seg(WarnSegment(Attack)), t / (warnTime / sp)); break;
            case State.Strike: s = At(Seg(StrikeSegment(Attack)), t / (strikeTime / sp)); break;
            case State.Recover: s = At(Seg(RecoverSegment(Attack)), t / (recoverTime / sp)); break;
            case State.Open: s = At(Seg("Open"), t / openTime); break;
            case State.Exposed: s = Loop(Seg("Exposed"), Time.time, 10f); break;
            case State.Close: s = At(Seg("Close"), t / closeTime); break;
            case State.Hurt: s = At(Seg("Hurt"), t / hurtTime); break;
            case State.Dead:
                s = At(Seg("Death"), t / deathTime);
                float fade = Mathf.Clamp01((t - deathTime - .6f) / .6f);
                view.color = new Color(1f, 1f, 1f, 1f - fade);
                if (fade >= 1f) gameObject.SetActive(false);
                break;
        }
        if (s != null) view.sprite = s;
        view.flipX = facing < 0f;
        if (Current != State.Dead) view.color = WeakOpen ? new Color(1f, 1f, .92f) : Color.white;
        var wo = WeakOffset;
        weak.localPosition = new Vector3(wo.x * facing, wo.y, 0f);
        bool live = StrikeLive && StrikeZone(Attack, out var c, out var z);
        if (live) { StrikeZone(Attack, out c, out z); zone.position = c; zoneBox.size = z; }
        zoneBox.enabled = live;
        if (telegraph == null) telegraph = AttackZoneTelegraph.For(this, true);
        if (Current == State.Warn && StrikeZone(Attack, out var warnAt, out var warnSize)) telegraph.Warn(warnAt, warnSize, (Time.time - since) / (warnTime / Speed));
        else if (live) telegraph.Strike(zone.position, zoneBox.size);
        else telegraph.Hide();
    }
}
