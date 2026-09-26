using UnityEngine;

// A Pod Spitter's seed: flies straight with a slight drop, costs Qori a heart on contact, and
// breaks on ground. Qori's weapons can knock it out of the air.
public sealed class SpitterSeed : MonoBehaviour, ICombatDamageReceiver
{
    Vector2 velocity; float life = 3f;

    public static void Launch(Sprite sprite, Vector2 from, Vector2 velocity)
    {
        var obj = new GameObject("Spitter Seed");
        obj.transform.position = from;
        var r = obj.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = 12;
        obj.transform.localScale = Vector3.one * (.32f / Mathf.Max(.01f, sprite.bounds.size.x));
        var c = obj.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = .12f / obj.transform.localScale.x;
        var rb = obj.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        obj.AddComponent<SpitterSeed>().velocity = velocity;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        velocity.y -= 2.5f * dt;
        Vector2 pos = transform.position, step = velocity * dt;
        if (Physics2D.Raycast(pos, step.normalized, step.magnitude + .08f, LayerMask.GetMask("Ground"))) { Destroy(gameObject); return; }
        transform.position = pos + step;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        if ((life -= dt) <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerHealth>() : null;
        if (health == null) return;
        health.TakeDamage(transform.position, new Vector2(4f, 4f));
        Destroy(gameObject);
    }

    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit) { Destroy(gameObject); return CombatDamageResponse.Applied(0f); }
}
