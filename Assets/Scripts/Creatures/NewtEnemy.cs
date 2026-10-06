using UnityEngine;

// Ripple Newt (E-07, A1): hides under a water channel, showing only a ripple. When Qori comes
// near it surfaces (splash and glint: the cue), leaps out in an arc to land beside him, stays a
// moment, then leaps back into the water. Out of the water it hurts on contact and can be hit;
// under it, it can't be touched.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class NewtEnemy : EnemyBase
{
    [SerializeField] float detectRange = 5.5f, detectHeight = 3.5f, leapReach = 4f, leapHeight = 2.2f, leapTime = .85f;
    [SerializeField] float telegraphTime = .5f, landedTime = 1.3f, cooldown = 1.6f;
    [SerializeField] Sprite splash, ripple;

    enum State { Submerged, Telegraph, Leap, Landed, Return }
    State state; float since, nextRipple, facing = -1f;
    Vector2 home, from, to;
    Collider2D hitbox;

    public bool IsSubmerged => state == State.Submerged;
    public override bool HurtsOnContact => IsAlive && state != State.Submerged && state != State.Telegraph;

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        hitbox = GetComponent<Collider2D>(); hitbox.isTrigger = true;
        home = body.position;
        Enter(State.Submerged);
    }

    SfxEmitter ripples;
    void Enter(State s)
    {
        if (ripples == null) ripples = SfxEmitter.Attach(gameObject, "Newt_Ripple", false);
        ripples.Playing = s == State.Submerged || s == State.Telegraph;
        if (s == State.Leap) Sfx.Play("Newt_Leap", body.position);
        else if (s == State.Submerged && state == State.Return) Sfx.Play("Newt_Leap", body.position, .5f);   // back into the water
        state = s; since = Time.time;
        bool hidden = s == State.Submerged;
        hitbox.enabled = !hidden;
        if (rig != null) foreach (var r in rig.renderers) if (r != null) r.enabled = !hidden;
    }

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit) =>
        state == State.Submerged ? new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored } : base.ReceiveCombatHit(hit);

    void Splash(Vector2 at, float size) { if (splash != null) Fx.Pop(splash, at + new Vector2(0f, .25f), size, .45f, 30); }

    // Where a leap toward Qori lands: up to leapReach away, on the first ground below.
    Vector2 LandingNear(Vector2 target)
    {
        float x = home.x + Mathf.Clamp(target.x - home.x, -leapReach, leapReach);
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, home.y + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
        return hit ? hit.point : new Vector2(x, home.y);
    }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 pos = body.position, next = pos;
        switch (state)
        {
            case State.Submerged:
                next = home;
                if (Time.time >= nextRipple && ripple != null) { nextRipple = Time.time + 1.1f; Fx.Pop(ripple, home + new Vector2(0f, .05f), .8f, .6f, 26); }
                if (Time.time - since >= cooldown && CanSee(home + Vector2.up * .6f, detectRange, detectHeight, out Vector2 seen))
                {
                    facing = Mathf.Sign(seen.x); Enter(State.Telegraph);
                    Splash(home, .9f); Fx.Glint(home + new Vector2(facing * .5f, .5f));
                }
                break;
            case State.Telegraph:
                next = home + Vector2.up * Mathf.Min(1f, t / telegraphTime) * .12f;   // eyes above the surface
                if (t >= telegraphTime)
                {
                    from = home; to = Player != null ? LandingNear(Player.transform.position) : home;
                    facing = to.x >= home.x ? 1f : -1f;
                    Splash(home, 1.2f); Enter(State.Leap);
                }
                break;
            case State.Leap:
            case State.Return:
            {
                float k = Mathf.Clamp01(t / leapTime);
                next = Vector2.Lerp(from, to, k) + Vector2.up * (4f * leapHeight * k * (1f - k));
                if (k >= 1f)
                {
                    if (state == State.Leap) Enter(State.Landed);
                    else { Splash(home, 1.2f); Enter(State.Submerged); }
                }
                break;
            }
            case State.Landed:
                if (t >= landedTime) { from = pos; to = home; facing = home.x >= pos.x ? 1f : -1f; Enter(State.Return); }
                break;
        }
        body.MovePosition(next);
    }

    protected override void OnHurt(CombatDamage hit)
    {
        // A hit on land sends it straight back to the water.
        if (state == State.Landed && IsAlive) { from = body.position; to = home; facing = home.x >= from.x ? 1f : -1f; Enter(State.Return); }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        float t = Time.time;
        bool airborne = state == State.Leap || state == State.Return;
        float pitch = 0f;
        if (airborne)
        {
            float k = Mathf.Clamp01((t - since) / leapTime);
            pitch = Mathf.Lerp(28f, -32f, k);   // nose up on the way up, down on the way in
        }
        rig.Pose("Body", pitch);
        rig.Pose("Head", state == State.Telegraph ? 12f : Mathf.Sin(t * 2f) * 3f);
        float sway = Mathf.Sin(t * (airborne ? 12f : 4f)) * (airborne ? 16f : 7f);
        rig.Pose("Tail1", sway); rig.Pose("Tail2", sway * 1.4f);
        float legs = airborne ? -35f : 0f;
        for (int i = 0; i < 2; i++)
        {
            rig.Pose("LegNear" + i, legs + (i == 0 ? 1 : -1) * (airborne ? 25f : 0f));
            rig.Pose("LegFar" + i, legs - (i == 0 ? 1 : -1) * (airborne ? 25f : 0f));
        }
    }
}
