using UnityEngine;

// Bark Sentinel (E-08, A2): a heavy guard behind a woven bark shield. It paces its post and turns
// to keep the shield toward Qori. Hits on the shield bounce off, except the spear, which pierces
// the weave; hits from behind or from above (a downward strike) always land. When Qori is close
// in front, it draws the shield back (the cue: a glint) and bashes forward. Touching it hurts.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class SentinelEnemy : EnemyBase
{
    [SerializeField] float patrolSpeed = .7f, patrolHalfWidth = 2f, watchRange = 6f, bashRange = 1.9f;
    [SerializeField] float telegraphTime = .5f, bashTime = .25f, recoverTime = .7f, bashDistance = .9f;

    enum State { Patrol, Guard, Telegraph, Bash, Recover }
    State state; float since, centreX, facing = -1f, direction = -1f, phase, blockedShakeUntil;
    ContactFilter2D ground; readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    public float Facing => facing;

    protected override void Awake()
    {
        base.Awake();
        maximumHealth = Mathf.Max(maximumHealth, 4); health = maximumHealth;
        body.bodyType = RigidbodyType2D.Dynamic; body.gravityScale = 3f; body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        centreX = body.position.x;
        ground = new ContactFilter2D(); ground.SetLayerMask(LayerMask.GetMask("Ground")); ground.useTriggers = false;
    }

    void Enter(State s) { state = s; since = Time.time; }

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        // The attacker is in front when the blow travels back toward the Sentinel's face.
        bool fromFront = hit.Direction.x * facing < 0f || Mathf.Approximately(hit.Direction.x, 0f) && !WeaponKinds.IsDownward(hit);
        if (fromFront && !WeaponKinds.IsDownward(hit) && WeaponKinds.Of(hit) != WeaponKind.Spear)
        {
            blockedShakeUntil = Time.time + .15f;
            return WeaponKinds.Blocked();
        }
        return base.ReceiveCombatHit(hit);
    }

    protected override void OnHurt(CombatDamage hit) { if (state == State.Telegraph) { Enter(State.Recover); HitStop.Freeze(HitStop.Stagger); } }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 pos = body.position;
        bool sees = CanSee(pos + Vector2.up * 1.2f, watchRange, 3f, out Vector2 to);
        float vx = 0f;
        switch (state)
        {
            case State.Patrol:
            {
                if (sees) { Enter(State.Guard); break; }
                Bounds b = GetComponent<Collider2D>().bounds;
                bool atLimit = direction < 0f ? pos.x <= centreX - patrolHalfWidth : pos.x >= centreX + patrolHalfWidth;
                bool wall = body.Cast(Vector2.right * direction, ground, hits, .08f) > 0;
                bool floorAhead = Physics2D.Raycast(new Vector2(b.center.x + direction * (b.extents.x + .1f), b.min.y + .1f), Vector2.down, .4f, LayerMask.GetMask("Ground"));
                if (atLimit || wall || !floorAhead) direction = -direction;
                facing = direction; vx = direction * patrolSpeed;
                break;
            }
            case State.Guard:   // stands its ground, shield toward Qori
                if (!sees) { direction = facing; Enter(State.Patrol); break; }
                facing = Mathf.Sign(to.x);
                if (Mathf.Abs(to.x) <= bashRange && Mathf.Abs(to.y) < 1.5f) { Enter(State.Telegraph); Fx.Glint(pos + new Vector2(facing * .8f, 1.6f)); }
                break;
            case State.Telegraph:
                if (t >= telegraphTime) Enter(State.Bash);
                break;
            case State.Bash:
                vx = facing * bashDistance / bashTime;
                if (t >= bashTime) Enter(State.Recover);
                break;
            case State.Recover:
                if (t >= recoverTime) Enter(State.Guard);
                break;
        }
        body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        float t = Time.time - since;
        float speed = Mathf.Abs(body.linearVelocity.x);
        phase += Time.deltaTime * Mathf.Min(speed, 1.2f) * 5f;
        float stride = state == State.Patrol ? Mathf.Sin(phase) * 14f : 0f;
        rig.Pose("LegNearUpper", stride); rig.Pose("LegFarUpper", -stride);
        rig.Pose("LegNearLower", Mathf.Max(0f, -stride) * .6f); rig.Pose("LegFarLower", Mathf.Max(0f, stride) * .6f);
        // Shield arm: drawn back while telegraphing, thrust out in the bash.
        float draw = state == State.Telegraph ? Mathf.Clamp01(t / telegraphTime) : 0f;
        float thrust = state == State.Bash ? 1f : state == State.Recover ? 1f - Mathf.Clamp01(t / .3f) : 0f;
        rig.Pose("ArmNearUpper", 18f * draw - 10f * thrust, new Vector2(-.12f * draw + .18f * thrust, 0f));
        rig.Pose("ArmFarUpper", Mathf.Sin(phase) * 6f);
        float shake = Time.time < blockedShakeUntil ? Mathf.Sin(Time.time * 90f) * .02f : 0f;
        rig.Pose("Body", -6f * draw + 8f * thrust + Mathf.Sin(Time.time * 1.6f) * 1f, new Vector2(shake, 0f));
        rig.Pose("Head", state == State.Guard ? 4f : 0f);
    }
}
