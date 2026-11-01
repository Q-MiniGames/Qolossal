using UnityEngine;

// A regular underground creature, drawn from its Ludo.ai frames (UndergroundCreatureLibrary). Subclasses write the
// behaviour; this holds the frames, the facing, a trigger body and a strike zone, and the hurt flash and collapse.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class UndergroundEnemy : EnemyBase
{
    public enum Phase { Idle, Walk, Warn, Attack, Recover }

    protected abstract string DesignId { get; }
    protected abstract Vector2 BodySize { get; }
    protected virtual int Hits => 3;
    protected virtual bool Flyer => false;
    // Where the strike hurts now, if it does.
    protected virtual bool StrikeZone(out Vector2 centre, out Vector2 size) { centre = size = Vector2.zero; return false; }

    protected Phase phase = Phase.Idle;
    protected float since, facing = -1f;
    protected float T => Time.time - since;
    protected Vector2 Home;
    protected Bounds Room;
    protected Vector2 Pos => transform.position;
    protected string segment = "Idle";
    protected float segmentU = -1f;

    UndergroundCreatureLibrary.Entry entry;
    SpriteRenderer view;
    Transform zone;
    BoxCollider2D zoneBox;
    float hurtAt = -9f, shove, safeUntil, pushAt = -9f, pushDir;
    // Stays put when hit (only its art rocks back).
    protected virtual bool Rooted => false;

    public bool StrikeLive { get; private set; }
    public string State => phase.ToString();
    // Its opening is safe to step into, with a moment after it to step back out. A perched creature can't be touched from under its deck.
    public override bool HurtsOnContact => IsAlive && phase != Phase.Recover && Time.time >= safeUntil && (Flyer || Player == null || Player.transform.position.y >= Pos.y);

    protected override void Awake()
    {
        maximumHealth = Hits;
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true; body.gravityScale = 0f;
        Home = transform.position;
        entry = UndergroundCreatureLibrary.Find(DesignId);
        var art = new GameObject(DesignId + " frames"); art.transform.SetParent(transform, false);
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 8;
        if (entry != null) art.transform.localScale = Vector3.one * entry.Scale;
        hitFeel.SetVisual(art.transform);
        var box = gameObject.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = BodySize;
        box.offset = new Vector2(0f, Flyer ? 0f : BodySize.y * .5f);
        var z = new GameObject("Strike"); z.transform.SetParent(transform, false);
        z.AddComponent<UndergroundEnemyStrike>().Set(this);
        zone = z.transform; zoneBox = z.AddComponent<BoxCollider2D>(); zoneBox.isTrigger = true; zoneBox.enabled = false;
        var room = GetComponentInParent<UndergroundRoom>();
        Room = room != null ? room.WorldBounds : new Bounds(transform.position, Vector3.one * 40f);
    }

    protected void Enter(Phase p)
    {
        if (phase == Phase.Recover) safeUntil = Time.time + .45f;
        phase = p; since = Time.time;
        if (p == Phase.Warn) Fx.Glint(Pos + Vector2.up * (Flyer ? .4f : BodySize.y));
    }

    protected void Face() { var p = Player; if (p != null && Mathf.Abs(p.transform.position.x - Pos.x) > .2f) facing = Mathf.Sign(p.transform.position.x - Pos.x); }
    protected bool InRoom(PlayerMovement p) { var r = Room; r.Expand(new Vector3(0f, 0f, 100f)); return r.Contains(p.transform.position); }
    protected bool Near(float range, float height) { var p = Player; return p != null && InRoom(p) && Mathf.Abs(p.transform.position.x - Pos.x) < range && Mathf.Abs(p.transform.position.y - Pos.y) < height && Sees(p); }
    // Rock blocks its view; one-way decks don't.
    static readonly RaycastHit2D[] sight = new RaycastHit2D[8];
    bool Sees(PlayerMovement p)
    {
        Vector2 eye = Pos + Vector2.up * (Flyer ? 0f : BodySize.y * .5f), to = (Vector2)p.transform.position - eye;
        var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(GroundLayer.Mask);
        int n = Physics2D.Raycast(eye, to.normalized, filter, sight, to.magnitude);
        for (int i = 0; i < n; i++) if (!sight[i].collider.TryGetComponent(out PlatformEffector2D e) || !e.useOneWay) return false;
        return true;
    }
    // Blocked hits (a shell, a guard) glance off.
    protected virtual bool Blocks(CombatDamage hit) => false;

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (IsAlive && Blocks(hit)) return WeaponKinds.Blocked();
        var response = base.ReceiveCombatHit(hit);
        if (response.Disposition == CombatHitDisposition.Damaged && IsAlive) { hurtAt = Time.time; shove = hit.Direction.x; pushAt = Time.time; pushDir = hit.Direction.x; }
        return response;
    }

    protected override void Die()
    {
        base.Die();
        var frames = entry != null ? entry.Get("Death") : null;
        if (view != null && frames != null && frames.Length > 0) { view.transform.SetParent(null, true); CrawlerCorpse.Leave(view, frames, 1.4f, facing, Flyer); view = null; }
    }

    // A walker or rooted creature stands on the ground below where it was placed.
    void Start()
    {
        if (Flyer) return;
        var hit = Physics2D.Raycast(Pos + Vector2.up * .5f, Vector2.down, 8f, GroundLayer.Mask);
        if (hit) { transform.position = hit.point; Home = hit.point; body.position = hit.point; }
    }

    Sprite[] Seg(string n)
    {
        if (entry == null) return null;
        foreach (var k in new[] { n, "Idle", "Walk", "Dormant" }) { var f = entry.Get(k); if (f != null && f.Length > 0) return f; }
        return null;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (view == null || entry == null) return;
        Pushed();
        var f = Seg(segment);
        var hurt = entry.Get("Hurt");
        Sprite s = null;
        if (Time.time - hurtAt < .25f && hurt != null && hurt.Length > 0) s = hurt[Mathf.Clamp((int)((Time.time - hurtAt) / .25f * hurt.Length), 0, hurt.Length - 1)];
        else if (f != null) s = segmentU >= 0f ? f[Mathf.Clamp((int)(segmentU * f.Length), 0, f.Length - 1)] : f[(int)Mathf.Repeat(Time.time * 12f, f.Length)];
        if (s != null) view.sprite = s;
        view.flipX = facing < 0f;
        // The blow rocks its art back.
        view.transform.localPosition = Vector3.right * (shove * .3f * Mathf.Clamp01(1f - (Time.time - hurtAt) / .18f));
        view.color = Color.white;
        bool live = IsAlive && StrikeZone(out _, out _);
        StrikeLive = live;
        if (live) { StrikeZone(out var c, out var z); zone.position = c; zoneBox.size = z; }
        zoneBox.enabled = live;
    }

    // The blow shoves it back a little, stopping at walls and ledges. A hovering creature's anchor moves with it.
    void Pushed()
    {
        var t = FeedbackTuning.Current;
        float u = (Time.time - pushAt) / t.hitPushSeconds;
        if (Rooted || u >= 1f || pushDir == 0f || !IsAlive) return;
        float step = pushDir * t.hitPushSpeed * (1f - u) * Time.deltaTime;
        if (WallAhead(pushDir, Mathf.Abs(step) + BodySize.x * .5f) || !Flyer && EdgeAhead(pushDir, BodySize.x * .5f)) return;
        Vector2 next = Pos + new Vector2(step, 0f);
        transform.position = next; body.position = next; Home += new Vector2(step, 0f);
    }

    // Plays a segment across `seconds` (or loops it, with no seconds).
    protected void Show(string name, float seconds = 0f) { segment = name; segmentU = seconds > 0f ? Mathf.Clamp01(T / seconds) : -1f; }
    protected void MoveTo(Vector2 p) => body.MovePosition(p);
    protected bool WallAhead(float dir, float reach) => Physics2D.Raycast(Pos + Vector2.up * .5f, new Vector2(dir, 0f), reach, GroundLayer.Mask);
    protected bool EdgeAhead(float dir, float reach) => !Physics2D.Raycast(Pos + new Vector2(dir * reach, .3f), Vector2.down, 1f, GroundLayer.Mask);
}
