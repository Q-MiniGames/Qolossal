using UnityEngine;

// Loose pieces thrown from a broken object: simple ballistic motion with spin, fading out.
public sealed class Debris : MonoBehaviour
{
    Vector2 velocity; float spin, life, age;
    SpriteRenderer image;

    // Scatters the pieces over `area` (world bounds of the intact object), pushed along `push`.
    public static void Burst(Sprite[] pieces, Bounds area, int sortingOrder, Vector2 push, float lifetime = 1.4f)
    {
        if (pieces == null) return;
        foreach (Sprite piece in pieces)
        {
            var obj = new GameObject("Debris " + piece.name);
            Rect r = piece.rect;
            // Keep each piece roughly where it sits in its sheet, mapped onto the object's bounds.
            Vector2 t = new Vector2(r.center.x / piece.texture.width, r.center.y / piece.texture.height);
            obj.transform.position = new Vector3(Mathf.Lerp(area.min.x, area.max.x, t.x), Mathf.Lerp(area.min.y, area.max.y, t.y), 0f);
            var d = obj.AddComponent<Debris>();
            d.image = obj.AddComponent<SpriteRenderer>();
            d.image.sprite = piece; d.image.sortingOrder = sortingOrder;
            d.velocity = push + new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(.5f, 3f));
            d.spin = Random.Range(-260f, 260f); d.life = lifetime * Random.Range(.8f, 1.2f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        velocity.y -= 22f * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, spin * dt);
        Color c = image.color; c.a = 1f - Mathf.Clamp01((age - life * .6f) / (life * .4f)); image.color = c;
        if (age >= life) Destroy(gameObject);
    }
}
