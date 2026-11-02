using UnityEngine;

// Test-only: a Bramble Crawler (the real GroundCreature, its behaviour unchanged) drawn with the Ludo.ai frames instead
// of its cutout rig (export_crawler.py): the walk while it patrols, the warning while it winds up (by its progress),
// the gallop while it charges, the flinch when hit, and the collapse when it dies (a corpse left behind, since the
// creature itself vanishes at once). The frames are drawn facing right.
[DefaultExecutionOrder(200)]
public sealed class CrawlerFramesOnCreature : MonoBehaviour
{
    public Sprite[] walk = new Sprite[0], warn = new Sprite[0], charge = new Sprite[0], hurt = new Sprite[0], death = new Sprite[0];
    [Tooltip("The walk's first frame, nose to tail, in world units.")] public float bodyLength = 1.7f;
    public float figureWidthPx = 772f;
    [Tooltip("Walk frames a second at the patrol speed (1.5 u/s); faster or slower with the speed.")] public float walkFps = 22f;
    public float chargeFps = 20f, hurtSeconds = .25f, deathSeconds = 1.4f;

    GroundCreature creature;
    Rigidbody2D body;
    Collider2D shape;
    SpriteRenderer view;
    float walkPhase, hurtAt = -9f, facing = 1f;
    bool wasFlashing, dead;

    float Scale => bodyLength / (figureWidthPx / 100f);

    void Start()
    {
        creature = GetComponent<GroundCreature>();
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<Collider2D>();
        var art = new GameObject("Crawler frames (test)");
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 8;
        art.transform.localScale = Vector3.one * Scale;
    }

    void LateUpdate()
    {
        if (creature == null || view == null || walk.Length == 0) return;
        // the rig and the creature's own sprite stay hidden
        foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) r.enabled = false;
        if (!creature.IsAlive)
        {
            if (!dead) { dead = true; CrawlerCorpse.Leave(view, death, deathSeconds, facing); view = null; }
            return;
        }
        float t = Time.time;
        bool flashing = creature.IsFlashing;
        if (flashing && !wasFlashing) hurtAt = t;
        wasFlashing = flashing;
        float vx = body != null ? body.linearVelocity.x : 0f;
        if (creature.IsTelegraphing || creature.IsCharging) facing = Mathf.Sign(creature.Direction);
        else if (Mathf.Abs(vx) > .1f) facing = Mathf.Sign(vx);

        Sprite s;
        if (t - hurtAt < hurtSeconds && hurt.Length > 0) s = hurt[Mathf.Min(hurt.Length - 1, (int)((t - hurtAt) / hurtSeconds * hurt.Length))];
        else if (creature.IsTelegraphing && warn.Length > 0) s = warn[Mathf.Min(warn.Length - 1, (int)(creature.Telegraph01 * warn.Length))];
        else if (creature.IsCharging && charge.Length > 0) s = charge[(int)Mathf.Repeat(t * chargeFps, charge.Length)];
        else
        {
            walkPhase += Time.deltaTime * walkFps * Mathf.Clamp(Mathf.Abs(vx) / 1.5f, 0f, 2f);
            s = walk[(int)Mathf.Repeat(walkPhase, walk.Length)];
        }
        view.sprite = s;
        view.flipX = facing < 0f;
        view.color = flashing ? new Color(1f, .8f, .8f) : Color.white;
        float feet = shape != null ? shape.bounds.min.y : transform.position.y;
        view.transform.position = new Vector3(transform.position.x, feet, transform.position.z);
    }

    void OnDestroy() { if (view != null) Destroy(view.gameObject); }
}
