using UnityEngine;

// Pod Spitter (E-04): a rooted turret. It turns to face Qori, swells its pod, opens and spits a
// hard seed at him, then rests before the next shot.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class SpitterEnemy : EnemyBase
{
    [SerializeField] float range = 7f, height = 3f, swellTime = .6f, cooldown = 2.2f, seedSpeed = 7.5f;
    [SerializeField] Sprite headClosed, headOpen, seed;

    float facing = -1f, swellStart = -1f, openUntil, nextShot;
    SpriteRenderer head;

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Kinematic;
        if (rig != null) head = rig.Bone("Head").GetComponentInChildren<SpriteRenderer>();
    }

    public override bool HurtsOnContact => false;   // only its seeds hurt

    void Update()
    {
        if (!IsAlive) return;
        Vector2 at = (Vector2)transform.position + Vector2.up * .9f;
        bool sees = CanSee(at, range, height, out Vector2 to);
        if (sees) facing = Mathf.Sign(to.x);
        if (swellStart < 0f && sees && Time.time >= nextShot)
        {
            swellStart = Time.time;
            Sfx.Play("Spitter_Swell", at);
            if (rig != null && rig.Point("Mouth") != null) Fx.Glint(rig.Point("Mouth").position);
        }
        if (swellStart >= 0f && Time.time - swellStart >= swellTime)
        {
            Spit();
            swellStart = -1f; openUntil = Time.time + .35f; nextShot = Time.time + cooldown;
        }
    }

    void Spit()
    {
        Transform mouth = rig != null ? rig.Point("Mouth") : transform;
        Vector2 from = mouth.position;
        Vector2 aim = Player != null ? ((Vector2)Player.transform.position + Vector2.up * .3f - from).normalized : new Vector2(facing, 0f);
        // Keep shots mostly forward: the flower can't spit behind or straight up.
        aim = new Vector2(Mathf.Sign(facing) * Mathf.Max(.55f, Mathf.Abs(aim.x)), Mathf.Clamp(aim.y, -.6f, .6f)).normalized;
        SpitterSeed.Launch(seed, from, aim * seedSpeed);
        Sfx.Play("Spitter_Spit", from);
    }

    protected override void OnHurt(CombatDamage hit) => swellStart = -1f;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(facing);
        float swell = swellStart >= 0f ? Mathf.Clamp01((Time.time - swellStart) / swellTime) : 0f;
        float sway = Mathf.Sin(Time.time * 1.3f) * 3f;
        rig.Pose("Stalk", sway - 6f * swell);
        rig.Pose("Head", -sway * .5f, Vector2.zero, Vector2.one * (1f + .14f * swell * swell));
        if (head != null) head.sprite = Time.time < openUntil || swell > .85f ? headOpen : headClosed;
    }
}
