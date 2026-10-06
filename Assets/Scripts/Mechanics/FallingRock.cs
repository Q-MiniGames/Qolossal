using UnityEngine;

// A loose rock in a ceiling. When Qori passes beneath, dust trickles and the rock shudders (the
// cue), then it drops; it costs a heart if it lands on him, and shatters in a puff of dust on the
// ground. A new rock settles back into the ceiling a few seconds later.
[DisallowMultipleComponent]
public sealed class FallingRock : MonoBehaviour, IContactHazard
{
    public SpriteRenderer image;
    public Sprite dust;
    [Tooltip("How far to each side of the rock Qori sets it off.")] public float triggerHalfWidth = 1.1f;
    [Tooltip("How far below the ceiling the trigger reaches.")] public float triggerDepth = 12f;
    public float warnTime = .7f, respawnTime = 4f, gravity = 30f;

    enum State { Resting, Warning, Falling, Gone }
    State state; float since, speed, nextDust;
    Vector3 rest; Collider2D hitbox; PlayerMovement player;

    public bool HurtsOnContact => state == State.Falling;
    public bool IsResting => state == State.Resting;

    void Awake()
    {
        rest = transform.position;
        hitbox = GetComponent<Collider2D>(); if (hitbox != null) hitbox.isTrigger = true;
    }

    void Enter(State s) { state = s; since = Time.time; }

    void Update()
    {
        float t = Time.time - since;
        switch (state)
        {
            case State.Resting:
                if (player == null) player = FindFirstObjectByType<PlayerMovement>();
                if (player != null && player.isActiveAndEnabled)
                {
                    Vector2 d = player.transform.position - rest;
                    if (Mathf.Abs(d.x) <= triggerHalfWidth && d.y < 0f && d.y > -triggerDepth) Enter(State.Warning);
                }
                break;
            case State.Warning:
                transform.position = rest + (Vector3)(Random.insideUnitCircle * .03f);
                if (Time.time >= nextDust && dust != null)
                {
                    nextDust = Time.time + .12f;
                    var fb = Fx.Pop(dust, (Vector2)rest + new Vector2(Random.Range(-.3f, .3f), -.35f), .5f, .6f, 31);
                    if (fb != null) { fb.velocity = new Vector2(0f, -1.5f); fb.gravity = 4f; }
                }
                if (t >= warnTime) { transform.position = rest; speed = 0f; Enter(State.Falling); }
                break;
            case State.Falling:
            {
                speed += gravity * Time.deltaTime;
                float step = speed * Time.deltaTime;
                float half = image != null ? image.bounds.extents.y * .8f : .4f;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, step + half, LayerMask.GetMask("Ground"));
                if (hit) { Shatter(hit.point); break; }
                transform.position += Vector3.down * step;
                break;
            }
            case State.Gone:
                if (t >= respawnTime)
                {
                    transform.position = rest; Enter(State.Resting);
                    image.enabled = true; if (hitbox != null) hitbox.enabled = true;
                }
                break;
        }
        if (state == State.Resting && image != null)   // fades back in after respawning
        {
            Color c = image.color; c.a = Mathf.Min(1f, t / .5f); image.color = c;
        }
    }

    void Shatter(Vector2 at)
    {
        Sfx.Play("Rock_Fall", at);
        var lib = Fx.Library;
        if (lib != null) { Fx.Play(lib.dustLand, at + Vector2.up * .15f, 14f, 1.6f, 31); Fx.Leaves(at, 2, .6f); }
        image.enabled = false; if (hitbox != null) hitbox.enabled = false;
        Enter(State.Gone);
    }
}
