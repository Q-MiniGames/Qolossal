using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class TrainingDummy : MonoBehaviour, IReedbladeTarget, ICombatDamageReceiver
{
    [SerializeField, Min(1)] private int maximumHealth = 3;
    private float health;
    private SpriteRenderer visual;
    private Color restingColor;
    private float flashUntil;
    private float resetAt;

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        visual = GetComponent<SpriteRenderer>();
        restingColor = visual.color;
        health = maximumHealth;
    }

    public void TakeHit()=>ReceiveCombatHit(new CombatDamage{Damage=1});
    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        if (health <= 0||hit.Damage<=0) return new CombatDamageResponse{Disposition=CombatHitDisposition.Ignored};
        float dealt=Mathf.Min(health,hit.Damage);health-=dealt;
        flashUntil = Time.time + 0.12f;
        if (health == 0) resetAt = Time.time + 1f;
        return CombatDamageResponse.Applied(dealt,true);
    }

    private void Update()
    {
        if (health <= 0 && Time.time >= resetAt) health = maximumHealth;
        visual.color = health <= 0 ? Color.gray
            : Time.time < flashUntil ? Color.white : restingColor;
    }
}
