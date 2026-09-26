using UnityEngine;

// Thornwing (E-03): hovers near its perch, and when Qori comes within reach it rears up (the
// telegraph) and dives at where he was, then flies back. Touching it costs a heart.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ThornwingEnemy : EnemyBase
{
    [SerializeField] float detectRange = 6f, detectHeight = 5f;
    [SerializeField] float telegraphTime = .55f, diveSpeed = 9f, diveTime = 1.1f, returnSpeed = 3.2f, cooldown = 1.2f;

    enum State { Hover, Telegraph, Dive, Return }
    State state; float since, nextDive; Vector2 home, diveDirection, velocity; float facing = -1f, flap;

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<CircleCollider2D>().isTrigger = true;
        home = body.position;
        maximumHealth = Mathf.Max(1, maximumHealth);
    }

    void Enter(State s) { state = s; since = Time.time; }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 pos = body.position, target = pos;
        switch (state)
        {
            case State.Hover:
                target = home + new Vector2(Mathf.Sin(Time.time * .9f) * .6f, Mathf.Sin(Time.time * 1.8f) * .25f);
                if (Time.time >= nextDive && CanSee(pos, detectRange, detectHeight, out Vector2 to) && to.y < .5f)
                { facing = Mathf.Sign(to.x); diveDirection = to.normalized; Enter(State.Telegraph); Fx.Glint(pos + new Vector2(facing * .5f, .2f)); }
                break;
            case State.Telegraph:
                target = pos + new Vector2(-facing * .6f, .9f) * Time.fixedDeltaTime;   // rears up and back
                if (t >= telegraphTime)
                {
                    if (Player != null) diveDirection = ((Vector2)Player.transform.position - pos).normalized;
                    Enter(State.Dive);
                }
                break;
            case State.Dive:
                target = pos + diveDirection * diveSpeed * Time.fixedDeltaTime;
                if (t >= diveTime || Physics2D.Raycast(pos, diveDirection, .5f, LayerMask.GetMask("Ground")))
                    Enter(State.Return);
                break;
            case State.Return:
                target = Vector2.MoveTowards(pos, home, returnSpeed * Time.fixedDeltaTime);
                if ((target - home).sqrMagnitude < .01f) { Enter(State.Hover); nextDive = Time.time + cooldown; }
                break;
        }
        velocity = (target - pos) / Time.fixedDeltaTime;
        if (state != State.Telegraph && Mathf.Abs(velocity.x) > .3f) facing = Mathf.Sign(velocity.x);
        body.MovePosition(target);
    }

    protected override void OnHurt(CombatDamage hit) { if (state == State.Telegraph) Enter(State.Return); }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        bool diving = state == State.Dive, rearing = state == State.Telegraph;
        flap += Time.deltaTime * (diving ? 3f : rearing ? 14f : 9f);
        float wing = diving ? 18f : Mathf.Sin(flap) * 28f;
        rig.Pose("WingFront", wing - (diving ? 30f : 0f));
        rig.Pose("WingBack", -wing * .8f + (diving ? 20f : 0f));
        rig.Pose("Body", diving ? -Mathf.Atan2(-diveDirection.y, Mathf.Abs(diveDirection.x)) * Mathf.Rad2Deg : rearing ? 16f : Mathf.Sin(flap * .5f) * 3f);
        rig.Pose("Tail", Mathf.Sin(flap * .5f + 1f) * 8f);
        rig.Pose("Head", rearing ? 14f : 0f);
    }
}
