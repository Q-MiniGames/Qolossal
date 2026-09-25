using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(ThreadAnchor))]
public sealed class FlyingCreature : MonoBehaviour, IReedbladeTarget, ICombatDamageReceiver
{
    [SerializeField, Min(0f)] private float horizontalRange = 1.2f;
    [SerializeField, Min(0f)] private float verticalRange = 0.25f;
    [SerializeField, Min(0.1f)] private float cycleSeconds = 4f;
    [SerializeField, Min(1)] private int maximumHealth = 3;
    private Rigidbody2D body;
    private ThreadAnchor anchor;
    private Vector2 home;
    private float startedAt;
    private float health;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anchor = GetComponent<ThreadAnchor>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<CircleCollider2D>().isTrigger = true;
        home = body.position;
        startedAt = Time.fixedTime;
        health = maximumHealth;
    }

    private void FixedUpdate()
    {
        float phase = (Time.fixedTime - startedAt) * (2f * Mathf.PI / cycleSeconds);
        body.MovePosition(home + new Vector2(Mathf.Sin(phase) * horizontalRange,
            Mathf.Sin(phase * 2f) * verticalRange));
    }

    public void TakeHit()=>ReceiveCombatHit(new CombatDamage{Damage=1});
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (health <= 0||hit.Damage<=0) return new CombatDamageResponse{Disposition=CombatHitDisposition.Ignored};
        float dealt=Mathf.Min(health,hit.Damage);health-=dealt;
        if (health == 0)
        {
            // Disabling unregisters the anchor. The player's rope releases
            // without resetting the velocity earned during the swing.
            gameObject.SetActive(false);
            return CombatDamageResponse.Applied(dealt,true);
        }
        anchor.FlashHit();
        return CombatDamageResponse.Applied(dealt,true);
    }
}
