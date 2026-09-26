using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(SpriteRenderer))]
public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 5;
    [SerializeField, Min(0.1f)] private float invulnerabilityTime = 1f;
    [SerializeField] private Vector2 knockback = new Vector2(7f, 5f);
    private int health;
    private float protectedUntil;
    public float LastHitTime { get; private set; } = float.NegativeInfinity;
    public int Health => health;
    public int MaximumHealth => maximumHealth;
    public float LastHitDirection { get; private set; }
    public bool IsDamageFlashVisible => isActiveAndEnabled && Time.time < protectedUntil
        && Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
    private PlayerMovement movement;
    private SpriteRenderer visual;
    private Color restingColor;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        visual = GetComponent<SpriteRenderer>();
        restingColor = visual.color;
        health = maximumHealth;
    }

    private void OnCollisionEnter2D(Collision2D collision) => CheckContact(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision.collider);
    // Flying enemies use trigger colliders.
    private void OnTriggerEnter2D(Collider2D other) => CheckContact(other);
    private void OnTriggerStay2D(Collider2D other) => CheckContact(other);

    private void CheckContact(Collider2D other)
    {
        var hazard = other.GetComponentInParent<IContactHazard>();
        if (hazard == null || hazard is Behaviour b && !b.isActiveAndEnabled || !hazard.HurtsOnContact) return;
        TakeDamage(((Component)hazard).transform.position, knockback);
    }

    // Costs one heart and knocks Qori away from `source` (x) with `push` (x magnitude, y up).
    // Ignored while invulnerable after a previous hit.
    public void TakeDamage(Vector2 source, Vector2 push)
    {
        if (!isActiveAndEnabled || !movement.isActiveAndEnabled || Time.time < protectedUntil) return;
        health--;
        protectedUntil = Time.time + invulnerabilityTime;
        if (health <= 0)
        {
            movement.Respawn();
            return;
        }
        float difference = transform.position.x - source.x;
        float direction = Mathf.Abs(difference) > 0.01f ? Mathf.Sign(difference) : -movement.FacingDirection;
        LastHitTime=Time.time;LastHitDirection=direction;
        movement.ApplyKnockback(new Vector2(direction * Mathf.Max(push.x, knockback.x * .5f), push.y));
    }

    public void RestoreAfterRespawn()
    {
        health = maximumHealth;
        protectedUntil = Time.time + invulnerabilityTime;
        LastHitTime=float.NegativeInfinity;
        if (visual != null) visual.color = restingColor;
    }

    private void Update()
    {
        bool flash = IsDamageFlashVisible;
        visual.color = flash ? Color.white : restingColor;
    }

    private void OnDisable()
    {
        if (visual != null) visual.color = restingColor;
    }
}
