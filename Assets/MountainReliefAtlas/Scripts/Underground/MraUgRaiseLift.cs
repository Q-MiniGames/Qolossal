using UnityEngine;

// A platform that lies under the water until `flag` is saved, then rises once to its deployed
// height and stays (the Twin Sluices' return walkway). It is solid only once it starts to rise. A
// scene with the flag saved loads with it already up, never part-way. It moves by velocity as a
// kinematic body, so it lifts Qori if he stands where it rises; if it ever ends inside him he is
// set on top of it.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MraUgRaiseLift : MonoBehaviour
{
    public string flag;
    [Tooltip("The walkway's centre while submerged and when deployed.")] public Vector2 lowered, raised;
    [Min(.2f)] public float riseSeconds = 1.6f;
    public SpriteRenderer art;
    [Tooltip("Painted states (Batch 29): drawn while under the water, and once raised.")] public Sprite submergedSprite, raisedSprite;
    void Paint(bool up) { if (art != null && raisedSprite != null) art.sprite = up ? raisedSprite : submergedSprite; }

    Rigidbody2D body; BoxCollider2D box; float k = -1f;
    public bool Deployed => k >= 1f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); box = GetComponent<BoxCollider2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        k = GameSave.HasFlag(flag) ? 1f : 0f;
        body.position = Vector2.Lerp(lowered, raised, k); transform.position = body.position;
        box.enabled = k > 0f;
        Paint(k >= 1f);
        if (art != null && raisedSprite == null) art.color = new Color(art.color.r, art.color.g, art.color.b, k > 0f ? 1f : .45f);
    }

    void FixedUpdate()
    {
        if (k < 0f || k >= 1f || !GameSave.HasFlag(flag)) { body.linearVelocity = Vector2.zero; return; }
        if (k == 0f) { box.enabled = true; Sfx.Play("Gate_Open", transform.position); }
        k = Mathf.Min(1f, k + Time.fixedDeltaTime / riseSeconds);
        float e = k * k * (3f - 2f * k);
        Vector2 next = Vector2.Lerp(lowered, raised, e);
        body.linearVelocity = (next - body.position) / Time.fixedDeltaTime;
        if (art != null && raisedSprite == null) art.color = new Color(art.color.r, art.color.g, art.color.b, 1f);
        if (k >= 1f) { Paint(true); Settle(); }
    }

    // Safety at the top: never leave Qori inside the walkway.
    void Settle()
    {
        body.linearVelocity = Vector2.zero;
        var q = FindAnyObjectByType<PlayerMovement>(); if (q == null) return;
        var qBox = q.GetComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        if (qBox != null && qBox.bounds.Intersects(box.bounds) && qBox.bounds.center.y > box.bounds.min.y)
            q.PlaceAt(new Vector2(q.transform.position.x, box.bounds.max.y + (q.transform.position.y - qBox.bounds.min.y) + .02f), 0f, false);
    }
}
