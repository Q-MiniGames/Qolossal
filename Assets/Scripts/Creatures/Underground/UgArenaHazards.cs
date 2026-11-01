using UnityEngine;

// The Sluice Warden's flood: water rising over the arena floor, staying a few seconds, then draining. It costs a heart
// on contact (never instant); the ledges stay dry. Placeholder art made in code until drawn water exists.
public sealed class UgRisingWater : MonoBehaviour, IContactHazard
{
    Rect full; float seconds, since; SpriteRenderer view; BoxCollider2D box;
    const float Rise = .5f, Drain = .5f;
    public bool HurtsOnContact => Time.time - since > .1f;

    public static void Flood(Rect area, float seconds)
    {
        var obj = new GameObject("Sluice flood");
        var w = obj.AddComponent<UgRisingWater>(); w.full = area; w.seconds = seconds; w.since = Time.time;
        w.view = obj.AddComponent<SpriteRenderer>(); w.view.sprite = UgArenaArt.Square; w.view.color = new Color(.25f, .55f, .8f, .6f); w.view.sortingOrder = 7;
        w.box = obj.AddComponent<BoxCollider2D>(); w.box.isTrigger = true;
        var rb = obj.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        Sfx.Play("Guardian_ArmSlam", area.center);
        w.Apply(0f);
    }

    void Update()
    {
        float t = Time.time - since;
        float k = t < Rise ? t / Rise : t < seconds - Drain ? 1f : 1f - (t - (seconds - Drain)) / Drain;
        if (t >= seconds) { Destroy(gameObject); return; }
        Apply(k);
    }

    void Apply(float k)
    {
        float h = Mathf.Max(.02f, full.height * k);
        transform.position = new Vector3(full.center.x, full.y + h * .5f, 0f);
        view.transform.localScale = new Vector3(full.width / UgArenaArt.Square.bounds.size.x, h / UgArenaArt.Square.bounds.size.y, 1f);
        box.size = UgArenaArt.Square.bounds.size;
    }
}

// The Root Matron's seed: a shadow marks the floor, then it falls, costs a heart on contact and breaks on the ground.
public sealed class UgFallingSeed : MonoBehaviour, IContactHazard
{
    float wait, fallSpeed = 13f; SpriteRenderer view; CircleCollider2D circle;
    public bool HurtsOnContact => wait <= 0f;

    public static void Drop(Vector2 from, float delay)
    {
        var obj = new GameObject("Falling seed"); obj.transform.position = from;
        var s = obj.AddComponent<UgFallingSeed>(); s.wait = delay + .45f;
        s.view = obj.AddComponent<SpriteRenderer>(); s.view.sprite = UgArenaArt.Square; s.view.color = new Color(.55f, .75f, .35f, 0f); s.view.sortingOrder = 7;
        obj.transform.localScale = Vector3.one * (.55f / UgArenaArt.Square.bounds.size.x);
        s.circle = obj.AddComponent<CircleCollider2D>(); s.circle.isTrigger = true; s.circle.radius = .28f / obj.transform.localScale.x; s.circle.enabled = false;
        var rb = obj.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (wait > 0f)
        {
            wait -= Time.deltaTime;
            view.color = new Color(view.color.r, view.color.g, view.color.b, Mathf.Clamp01(.45f - wait) / .45f);   // fades in at the ceiling: the warning
            if (wait <= 0f) circle.enabled = true;
            return;
        }
        float step = fallSpeed * Time.deltaTime;
        Vector2 pos = transform.position;
        if (Physics2D.Raycast(pos, Vector2.down, step + .15f, GroundLayer.Mask)) { if (Fx.Library != null) Fx.Leaves(pos, 3, .6f); Destroy(gameObject); return; }
        transform.position = pos + Vector2.down * step;
    }
}

// A small white square for placeholder arena effects.
public static class UgArenaArt
{
    static Sprite square;
    public static Sprite Square
    {
        get
        {
            if (square != null) return square;
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
            return square;
        }
    }
}
