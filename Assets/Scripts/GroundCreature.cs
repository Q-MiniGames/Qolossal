using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class GroundCreature : MonoBehaviour, IReedbladeTarget, ICombatDamageReceiver, IContactHazard
{
    public bool IsAlive => health > 0;
    public bool HurtsOnContact => IsAlive;
    public bool IsFlashing => Time.time < flashUntil;
    public bool IsTelegraphing => state == State.Telegraph;
    public bool IsCharging => state == State.Charge;
    public float Telegraph01 => state == State.Telegraph ? Mathf.Clamp01((Time.time - stateSince) / telegraphTime) : 0f;
    public float Direction => direction;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.5f;
    [SerializeField, Min(0.1f)] private float patrolHalfWidth = 1.5f;
    [SerializeField, Min(1)] private int maximumHealth = 3;
    [SerializeField] private LayerMask groundLayers;
    [Header("Charge (spots Qori, lowers its thorny back, then charges)")]
    [SerializeField, Min(0f)] private float detectRange = 4.5f;
    [SerializeField, Min(0f)] private float detectHeight = .9f;
    [SerializeField, Min(.05f)] private float telegraphTime = .45f;
    [SerializeField, Min(.1f)] private float chargeSpeed = 5.5f;
    [SerializeField, Min(.1f)] private float chargeTime = 1.1f;
    [SerializeField, Min(0f)] private float chargeCooldown = 1.2f;
    private enum State { Patrol, Telegraph, Charge }
    private State state;
    private float stateSince, nextChargeAt;
    private PlayerMovement player;
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
        skitter = SfxEmitter.Attach(gameObject, "Crawler_Move", false);
    }
    private SfxEmitter skitter;

    private void FixedUpdate()
    {
        if (health <= 0) return;
        if(pendingHitKnockback){body.linearVelocity=hitKnockback;pendingHitKnockback=false;}
        if(Time.time<knockbackUntil)return;
        bool grounded = false;
        int count = body.Cast(Vector2.down, groundFilter, checks, 0.08f);
        for (int i = 0; i < count; i++)
            if (checks[i].normal.y > 0.65f) grounded = true;

        if (grounded && state == State.Patrol && Time.time >= nextChargeAt && SpotsPlayer(out float toward))
        { direction = toward; Enter(State.Telegraph); Fx.Glint(body.position + new Vector2(direction * .55f, .35f)); }
        if (state == State.Telegraph)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            if (Time.time - stateSince >= telegraphTime) Enter(State.Charge);
            return;
        }

        if (grounded)
        {
            bool blocked = BlockedAhead();
            if (state == State.Charge)
            {
                if (blocked || Time.time - stateSince >= chargeTime)
                {
                    Enter(State.Patrol);
                    centerX = body.position.x;   // resume patrolling where the charge ended
                    nextChargeAt = Time.time + chargeCooldown;
                    if (blocked) direction *= -1f;
                }
            }
            else
            {
                bool atLimit = direction < 0f ? body.position.x <= centerX - patrolHalfWidth
                    : body.position.x >= centerX + patrolHalfWidth;
                if (atLimit || blocked) direction *= -1f;
            }
        }
        float speed = state == State.Charge ? chargeSpeed : patrolSpeed;
        body.linearVelocity = new Vector2(grounded ? direction * speed : 0f, body.linearVelocity.y);
    }

    private void Enter(State next) { state = next; stateSince = Time.time; }

    private bool BlockedAhead()
    {
        int count = body.Cast(Vector2.right * direction, groundFilter, checks, 0.08f);
        for (int i = 0; i < count; i++)
            if (checks[i].normal.x * direction < -0.5f) return true;
        Bounds bounds = shape.bounds;
        Vector2 ahead = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.12f), bounds.min.y + 0.15f);
        return Physics2D.Raycast(ahead, Vector2.down, groundFilter, checks, 0.35f) == 0;
    }

    // Qori on roughly the same level, within range, with no wall in between.
    private bool SpotsPlayer(out float toward)
    {
        toward = direction;
        if (player == null) player = FindFirstObjectByType<PlayerMovement>();
        if (player == null || !player.isActiveAndEnabled) return false;
        Vector2 to = (Vector2)player.transform.position - body.position;
        if (Mathf.Abs(to.x) > detectRange || Mathf.Abs(to.y) > detectHeight) return false;
        toward = Mathf.Sign(to.x);
        return Physics2D.Raycast(body.position, new Vector2(toward, 0f), groundFilter, checks, Mathf.Abs(to.x)) == 0;
    }

    public void TakeHit()=>ReceiveCombatHit(new CombatDamage{Damage=1});
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (health <= 0||hit.Damage<=0) return new CombatDamageResponse{Disposition=CombatHitDisposition.Ignored};
        float dealt=Mathf.Min(health,hit.Damage);health-=dealt;
        hitKnockback=hit.Knockback;pendingHitKnockback=hitKnockback.sqrMagnitude>0;knockbackUntil=Time.time+.10f;
        flashUntil = Time.time + 0.12f;
        if (state == State.Telegraph) Enter(State.Patrol);   // a hit interrupts the wind-up
        if (health == 0)
        {
            Fx.DeathPuff(shape.bounds.center, 1.1f);
            Sfx.Play("Combat_EnemyDefeat", shape.bounds.center);
            SapOrb.MaybeDrop(shape.bounds.center);
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            vanishAt = Time.time + 0.2f;
        }
        var response=CombatDamageResponse.Applied(dealt,true);response.Killed=health<=0;
        return response;
    }

    private void Update()
    {
        if (skitter != null) skitter.Playing = health > 0 && Mathf.Abs(body.linearVelocity.x) > .2f;
        visual.color = health <= 0 ? Color.gray : Time.time < flashUntil ? Color.white : restingColor;
        if (health <= 0 && Time.time >= vanishAt) gameObject.SetActive(false);
    }
}
