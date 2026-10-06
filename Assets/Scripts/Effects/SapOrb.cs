using UnityEngine;

// A glowing amber sap orb dropped by enemies: bobs, drifts to Qori when he's close, and restores
// one heart when collected. Fades away after a while if left.
public sealed class SapOrb : MonoBehaviour
{
    const float Life = 9f, Attract = 1.6f, Collect = .35f;
    float age; Vector2 velocity; SpriteRenderer image; PlayerHealth player;

    // `chance` (0-1) that a defeated enemy drops one.
    public static void MaybeDrop(Vector2 at, float chance = .4f)
    {
        var lib = Fx.Library; if (lib == null || lib.sapOrb == null || Random.value > chance) return;
        var obj = new GameObject("Sap Orb"); obj.transform.position = at;
        var orb = obj.AddComponent<SapOrb>();
        orb.image = obj.AddComponent<SpriteRenderer>(); orb.image.sprite = lib.sapOrb; orb.image.sortingOrder = 25;
        obj.transform.localScale = Vector3.one * (.34f / lib.sapOrb.bounds.size.x);
        orb.velocity = new Vector2(Random.Range(-.8f, .8f), 2.5f);
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        Vector2 pos = transform.position;
        Vector2 to = player != null ? (Vector2)player.GetComponent<Collider2D>().bounds.center - pos : Vector2.one * 99f;
        if (to.magnitude < Attract) velocity = Vector2.Lerp(velocity, to.normalized * 6f, dt * 8f);
        else
        {
            velocity.y -= 6f * dt; velocity.x *= 1f - dt * 2f;
            if (Physics2D.Raycast(pos, Vector2.down, .2f, LayerMask.GetMask("Ground")) && velocity.y < 0f) velocity = Vector2.zero;
        }
        transform.position = pos + velocity * dt + Vector2.up * Mathf.Sin(age * 3f) * .003f;
        if (player != null && to.magnitude < Collect && player.Health < player.MaximumHealth)
        {
            player.Heal(1);
            Sfx.Play("Heal");
            Fx.Pop(image.sprite, transform.position, .6f, .25f, 26);
            Destroy(gameObject); return;
        }
        if (age > Life - 1.5f) { Color c = image.color; c.a = Mathf.PingPong(age * 6f, 1f); image.color = c; }
        if (age > Life) Destroy(gameObject);
    }
}
