using UnityEngine;

// Shellback (E-05): a slow beetle. Its stone shell blocks the sword and spear; only the
// mace cracks it (shards fly, the mint glow shows through), and after that it can be hurt.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class ShellbackEnemy : EnemyBase
{
    [SerializeField] float patrolSpeed = .8f, patrolHalfWidth = 2.5f;
    [SerializeField] Sprite shellCracked;
    [SerializeField] Sprite[] shards;

    bool shellIntact = true; float centreX, direction = -1f, phase, blockedShakeUntil;
    ContactFilter2D ground;
    readonly RaycastHit2D[] hits = new RaycastHit2D[4];
    static readonly string[] Legs = { "LegNear0", "LegNear1", "LegNear2", "LegFar0", "LegFar1", "LegFar2" };

    public bool ShellIntact => shellIntact;

    protected override void Awake()
    {
        base.Awake();
        body.bodyType = RigidbodyType2D.Dynamic; body.gravityScale = 3f; body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        centreX = body.position.x;
        ground = new ContactFilter2D(); ground.SetLayerMask(LayerMask.GetMask("Ground")); ground.useTriggers = false;
    }

    public override CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        if (shellIntact)
        {
            if (WeaponKinds.Of(hit) != WeaponKind.Mace) { blockedShakeUntil = Time.time + .15f; return WeaponKinds.Blocked(); }
            shellIntact = false;
            HitStop.Freeze(HitStop.Stagger);
            flashUntil = Time.time + .12f;
            var shell = rig != null ? rig.Bone("Shell").GetComponentInChildren<SpriteRenderer>() : null;
            if (shell != null) { Debris.Burst(shards, shell.bounds, shell.sortingOrder + 1, hit.Direction * 2f + Vector2.up); shell.sprite = shellCracked; }
            return CombatDamageResponse.Applied(0f);
        }
        return base.ReceiveCombatHit(hit);
    }

    void FixedUpdate()
    {
        if (!IsAlive) return;
        bool grounded = body.Cast(Vector2.down, ground, hits, .08f) > 0;
        if (grounded)
        {
            bool atLimit = direction < 0f ? body.position.x <= centreX - patrolHalfWidth : body.position.x >= centreX + patrolHalfWidth;
            Bounds b = GetComponent<Collider2D>().bounds;
            bool wall = body.Cast(Vector2.right * direction, ground, hits, .08f) > 0;
            bool floorAhead = Physics2D.Raycast(new Vector2(b.center.x + direction * (b.extents.x + .1f), b.min.y + .1f), Vector2.down, .4f, LayerMask.GetMask("Ground"));
            if (atLimit || wall || !floorAhead) direction = -direction;
        }
        body.linearVelocity = new Vector2(grounded ? direction * patrolSpeed : 0f, body.linearVelocity.y);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (rig == null) return;
        rig.Face(direction);
        float speed = Mathf.Abs(body.linearVelocity.x);
        phase += Time.deltaTime * speed * 4.5f;
        for (int i = 0; i < Legs.Length; i++)
        {
            // Alternating tripod gait: legs 0 and 2 of one side move with leg 1 of the other.
            float offset = ((i % 3) + (i / 3)) % 2 == 0 ? 0f : Mathf.PI;
            rig.Pose(Legs[i], Mathf.Sin(phase + offset) * 16f * Mathf.Clamp01(speed * 2f));
        }
        float shake = Time.time < blockedShakeUntil ? Mathf.Sin(Time.time * 90f) * .02f : 0f;
        rig.Pose("Body", Mathf.Sin(phase * 2f) * 1.2f, new Vector2(shake, Mathf.Abs(Mathf.Sin(phase)) * .01f));
    }
}
