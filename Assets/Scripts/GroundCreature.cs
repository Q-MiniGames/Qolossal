using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class GroundCreature : MonoBehaviour, IReedbladeTarget, ICombatDamageReceiver
{
    public bool IsAlive => health > 0;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float patrolHalfWidth = 1.5f;
    [SerializeField, Min(1)] private int maximumHealth = 3;
    [SerializeField] private LayerMask groundLayers;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private SpriteRenderer visual;
    private Color restingColor;
    private float centerX;
    private float direction = -1f;
    private float health;
    private Vector2 hitKnockback;
    private float knockbackUntil;
    private bool pendingHitKnockback;
    private float flashUntil;
    private float vanishAt;
    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] checks = new RaycastHit2D[8];

    private void Reset() => groundLayers = LayerMask.GetMask("Ground");

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        if (Resources.Load<Texture2D>("Creatures/BrambleCrawler_Keyed_v1") != null)
        {
            visual.color = new Color(.88f, .88f, .88f, 1f);
            if (GetComponent<GroundCreatureVisual>() == null) gameObject.AddComponent<GroundCreatureVisual>();
        }
        restingColor = visual.color;
        centerX = body.position.x;
        health = maximumHealth;
        shape.isTrigger = false;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Ground");
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayers);
        groundFilter.useTriggers = false;
    }

    private void FixedUpdate()
    {
        if (health <= 0) return;
        if(pendingHitKnockback){body.linearVelocity=hitKnockback;pendingHitKnockback=false;}
        if(Time.time<knockbackUntil)return;
        bool grounded = false;
        int count = body.Cast(Vector2.down, groundFilter, checks, 0.08f);
        for (int i = 0; i < count; i++)
            if (checks[i].normal.y > 0.65f) grounded = true;

        if (grounded)
        {
            bool atLimit = direction < 0f ? body.position.x <= centerX - patrolHalfWidth
                : body.position.x >= centerX + patrolHalfWidth;
            bool wall = false;
            count = body.Cast(Vector2.right * direction, groundFilter, checks, 0.08f);
            for (int i = 0; i < count; i++)
                if (checks[i].normal.x * direction < -0.5f) wall = true;
            Bounds bounds = shape.bounds;
            Vector2 ahead = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.12f),
                bounds.min.y + 0.15f);
            bool floorAhead = Physics2D.Raycast(ahead, Vector2.down, groundFilter, checks, 0.35f) > 0;
            if (atLimit || wall || !floorAhead) direction *= -1f;
        }
        body.linearVelocity = new Vector2(grounded ? direction * patrolSpeed : 0f,
            body.linearVelocity.y);
    }

    public void TakeHit()=>ReceiveCombatHit(new CombatDamage{Damage=1});
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (health <= 0||hit.Damage<=0) return new CombatDamageResponse{Disposition=CombatHitDisposition.Ignored};
        float dealt=Mathf.Min(health,hit.Damage);health-=dealt;
        hitKnockback=hit.Knockback;pendingHitKnockback=hitKnockback.sqrMagnitude>0;knockbackUntil=Time.time+.10f;
        flashUntil = Time.time + 0.12f;
        if (health == 0)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            vanishAt = Time.time + 0.2f;
        }
        return CombatDamageResponse.Applied(dealt,true);
    }

    private void Update()
    {
        visual.color = health <= 0 ? Color.gray : Time.time < flashUntil ? Color.white : restingColor;
        if (health <= 0 && Time.time >= vanishAt) gameObject.SetActive(false);
    }
}
