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

    private void OnCollisionEnter2D(Collision2D collision) => CheckContact(collision);
    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision);

    private void CheckContact(Collision2D collision)
    {
        if (!isActiveAndEnabled || !movement.isActiveAndEnabled || Time.time < protectedUntil) return;
        GroundCreature enemy = collision.gameObject.GetComponentInParent<GroundCreature>();
        if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsAlive) return;
        health--;
        protectedUntil = Time.time + invulnerabilityTime;
        if (health <= 0)
        {
            movement.Respawn();
            return;
        }
        float difference = transform.position.x - enemy.transform.position.x;
        float direction = Mathf.Abs(difference) > 0.01f ? Mathf.Sign(difference) : -movement.FacingDirection;
        LastHitTime=Time.time;LastHitDirection=direction;
        movement.ApplyKnockback(new Vector2(direction * knockback.x, knockback.y));
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

    private void OnGUI()
    {
        GUI.Box(new Rect(16f, 16f, 150f, 32f), $"Health: {health} / {maximumHealth}");
    }
}
