using UnityEngine;

// A fragile platform: shakes when Qori lands on it, crumbles `delay` seconds later, and
// re-forms after `respawn` seconds once nothing is in the way.
[DisallowMultipleComponent]
public sealed class CrumblePlatform : MonoBehaviour
{
    public SpriteRenderer image;
    public Sprite[] pieces;
    public Collider2D solid;
    [Min(.05f)] public float delay = .5f;
    [Min(.5f)] public float respawn = 3f;

    enum State { Solid, Shaking, Gone }
    State state; float since; Vector3 rest;

    public bool IsGone => state == State.Gone;

    void Awake() { rest = image.transform.localPosition; }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (state != State.Solid || collision.rigidbody == null || collision.rigidbody.GetComponent<PlayerMovement>() == null) return;
        // Only when landed on top (contact normal points down into the platform from the player).
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y < -.6f) { state = State.Shaking; since = Time.time; Sfx.Play("Platform_Crumble", transform.position); return; }
    }

    void Update()
    {
        switch (state)
        {
            case State.Shaking:
                float k = (Time.time - since) / delay;
                image.transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * .025f * (.4f + k));
                if (k >= 1f)
                {
                    state = State.Gone; since = Time.time;
                    solid.enabled = false; image.enabled = false;
                    image.transform.localPosition = rest;
                    Debris.Burst(pieces, image.bounds, image.sortingOrder + 1, Vector2.down * 1.5f);
                }
                break;
            case State.Gone:
                if (Time.time - since >= respawn && !Blocked())
                {
                    state = State.Solid; solid.enabled = true; image.enabled = true;
                    Sfx.Play("Platform_Reform", transform.position);
                    image.color = new Color(1f, 1f, 1f, 0f);
                }
                break;
            default:
                if (image.color.a < 1f) { Color c = image.color; c.a = Mathf.MoveTowards(c.a, 1f, Time.deltaTime * 3f); image.color = c; }
                break;
        }
    }

    // Don't re-form inside Qori.
    bool Blocked()
    {
        Bounds b = solid.bounds;
        var hit = Physics2D.OverlapBox(b.center, b.size, 0f);
        return hit != null && hit != solid && hit.attachedRigidbody != null && hit.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    }
}
