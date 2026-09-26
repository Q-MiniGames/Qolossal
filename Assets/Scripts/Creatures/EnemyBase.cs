using UnityEngine;

// Something that costs Qori a heart when he touches it.
public interface IContactHazard { bool HurtsOnContact { get; } }

// Shared enemy plumbing: health, hit flash, knockback hand-off, death (grey out, stop colliding,
// vanish), a CreatureRig for visuals, and the player it reacts to.
[DisallowMultipleComponent]
public abstract class EnemyBase : MonoBehaviour, ICombatDamageReceiver, IReedbladeTarget, IContactHazard
{
    [SerializeField, Min(1)] protected int maximumHealth = 3;
    [SerializeField] protected CreatureRig rig;

    protected float health;
    protected float flashUntil, diedAt = -1f;
    protected Rigidbody2D body;
    PlayerMovement player;

    public bool IsAlive => health > 0f;
    public bool IsFlashing => Time.time < flashUntil;
    public virtual bool HurtsOnContact => IsAlive;
    protected PlayerMovement Player => player != null ? player : player = FindFirstObjectByType<PlayerMovement>();

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (rig == null) rig = GetComponentInChildren<CreatureRig>();
        health = maximumHealth;
    }

    public void TakeHit() => ReceiveCombatHit(new CombatDamage { Damage = 1 });

    public virtual CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (!IsAlive || hit.Damage <= 0f) return new CombatDamageResponse { Disposition = CombatHitDisposition.Ignored };
        float dealt = Mathf.Min(health, hit.Damage);
        health -= dealt;
        flashUntil = Time.time + .12f;
        OnHurt(hit);
        if (!IsAlive) Die();
        return CombatDamageResponse.Applied(dealt, true);
    }

    protected virtual void OnHurt(CombatDamage hit) { }

    // Where the death puff and any sap orb appear.
    protected virtual Vector2 DeathPoint => GetComponentInChildren<Collider2D>() is Collider2D c ? (Vector2)c.bounds.center : (Vector2)transform.position;

    protected virtual void Die()
    {
        diedAt = Time.time;
        Vector2 centre = DeathPoint;
        Fx.DeathPuff(centre);
        SapOrb.MaybeDrop(centre);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }
    }

    protected virtual void LateUpdate()
    {
        if (rig != null) rig.Tint(CreatureRig.FeedbackTint(IsAlive, IsFlashing));
        if (!IsAlive && Time.time - diedAt > .25f) gameObject.SetActive(false);
    }

    // True when Qori is within `range` horizontally and `height` vertically, with no ground between.
    protected bool CanSee(Vector2 from, float range, float height, out Vector2 toPlayer)
    {
        toPlayer = Vector2.zero;
        if (Player == null || !Player.isActiveAndEnabled) return false;
        toPlayer = (Vector2)Player.transform.position - from;
        if (Mathf.Abs(toPlayer.x) > range || Mathf.Abs(toPlayer.y) > height) return false;
        return !Physics2D.Linecast(from, Player.transform.position, LayerMask.GetMask("Ground"));
    }
}
