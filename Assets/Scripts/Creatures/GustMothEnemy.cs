using UnityEngine;

// Gust Moth (E-09, A3): hovers lazily near its perch. When Qori is in front of it, it raises
// its wings (the cue: a glint), then beats them in a wind blast that pushes him away. The blast
// never hurts, and neither does touching it; it's an obstacle, not a threat.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class GustMothEnemy : EnemyBase
{
    [SerializeField] float detectRange = 6f, detectHeight = 3f, gustReach = 5f, gustHeight = 1.8f;
    [SerializeField] Vector2 push = new Vector2(8f, 3f);
    [SerializeField] float telegraphTime = .6f, beatTime = .35f, cooldown = 2.2f;
    [SerializeField] Sprite gust;

    enum State { Hover, Telegraph, Beat }
    State state; float since, nextGust, facing = -1f, flap;
    Vector2 home;

    public override bool HurtsOnContact => false;

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<CircleCollider2D>().isTrigger = true;
        home = body.position;
    }

    void Enter(State s) { state = s; since = Time.time; }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        float t = Time.time - since;
        Vector2 pos = body.position;
        Vector2 hover = home + new Vector2(Mathf.Sin(Time.time * .7f) * .5f, Mathf.Sin(Time.time * 1.4f) * .3f);
        switch (state)
        {
            case State.Hover:
                if (Time.time >= nextGust && CanSee(pos, detectRange, detectHeight, out Vector2 to))
                { facing = Mathf.Sign(to.x); Enter(State.Telegraph); Fx.Glint(pos + new Vector2(facing * .4f, .4f)); }
                break;
            case State.Telegraph:
                hover = pos;   // holds still, wings up
                if (t >= telegraphTime) { Blast(pos); Enter(State.Beat); }
                break;
            case State.Beat:
                hover = pos - new Vector2(facing * .6f, 0f) * Time.fixedDeltaTime;   // recoils slightly
                if (t >= beatTime) { Enter(State.Hover); nextGust = Time.time + cooldown; }
                break;
        }
        body.MovePosition(state == State.Hover ? Vector2.MoveTowards(pos, hover, 2f * Time.fixedDeltaTime) : hover);
    }

    // The wind blast: painted streaks rolling forward, and a push for Qori if he's in its path.
    void Blast(Vector2 pos)
    {
        if (gust != null)
            for (int i = 0; i < 3; i++)
            {
                var fb = Fx.Play(new[] { gust }, pos + new Vector2(facing * (.6f + i * .3f), (i - 1) * .35f), .9f, 1.3f, 32, facing < 0f);
                if (fb != null) { fb.velocity = new Vector2(facing * (7f + i), 0f); fb.fadeFrom = .2f; fb.popLife = 0f; }
            }
        if (Player == null) return;
        Vector2 to = (Vector2)Player.transform.position - pos;
        if (Mathf.Sign(to.x) == facing && Mathf.Abs(to.x) <= gustReach && Mathf.Abs(to.y) <= gustHeight)
            Player.ApplyKnockback(new Vector2(facing * push.x, push.y));
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        if (state == State.Hover && Player != null) facing = Player.transform.position.x >= transform.position.x ? 1f : -1f;
        rig.Face(facing);
        float t = Time.time - since, wing;
        if (state == State.Telegraph) wing = Mathf.Lerp(0f, 40f, t / telegraphTime);                       // raised high
        else if (state == State.Beat) wing = Mathf.Lerp(40f, -45f, Mathf.Clamp01(t / (beatTime * .5f)));   // one hard beat
        else { flap += Time.deltaTime * 5f; wing = Mathf.Sin(flap) * 22f; }
        rig.Pose("WingFront", wing); rig.Pose("WingBack", wing * .85f - 6f);
        rig.Pose("Body", state == State.Beat ? -8f : Mathf.Sin(flap * .5f) * 3f);
    }
}
